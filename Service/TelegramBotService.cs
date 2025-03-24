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
            // Обработка текстовых сообщений
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                string text = update.Message.Text.Trim();
                if (text == "/start" || text == "Вернуться на главную")
                {
                    // Главное меню: reply-клавиатура с двумя кнопками
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
                else if (text == "Купил 1 л молока")
                {
                    decimal cartonAmount = _form.GetCartonAmount();
                    string userId = update.Message.From.Id.ToString();

                    // Формируем имя пользователя из FirstName и (при наличии) LastName
                    string userName = update.Message.From.FirstName;
                    if (!string.IsNullOrEmpty(update.Message.From.LastName))
                        userName += " " + update.Message.From.LastName;

                    DataAccess.AddTransaction(userId, userName, "BUY", cartonAmount);
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: $"Покупка зафиксирована: {cartonAmount} л молока.\nПользователь: {userName}",
                        cancellationToken: cancellationToken);
                }
                else if (text == "Статистика")
                {
                    // При выборе "Статистика" отправляем сообщение с inline‑клавиатурой
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData("Статистика за день", "DAY"),
                            InlineKeyboardButton.WithCallbackData("Статистика за неделю", "WEEK")
                        },
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData("Статистика за месяц", "MONTH"),
                            InlineKeyboardButton.WithCallbackData("Статистика за год", "YEAR")
                        },
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData("Вернуться на главную", "BACK")
                        }
                    });

                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Выберите период статистики:",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: cancellationToken);
                }
            }
            // Обработка callback-запросов от inline-кнопок
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var callback = update.CallbackQuery;
                string data = callback.Data;

                if (data == "DAY")
                {
                    DataTable dt = DataAccess.GetDailySummary(DateTime.Today);
                    string stats = FormatSummary(dt, "Дневная статистика");
                    await botClient.SendMessage(
                        chatId: callback.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
                else if (data == "WEEK")
                {
                    DateTime today = DateTime.Today;
                    DateTime startDate = today.AddDays(-6);
                    DataTable dt = DataAccess.GetPeriodSummary(startDate, today);
                    string stats = FormatSummary(dt, "Статистика за неделю");
                    await botClient.SendMessage(
                        chatId: callback.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
                else if (data == "MONTH")
                {
                    DateTime today = DateTime.Today;
                    DateTime startDate = today.AddDays(-29);
                    DataTable dt = DataAccess.GetPeriodSummary(startDate, today);
                    string stats = FormatSummary(dt, "Статистика за месяц");
                    await botClient.SendMessage(
                        chatId: callback.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
                else if (data == "YEAR")
                {
                    DateTime today = DateTime.Today;
                    DateTime startDate = today.AddDays(-364);
                    DataTable dt = DataAccess.GetPeriodSummary(startDate, today);
                    string stats = FormatSummary(dt, "Статистика за год");
                    await botClient.SendMessage(
                        chatId: callback.Message.Chat.Id,
                        text: stats,
                        cancellationToken: cancellationToken);
                }
                else if (data == "BACK")
                {
                    // Возвращаем пользователя в главное меню (reply-клавиатура)
                    var mainKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Купил 1 л молока", "Статистика" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = false
                    };

                    await botClient.SendMessage(
                        chatId: callback.Message.Chat.Id,
                        text: "Главное меню:",
                        replyMarkup: mainKeyboard,
                        cancellationToken: cancellationToken);
                }
                // Ответ на callback-запрос для снятия "крутилки" в клиенте
                await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);
            }
        }

        // Вспомогательный метод для форматирования данных статистики в строку
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
