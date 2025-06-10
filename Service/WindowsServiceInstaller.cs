using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TelegramBotWindowsService.Service
{
    public class WindowsServiceInstaller
    {
        private const string ServiceDefaultName = "TelegramMilkBot";

        public static void InstallService(HostApplicationBuilder builder)
        {
            if (!OperatingSystem.IsWindows()) return; // Только для Windows

            string exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                Console.WriteLine("Не удалось определить путь к исполняемому файлу");
                return;
            }

            try
            {
                // Проверяем, установлена ли служба
                if (IsServiceInstalled(ServiceDefaultName))
                {
                    Console.WriteLine($"Служба '{ServiceDefaultName}' уже установлена.");
                    return;
                }

                Console.WriteLine($"Создаём Windows Service: {ServiceDefaultName}");

                // Создаём службу
                RunProcess("sc", $"create {ServiceDefaultName} binPath= \"{exePath}\" start= auto");

                // Добавляем описание
                RunProcess("sc", $"description {ServiceDefaultName} \"Telegram бот для учета покупок молока\"");

                // Запускаем службу
                RunProcess("sc", $"start {ServiceDefaultName}");

                Console.WriteLine($"Служба '{ServiceDefaultName}' успешно установлена и запущена!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке службы: {ex.Message}");
                Logger.LogError("Ошибка при установке службы", ex);
            }
        }

        public static void UninstallService()
        {
            if (!OperatingSystem.IsWindows()) return;

            try
            {
                if (!IsServiceInstalled(ServiceDefaultName))
                {
                    Console.WriteLine($"Служба '{ServiceDefaultName}' не установлена.");
                    return;
                }

                Console.WriteLine($"Удаляем службу: {ServiceDefaultName}");

                // Останавливаем службу
                RunProcess("sc", $"stop {ServiceDefaultName}");

                // Удаляем службу
                RunProcess("sc", $"delete {ServiceDefaultName}");

                Console.WriteLine($"Служба '{ServiceDefaultName}' успешно удалена!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении службы: {ex.Message}");
                Logger.LogError("Ошибка при удалении службы", ex);
            }
        }

        private static bool IsServiceInstalled(string serviceName)
        {
            try
            {
                Process process = RunProcess("sc", $"query {serviceName}");
                string output = process.StandardOutput.ReadToEnd();
                return output.Contains("STATE");
            }
            catch
            {
                return false;
            }
        }

        private static Process RunProcess(string fileName, string arguments)
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new Exception($"Команда завершилась с ошибкой (код {process.ExitCode}): {error}");
            }

            return process;
        }
    }
} 