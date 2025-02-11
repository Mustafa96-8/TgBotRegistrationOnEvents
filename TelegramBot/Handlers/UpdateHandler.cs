using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Enums;
using TelegramBot.Domain.Collections;
using TelegramBot.Contracts;
using TelegramBot.Helpers;
using TelegramBot.Services;
using static TelegramBot.Domain.Collections.Keyboards;
using static TelegramBot.Helpers.GetInfoHelper;

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
    private readonly SendingService sendInfoService;

    public UpdateHandler(
        ITelegramBotClient bot,
        ILogger<UpdateHandler> logger,
        HttpClient httpClient,
        UserProfileService userProfileService,
        AdminProfileService adminProfileService,
        PersonService personService,
        EventService eventService,
        SendingService sendInfoService
        )
    {
        this.bot = bot;
        this.logger = logger;
        this.httpClient = httpClient;
        this.userProfileService = userProfileService;
        this.adminProfileService = adminProfileService;
        this.personService = personService;
        this.eventService = eventService;
        this.sendInfoService = sendInfoService;
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
            AdminUpdateHandler adminUpdateHandler = new AdminUpdateHandler(adminProfileService,sendInfoService,userProfileService,eventService,logger);
            await adminUpdateHandler.OnMessage(msg,Admin,cancellationToken);
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
                    AdminUpdateHandler adminUpdateHandler = new AdminUpdateHandler(adminProfileService, sendInfoService, userProfileService,eventService, logger);
                    await adminUpdateHandler.OnCallbackQuery(msg,Admin,command,eventId,cancellationToken);
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
                AdminUpdateHandler adminUpdateHandler = new AdminUpdateHandler(adminProfileService, sendInfoService, userProfileService,eventService,logger);
                await adminUpdateHandler.OnCallbackQuery(msg,Admin,callbackQuery,cancellationToken);
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
            return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.Menu, GetUserMenuKeyboard(), cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.RegistrationOnly18, null, cancellationToken);

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
            return await sendInfoService.EditOrSendMessage(msg, userProfile, GetEventsString(events, Messages.Event.AllowedToRegistr), GetEventKeyboard(events, 'r'), cancellationToken: cancellationToken);
        }
        return await bot.SendMessage(chatId, Messages.SomethingWentWrong, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> ChooseEvent(Message msg, UserProfile userProfile, Guid eventId, CancellationToken cancellationToken)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if (myEvent == null)
        {
            return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.SomethingWentWrong, GetUserMenuKeyboard(), cancellationToken);
        }
        if (!await userProfileService.Register(userProfile, myEvent, cancellationToken))
        {
            return await bot.SendMessage(msg.Chat.Id, Messages.ErrorInRegistrOnEvent, replyMarkup: GetUserMenuKeyboard());
        }
        await AdminGetUsersNotification(userProfile, myEvent, Messages.Admin.NewUserRegistered +" "+myEvent.ToString(), cancellationToken);
        return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.YouHaveRegisteredForTheEvent + "\n" + myEvent.ToString(), GetUserMenuKeyboard(), cancellationToken);

    }

    private async Task<Message> HandleUnregisterEvent(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var events = await eventService.GetAll(cancellationToken, u => u.UserProfiles.Contains(userProfile));
        return await sendInfoService.EditOrSendMessage(msg, userProfile, GetEventsString(events, Messages.Event.YourEvents), GetEventKeyboard(events, 'u'), cancellationToken: cancellationToken);
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
        return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.YouHasUnregistered + " " + unregisterEvent.ToString(), GetUserMenuKeyboard(), cancellationToken);
    }

    private async Task<Message> HandleGetUserRegisteredEvents(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var events = await eventService.GetAll(cancellationToken, u => u.UserProfiles.Contains(userProfile));
        return await sendInfoService.EditOrSendMessage(msg, userProfile, GetEventsString(events, Messages.Event.YourEvents), GetUserMenuInEventsKeyboard(), cancellationToken);
    }
    #endregion

    

    #region Admin panel

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
            return await bot.SendMessage(chatId, Messages.Admin.YouHaveBeenAssignedTheAdminRole);
        }
        return await bot.SendMessage(chatId, Messages.YouHaveNoPermissionsToUseThisCommand);
    }

    #endregion

  
    #region Debug Operations
    private async Task<Message> DeleteProfileDebug(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        logger.LogInformation(await userProfileService.Delete(userProfile, cancellationToken));
        return await bot.SendMessage(msg.Chat.Id, Messages.ProfileWasDeleted);
    }




    #endregion

    #region Неиспользуемые методы

    private async Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {Update}", update.Type);
        return;
    }

    #endregion

}