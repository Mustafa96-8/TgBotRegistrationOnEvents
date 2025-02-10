using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain.Collections
{
    public static class Keyboards
    {

        /// <summary>
        /// Инициализация кнопок для главного меню бота.
        /// </summary>
        /// <returns><see langword="InlineKeyboardMarkup"/> - клавиатура для отображения в сообщении.</returns>

        public static InlineKeyboardMarkup GetEventKeyboard(IEnumerable<Event> events, char operation)
        {
            var inlineButtons = events
                .Select(n => new[] { InlineKeyboardButton.WithCallbackData(text: n.Name + " " + n.Date.ToString(), callbackData: ":" + operation + n.Id) })
                .ToArray();

            var backButton = new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "/getMenu")
            };

            return new InlineKeyboardMarkup(inlineButtons.Append(backButton));
        }
        public static InlineKeyboardMarkup GetUserMenuKeyboard()
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Мои мероприятия", callbackData: "/getRegisterEvents")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Отменить регистрацию", callbackData: "/unregister")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Зарегестрироваться на мероприятие", callbackData: "/getEvent")
            }
            });
            return inlineKeyboard;
        }
        public static InlineKeyboardMarkup GetUserMenuInEventsKeyboard()
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "/getMenu")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Отменить регистрацию", callbackData: "/unregister")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Зарегестрироваться на мероприятие", callbackData: "/getEvent")
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
                InlineKeyboardButton.WithCallbackData(text: "Получить список зарегестрированных", callbackData: "/getUsers"),
                InlineKeyboardButton.WithCallbackData(text: "Переключить оповещения о новых пользователях", callbackData: "/switchNotification"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Создать новое мероприятие", callbackData: "/createEvent"),
                InlineKeyboardButton.WithCallbackData(text: "Удалить мероприятие", callbackData: "/deleteEvent"),
            }, 
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Добавить администратора", callbackData: "/addAdmin"),            }
        });
            return inlineKeyboard;
        }

        public static InlineKeyboardMarkup GetToMenuKeyboard() {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            { 
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "/getMenu")
            }
            });  
        return inlineKeyboard;
        }
    }
}
