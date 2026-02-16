using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramAds.Features.Bot.Chat;
using TelegramAds.Features.Deals;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;
using TelegramAds.Shared.Ton;

namespace TelegramAds.Workers;

public sealed class PaymentVerificationWorker : IInvocable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentVerificationWorker> _logger;

    public PaymentVerificationWorker(
        IServiceProvider serviceProvider,
        ILogger<PaymentVerificationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Invoke()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tonApiClient = scope.ServiceProvider.GetRequiredService<TonApiClient>();
        var tonPaymentService = scope.ServiceProvider.GetRequiredService<TonPaymentService>();
        var tonOptions = scope.ServiceProvider.GetRequiredService<TonOptions>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        try
        {
            var pendingPayments = await db.Payments
                .Include(p => p.Deal)
                    .ThenInclude(d => d.Campaign)
                .Include(p => p.Deal)
                    .ThenInclude(d => d.ChannelOwnerUser)
                .Include(p => p.Deal)
                    .ThenInclude(d => d.AdvertiserUser)
                .Where(p => p.Status == PaymentStatus.Pending)
                .Where(p => p.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            if (pendingPayments.Count == 0)
            {
                _logger.LogDebug("No pending payments to verify");
                return;
            }

            _logger.LogInformation("Checking {Count} pending payments", pendingPayments.Count);

            foreach (var p in pendingPayments)
            {
                _logger.LogInformation(
                    "Pending payment: PaymentId={PaymentId}, DealId={DealId}, Amount={Amount} TON, ExpiresAt={ExpiresAt}",
                    p.Id, p.DealId, p.AmountInTon, p.ExpiresAt);
            }

            var transactions = await tonApiClient.GetAccountTransactionsAsync(
                tonOptions.DepositWalletAddress,
                20,
                CancellationToken.None);

            _logger.LogInformation("Fetched {Count} transactions from deposit wallet", transactions.Count);

            if (transactions.Count == 0)
            {
                _logger.LogDebug("No transactions found for deposit wallet");
                return;
            }

            foreach (var tx in transactions)
            {
                var txAmount = tx.InMsg?.Value ?? 0;
                var txMemo = tx.InMsg?.DecodedBody?.Text ?? "(no memo)";
                var txSuccess = tx.Success;
                var txOpName = tx.InMsg?.DecodedOpName ?? "(no op)";
                _logger.LogInformation(
                    "Transaction: Hash={Hash}, Success={Success}, OpName={OpName}, Amount={Amount} nanoTON, Memo={Memo}",
                    tx.Hash, txSuccess, txOpName, txAmount, txMemo);
            }

            var processedCount = 0;

            foreach (var payment in pendingPayments)
            {
                _logger.LogInformation(
                    "Processing payment {PaymentId}: Looking for memo='{Memo}', amount>={Amount} nanoTON",
                    payment.Id,
                    payment.Id.ToString(),
                    (long)(payment.AmountInTon * 1_000_000_000));

                var matchedTransaction = transactions.FirstOrDefault(tx =>
                    tonPaymentService.MatchesPayment(tx, payment.Id, payment.AmountInTon));

                if (matchedTransaction != null)
                {
                    _logger.LogInformation(
                        "✓ Match found for payment {PaymentId}: Transaction {Hash}",
                        payment.Id,
                        matchedTransaction.Hash);
                }
                else
                {
                    _logger.LogWarning(
                        "✗ No matching transaction found for payment {PaymentId} (memo: {Memo}, amount: {Amount} TON)",
                        payment.Id,
                        payment.Id.ToString(),
                        payment.AmountInTon);
                }

                if (matchedTransaction != null)
                {
                    var alreadyProcessed = await db.Payments
                        .AnyAsync(p => p.TransactionHash == matchedTransaction.Hash);

                    if (alreadyProcessed)
                    {
                        _logger.LogWarning(
                            "Transaction {Hash} already processed, skipping duplicate",
                            matchedTransaction.Hash);
                        continue;
                    }

                    await ProcessPaymentConfirmationAsync(
                        payment,
                        matchedTransaction,
                        tonPaymentService,
                        db,
                        clock,
                        scope,
                        _logger);

                    processedCount++;

                    _logger.LogInformation(
                        "Payment confirmed: PaymentId={PaymentId}, Amount={Amount} TON, TxHash={Hash}",
                        payment.Id,
                        payment.AmountInTon,
                        matchedTransaction.Hash);
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Verified {Count} payments this cycle", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in payment verification worker");
        }
    }

    private static async Task ProcessPaymentConfirmationAsync(
        Payment payment,
        TonApiTransaction transaction,
        TonPaymentService tonPaymentService,
        AppDbContext db,
        IClock clock,
        IServiceScope scope,
        ILogger<PaymentVerificationWorker> logger)
    {

        var now = clock.UtcNow;

        var (paidByAddress, actualAmountNanoTon) = tonPaymentService.ExtractPaymentDetails(transaction);

        payment.Status = PaymentStatus.Confirmed;
        payment.TransactionHash = transaction.Hash;
        payment.TransactionTimestamp = transaction.Utime;
        payment.PaidByAddress = paidByAddress;
        payment.ActualAmountInTon = actualAmountNanoTon / 1_000_000_000m;
        payment.ConfirmedAt = now;

        var deal = payment.Deal;
        deal.Status = DealStatus.Paid;
        deal.PaymentId = payment.Id;
        deal.UpdatedAt = now;

        var escrowBalance = await db.EscrowBalances
            .FirstOrDefaultAsync(b => b.UserId == deal.ChannelOwnerUserId && b.Currency == payment.Currency);

        if (escrowBalance is null)
        {
            escrowBalance = new EscrowBalance
            {
                Id = Guid.NewGuid(),
                UserId = deal.ChannelOwnerUserId,
                TotalEarnedInTon = 0,
                AvailableBalanceInTon = 0,
                LockedInDealsInTon = 0,
                WithdrawnInTon = 0,
                Currency = payment.Currency,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.EscrowBalances.Add(escrowBalance);
        }
        else
        {
        }

        escrowBalance.LockedInDealsInTon += payment.AmountInTon;
        escrowBalance.UpdatedAt = now;

        var escrowTransaction = new DealEscrowTransaction
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            TransactionType = EscrowTransactionType.PaymentReceived,
            AmountInTon = payment.AmountInTon,
            FromUserId = deal.AdvertiserUserId,
            ToUserId = null,
            PaymentId = payment.Id,
            CreatedAt = now
        };
        db.DealEscrowTransactions.Add(escrowTransaction);

        var dealEvent = new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            FromStatus = DealStatus.AwaitingPayment,
            ToStatus = DealStatus.Paid,
            EventType = DealEventType.PaymentConfirmed,
            ActorUserId = deal.AdvertiserUserId,
            CreatedAt = now
        };
        db.DealEvents.Add(dealEvent);

        await db.SaveChangesAsync();

        var creativeFlowHandler = scope.ServiceProvider.GetRequiredService<CreativeFlowHandler>();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        creativeFlowHandler.EnterAwaitingCreativePost(
            deal.AdvertiserUser.TgUserId,
            deal.Id);

        await botClient.SendMessage(
            deal.AdvertiserUser.TgUserId,
            "💰 <b>Payment Verified!</b>\n\n" +
            $"Your payment of <b>{payment.AmountInTon} TON</b> has been confirmed.\n\n" +
            "📝 Please now send the post you'd like to publish. " +
            "You can send text, photos, videos, or documents.",
            parseMode: ParseMode.Html);
    }
}
