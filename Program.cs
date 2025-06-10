using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramBotWindowsService.Service;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Diagnostics;

// P/Invoke constants and declarations
const int SW_HIDE = 0;

[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

var builder = Host.CreateApplicationBuilder(args);

// Настраиваем службу Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "TelegramMilkBot";
});

// Добавляем наши сервисы
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService<Worker>();

// Скрываем консольное окно
if (OperatingSystem.IsWindows())
{
    var handle = GetConsoleWindow();
    ShowWindow(handle, SW_HIDE);
}

var host = builder.Build();

// Если запущено как консольное приложение, устанавливаем службу
if (Environment.UserInteractive)
{
    Console.WriteLine("Установка службы TelegramMilkBot...");
    
    // Проверяем аргументы командной строки
    if (args.Length > 0 && args[0].Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
    {
        WindowsServiceInstaller.UninstallService();
    }
    else
    {
        WindowsServiceInstaller.InstallService(builder);
    }
    
    Console.WriteLine("\nНажмите любую клавишу для выхода...");
    Console.ReadKey();
}
else
{
    // Запускаем как службу
    host.Run();
}