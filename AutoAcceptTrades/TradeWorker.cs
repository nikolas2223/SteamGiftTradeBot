namespace AutoAcceptTrades
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Монниторинг предложений обмена
    /// </summary>
    internal class TradeWorker : BackgroundService
    {
        #region private fields

        /// <summary>
        /// Клиент для выполнения действий над 
        /// предложениями обмена в Steam
        /// </summary>
        private readonly SteamClient _steamClient;
        
        /// <summary>
        /// Логер
        /// </summary>
        private readonly ILogger<TradeWorker> _logger;

        /// <summary>
        /// Интервал вызова обработки обращений
        /// </summary>
        private readonly TimeSpan _interval;

        #endregion

        /// <inheritdoc/>
        public TradeWorker(SteamClient steamClient, ILogger<TradeWorker> logger, IConfiguration config)
        {
            _steamClient = steamClient;
            _logger = logger;
            var minutes = config.GetValue<int>("Polling:IntervalMinutes");
            _interval = TimeSpan.FromMinutes(minutes);
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TradeWorker started with interval {Interval}.", _interval);

            using var timer = new PeriodicTimer(_interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    _logger.LogInformation("Checking trade offers...");
                    await _steamClient.CheckAndAcceptGiftTradesAsync();
                }
            }
            catch (OperationCanceledException cancelExcetion)
            {
                _logger.LogInformation(cancelExcetion, "TradeWorker is stopping.");
            }
        }
    }
}
