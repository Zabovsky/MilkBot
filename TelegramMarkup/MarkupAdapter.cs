using Telegram.Bot.Types.ReplyMarkups;
using My = MilkBot.TelegramMarkup; // псевдоним

public static class MarkupAdapter
{
    public static InlineKeyboardMarkup ToTelegramInline(My.InlineKeyboardMarkup custom)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        foreach (var row in custom.InlineKeyboard)
        {
            var btnRow = new List<InlineKeyboardButton>();
            foreach (var btn in row)
            {
                btnRow.Add(InlineKeyboardButton.WithCallbackData(btn.Text, btn.CallbackData));
            }
            rows.Add(btnRow);
        }

        return new InlineKeyboardMarkup(rows);
    }

    public static ReplyKeyboardMarkup ToTelegramReply(My.ReplyKeyboardMarkup custom)
    {
        var rows = new List<List<KeyboardButton>>();

        foreach (var row in custom.Keyboard)
        {
            var btnRow = new List<KeyboardButton>();
            foreach (var btn in row)
            {
                btnRow.Add(new KeyboardButton(btn.Text));
            }
            rows.Add(btnRow);
        }

        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = custom.ResizeKeyboard,
            OneTimeKeyboard = custom.OneTimeKeyboard
        };
    }
}
