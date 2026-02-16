using System.Web;

namespace TelegramAds.Shared.Ton;

public sealed class TonPaymentService
{
    private readonly TonOptions _tonOptions;

    public TonPaymentService(TonOptions tonOptions)
    {
        _tonOptions = tonOptions;
    }

    public string GeneratePaymentUrl(decimal amountInTon, Guid paymentId)
    {
        var destinationAddress = _tonOptions.DepositWalletAddress;
        var amountInNanoTon = (long)(amountInTon * 1_000_000_000);
        var memo = paymentId.ToString();

        return $"ton://transfer/{destinationAddress}?amount={amountInNanoTon}&text={memo}";
    }

    public bool MatchesPayment(TonApiTransaction transaction, Guid paymentId, decimal expectedAmountInTon)
    {
        if (!transaction.Success)
            return false;

        if (transaction.InMsg == null)
            return false;

        var expectedAmountInNanoTon = (long)(expectedAmountInTon * 1_000_000_000);
        var expectedMemo = paymentId.ToString();

        var actualMemo = transaction.InMsg.DecodedBody?.Text?.Trim() ?? string.Empty;
        var actualAmount = transaction.InMsg.Value;
        var opName = transaction.InMsg.DecodedOpName;

        var isMatch = opName == "text_comment" &&
                      !string.IsNullOrEmpty(actualMemo) &&
                      actualMemo.Equals(expectedMemo, StringComparison.OrdinalIgnoreCase) &&
                      actualAmount >= expectedAmountInNanoTon;

        return isMatch;
    }

    public (string? paidByAddress, long actualAmount) ExtractPaymentDetails(TonApiTransaction transaction)
    {
        if (transaction.InMsg != null &&
            transaction.InMsg.DecodedOpName == "text_comment")
        {
            var sourceAddress = transaction.InMsg.Source?.Address ?? "unknown";
            return (sourceAddress, transaction.InMsg.Value);
        }

        return (null, 0);
    }
}
