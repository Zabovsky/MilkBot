using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MilkBot
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private CancellationTokenSource _cts;
        private MainForm _form;

        public TelegramBotService(string token, MainForm form)
        {
            token = token.Trim();
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Токен не может быть пустым.", nameof(token));

            _botClient = new TelegramBotClient(token);
            _form = form;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _botClient.StartReceiving(
                updateHandler: UpdateHandler,
                errorHandler: ErrorHandler,
                cancellationToken: _cts.Token);
            Console.WriteLine("Бот запущен");
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            await Task.CompletedTask;
        }

        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                string text = update.Message.Text.Trim();

                // Если команда /start или кнопка "Вернуться на главную" – показываем главное меню
                if (text == "/start" || text == "Вернуться на главную")
                {
                    var mainKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Купил 1 л молока", "Статистика" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = false
                    };

                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Привет! Выберите действие:",
                        replyMarkup: mainKeyboard,
                        cancellationToken: cancellationToken);
                }
                // Если нажата кнопка покупки
                else if (text == "Купил 1 л молока")
                {
                    decimal cartonAmount = _form.GetCartonAmount();
                    string userId = update.Message.From.Id.ToString();

                    // Формируем имя пользователя (FirstName + LastName, если есть)
                    string userName = update.Message.From.FirstName;
                    if (!string.IsNullOrEmpty(update.Message.From.LastName))
                        userName += " " + update.Message.From.LastName;

                    DataAccess.AddTransaction(userId, userName, "BUY", cartonAmount);
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: $"Покупка зафиксирована: {cartonAmount} л молока.\nПользователь: {userName}",
                        cancellationToken: cancellationToken);
                }
                // Если нажата кнопка "Статистика" – показываем подменю статистики
                else if (text == "Статистика")
                {
                    var statsKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Статистика за день", "Статистика за неделю" },
                        new KeyboardButton[] { "Статистика за месяц", "Статистика за год" },
                        new KeyboardButton[] { "Вернуться на главную" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = false
                    };

                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Выберите период статистики:",
                        replyMarkup: statsKeyboard,
                        cancellationToken: cancellationToken);
                }
                // Обработка статистики за день
                else if (text == "Статистика за день")
                {
                    DataTable dt = DataAccess.GetDailySummary(DateTime.Today);
                    string stats = FormatSummary(dt, "Дневная статистика");
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
                // Обработка статистики за неделю
                else if (text == "Статистика за неделю")
                {
                    DateTime today = DateTime.Today;
                    DateTime startDate = today.AddDays(-6);
                    DataTable dt = DataAccess.GetPeriodSummary(startDate, today);
                    string stats = FormatSummary(dt, "Статистика за неделю");
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
                // Обработка статистики за месяц
                else if (text == "Статистика за месяц")
                {
                    DateTime today = DateTime.Today;
                    DateTime startDate = today.AddDays(-29);
                    DataTable dt = DataAccess.GetPeriodSummary(startDate, today);
                    string stats = FormatSummary(dt, "Статистика за месяц");
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
                // Обработка статистики за год
                else if (text == "Статистика за год")
                {
                    DateTime today = DateTime.Today;
                    DateTime startDate = today.AddDays(-364);
                    DataTable dt = DataAccess.GetPeriodSummary(startDate, today);
                    string stats = FormatSummary(dt, "Статистика за год");
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
            }
        }

        // Вспомогательный метод для форматирования статистики в текстовое сообщение
        private string FormatSummary(DataTable dt, string header)
        {
            if (dt.Rows.Count == 0)
                return $"{header}:\nНет данных.";

            string result = $"{header}:\n";
            foreach (DataRow row in dt.Rows)
            {
                string userName = row["UserName"].ToString();
                decimal total = Convert.ToDecimal(row["TotalBought"]);
                result += $"{userName}: {total} л\n";
            }
            return result;
        }

        private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
