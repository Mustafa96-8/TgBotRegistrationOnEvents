using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Enums;
using TelegramBot.Domain.Collections;
using TelegramBot.Services;

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
        if(msg.Text == "/getcontact")
        {
            sendInfoService.SendMessage(msg, person, Messages.Contacts.GetSommelierContact, null, cancellationToken);
        }
        if (msg.Text == "/getdevelopercontact")
        {
            sendInfoService.SendMessage(msg, person, Messages.Contacts.GetDeveloperContact, null, cancellationToken);
        }

        if (person.role == Roles.Admin)
        {
            AdminProfile Admin = (AdminProfile)person;
            AdminUpdateHandler adminUpdateHandler = new AdminUpdateHandler(adminProfileService,sendInfoService,userProfileService,eventService,logger,cancellationToken);
            await adminUpdateHandler.OnMessage(msg,Admin);
        }
        else
        {
            UserProfile User = (UserProfile)person;
            UserUpdateHandler userUpdateHandler = new UserUpdateHandler(userProfileService, sendInfoService, eventService,adminProfileService, logger,cancellationToken);
            await userUpdateHandler.OnMessage(msg,User);
            
        }
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
                    AdminUpdateHandler adminUpdateHandler = new AdminUpdateHandler(adminProfileService, sendInfoService, userProfileService, eventService, logger, cancellationToken);
                    await adminUpdateHandler.OnCallbackQuery(msg, Admin, command, eventId);
                }
                else
                {
                    UserProfile User = (UserProfile)person;
                    UserUpdateHandler userUpdateHandler = new UserUpdateHandler(userProfileService, sendInfoService, eventService,adminProfileService, logger,cancellationToken);
                    await userUpdateHandler.OnCallbackQuery(msg, User, command, eventId);
                }
            }
        }
        else
        {
            if (person.role == Roles.Admin)
            {
                AdminProfile Admin = (AdminProfile)person;
                AdminUpdateHandler adminUpdateHandler = new AdminUpdateHandler(adminProfileService, sendInfoService, userProfileService, eventService, logger, cancellationToken);
                await adminUpdateHandler.OnCallbackQuery(msg, Admin, callbackQuery);
            }
            else
            {
                UserProfile User = (UserProfile)person;
                UserUpdateHandler userUpdateHandler = new UserUpdateHandler(userProfileService, sendInfoService, eventService, adminProfileService, logger, cancellationToken);
                await userUpdateHandler.OnCallbackQuery(msg, User, callbackQuery);
            }
        }
        // Отправляем ответ на нажатие кнопки, чтобы сразу отключить анимацию
        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
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