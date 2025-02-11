using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Enums;
using static TelegramBot.Domain.Collections.Keyboards;
using TelegramBot.Domain.Collections;
using TelegramBot.Contracts;
using TelegramBot.Helpers;
using TelegramBot.Services;
using Microsoft.VisualBasic;
using System.Threading;

namespace TelegramBot.Handlers;


public class UpdateHandler : IUpdateHandler
{

    private readonly ITelegramBotClient bot;
    private readonly ILogger<UpdateHandler> logger;
    private readonly HttpClient httpClient;

    private readonly UserProfileService userProfileService;
    private readonly AdminProfileService adminProfileService;
    private readonly PersonService personService;
    private readonly EventService eventService;




    public UpdateHandler(
        ITelegramBotClient bot,
        ILogger<UpdateHandler> logger,
        HttpClient httpClient,
        UserProfileService userProfileService,
        AdminProfileService adminProfileService,
        PersonService personService,
        EventService eventService
        )
    {
        this.bot = bot;
        this.logger = logger;
        this.httpClient = httpClient;
        this.userProfileService = userProfileService;
        this.adminProfileService = adminProfileService;
        this.personService = personService;
        this.eventService = eventService;
    }

    public async Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("HandleError: {Exception}", exception);

        if (exception is ApiRequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }


