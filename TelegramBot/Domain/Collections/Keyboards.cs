using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Domain.Collections
{
    public static class Keyboards
    {

        /// <summary>
        /// Инициализация кнопок для главного меню бота.
        /// </summary>
        /// <returns><see langword="InlineKeyboardMarkup"/> - клавиатура для отображения в сообщении.</returns>

        public static InlineKeyboardMarkup GetUnregisterKeyboard()
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {

            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Отменить регистрацию", callbackData: "/unregister")
            }
            });
            return inlineKeyboard;
        }

        public static InlineKeyboardMarkup GetKeyBoardYesOrNo()
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "yes"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "no"),
            }
        });
            return inlineKeyboard;
        }

        public static InlineKeyboardMarkup GetKeyBoardInRegistration()
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "/back"),
            }
        });
            return inlineKeyboard;
        }
        public static InlineKeyboardMarkup GetKeyBoardCancel()
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Отмена", callbackData: "/cancel"),
            }
        });
            return inlineKeyboard;
        }

        public static InlineKeyboardMarkup GetAdminKeyboard()
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Получить список зарегестрированных", callbackData: "/getall"),
                InlineKeyboardButton.WithCallbackData(text: "Переключить оповещения о новых пользователях", callbackData: "/switchNotify"),
            },  
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Создать новое мероприятие", callbackData: "/switchNotify"),
                InlineKeyboardButton.WithCallbackData(text: "Переключить оповещения о новых пользователях", callbackData: "/switchNotify"),
            }
        });
            return inlineKeyboard;
        }
    }
}
