using ErrorOr;
using TonSdk.Client;
using TonSdk.Client.Stack;
using TonSdk.Contracts.Wallet;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TelegramAds.Shared.Ton;

public sealed class TonWalletService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TonWalletService> _logger;

    public TonWalletService(IConfiguration configuration, ILogger<TonWalletService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ErrorOr<string>> TransferAsync(string destinationAddress, decimal amountInTon, string comment)
    {
        try
        {
            var mnemonicWords = _configuration["Ton:WalletMnemonic"];
            if (string.IsNullOrEmpty(mnemonicWords))
            {
                _logger.LogError("TON wallet mnemonic not configured");
                return ErrorOr.Error.Failure("TON.NotConfigured", "TON wallet mnemonic not configured");
            }

            return await RetryAsync(async () => await TransferInternalAsync(destinationAddress, amountInTon, comment, mnemonicWords), maxRetries: 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer {Amount} TON to {Address}", amountInTon, destinationAddress);
            return ErrorOr.Error.Failure("TON.TransferFailed", $"Transfer failed: {ex.Message}");
        }
    }

    private async Task<ErrorOr<string>> TransferInternalAsync(string destinationAddress, decimal amountInTon, string comment, string mnemonicWords)
    {
        try
        {
            var mnemonic = new Mnemonic(mnemonicWords.Split(" "));

            var options = new WalletV4Options()
            {
                PublicKey = mnemonic.Keys.PublicKey,
                Workchain = 0,
            };

            var wallet = new WalletV4(options);

            var tonCenterEndpoint = _configuration.GetValue<string>("Ton:TonCenterEndpoint", "https://toncenter.com/api/v2/jsonRPC");

            var tonClientParams = new HttpParameters
            {
                Endpoint = tonCenterEndpoint,
                ApiKey = ""
            };

            var client = new TonClient(TonClientType.HTTP_TONCENTERAPIV2, tonClientParams);

            _logger.LogInformation("System wallet address: {Address}", wallet.Address);
            _logger.LogInformation("Preparing to transfer {Amount} TON to {Address}", amountInTon, destinationAddress);

            var receiver = new Address(destinationAddress);
            var amount = new Coins(amountInTon);
            var body = new CellBuilder().StoreUInt(0, 32).StoreString(comment).Build();

            var seqno = await client.Wallet.GetSeqno(wallet.Address);

            var message = wallet.CreateTransferMessage(
            [
                new WalletTransfer
                {
                    Message = new InternalMessage(new InternalMessageOptions
                    {
                        Info = new IntMsgInfo(new IntMsgInfoOptions
                        {
                            Dest = receiver,
                            Value = amount,
                            Bounce = true
                        }),
                        Body = body
                    }),
                    Mode = 1
                }
            ], seqno ?? 0);

            message.Sign(mnemonic.Keys.PrivateKey);

            var cell = message.Cell;

            await client.SendBoc(cell);

            _logger.LogInformation("Successfully transferred {Amount} TON to {Address}", amountInTon, destinationAddress);

            return "success";
        }
        catch (Exception ex) when (ex.Message.Contains("LITE_SERVER_NOTREADY") || ex.Message.Contains("not in db") || ex.Message.Contains("error"))
        {
            _logger.LogWarning("Server error, will retry: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<ErrorOr<T>> RetryAsync<T>(Func<Task<ErrorOr<T>>> operation, int maxRetries)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when ((ex.Message.Contains("LITE_SERVER_NOTREADY") || ex.Message.Contains("not in db") || ex.Message.Contains("error")) && attempt < maxRetries)
            {
                var delayMs = attempt * 2000;
                _logger.LogWarning("Attempt {Attempt}/{MaxRetries} failed with server error, retrying in {Delay}ms", attempt, maxRetries, delayMs);
                await Task.Delay(delayMs);
            }
        }

        return ErrorOr.Error.Failure("TON.ServerNotReady", "TON lite servers are not ready after multiple retries");
    }
}
