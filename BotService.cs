using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotWindowsService.Service;
using System.Text.Json;

public class BotService
{
    private readonly ITelegramBotClient? _botClient;

    public BotService()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot_config.json");
        if (!File.Exists(configPath))
        {
            Console.WriteLine("Файл конфигурации bot_config.json не найден. Создаю шаблон...");
            
            var templateConfig = new BotConfig
            {
                Token = "YOUR_BOT_TOKEN_HERE",
                AdminId = 0,
                CartonAmount = 1.0m,
                AutoStart = false
            };

            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Добавляем комментарии в начало файла
            string configWithComments = @"// Конфигурация Telegram бота
            // Token - токен вашего бота, полученный от @BotFather
            // AdminId - ваш Telegram ID (можно узнать у @userinfobot)
            // CartonAmount - количество литров в одной покупке
            // AutoStart - автозапуск при старте Windows (true/false)

" + JsonSerializer.Serialize(templateConfig, jsonOptions);

            File.WriteAllText(configPath, configWithComments);

            Console.WriteLine("Шаблон конфигурации создан. Пожалуйста, отредактируйте файл bot_config.json, указав ваш токен бота.");
            Console.WriteLine("После настройки перезапустите сервис.");
            Environment.Exit(0);
            return;
        }

        var existingConfigJson = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<BotConfig>(existingConfigJson);
        if (string.IsNullOrEmpty(config?.Token) || config.Token == "YOUR_BOT_TOKEN_HERE")
        {
            Console.WriteLine("ОШИБКА: Токен бота не настроен в конфигурационном файле.");
            Console.WriteLine("Пожалуйста, отредактируйте файл bot_config.json, указав ваш токен бота.");
            Console.WriteLine("После настройки перезапустите сервис.");
            Environment.Exit(0);
            return;
        }

        _botClient = new TelegramBotClient(config.Token);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_botClient == null)
        {
            Console.WriteLine("ОШИБКА: Бот не инициализирован. Сервис будет остановлен.");
            Environment.Exit(1);
            return;
        }

        var me = await _botClient.GetMe(ct);
        Console.WriteLine($"Бот {me.Username} запущен.");

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        await _botClient.ReceiveAsync(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            ct
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.Message && update.Message.Text != null)
        {
            string text = update.Message.Text.Trim();
            long chatId = update.Message.Chat.Id;
            long userId = update.Message.From.Id;
            string userName = update.Message.From.FirstName;
            if (!string.IsNullOrEmpty(update.Message.From.LastName))
                userName += " " + update.Message.From.LastName;

            // Добавляем пользователя в базу данных при первом сообщении
            DataAccess.AddTransaction(
                userId.ToString(),
                userName,
                "INIT",
                0,
                DateTime.Now
            );

            if (text == "/start")
            {
                await botClient.SendMessage(chatId, "Привет! Я сохраню ваш ID.", cancellationToken: ct);
            }
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        Console.WriteLine($"Ошибка Telegram: {exception.Message}");
        return Task.CompletedTask;
    }
}