    #region Message input Handlers
    /// <summary>
    /// Обработчик нового события от пользователя.
    /// Определяет какой тип события произошёл и в соответствии с этим вызывает нужный метод для обработки
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="update"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await (update switch
        {
            { Message: { } message } => OnMessage(message, cancellationToken),
            { EditedMessage: { } message } => OnMessage(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll } => OnPoll(poll),
            { PollAnswer: { } pollAnswer } => OnPollAnswer(pollAnswer),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Received message type: {msg.Type}");

        if (msg.Text is not { } messageText)
            return;

        Person? person = await personService.Get(msg.Chat.Id, cancellationToken);

        // Инициализация профиля пользователя при первом обращении
        if (person == null)
        {
            person = Person.Create(msg.Chat.Id);
        }

        if (person.role == Roles.Admin)
        {
            AdminProfile Admin = (AdminProfile)person;
            Message sentMessage = await (messageText.Split(' ')[0] switch
            {
                Commands.Admin.GetAdminPanel => GetAdminPanel(msg, Admin, cancellationToken),
                "/addAdmin"=>HandleAdminCreateNewAdmin(msg, Admin, cancellationToken),
                "/menu"=>GetAdminPanel(msg, Admin, cancellationToken),
                "/getEventDebug" => HandleGetEvent(msg, Admin, cancellationToken),
                "/deleteDebug" => DeleteProfileDebug(msg, Admin, cancellationToken),
                _ => HandleAdminInput(msg, Admin, cancellationToken)
            });
            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
        }
        else
        {
            UserProfile User = (UserProfile)person;
            if (User.IsRegistered)
            {
                Message sentMessage = await (messageText.Split(' ')[0] switch
                {
                    "/getEvent" => HandleGetEvent(msg, User, cancellationToken),
                    "/getAdminRole" => SetAdminRole(msg.Chat.Id, User, cancellationToken),
                    _ => HandleUserInput(msg, User, cancellationToken)
                });
                logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
            }
            else
            {
                Message sentMessage = await (messageText.Split(' ')[0] switch
                {

                    "/start" => StartRegistration(msg, User, cancellationToken),
                    "/getAdminRole" => SetAdminRole(msg.Chat.Id, User,cancellationToken),
                    "/deleteDebug" => DeleteProfileDebug(msg, User, cancellationToken),
                    _ => HandleUserInput(msg, User, cancellationToken)
                });

                logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
            }
        }
    }

    private async Task<Message> HandleUserInput(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        return await (userProfile.UserState switch
        {
            UserStates.awaiting_registration => StartRegistration(msg, userProfile, cancellationToken),
            UserStates.awaiting_Eighteen => HandleChekIsEighteen(msg, userProfile, cancellationToken),
            UserStates.awaiting_name => HandleAwaitingName(msg, userProfile, cancellationToken),
            UserStates.awaiting_phone => HandleAwaitingPhone(msg, userProfile, cancellationToken),
            _ => HandleGetUserMenu(msg, userProfile, cancellationToken)
        });
    }

    #endregion


    #region Обработка Нажатий кнопок

    private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        Message msg = callbackQuery.Message;

        logger.LogInformation($"Received callback type: {msg.Type}");

        Person? person = await personService.Get(msg.Chat.Id, cancellationToken);

        // Инициализация профиля пользователя при первом обращении
        if (person == null)
        {
            person = Person.Create(msg.Chat.Id);
        }
        Message sentMessage = null;
        if (callbackQuery.Data[0] == ':')
        {
            Guid eventId;
            char command = callbackQuery.Data[1];
            string temp = callbackQuery.Data.Substring(2);
            if (Guid.TryParse(temp, out eventId))
            {
                if (person.role == Roles.Admin)
                {
                    AdminProfile Admin = (AdminProfile)person;
                    sentMessage = await (command switch
                    {
                        'a' => AdminGetAllRegistratedUsers(msg, Admin, eventId, cancellationToken),
                        's' => SwitchNotificationNewUsers(msg, Admin, eventId, cancellationToken),
                        'd' => AdminDeleteEvent(msg, Admin, eventId, cancellationToken)
                    });
                }
                else
                {
                    UserProfile User = (UserProfile)person;
                    sentMessage = await (command switch
                    {
                        'r' => ChooseEvent(msg, User, eventId, cancellationToken),
                        'u' => UnregisterUser(msg, User, eventId, cancellationToken)
                    });
                }
            }
        }
        else
        {
            if (person.role == Roles.Admin)
            {
                AdminProfile Admin = (AdminProfile)person;
                sentMessage = await (callbackQuery.Data switch
                {
                    "/addAdmin" => HandleAdminCreateNewAdmin(msg, Admin, cancellationToken),
                    "/getMenu" => GetAdminPanel(msg, Admin, cancellationToken),
                    "/getUsers" => HandleAdminGetAllRegistratedUsers(msg, Admin, cancellationToken),
                    "/switchNotification" => HandleSwitchNotificationNewUsers(msg, Admin, cancellationToken),
                    "/deleteEvent" => HandleAdminDeleteEvent(msg, Admin, cancellationToken),
                    "/createEvent" => HandleCreateEvent(msg, Admin, cancellationToken),
                    _ => HandleAdminInput(msg, Admin, cancellationToken)
                });
            }
            else
            {
                UserProfile User = (UserProfile)person;
                sentMessage = await (callbackQuery.Data switch
                {
                    "//delete" => DeleteProfileDebug(msg, User, cancellationToken),
                    "/getMenu" => HandleGetUserMenu(msg, User, cancellationToken),
                    "/getEvent" => HandleGetEvent(msg, User, cancellationToken),
                    "/getRegisterEvents" => HandleGetUserRegisteredEvents(msg, User, cancellationToken),
                    "/unregister" => HandleUnregisterEvent(msg, User, cancellationToken),
                    "yes" => HandleChekIsEighteen(msg, User, cancellationToken, "yes"),
                    "no" => HandleChekIsEighteen(msg, User, cancellationToken, "no"),
                    "/start" => StartRegistration(msg, User, cancellationToken),
                    "/back" => ChangeThePreviousField(msg, User, cancellationToken),
                    _ => HandleUserInput(msg, User, cancellationToken)
                });
            }
        }
        // Отправляем ответ на нажатие кнопки, чтобы сразу отключить анимацию
        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        person.LastProfileMessageId = sentMessage.Id;

        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }

    #endregion


