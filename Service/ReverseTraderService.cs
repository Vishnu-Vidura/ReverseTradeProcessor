
using BotModels;
using BotModels.Repository;
using BotServices;
using Services.Interfaces.V2;

namespace ForwardTraderBot.Service
{
    public class ReverseTraderService : IHostedService, IDisposable
    {
        private readonly ILogger<ReverseTraderService> _logger;
        private Timer? _timer = null;
        private readonly AppSettings _appSettings;
        private readonly ICoinCheckService _coinService;

        private readonly ICoinCheckServiceV2 _coinServiceV2;
        public ReverseTraderService(ILogger<ReverseTraderService> logger, IConfiguration configuration, ICoinCheckServiceV2 coinCheckServiceV2)
        {
            _logger = logger;
            _appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
            _coinServiceV2 = coinCheckServiceV2;

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ReverseTraderService Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(_appSettings.botinterval));

            return Task.CompletedTask;
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
            _logger.LogInformation("ReverseTraderService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
