
using BlockChainServices.Interfaces;
using BotModels;
using BotModels.Repository;
using BotModels.Response.HascorpVault;
using BotServices;
using CoinSwapper;
using Services.Interfaces.V2;
using Utility;

namespace ForwardTraderBot.Service
{
    public class ReverseTraderService : IHostedService, IDisposable
    {
        private readonly ILogger<ReverseTraderService> _logger;
        private Timer? _timer = null;
        private readonly AppSettings _appSettings;
        private readonly ICoinCheckService _coinService;
        private readonly IAuthService _authService;
        private readonly IBlockChainApiService _blockChainApiService;
        private readonly IConfiguration _configuration;

        private readonly ICoinCheckServiceV2 _coinServiceV2;
        public ReverseTraderService(ILogger<ReverseTraderService> logger, IConfiguration configuration, ICoinCheckServiceV2 coinCheckServiceV2,
           IAuthService authService, IBlockChainApiService blockChainApiService)
        {
            _logger = logger;
            _appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
            _coinServiceV2 = coinCheckServiceV2;
            _authService = authService;
            _blockChainApiService = blockChainApiService;
            _configuration = configuration;

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            List<SecretDto> vaultKeys = await _authService.GetSecrets(_appSettings.vaultProjectKey, _appSettings.identifier, true);

            foreach (var vaultKey in vaultKeys)
            {
                if (vaultKey.key.Contains("wallet_key"))
                {
                    try
                    {
                        var account = WalletExtensions.Account(vaultKey.value);
                        if (account != null && !string.IsNullOrEmpty(account.PublicKey))
                        {
                            vaultKey.publicKey = account.PublicKey;
                        }
                        else
                        {
                            //incase failed to convert
                            vaultKey.publicKey = await _blockChainApiService.GetPublicKey(vaultKey.value);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerUtil.LogError(_logger, "Error handling key conversion", exception: ex);
                    }
                }

            }
            CacheUtil.vaultSecrets = vaultKeys;
            _configuration["ConnectionStrings:CryptoDbConnection"] = CacheUtil.vaultSecrets.Where(x => x.key == "DbConnection").Select(x => x.value).FirstOrDefault();
            LoggerUtil.LogInformation(_logger, "ReverseTraderService Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(_appSettings.botinterval));

            //return Task.CompletedTask;
        }


        private async void DoWork(object? state)
        {
            try
            {
                List<ReverseSwapCoinConfiguration> coinConfigurationRequests = await _coinServiceV2.GetPendingCoinsToReverseSwap();
                if (coinConfigurationRequests != null && coinConfigurationRequests.Count > 0)
                {
                    Parallel.ForEach(coinConfigurationRequests, async reverseSwapModel =>
                    {
                        await _coinServiceV2.ProcessReverseSwap(reverseSwapModel);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            LoggerUtil.LogInformation(_logger, "ReverseTraderService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
