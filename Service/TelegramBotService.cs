using MilkBot.TelegramMarkup;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MilkBot
{
    public class TelegramBotService
    {
        private CancellationTokenSource _cts;
        private MainFormNew _form;
        private readonly long _adminId;
        private bool _wasRestartedOnce = false;
        private bool _isManualRestarting = false;
        private string _editTargetUserId;
        private long _editAdminUserId;
        private readonly TelegramBotClient _botClient;
        private int? _lastAdminMessageId;
        private string _lastAdminMessageText;
        private readonly Dictionary<string, DateTime> _lastBuyTime = new();
        private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(1);
        private string _lastHandledCallbackId;

        public TelegramBotService(string token, MainFormNew form, long adminId = 0)
        {
            token = token.Trim();
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Токен не может быть пустым.", nameof(token));

            _botClient = new TelegramBotClient(token);
            _form = form;
            _adminId = adminId;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _botClient.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: _cts.Token);
            Console.WriteLine("Бот запущен");
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            await Task.CompletedTask;
        }

        private async Task UpdateHandler(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                string text = update.Message.Text.Trim();

                if (!string.IsNullOrEmpty(_editTargetUserId) && update.Message.From.Id == _editAdminUserId)
                {
                    if (decimal.TryParse(text, out decimal newValue))
                    {
                        bool result = DataAccess.ReplaceTodayAmount(_editTargetUserId, newValue);
                        await _botClient.SendMessage(update.Message.Chat.Id,
                            result ? $"✅ Обновлено: теперь {_editTargetUserId} имеет {newValue} л за сегодня."
                                   : $"❌ Не удалось обновить данные.");
                    }
                    else
                    {
                        await _botClient.SendMessage(update.Message.Chat.Id, "Введите корректное число (например: 2.5)");
                    }

                    _editTargetUserId = null;
                    _editAdminUserId = 0;
                    return;
                }

                if (text == "/start" || text == "Вернуться на главную")
                {
                    var mainKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("Купил 1 л молока"), new KeyboardButton("Статистика") }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    await _botClient.SendMessage(update.Message.Chat.Id, "Привет! Выберите действие:",
                        replyMarkup: MarkupAdapter.ToTelegramReply(mainKeyboard));
                }
                else if (text == "Купил 1 л молока")
                {
                    string userId = update.Message.From.Id.ToString();
                    if (_lastBuyTime.TryGetValue(userId, out var lastTime))
                    {
                        if (DateTime.Now - lastTime < _cooldown)
                        {
                            await _botClient.SendMessage(update.Message.Chat.Id, "⏳ Подождите немного перед следующей покупкой.");
                            return;
                        }
                    }
                    _lastBuyTime[userId] = DateTime.Now;

                    decimal cartonAmount = _form.GetCartonAmount();
                    string userName = update.Message.From.FirstName;
                    if (!string.IsNullOrEmpty(update.Message.From.LastName))
                        userName += " " + update.Message.From.LastName;

                    DataAccess.AddTransaction(userId, userName, "BUY", cartonAmount, DateTime.Now);

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("❌ Отменить покупку", $"CANCEL_BUY:{userId}") }
                    });

                    await _botClient.SendMessage(update.Message.Chat.Id,
                        $"Покупка зафиксирована: {cartonAmount} л молока.",
                        replyMarkup: MarkupAdapter.ToTelegramInline(keyboard));
                }

                else if (text == "Статистика")
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] {
                            InlineKeyboardButton.WithCallbackData("Статистика за день", "DAY"),
                            InlineKeyboardButton.WithCallbackData("Статистика за неделю", "WEEK")
                        },
                        new[] {
                            InlineKeyboardButton.WithCallbackData("Статистика за месяц", "MONTH"),
                            InlineKeyboardButton.WithCallbackData("Статистика за год", "YEAR")
                        }
                    });

                    await _botClient.SendMessage(update.Message.Chat.Id,
                        "Выберите период статистики:",
                        replyMarkup: MarkupAdapter.ToTelegramInline(keyboard));
                }
                else if (text == "/admin" && update.Message.From.Id == _adminId)
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] {
                            InlineKeyboardButton.WithCallbackData("🔁 Перезапустить бота", "RESTART_BOT"),
                            InlineKeyboardButton.WithCallbackData("♻ Перезапустить приложение", "RESTART_APP")
                        }
                    });

                    await _botClient.SendMessage(update.Message.Chat.Id, "🛠 Админ-панель:",
                        replyMarkup: MarkupAdapter.ToTelegramInline(keyboard));
                }
                else if (text == "/правка" && update.Message.From.Id == _adminId)
                {
                    var dt = DataAccess.GetDailySummary(DateTime.Today);
                    if (dt.Rows.Count == 0)
                    {
                        await _botClient.SendMessage(update.Message.Chat.Id, "Сегодня никто ничего не покупал.");
                        return;
                    }

                    var buttons = new List<InlineKeyboardButton[]>();
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["UserId"].ToString() == "") continue;

                        string userId = row["UserId"].ToString();
                        string userName = row["UserName"].ToString();
                        decimal amount = Convert.ToDecimal(row["TotalBought"]);

                        buttons.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData($"{userName} — {amount} л", $"EDIT:{userId}")
                        });
                    }

                    var markup = new InlineKeyboardMarkup(buttons);
                    await _botClient.SendMessage(update.Message.Chat.Id,
                        "✏️ Выберите пользователя для редактирования:",
                        replyMarkup: MarkupAdapter.ToTelegramInline(markup));
                }




            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                var callback = update.CallbackQuery;
                var data = callback.Data;

                if (data == "RESTART_BOT")
                {
                    if (_adminId > 0 && callback.From.Id == _adminId)
                    {
                        if (_isManualRestarting)
                        {
                            await _botClient.AnswerCallbackQuery(callback.Id, "⏳ Уже выполняется перезапуск.");
                            return;
                        }
                        else if (IsDuplicateCallback(callback.Id))
                        {
                            Console.WriteLine("[INFO] Callback уже обработан ранее — RESTART_BOT");
                            return;
                        }

                        _isManualRestarting = true;

                        await _botClient.AnswerCallbackQuery(callback.Id);
                        await SendToAdminIfChanged("🔁 Перезапускаю бота...");
                        await RestartBot();

                        _isManualRestarting = false;
                    }
                    return;
                }
                else if (data == "RESTART_APP")
                {
                    if (_adminId > 0 && callback.From.Id == _adminId)
                    {
                        if (IsDuplicateCallback(callback.Id))
                        {
                            Console.WriteLine("[INFO] Callback уже обработан ранее — RESTART_APP");
                            return;
                        }

                        await _botClient.SendMessage(callback.Message.Chat.Id, "♻ Перезапускаю приложение...");
                        RestartApplication();
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQuery(callback.Id, "Недостаточно прав.", showAlert: true);
                    }
                }



                if (data.StartsWith("CANCEL_BUY:"))
                {
                    string userId = data.Split(':')[1];
                    if (callback.From.Id.ToString() != userId)
                    {
                        await _botClient.AnswerCallbackQuery(callback.Id, "Эта кнопка не для вас.", showAlert: true);
                        return;
                    }

                    bool success = DataAccess.CancelLastBuy(userId);
                    if (success)
                    {
                        await _botClient.EditMessageText(callback.Message.Chat.Id, callback.Message.MessageId, "✅ Последняя покупка отменена.");
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQuery(callback.Id, "❌ Отмена недоступна. Возможно, прошло больше часа.", showAlert: true);
                    }
                }
                else if (data.StartsWith("EDIT:"))
                {
                    _editTargetUserId = data.Substring("EDIT:".Length);
                    _editAdminUserId = callback.From.Id;

                    await _botClient.SendMessage(callback.Message.Chat.Id,
                        $"Введите новое количество литров для пользователя {_editTargetUserId} (например: `2.5`)",
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    await HandleStatistics(data, callback);
                }

                await _botClient.AnswerCallbackQuery(callback.Id);
            }
        }
        private static readonly string LastCallbackFile = "last_callback_id.txt";

        private bool IsDuplicateCallback(string callbackId)
        {
            try
            {
                if (File.Exists(LastCallbackFile))
                {
                    string last = File.ReadAllText(LastCallbackFile);
                    if (last == callbackId)
                        return true;
                }

                File.WriteAllText(LastCallbackFile, callbackId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Не удалось проверить или сохранить callback ID: " + ex.Message);
            }

            return false;
        }


        private void RestartApplication()
        {
            try
            {
                string exePath = Environment.ProcessPath ?? Application.ExecutablePath;

                // передаём специальный аргумент --restarted
                System.Diagnostics.Process.Start(exePath, "/minimized --restarted");

                Environment.Exit(0); // Завершаем текущий процесс
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при перезапуске приложения: " + ex.Message);
                _botClient.SendMessage(_adminId, $"❌ *Ошибка в боте:*```{ex.Message}```",
               parseMode: ParseMode.Markdown);
            }
        }


        private async Task HandleStatistics(string data, CallbackQuery callback)
        {
            DateTime today = DateTime.Today;
            DataTable dt = null;
            string header = "";

            if (data == "DAY")
            {
                dt = DataAccess.GetDailySummary(today);
                header = "Дневная статистика";
            }
            else if (data == "WEEK")
            {
                dt = DataAccess.GetPeriodSummary(today.AddDays(-6), today);
                header = "Статистика за неделю";
            }
            else if (data == "MONTH")
            {
                dt = DataAccess.GetPeriodSummary(today.AddDays(-29), today);
                header = "Статистика за месяц";
            }
            else if (data == "YEAR")
            {
                dt = DataAccess.GetPeriodSummary(today.AddDays(-364), today);
                header = "Статистика за год";
            }

            if (dt != null)
            {
                string summary = FormatSummary(dt, header);
                await _botClient.SendMessage(callback.Message.Chat.Id, summary);
            }
        }

        private async Task SendToAdminIfChanged(string text)
        {
            if (_adminId == 0) return;

            if (_lastAdminMessageText == text)
                return;

            var msg = await _botClient.SendMessage(_adminId, text);
            _lastAdminMessageId = msg.MessageId;
            _lastAdminMessageText = text;
        }

        private async Task RestartBot()
        {
            await StopAsync();
            await Task.Delay(1000);
            await StartAsync();
            _wasRestartedOnce = false;
        }

        //private async Task ErrorHandler(ITelegramBotClient _, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        //{
        //    Console.WriteLine($"[Ошибка бота] {source}: {exception.Message}");
        //    File.AppendAllText("bot_errors.log", $"[{DateTime.Now}] {source}: {exception}");

        //    if (!_wasRestartedOnce)
        //    {
        //        _wasRestartedOnce = true;
        //        try
        //        {
        //            await SendToAdminIfChanged("⚠ Обнаружена ошибка, пробую перезапустить бота...");
        //            await RestartBot();
        //            return;
        //        }
        //        catch (Exception ex)
        //        {
        //            await SendToAdminIfChanged($"❌ Ошибка повторилась после перезапуска:`{ ex.Message}`");
        //        }
        //    }
        //    else
        //    {
        //        var keyboard = MarkupAdapter.ToTelegramInline(new InlineKeyboardMarkup(new[]
        //        {
        //            new[] {
        //                InlineKeyboardButton.WithCallbackData("🔁 Перезапустить бота", "RESTART_BOT"),
        //                InlineKeyboardButton.WithCallbackData("♻ Перезапустить приложение", "RESTART_APP")
        //            }
        //        }));

        //        await _botClient.SendMessage(_adminId,$"❌ *Ошибка в боте:*```{ exception.Message}```",
        //            parseMode: ParseMode.Markdown,
        //            replyMarkup: keyboard);
        //    }
        //}

        private async Task ErrorHandler(ITelegramBotClient _, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.Now;
            var errorType = exception.GetType().Name;
            var fullMessage = exception.ToString();

            // Лог в консоль и файл
            Console.WriteLine($"[{timestamp}] PollingError: {errorType}: {exception.Message}");
            File.AppendAllText("bot_errors.log", $"[{timestamp}] PollingError: {fullMessage}\n");

            if (_adminId > 0)
            {
                var keyboard = MarkupAdapter.ToTelegramInline(new InlineKeyboardMarkup(new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔁 Перезапустить бота", "RESTART_BOT"),
                InlineKeyboardButton.WithCallbackData("♻ Перезапустить приложение", "RESTART_APP")
            }
        }));

                var userMessage = $"❌ *Ошибка при работе с Telegram API*\n" +
                                  $"*Тип:* `{errorType}`\n" +
                                  $"```\n{exception.Message}```";

                try
                {
                    await _botClient.SendMessage(
                        chatId: _adminId,
                        text: userMessage,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Не удалось уведомить админа: " + ex.Message);
                }
            }
        }


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

            return result.TrimEnd(); // убираем последний \n на всякий случай
        }

    }
}
