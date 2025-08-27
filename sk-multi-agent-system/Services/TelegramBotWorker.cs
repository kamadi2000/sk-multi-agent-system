using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace sk_multi_agent_system
{
    public class TelegramBotWorker : BackgroundService
    {
        private readonly ILogger<TelegramBotWorker> _logger;
        private readonly TelegramBotService _botService;

        public TelegramBotWorker(ILogger<TelegramBotWorker> logger, TelegramBotService botService)
        {
            _logger = logger;
            _botService = botService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telegram bot worker starting...");

            _botService.Start();

            _logger.LogInformation("Telegram bot is running.");
            return Task.CompletedTask; // keeps host alive
        }
    }
}

