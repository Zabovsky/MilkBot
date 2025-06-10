using System;
using System.ServiceProcess;
using TelegramBotWindowsService.Service;

namespace TelegramBotWindowsService.Service
{
    public class WindowsService : ServiceBase
    {
        private TelegramBotService _botService;

        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.LogServiceEvent("WindowsService", "Service starting");
                _botService = new TelegramBotService();
                _botService.StartAsync().GetAwaiter().GetResult();
                Logger.LogServiceEvent("WindowsService", "Service started successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при запуске службы Windows", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                Logger.LogServiceEvent("WindowsService", "Service stopping");
                if (_botService != null)
                {
                    _botService.StopAsync().GetAwaiter().GetResult();
                }
                Logger.LogServiceEvent("WindowsService", "Service stopped successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при остановке службы Windows", ex);
                throw;
            }
        }

        protected override void OnShutdown()
        {
            try
            {
                Logger.LogServiceEvent("WindowsService", "Service shutting down");
                if (_botService != null)
                {
                    _botService.StopAsync().GetAwaiter().GetResult();
                }
                Logger.LogServiceEvent("WindowsService", "Service shutdown completed");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при завершении работы службы Windows", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    Logger.LogServiceEvent("WindowsService", "Service disposing");
                    if (_botService != null)
                    {
                        _botService.StopAsync().GetAwaiter().GetResult();
                    }
                }
                base.Dispose(disposing);
                Logger.LogServiceEvent("WindowsService", "Service disposed");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при освобождении ресурсов службы Windows", ex);
            }
        }
    }
} 