    #region работа с данными пользователя
    private async Task<Message> HandleGetUserMenu(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        if (userProfile.IsRegistered)
        {
            return await EditOrSendMessage(msg, userProfile, "Меню", GetUserMenuKeyboard(), cancellationToken);
        }
        return await EditOrSendMessage(msg, userProfile, Messages.RegistrationOnly18, null, cancellationToken);

    }

    private async Task<Message> ChangeThePreviousField(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        userProfile.ToPreviousState();
        await userProfileService.Update(userProfile, cancellationToken);

        string promptMessage = userProfile.UserState switch
        {
            UserStates.awaiting_registration => Messages.GoRegistration,
            UserStates.awaiting_Eighteen => Messages.AreYou18,
            UserStates.awaiting_name => Messages.PrintYouName,
            UserStates.awaiting_phone => Messages.PrintPhoneNumber,
            _ => Messages.SomethingWentWrong
        };

        if (userProfile.UserState is UserStates.awaiting_registration or UserStates.awaiting_name)
            return await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: promptMessage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken
            );
        return await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: promptMessage,
            replyMarkup: GetKeyBoardInRegistration(),
            cancellationToken: cancellationToken
        );
    }

    private async Task<Message> StartRegistration(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        if (userProfile.StartRegistration())
        {
            await userProfileService.Update(userProfile, cancellationToken);

            return await bot.SendMessage(msg.Chat.Id, Messages.AreYou18, parseMode: ParseMode.Html, replyMarkup: GetKeyBoardYesOrNo(), cancellationToken: cancellationToken);
        }
        return await bot.SendMessage(msg.Chat.Id, Messages.YouHaveAlreadyRegistered, ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }
    private async Task<Message> HandleChekIsEighteen(Message msg, UserProfile userProfile, CancellationToken cancellationToken, string messageText = null)
    {
        messageText ??= msg.Text;
        if (userProfile.ChekUserIsEighteen(messageText))
        {
            await userProfileService.Update(userProfile, cancellationToken);
            return await bot.SendMessage(msg.Chat.Id, Messages.PrintYouName, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        }
        await userProfileService.Delete(userProfile, cancellationToken);
        return await bot.SendMessage(msg.Chat.Id, Messages.RegistrationOnly18, ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> HandleAwaitingName(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var chatId = msg.Chat.Id;
        if (userProfile.SetName(msg.Text))
        {
            await userProfileService.Update(userProfile, cancellationToken);
            return await bot.SendMessage(chatId, Messages.PrintPhoneNumber, replyMarkup: GetKeyBoardInRegistration());
        }
        return await bot.SendMessage(chatId, Messages.SomethingWentWrong, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> HandleAwaitingPhone(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var chatId = msg.Chat.Id;
        if (userProfile.IsRegistered)
        {
            if (userProfile.SetPhoneNumber(msg.Text))
            {
                await userProfileService.Update(userProfile, cancellationToken);
                return await bot.SendMessage(chatId, Messages.ChangingPhoneNumber + userProfile.PhoneNumber, replyMarkup: new ReplyKeyboardRemove());
            }
            return await bot.SendMessage(chatId, Messages.WrongPhoneNumberFormat, replyMarkup: GetKeyBoardCancel());
        }
        else
        {
            if (userProfile.SetPhoneNumber(msg.Text))
            {
                await userProfileService.Update(userProfile, cancellationToken);
                return await HandleGetEvent(msg, userProfile, cancellationToken);
            }

        }
        return await bot.SendMessage(chatId, Messages.WrongPhoneNumberFormat, replyMarkup: GetKeyBoardInRegistration());

    }

    private async Task<Message> HandleGetEvent(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        if (userProfile.IsRegistered)
        {
            var events = await eventService.GetAll(cancellationToken, u => !u.UserProfiles.Contains(userProfile));
            return await EditOrSendMessage(msg, userProfile, GetEventsString(events, Messages.Event.AllowedToRegistr), GetEventKeyboard(events, 'r'), cancellationToken: cancellationToken);
        }
        return await bot.SendMessage(chatId, Messages.SomethingWentWrong, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> ChooseEvent(Message msg, UserProfile userProfile, Guid eventId, CancellationToken cancellationToken)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if (myEvent == null)
        {
            return await EditOrSendMessage(msg, userProfile, Messages.SomethingWentWrong, GetUserMenuKeyboard(), cancellationToken);
        }
        if (!await userProfileService.Register(userProfile, myEvent, cancellationToken))
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.ErrorInRegistrOnEvent, replyMarkup: GetUserMenuKeyboard());
        }
        await AdminGetUsersNotification(userProfile, myEvent, Messages.Admin.NewUserRegistered +" "+myEvent.ToString(), cancellationToken);
        return await EditOrSendMessage(msg, userProfile, Messages.YouHaveRegisteredForTheEvent + "\n" + myEvent.ToString(), GetUserMenuKeyboard(), cancellationToken);

    }

    private async Task<Message> HandleUnregisterEvent(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var events = await eventService.GetAll(cancellationToken, u => u.UserProfiles.Contains(userProfile));
        return await EditOrSendMessage(msg, userProfile, GetEventsString(events, Messages.Event.YourEvents), GetEventKeyboard(events, 'u'), cancellationToken: cancellationToken);
    }

    private async Task<Message> UnregisterUser(Message msg, UserProfile userProfile, Guid eventId, CancellationToken cancellationToken)
    {
        Event? unregisterEvent = await eventService.Get(eventId, cancellationToken);
        if (unregisterEvent == null)
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.Admin.EventNotFound);
        }
        await userProfileService.Unregister(userProfile, unregisterEvent, cancellationToken);
        await AdminGetUsersNotification(userProfile, unregisterEvent, Messages.Admin.UserUnregrisered + " " + unregisterEvent.ToString(), cancellationToken);
        return await EditOrSendMessage(msg, userProfile, Messages.YouHasUnregistered + " " + unregisterEvent.ToString(), GetUserMenuKeyboard(), cancellationToken);
    }

    private async Task<Message> HandleGetUserRegisteredEvents(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var events = await eventService.GetAll(cancellationToken, u => u.UserProfiles.Contains(userProfile));
        return await EditOrSendMessage(msg, userProfile, GetEventsString(events, Messages.Event.YourEvents), GetUserMenuInEventsKeyboard(), cancellationToken);
    }


    #endregion

    #region Get Info
    private string GetUserProfileInfo(UserProfile profile)
    {
        return $"Имя: {profile.Name}\n" +
               $"Телефон: {profile.PhoneNumber}\n";
    }
    private string GetEventsString(IEnumerable<Event> events, string message = "Мероприятия")
    {
        string eventString = message + ":\n";
        int i = 0;
        foreach (var x in events)
        {
            i++;
            eventString += $"№{i}. {x.Date.ToString()} {x.Name} \n";
        }
        if (i == 0)
        {
            return "Мероприятий не найдено";
        }
        return eventString;
    }
    private async Task<Message> EditOrSendMessage(Message msg, Person person, string _text, InlineKeyboardMarkup _replyMarkup, CancellationToken cancellationToken)
    {

        // Проверка, является ли сообщение с профилем последним в переписке
        if (person.LastProfileMessageId.HasValue && person.LastProfileMessageId==msg.Id)
        {
            try
            {
                // Пытаемся обновить ранее отправленное сообщение
                await bot.EditMessageText(
                    chatId: msg.Chat.Id,
                    messageId: person.LastProfileMessageId.Value,
                    text: _text,
                    replyMarkup: _replyMarkup,
                    cancellationToken: cancellationToken
                );

                return msg;
            }
            catch (ApiRequestException ex)
            {
                // Если сообщение не удалось обновить (например, оно было удалено), отправляем новое
                if (ex.ErrorCode == 400)
                {
                    person.LastProfileMessageId = null;
                }
                else
                {
                    throw;
                }
            }
        }

        // Отправляем новое сообщение и сохраняем его ID
        var sentMessage = await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: _text,
            replyMarkup: _replyMarkup,
            cancellationToken: cancellationToken
        );

        // Сохраняем ID последнего сообщения с профилем
        person.LastProfileMessageId = sentMessage.MessageId;
        await personService.Update(person, cancellationToken);

        return sentMessage;
    }
    #endregion

    #region Admin panel


    private async Task<Message> HandleSwitchNotificationNewUsers(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await EditOrSendMessage(msg, adminProfile, GetEventsString(events), GetEventKeyboard(events, 's'), cancellationToken);
    }
    private async Task<Message> SwitchNotificationNewUsers(Message msg, AdminProfile adminProfile, Guid eventId, CancellationToken cancellationToken)
    {
        Event myEvent = await eventService.Get(eventId, cancellationToken);
        adminProfile.ChangeNotification(myEvent);
        await adminProfileService.Update(adminProfile, cancellationToken);
        if (adminProfile.IsNotification(myEvent))
        {
            return await EditOrSendMessage(msg, adminProfile, Messages.Admin.YouWillReceiveNotifications, GetAdminKeyboard(), cancellationToken);
        }
        return await EditOrSendMessage(msg, adminProfile, Messages.Admin.YouWillNotReceiveNotifications, GetAdminKeyboard(), cancellationToken);
    }

    private async Task<Message> HandleAdminDeleteEvent(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await EditOrSendMessage(msg, adminProfile, GetEventsString(events), GetEventKeyboard(events, 'd'), cancellationToken);
    }
    private async Task<Message> AdminDeleteEvent(Message msg, AdminProfile admin, Guid eventId, CancellationToken cancellationToken)
    {
        Event deleteEvent = await eventService.Get(eventId, cancellationToken);
        logger.LogInformation(await eventService.Delete(deleteEvent, cancellationToken));
        return await EditOrSendMessage(msg, admin, Messages.Event.EventWasDeleted, GetAdminKeyboard(), cancellationToken);
    }


    private async Task<Message> HandleAdminGetAllRegistratedUsers(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await EditOrSendMessage(msg, adminProfile, GetEventsString(events), GetEventKeyboard(events, 'a'), cancellationToken);

    }
    private async Task<Message> AdminGetAllRegistratedUsers(Message msg, AdminProfile adminProfile, Guid eventId, CancellationToken cancellationToken)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if (myEvent == null)
        {
            return await EditOrSendMessage(msg, adminProfile, Messages.Event.EventNotFound, GetAdminKeyboard(), cancellationToken: cancellationToken);
        }
        var result = await userProfileService.GetAllByEvent(myEvent, cancellationToken);
        string? listAllUsers = null;
        IEnumerable<UserProfileResponse> users = result.Select(u => new UserProfileResponse { Id = u.Id, Name = u.Name, PhoneNumber = u.PhoneNumber });
        int i = 0;
        foreach (var user in users)
        {
            i++;
            listAllUsers += $" №{i} Имя: {user.Name} Номер телефона: {user.PhoneNumber} ";
        }
        if (i != 0)
        {
            //Message csvFileMessage = await CsvFileHelper<UserProfileResponse>.WriteFileToCsv(bot, msg.Chat.Id, users,myEvent.Name+DateTime.Now);
            Message excelFileMessage = await ExcelFileHelper<UserProfileResponse>.WriteFileToExcel(bot, msg.Chat.Id, users,myEvent.Name+" "+DateTime.Now);
            logger.LogInformation("The message with CSV file send with Id: {SentMessageId}", excelFileMessage?.Id);
        }
        return await EditOrSendMessage(msg, adminProfile, listAllUsers ??= "Пользователей не найдено", GetAdminKeyboard(), cancellationToken: cancellationToken);
    }

    private async Task<Message> HandleAdminCreateNewAdmin(Message msg,AdminProfile adminProfile,CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        adminProfile.SetAdminState(AdminStates.awaiting_newAdminId);
        logger.LogInformation(await adminProfileService.Update(adminProfile, cancellationToken));
        return await EditOrSendMessage(msg, adminProfile, Messages.Admin.PrintNewAdminId, null, cancellationToken);
    }

    private async Task<Message> AdminCreateNewAdmin(Message msg,AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        long newAdminId;
        if (long.TryParse(msg.Text.Split(' ')[0],out newAdminId))
        {
            UserProfile? user = await userProfileService.Get(newAdminId, cancellationToken);
            if (user != null) 
            {
                await userProfileService.Delete(user,cancellationToken);                
            }
            AdminProfile newAdmin = new(newAdminId); 
            adminProfile.SetAdminState(AdminStates.completed);
            await Task.WhenAll(
                adminProfileService.Create(newAdmin, cancellationToken),
                adminProfileService.Update(adminProfile,cancellationToken));
        }
        return await EditOrSendMessage(msg, adminProfile, Messages.Admin.NewAdminAdded, GetAdminKeyboard(), cancellationToken);
    }


    private async Task AdminGetUsersNotification(UserProfile userProfile, Event myEvent, string messageText, CancellationToken cancellationToken)
    {
        var admins = await adminProfileService.GetAll(cancellationToken,u=>u.NotificationList.Contains(myEvent));
        int usersCount = (await userProfileService.GetAllByEvent(myEvent, cancellationToken)).Count();
        Message message = new Message();
        foreach (var admin in admins)
        {
            message = await bot.SendMessage(admin.Id, messageText +" "+ GetUserProfileInfo(userProfile) + "\n" + Messages.Admin.CountOfRegisteredUsersOnEvent + usersCount, replyMarkup: new ReplyKeyboardRemove());
            logger.LogInformation("The Admin message was sent with id: {SentMessageId}", message?.MessageId);
        }
        return;
    }
    private async Task<Message> SetAdminRole(ChatId chatId, UserProfile userProfile, CancellationToken cancellationToken)
    {
        Person person = Person.Create(userProfile.Id);
        if (person.role == Roles.Admin)
        {
            logger.LogInformation(await userProfileService.Delete(userProfile, cancellationToken));
            AdminProfile Admin = (AdminProfile)person;
            logger.LogInformation(await adminProfileService.Create(Admin, cancellationToken));
            return await bot.SendMessage(chatId, "Вам выдана роль администратора");
        }
        return await bot.SendMessage(chatId, "У вас нет прав на использование этой команды ");
    }

    private async Task<Message> HandleAdminInput(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {

        return await (adminProfile.AdminState switch
        {
            AdminStates.create_event => HandleCreateEvent(msg, adminProfile, cancellationToken),
            AdminStates.awaiting_eventName => HandleEventName(msg, adminProfile, cancellationToken),
            AdminStates.awaiting_eventDateTime => HandleEventDate(msg, adminProfile, cancellationToken),
            AdminStates.awaiting_eventDescription => HandleEventDescription(msg, adminProfile, cancellationToken),
            AdminStates.awaiting_newAdminId => AdminCreateNewAdmin(msg, adminProfile, cancellationToken),
            _ => GetAdminPanel(msg, adminProfile, cancellationToken),
        });
    }

    private async Task<Message> GetAdminPanel(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        return await EditOrSendMessage(msg, adminProfile, Messages.Admin.Menu, GetAdminKeyboard(), cancellationToken: cancellationToken);
    }



    #endregion

    #region Процесс создания мероприятия

    private async Task<Message> HandleCreateEvent(Message msg, AdminProfile admin, CancellationToken cancellationToken)
    {
        if (admin.CurrentEvent != null)
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.Admin.YouAlreadyOperatingWithEvent, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }
        Event newEvent = new();
        await eventService.Create(newEvent, cancellationToken);
        admin.SetCurrentEvent(newEvent.Id);
        await adminProfileService.Update(admin, cancellationToken);

        return await EditOrSendMessage(msg, admin, Messages.Admin.PrintEventName, null, cancellationToken);
    }


    private async Task<Message> HandleEventName(Message msg, AdminProfile admin, CancellationToken cancellationToken)
    {
        if (admin.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.SomethingWentWrong, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }

        Event newEvent = await eventService.Get(admin.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            admin.ResetCurrentEvent();
            await adminProfileService.Update(admin, cancellationToken);
            return await bot.SendMessage(msg.Chat.Id, Messages.Admin.EventNotFound, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }
        newEvent.Name = msg.Text;
        admin.SetAdminState(AdminStates.awaiting_eventDateTime);
        await Task.WhenAll(
            adminProfileService.Update(admin, cancellationToken),
            eventService.Update(newEvent, cancellationToken)
            );
        return await bot.SendMessage(msg.Chat.Id, Messages.Admin.PrintEventDateTime, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }


    private async Task<Message> HandleEventDate(Message msg, AdminProfile admin, CancellationToken cancellationToken)
    {
        if (admin.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.SomethingWentWrong, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        Event newEvent = await eventService.Get(admin.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            admin.ResetCurrentEvent();
            await adminProfileService.Update(admin, cancellationToken);
            return await bot.SendMessage(msg.Chat.Id, Messages.Admin.EventNotFound, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }

        if (!newEvent.SetDate(msg.Text))
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.Admin.WrongDateTimeFormat, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        admin.SetAdminState(AdminStates.awaiting_eventDescription);
        await Task.WhenAll(
            adminProfileService.Update(admin, cancellationToken),
            eventService.Update(newEvent, cancellationToken)
            );
        return await bot.SendMessage(msg.Chat.Id, Messages.Admin.PrintEventDescription, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }
    private async Task<Message> HandleEventDescription(Message msg, AdminProfile admin, CancellationToken cancellationToken)
    {
        if (admin.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.SomethingWentWrong, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }

        Event newEvent = await eventService.Get(admin.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            admin.ResetCurrentEvent();
            await adminProfileService.Update(admin, cancellationToken);
            return await bot.SendMessage(msg.Chat.Id, Messages.Admin.EventNotFound, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }
        newEvent.Description = msg.Text;
        admin.ResetCurrentEvent();
        admin.SetAdminState(AdminStates.completed);
        await Task.WhenAll(
            adminProfileService.Update(admin, cancellationToken),
            eventService.Update(newEvent, cancellationToken)
            );
        return await EditOrSendMessage(msg, admin, Messages.Admin.EventsuccessfullyCreated, GetAdminKeyboard(), cancellationToken);
    }


    private async Task<Message> HandleGetEvent(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await bot.SendMessage(chatId, GetEventsString(events), replyMarkup: new ReplyKeyboardRemove());
    }
    #endregion

    #region Неиспользуемые методы

    private async Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {Update}", update.Type);

    }
    private async Task OnPoll(Poll poll)
    {
        logger.LogInformation("Poll received");
    }
    private async Task OnPollAnswer(PollAnswer pollAnswer)
    {
        logger.LogInformation("Poll answer received");
    }

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        logger.LogInformation("Inline query received");
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        logger.LogInformation("Chosen inline result received");
    }
    #endregion

    #region Debug Operations
    private async Task<Message> DeleteProfileDebug(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        logger.LogInformation(await userProfileService.Delete(userProfile, cancellationToken));
        return await bot.SendMessage(msg.Chat.Id, Messages.ProfileWasDeleted);
    }
    private async Task<Message> DeleteProfileDebug(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        logger.LogInformation(await adminProfileService.Delete(adminProfile, cancellationToken));
        return await bot.SendMessage(msg.Chat.Id, Messages.ProfileWasDeleted);
    }



    #endregion

}