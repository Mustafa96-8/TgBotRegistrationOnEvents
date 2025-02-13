﻿using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Domain.Entities;
using TelegramBot.Extensions;

namespace TelegramBot.Domain.Collections
{
    public static class Keyboards
    {

        public static InlineKeyboardMarkup GetEventKeyboard(IEnumerable<Event> events, string operation, int page = 0, bool IsButtonsOn = true)
        {
            var buttonsList = new List<InlineKeyboardButton[]>();

            int totalEvents = events.Count();
            var pageButtons = new List<InlineKeyboardButton>();

            if(page > 0) // Кнопка "Prev" появляется только если это НЕ первая страница
            {
                pageButtons.Add(getButtonPrev(operation, page));
            }
            if(totalEvents >= ApplicationConstants.numberOfObjectsPerPage) // Кнопка "Next" появляется только если это НЕ последняя страница
            {
                pageButtons.Add(getButtonNext(operation, page, totalEvents));
            }

            if(operation != "g")
            {
                int i = page * ApplicationConstants.numberOfObjectsPerPage + 1;
                var inlineButtons = events
                 .Select(n => new[] {
                     InlineKeyboardButton.WithCallbackData(
                         text: (i++).AddUnicodeSymbols("\uFE0F\u20E3") + " " + n.Name + " ",
                         callbackData: ":" + operation + n.Id) })
                 .ToArray();
                buttonsList.AddRange(inlineButtons);
            }
            if(pageButtons.Any())
            {
                buttonsList.Add(pageButtons.ToArray());
            }
            buttonsList.Add(new[] { InlineKeyboardButton.WithCallbackData(text: "\u21A9 Назад", callbackData: "/getMenu") });

            return new InlineKeyboardMarkup(buttonsList);
        }

        public static InlineKeyboardButton getButtonPrev(string operation,int page)
            => InlineKeyboardButton.WithCallbackData(
                text: "\u2B05", 
                callbackData: string.Concat("<-", '|', operation, '|', page));

        public static InlineKeyboardButton getButtonNext(string operation, int page,int eventCount)
            => InlineKeyboardButton.WithCallbackData(
                text: "\u27A1", 
                callbackData: string.Concat("->", '|', operation, '|', page));
        

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
