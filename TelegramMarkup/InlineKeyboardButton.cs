using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilkBot.TelegramMarkup
{
    public class InlineKeyboardMarkup : IReplyMarkup
    {
        public List<List<InlineKeyboardButton>> InlineKeyboard { get; set; }

        public InlineKeyboardMarkup(IEnumerable<IEnumerable<InlineKeyboardButton>> keyboard)
        {
            InlineKeyboard = keyboard.Select(row => row.ToList()).ToList();
        }
    }

    public class InlineKeyboardButton
    {
        public string Text { get; set; }
        public string CallbackData { get; set; }

        public InlineKeyboardButton(string text, string callbackData)
        {
            Text = text;
            CallbackData = callbackData;
        }

        public static InlineKeyboardButton WithCallbackData(string text, string callbackData)
        {
            return new InlineKeyboardButton(text, callbackData);
        }
    }
}