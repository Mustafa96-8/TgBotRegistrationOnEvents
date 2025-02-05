using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Enums;
using static TelegramBot.Domain.Collections.Keyboards;
using static System.Runtime.InteropServices.JavaScript.JSType;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Repositories;
using System.Diagnostics;
using TelegramBot.Contracts;


namespace TelegramBot.Services;


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
                "/start" => GetAdminPanel(msg, Admin, cancellationToken),
                "/admin" => GetAdminPanel(msg, Admin, cancellationToken),
                "/getall" => AdminGetAllRegistratedUsers(msg, Admin, cancellationToken),
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
                    "/getadmin" => GetAdminRole(msg, User, cancellationToken),
                    "/deleteDebug" => DeleteProfileDebug(msg, User, cancellationToken),
                    "/unregister" => UnregisterUser(msg, User, cancellationToken),
                    _ => HandleUserInput(msg, User, cancellationToken)
                });
                logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
            }
            else
            {
                Message sentMessage = await (messageText.Split(' ')[0] switch
                {
                    "/deleteDebug" => DeleteProfileDebug(msg, User, cancellationToken),
                    "/unregister" => UnregisterUser(msg, User, cancellationToken),
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
            _ => GetRegistrationInfo(msg, userProfile, cancellationToken)
        });
    }

    #endregion

    #region Filling in user data

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
        if (userProfile.IsRegistered)
        {
            if (userProfile.SetName(msg.Text, UserStates.completed))
            {
                await userProfileService.Update(userProfile, cancellationToken);
                return await bot.SendMessage(chatId, $"Имя успешно изменёно на {msg.Text}.\n" + GetUserProfileInfo(userProfile), replyMarkup: new ReplyKeyboardRemove());
            }
            return await bot.SendMessage(chatId, Messages.SomethingWentWrong, replyMarkup: GetKeyBoardCancel());
        }
        else 
        {
            if (userProfile.SetName(msg.Text))
            {
                await userProfileService.Update(userProfile, cancellationToken);
                return await bot.SendMessage(chatId, Messages.PrintPhoneNumber, replyMarkup: GetKeyBoardInRegistration());
            }
        }
        return await bot.SendMessage(chatId,Messages.SomethingWentWrong, replyMarkup: new ReplyKeyboardRemove());
    }

    /// <summary>
    /// Обработка ввода номера телефона в систему
    /// </summary>
    /// <param name="msg">Сообщение с номером телефона</param>
    /// <param name="userProfile">Профиль для изменения номера телефона</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Возвращает сообщение для вывода пользователю</returns>
    private async Task<Message> HandleAwaitingPhone(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var chatId = msg.Chat.Id;
        if (userProfile.IsRegistered)
        {
            if (userProfile.SetPhoneNumber(msg.Text)) 
            {
                await userProfileService.Update(userProfile,cancellationToken);
                return await bot.SendMessage(chatId, Messages.ChangingPhoneNumber+userProfile.PhoneNumber, replyMarkup: new ReplyKeyboardRemove());
            } 
            return await bot.SendMessage(chatId, Messages.WrongPhoneNumberFormat, replyMarkup: GetKeyBoardCancel());
        }
        else
        {
            if (userProfile.SetPhoneNumber(msg.Text))
            {
                await userProfileService.Update(userProfile, cancellationToken);
                await AdminGetUsersNotification(userProfile,Messages.NewUserRegistered, cancellationToken);
                return await bot.SendMessage(chatId, Messages.YouHaveRegisteredForTheEvent, replyMarkup: new ReplyKeyboardRemove());
            }
            
        }
        return await bot.SendMessage(chatId, Messages.WrongPhoneNumberFormat, replyMarkup: GetKeyBoardInRegistration());

    }

    private async Task<Message> GetRegistrationInfo(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        var chatId = msg.Chat.Id;
        if (userProfile.IsRegistered)
        {
            return await bot.SendMessage(chatId, Messages.YouHaveRegisteredForTheEvent, replyMarkup: new ReplyKeyboardRemove());
        }
        return await bot.SendMessage(chatId, Messages.RegistrationOnly18, replyMarkup: new ReplyKeyboardRemove());
        
    }

    #endregion


    #region Get Profile Info
    private string GetUserProfileInfo(UserProfile profile)
    {
        return $"Профиль: Имя: {profile.Name}\n" +
               $"Телефон: {profile.PhoneNumber}\n";
    }

    private async Task<Message> EditOrSendMessage(Message msg, Person person,string _text, InlineKeyboardMarkup _replyMarkup, CancellationToken cancellationToken)
    {

        // Проверка, является ли сообщение с профилем последним в переписке
        if (person.LastProfileMessageId.HasValue)
        {
            try
            {
                // Пытаемся обновить ранее отправленное сообщение
                await bot.EditMessageText(
                    chatId: msg.Chat.Id,
                    messageId: person.LastProfileMessageId.Value,
                    text: _text,
                    replyMarkup: _replyMarkup  ,
                    cancellationToken: cancellationToken
                );

                return msg;
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
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


    /// <summary>
    /// Вывод основных команд для работы с телеграм ботом.
    /// </summary>
    /// <param name="msg">Принятое сообщение от пользователя.</param>
    /// <returns>Задачу вывода сообщения пользователю</returns>
    private async Task<Message> Usage(Message msg)
    {
        const string usage = """
            <b><u>Bot menu</u></b>:
            /start        - начать регистрацию
            /me           - показать профиль
            /debug        - показать отладочную информацию о пользователе
        """;
        return await bot.SendMessage(msg.Chat.Id, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    }


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
        if (person.role == Roles.Admin)
        {
            AdminProfile Admin = (AdminProfile)person;
            sentMessage = await (callbackQuery.Data switch
            {
                "/getall" => AdminGetAllRegistratedUsers(msg, Admin, cancellationToken),
                ///"/switchNotify" => SwitchNotificationNewUsers(msg, adminProfile, cancellationToken),
                _ => HandleAdminInput(msg, Admin, cancellationToken)
            });
        }
        else
        {
            UserProfile User = (UserProfile)person;
            sentMessage = await (callbackQuery.Data switch
            {
                "yes" => HandleChekIsEighteen(msg, User, cancellationToken, "yes"),
                "no" => HandleChekIsEighteen(msg, User, cancellationToken, "no"),
                "/start" => StartRegistration(msg, User, cancellationToken),
                "/back" => ChangeThePreviousField(msg, User, cancellationToken),
                "/unregister" => UnregisterUser(msg, User, cancellationToken),
                _ => HandleUserInput(msg, User, cancellationToken)
            });
        }

        // Отправляем ответ на нажатие кнопки, чтобы сразу отключить анимацию
        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        person.LastProfileMessageId = sentMessage.Id;

        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }


    private async Task<Message> ChangeThePreviousField(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        userProfile.ToPreviousState();
        await userProfileService.Update(userProfile, cancellationToken);

        string promptMessage = userProfile.UserState switch
        {
            UserStates.awaiting_registration=>Messages.GoRegistration,
            UserStates.awaiting_Eighteen=>Messages.AreYou18,
            UserStates.awaiting_name => Messages.PrintYouName,
            UserStates.awaiting_phone => Messages.PrintPhoneNumber,
            _ => Messages.SomethingWentWrong
        };

        if(userProfile.UserState is UserStates.awaiting_registration or UserStates.awaiting_name)
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

    private async Task<Message> StartRegistration(Message msg,UserProfile userProfile,CancellationToken cancellationToken)
    {
        if (userProfile.StartRegistration())
        {
            await userProfileService.Update(userProfile, cancellationToken);
            
            return await bot.SendMessage(msg.Chat.Id, Messages.AreYou18, parseMode: ParseMode.Html, replyMarkup: GetKeyBoardYesOrNo(),cancellationToken:cancellationToken);
        }
        return await bot.SendMessage(msg.Chat.Id, "Вы уже зарегестрированы", ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken:cancellationToken);
    }
    private async Task<Message> UnregisterUser(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        logger.LogInformation(await userProfileService.Delete(userProfile, cancellationToken));
        await AdminGetUsersNotification(userProfile,Messages.UserUnregrisered, cancellationToken);
        return await bot.SendMessage(msg.Chat.Id, "Вы отменили регистрацию");
    }

    #region Admin panel

    private async Task<Message> GetAdminRole(Message msg,UserProfile userProfile,CancellationToken cancellationToken)
    {
        if (userProfile.SetAdmin()) 
        {
            await userProfileService.Update(userProfile, cancellationToken);
            return await bot.SendMessage(msg.Chat.Id, "Вам выдана роль администратора", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        }
        return await bot.SendMessage(msg.Chat.Id, "Вы не внесены в список администраторов, обратитесь к разработчику", parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    }
           //TO DO Реализовать получение списка евентов и выбор их
    /*private async Task<Message> SwitchNotificationNewUsers(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        Event myEvent = eventService.Get(msg.Text)
        adminProfile.ChangeNotification();
        await adminProfileService.Update(adminProfile, cancellationToken);
        if(adminProfile.)
        {
            return await EditOrSendMessage(msg, adminProfile, "Вы будете получать оповещения о новых пользователях", GetAdminKeyboard(), cancellationToken);
        }
        return await EditOrSendMessage(msg, adminProfile, "Вы больше не будете получать оповещения о новых пользователях", GetAdminKeyboard(), cancellationToken);

    }*/

    private async Task<Message> GetAdminPanel(Message msg,AdminProfile adminProfile,CancellationToken cancellationToken)
    {
        return await bot.SendMessage(msg.Chat.Id, "Menu", replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
    }
    private async Task<Message> AdminGetAllRegistratedUsers(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        var result = await userProfileService.GetAll(cancellationToken);
        string? listAllUsers = null;
        IEnumerable <UserProfileResponse> users = result.Select(u => new UserProfileResponse {Id=u.Id, Name = u.Name, PhoneNumber = u.PhoneNumber });
        int i = 0;
        foreach (var user in users)
        {
            i++;
            listAllUsers += $" №{i} Имя: {user.Name} Номер телефона: {user.PhoneNumber} ";
        }
        Message csvFileMessage = await CsvFileHelper<UserProfileResponse>.WriteFileToCsv(bot, msg.Chat.Id, users);
        logger.LogInformation("The message with CSV file send with Id: {SentMessageId}", csvFileMessage?.Id);

        return await EditOrSendMessage(msg,adminProfile, listAllUsers??="Пользователей не найдено", GetAdminKeyboard(), cancellationToken: cancellationToken);
    }               


    private async Task AdminGetUsersNotification(UserProfile userProfile,string messageText,CancellationToken cancellationToken)
    {
        var admins = await adminProfileService.GetAll(cancellationToken);
        Message message = new Message();
        foreach (var admin in admins)
        {
            message = await bot.SendMessage(admin.Id, messageText + GetUserProfileInfo(userProfile), replyMarkup: new ReplyKeyboardRemove());
            logger.LogInformation("The Admin message was sent with id: {SentMessageId}", message?.MessageId);
        }
        return;
    }

    private async Task<Message> HandleAdminInput(Message msg,AdminProfile adminProfile,CancellationToken cancellationToken)
    {
        return await (adminProfile.AdminState switch
        {
            _ => GetAdminPanel(msg, adminProfile, cancellationToken),
        });
    }

    #endregion

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

    #region Debug Operations
    private async Task<Message> DeleteProfileDebug(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        logger.LogInformation(await userProfileService.Delete(userProfile, cancellationToken));
        return await bot.SendMessage(msg.Chat.Id, "Профиль удалён");
    }
    private async Task<Message> DeleteProfileDebug(Message msg, AdminProfile adminProfile, CancellationToken cancellationToken)
    {
        logger.LogInformation(await adminProfileService.Delete(adminProfile, cancellationToken));
        return await bot.SendMessage(msg.Chat.Id, "Профиль удалён");
    }

    #endregion

}