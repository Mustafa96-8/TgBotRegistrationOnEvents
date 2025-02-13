using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Domain.Entities;

namespace TelegramBot.Domain.Collections
{
    public static class Keyboards
    {

        public static InlineKeyboardMarkup GetEventKeyboard(IEnumerable<Event> events, string operation, int page = 0, bool IsButtonsOn = true)
        {
            InlineKeyboardButton[]? pageButtons = new[]{
                getButtonPrev(operation, page),
                getButtonNext(operation, page,events.Count())
             };

            var backButton = new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "/getMenu")
            };

            if(operation == "g")
            {
                return new InlineKeyboardMarkup(new[] { pageButtons, backButton });
            }

            int i = page * ApplicationConstants.numberOfObjectsPerPage;
            var inlineButtons = events
             .Select(n => new[] {InlineKeyboardButton.WithCallbackData(text: "№"+ (i++).ToString()+" " + n.Name + " ", callbackData: ":" + operation + n.Id)})
             .ToArray();

            return new InlineKeyboardMarkup(inlineButtons.Append(pageButtons).Append(backButton));
        }
        public static InlineKeyboardButton getButtonPrev(string operation,int page)
        {
            if (page != 0)
            {
                return InlineKeyboardButton.WithCallbackData(text: "<-", callbackData: string.Concat("<-", '|', operation, '|', page));
            }
            return InlineKeyboardButton.WithCallbackData(text: "-|-", callbackData: "/pass");
        }
        public static InlineKeyboardButton getButtonNext(string operation, int page,int eventCount)
        {
            if (eventCount < ApplicationConstants.numberOfObjectsPerPage) 
                return InlineKeyboardButton.WithCallbackData(text: "-|-",callbackData: "/pass");
            return InlineKeyboardButton.WithCallbackData(text: "->", callbackData: string.Concat("->", '|', operation, '|', page));
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
        public static InlineKeyboardMarkup GetUserMenuInEventsKeyboard(int eventsCount, string operation, int page)
        {
            InlineKeyboardButton[]? pageButtons = new[]{
                getButtonPrev(operation, page),
                getButtonNext(operation, page,eventsCount)
            };
            var inlineKeyboard = new[]
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
            };
            return new InlineKeyboardMarkup(inlineKeyboard.Append(pageButtons));
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
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Получить список мероприятий", callbackData: "/getevents"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Переключить оповещения о новых пользователях", callbackData: "/switchNotification"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Создать новое мероприятие", callbackData: "/createEvent"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Удалить мероприятие", callbackData: "/deleteEvent"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Добавить администратора", callbackData: "/addAdmin"),  
                InlineKeyboardButton.WithCallbackData(text: "Удалить пользователя", callbackData: "/deleteperson"),  
            }
        });
            return inlineKeyboard;
        }
    }
}
