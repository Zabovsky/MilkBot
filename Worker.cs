using Microsoft.Extensions.Hosting;
using TelegramBotWindowsService.Service;

public class Worker : BackgroundService
{
    private readonly TelegramBotService _botService;

    public Worker(TelegramBotService botService)
    {
        _botService = botService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _botService.StartAsync();
    }
}