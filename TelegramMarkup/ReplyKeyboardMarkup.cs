using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilkBot.TelegramMarkup
{
    public class ReplyKeyboardMarkup : IReplyMarkup
    {
        public List<List<KeyboardButton>> Keyboard { get; set; }
        public bool ResizeKeyboard { get; set; }
        public bool OneTimeKeyboard { get; set; }

        public ReplyKeyboardMarkup(IEnumerable<IEnumerable<KeyboardButton>> keyboard)
        {
            Keyboard = keyboard.Select(row => row.ToList()).ToList();
        }
    }

    public class KeyboardButton
    {
        public string Text { get; set; }

        public KeyboardButton(string text)
        {
            Text = text;
        }
    }
}
