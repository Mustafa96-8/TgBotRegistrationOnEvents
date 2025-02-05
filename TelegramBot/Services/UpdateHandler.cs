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

    


    public UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger, HttpClient httpClient, UserProfileService userProfileService)
    {
        this.bot = bot;
        this.logger = logger;
        this.httpClient = httpClient;
        this.userProfileService = userProfileService;
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
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

        UserProfile? userProfile = await userProfileService.Get(msg.Chat.Id, cancellationToken);

        // Инициализация профиля пользователя при первом обращении
        if (userProfile == null)
        {
            userProfile = new UserProfile { Id = msg.Chat.Id };
        }

        

        if (userProfile.IsRegistered)
        {
            if (userProfile.role == Roles.Admin)
            {
                Message sentMessage = await (messageText.Split(' ')[0] switch
                {
                    "/start" => GetAdminPanel(msg, userProfile, cancellationToken),
                    "/admin" => GetAdminPanel(msg, userProfile, cancellationToken),
                    "/getall" => AdminGetAllRegistratedUsers(msg, userProfile, cancellationToken),
                    "/debug" => GetDebugInfo(msg, userProfile, cancellationToken),                                                                     
                    "/deleteDebug" => DeleteProfileDebug(msg, userProfile, cancellationToken),
                    _ => HandleAdminInput(msg, userProfile, cancellationToken)
                });
                logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
            }
            else
            {
                Message sentMessage = await (messageText.Split(' ')[0] switch
                {
                    "/getadmin" => GetAdminRole(msg, userProfile, cancellationToken),
                    "/debug" => GetDebugInfo(msg, userProfile, cancellationToken),
                    "/deleteDebug" => DeleteProfileDebug(msg, userProfile, cancellationToken),
                    "/unregister" => UnregisterUser(msg, userProfile, cancellationToken),
                    _ => HandleUserInput(msg, userProfile, cancellationToken)
                });
                logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
            }
        }
        else
        {
            Message sentMessage = await (messageText.Split(' ')[0] switch
            {
                "/debug" => GetDebugInfo(msg, userProfile, cancellationToken),
                "/deleteDebug" => DeleteProfileDebug(msg, userProfile, cancellationToken),
                "/unregister" => UnregisterUser(msg, userProfile, cancellationToken),
                _ => HandleUserInput(msg, userProfile, cancellationToken)
            });

            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
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

    private async Task<Message> EditOrSendMessage(Message msg, UserProfile userProfile,string _text, InlineKeyboardMarkup _replyMarkup, CancellationToken cancellationToken)
    {

        // Проверка, является ли сообщение с профилем последним в переписке
        if (userProfile.LastProfileMessageId.HasValue)
        {
            try
            {
                // Пытаемся обновить ранее отправленное сообщение
                await bot.EditMessageText(
                    chatId: msg.Chat.Id,
                    messageId: userProfile.LastProfileMessageId.Value,
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
                    userProfile.LastProfileMessageId = null;
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
        userProfile.LastProfileMessageId = sentMessage.MessageId;
        await userProfileService.Update(userProfile, cancellationToken);

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


        UserProfile? userProfile = await userProfileService.Get(msg.Chat.Id, cancellationToken);

        // Инициализация профиля пользователя при первом обращении
        if (userProfile == null)
        {
            userProfile = new UserProfile { Id = msg.Chat.Id};
        }
        Message sentMessage = null;

        if (userProfile.role == Roles.Admin) {
            sentMessage = await (callbackQuery.Data switch
            {
                "/getall" => AdminGetAllRegistratedUsers(msg, userProfile, cancellationToken),
                "/switchNotify" => SwitchNotificationNewUsers(msg,userProfile,cancellationToken),
                _ => HandleAdminInput(msg, userProfile, cancellationToken)
            });
        }
        else
        {
            sentMessage = await (callbackQuery.Data switch
            {
                "yes" => HandleChekIsEighteen(msg, userProfile, cancellationToken, "yes"),
                "no" => HandleChekIsEighteen(msg, userProfile, cancellationToken, "no"),
                "/start" => StartRegistration(msg, userProfile, cancellationToken),
                "/debug" => GetDebugInfo(msg, userProfile, cancellationToken),
                "/back" => ChangeThePreviousField(msg, userProfile, cancellationToken),
                "/unregister" => UnregisterUser(msg, userProfile, cancellationToken),
                _ => HandleUserInput(msg, userProfile, cancellationToken)
            });
        }

        // Отправляем ответ на нажатие кнопки, чтобы сразу отключить анимацию
        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        userProfile.LastProfileMessageId = sentMessage.Id;

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
            return await bot.SendMessage(msg.Chat.Id, Messages.AreYou18, parseMode: ParseMode.Html, replyMarkup: GetKeyBoardYesOrNo());
        }
        return await bot.SendMessage(msg.Chat.Id,"Вы уже зарегестрированы", ParseMode.Html, replyMarkup:new ReplyKeyboardRemove());
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

    private async Task<Message> SwitchNotificationNewUsers(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        userProfile.SetNotification(!userProfile.IsNotificationNewUser);
        await userProfileService.Update(userProfile, cancellationToken);
        if(userProfile.IsNotificationNewUser)
        {
            return await EditOrSendMessage(msg, userProfile, "Вы будете получать оповещения о новых пользователях", GetAdminKeyboard(), cancellationToken);
        }
        return await EditOrSendMessage(msg, userProfile, "Вы больше не будете получать оповещения о новых пользователях", GetAdminKeyboard(), cancellationToken);

    }

    private async Task<Message> GetAdminPanel(Message msg,UserProfile userProfile,CancellationToken cancellationToken)
    {
        return await bot.SendMessage(msg.Chat.Id, "Menu", replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
    }
    private async Task<Message> AdminGetAllRegistratedUsers(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
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

        return await EditOrSendMessage(msg,userProfile, listAllUsers??="Пользователей не найдено", GetAdminKeyboard(), cancellationToken: cancellationToken);
    }               


    private async Task AdminGetUsersNotification(UserProfile userProfile,string messageText,CancellationToken cancellationToken)
    {
        var admins = await userProfileService.GetAdminList(cancellationToken);
        Message message = new Message();
        foreach (var admin in admins)
        {
            message = await bot.SendMessage(admin.Id, messageText + GetUserProfileInfo(userProfile), replyMarkup: new ReplyKeyboardRemove());
            logger.LogInformation("The Admin message was sent with id: {SentMessageId}", message?.MessageId);
        }
        return;
    }

    private async Task<Message> HandleAdminInput(Message msg,UserProfile userProfile,CancellationToken cancellationToken)
    {
        return await (userProfile.UserState switch
        {
            _ => GetAdminPanel(msg, userProfile, cancellationToken),
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


    private async Task<Message> GetDebugInfo(Message msg, UserProfile userProfile, CancellationToken cancellationToken)
    {
        if (userProfile.UserState == UserStates.awaiting_registration)
        {
            return await Usage(msg);
        }

        return await bot.SendMessage(msg.Chat.Id,
                $"Ваш профиль:\n" +
                $"Имя: {userProfile.Name}\n" +
                $"Телефон: {userProfile.PhoneNumber}\n" +
                $"Id: {userProfile.Id}\n" +
                $"UserState: {userProfile.UserState}\n" +
                $"IsRegisted: {userProfile.IsRegistered}\n"
                );
    }
    #endregion 

}