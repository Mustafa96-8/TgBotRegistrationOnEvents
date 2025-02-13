using Telegram.Bot.Types;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Enums;
using TelegramBot.Domain.Collections;
using TelegramBot.Services;
using static TelegramBot.Domain.Collections.Keyboards;
using static TelegramBot.Helpers.GetInfoHelper;
using TelegramBot.Helpers;

namespace TelegramBot.Handlers;
public class UserUpdateHandler
{
    private Message? msg;
    private string? messageText;
    private readonly CancellationToken cancellationToken;
    private UserProfile? userProfile;
    private readonly ILogger<UpdateHandler> logger;
    private readonly UserProfileService userProfileService;
    private readonly EventService eventService;
    private readonly AdminProfileService adminProfileService;

    private readonly SendingService sendInfoService;


    public UserUpdateHandler(
        UserProfileService userProfileService,
        SendingService sendInfoService,
        EventService eventService,
        AdminProfileService adminProfileService,
        ILogger<UpdateHandler> logger,
        CancellationToken cancellationToken)
    {
        this.sendInfoService = sendInfoService;
        this.userProfileService = userProfileService;
        this.eventService = eventService;
        this.adminProfileService = adminProfileService;
        this.logger = logger;
        this.cancellationToken = cancellationToken;
    }

    public async Task OnMessage(Message _msg, UserProfile _userProfile)
    {
        msg = _msg;
        userProfile = _userProfile;
        messageText = msg.Text ?? "";
        if (userProfile.IsRegistered)
        {
            Message sentMessage = await (messageText.Split(' ')[0] switch
            {
                "/getevent" => PrintEventsOnPage("r"),
                "/deleteprofile" => DeleteProfile(),
                //"/getAdminRole" => SetAdminRole(),
                _ => HandleUserInput()
            });
            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
        }
        else
        {
            Message sentMessage = await (messageText.Split(' ')[0] switch
            {
                "/start" => StartRegistration(),
                //"/getAdminRole" => SetAdminRole(),
                _ => HandleUserInput()
            });

            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
        }
    }

    public async Task OnCallbackQuery(Message _msg, UserProfile _userProfile, char command, Guid eventId)
    {
        this.msg = _msg;
        this.userProfile = _userProfile;
        messageText = msg.Text ?? "";
        Message sentMessage = await (command switch
        {
            'r' => ChooseEvent(eventId),
            'u' => UnregisterUser(eventId),
            'g' => GetEventInfo(eventId)
        });
        userProfile.LastProfileMessageId = sentMessage.Id;

        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }
    public async Task OnCallbackQuery(Message _msg, UserProfile _userProfile, CallbackQuery callbackQuery)
    {
        this.msg = _msg;
        this.userProfile = _userProfile;
        messageText = msg.Text;
        var callbackQueryDataArgs = (callbackQuery.Data ?? " ").Split('|');
        Message sentMessage = await (callbackQueryDataArgs[0] switch
        {
            "/getMenu" => HandleGetUserMenu(),
            "/getEvent" => PrintEventsOnPage("r"),
            "/getRegisterEvents" => PrintEventsOnPage("g"),
            "/unregister" => PrintEventsOnPage("u"),
            "yes" => HandleChekIsEighteen("yes"),
            "no" => HandleChekIsEighteen("no"),
            "/start" => StartRegistration(),
            "/back" => ChangeThePreviousField(),
            "->" => PrintEventsOnPage(callbackQueryDataArgs[1], int.Parse(callbackQueryDataArgs[2]) + 1),
            "<-" => PrintEventsOnPage(callbackQueryDataArgs[1], int.Parse(callbackQueryDataArgs[2]) - 1),
            _ => HandleUserInput()
        });
        userProfile.LastProfileMessageId = sentMessage.Id;
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }
    private async Task<Message> HandleUserInput()
    {
        return await (userProfile.UserState switch
        {
            UserStates.awaiting_registration => StartRegistration(),
            UserStates.awaiting_Eighteen => HandleChekIsEighteen(),
            UserStates.awaiting_name => HandleAwaitingName(),
            UserStates.awaiting_phone => HandleAwaitingPhone(),
            _ => HandleGetUserMenu()
        });
    }

    #region работа с данными пользователя
    private async Task<Message> HandleGetUserMenu()
    {
        if (userProfile.IsRegistered)
        {
            return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.Menu, GetUserMenuKeyboard(), cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.RegistrationOnly18, null, cancellationToken);
    }

    private async Task<Message> ChangeThePreviousField()
    {
        userProfile.ToPreviousState();

        string promptMessage = userProfile.UserState switch
        {
            UserStates.awaiting_registration => Messages.GoRegistration,
            UserStates.awaiting_Eighteen => Messages.AreYou18,
            UserStates.awaiting_name => Messages.PrintYouName,
            UserStates.awaiting_phone => Messages.PrintPhoneNumber,
            _ => Messages.SomethingWentWrong
        };

        if (userProfile.UserState is UserStates.awaiting_registration or UserStates.awaiting_name)
            return await sendInfoService.SendMessage(userProfile,promptMessage,null,cancellationToken);
        return await sendInfoService.SendMessage( userProfile, promptMessage, Keyboards.GetKeyBoardInRegistration(), cancellationToken);

    }

    private async Task<Message> StartRegistration()
    {
        if (userProfile.StartRegistration())
        {
            return await sendInfoService.SendMessage(userProfile, Messages.AreYou18, GetKeyBoardYesOrNo(), cancellationToken);
        }
        return await sendInfoService.SendMessage(userProfile, Messages.YouHaveAlreadyRegistered, null, cancellationToken);
    }

    private async Task<Message> HandleChekIsEighteen(string messageText = null)
    {
        messageText ??= msg.Text;
        if (userProfile.ChekUserIsEighteen(messageText))
        {
            return await sendInfoService.SendMessage(userProfile, Messages.PrintYouName, null,cancellationToken);
        }
        await userProfileService.Delete(userProfile, cancellationToken);
        return await sendInfoService.SendSimpleMessage(msg, Messages.RegistrationOnly18, null,cancellationToken);
    }

    private async Task<Message> HandleAwaitingName()
    {
        if (userProfile.SetName(msg.Text))
        {
            return await sendInfoService.SendMessage(userProfile, Messages.PrintPhoneNumber, GetKeyBoardInRegistration(),cancellationToken);
        }
        return await sendInfoService.SendMessage(userProfile, Messages.SomethingWentWrong, null,cancellationToken);
    }

    private async Task<Message> HandleAwaitingPhone()
    {
        if (userProfile.IsRegistered)
        {
            if (userProfile.SetPhoneNumber(msg.Text))
            {
                return await sendInfoService.SendMessage(userProfile, Messages.ChangingPhoneNumber + userProfile.PhoneNumber, null,cancellationToken);
            }
            return await sendInfoService.SendMessage(userProfile, Messages.WrongPhoneNumberFormat, GetKeyBoardCancel(),cancellationToken);
        }
        else
        {
            if (userProfile.SetPhoneNumber(msg.Text))
            {
                return await PrintEventsOnPage("r");
            }
        }
        return await sendInfoService.SendMessage(userProfile, Messages.WrongPhoneNumberFormat, GetKeyBoardInRegistration(),cancellationToken);
    }

    private async Task<Message> PrintEventsOnPage(string command,int page=0)
    {
        IEnumerable<Event> events= new List<Event>();
        if (command == "r")
        {
            events = await eventService.GetWithPagination(cancellationToken, page, u => !u.UserProfiles.Contains(userProfile));
        }
        else if (command == "u"||command=="g")
        {
            events = await eventService.GetWithPagination(cancellationToken, page, u => u.UserProfiles.Contains(userProfile));
        }
        return await sendInfoService.EditOrSendMessage(msg, userProfile, GetEventsString(events, Messages.Event.AllowedToRegistr,page), GetEventKeyboard(events, command,page), cancellationToken: cancellationToken);

    }

    private async Task<Message> GetEventInfo(Guid eventId)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if(myEvent == null)
        {
            return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.SomethingWentWrong, GetUserMenuKeyboard(), cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, userProfile, $"{myEvent.ToString()}\n{myEvent.GetDescription()}", GetUserMenuKeyboard(), cancellationToken);
    }

    private async Task<Message> ChooseEvent(Guid eventId)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if (myEvent == null)
        {
            return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.SomethingWentWrong, GetUserMenuKeyboard(), cancellationToken);
        }
        if (!await userProfileService.Register(userProfile, myEvent, cancellationToken))
        {
            return await sendInfoService.SendMessage(userProfile, Messages.ErrorInRegistrOnEvent, GetUserMenuKeyboard(),cancellationToken);
        }
        await AdminGetUsersNotification(myEvent, Messages.Admin.NewUserRegistered + "\n" + myEvent.ToString());
        return await sendInfoService.EditOrSendMessage(msg, userProfile, String.Concat(Messages.YouHaveRegisteredForTheEvent,"\n",myEvent.ToString(),"\n",myEvent.GetDescription()), GetUserMenuKeyboard(), cancellationToken);

    }

    private async Task<Message> UnregisterUser(Guid eventId)
    {
        Event? unregisterEvent = await eventService.Get(eventId, cancellationToken);
        if (unregisterEvent == null)
        {
            return await sendInfoService.SendMessage(userProfile, Messages.Admin.EventNotFound,GetUserMenuKeyboard(),cancellationToken);
        }
        await userProfileService.Unregister(userProfile,unregisterEvent, cancellationToken);
        
        await AdminGetUsersNotification(unregisterEvent, Messages.Admin.UserUnregrisered + "\n" + unregisterEvent.ToString());

        return await sendInfoService.EditOrSendMessage(msg, userProfile, Messages.YouHasUnregistered + " " + unregisterEvent.ToString(), GetUserMenuKeyboard(), cancellationToken);
    }

    private async Task<Message> DeleteProfile()
    {
        foreach(Event myEvent in userProfile.Events)
        {
            await userProfileService.Unregister(userProfile,myEvent,cancellationToken);
        }
        logger.LogInformation(await userProfileService.Delete(userProfile, cancellationToken)+" Deleted user");
        return await sendInfoService.SendSimpleMessage(msg, Messages.ProfileWasDeleted, null, cancellationToken);
    }

    #endregion

    #region Admin panel

    private async Task AdminGetUsersNotification(Event myEvent, string messageText)
    {
        var admins = await adminProfileService.GetAll(cancellationToken, u => u.NotificationList.Contains(myEvent));
        int usersCount = (await userProfileService.GetAllByEvent(myEvent, cancellationToken)).Count();
        Message message = new Message();
        foreach (var admin in admins)
        {
            message = await sendInfoService.SendMessage(admin, $"{messageText} \n{GetUserProfileInfo(userProfile)} \n{Messages.Admin.CountOfRegisteredUsersOnEvent} - {usersCount}", null,cancellationToken);
            logger.LogInformation("The Admin message was sent with id: {SentMessageId}", message?.MessageId);
        }
        return;
    }
    private async Task<Message> SetAdminRole()
    {
        Person person = Person.Create(userProfile.Id);
        if (person.role == Roles.Admin)
        {
            logger.LogInformation(await userProfileService.Delete(userProfile, cancellationToken));
            AdminProfile Admin = (AdminProfile)person;
            logger.LogInformation(await adminProfileService.Create(Admin, cancellationToken));
            return await sendInfoService.SendMessage(Admin, Messages.Admin.YouHaveBeenAssignedTheAdminRole,GetAdminKeyboard(),cancellationToken);
        }
        return await sendInfoService.SendMessage(userProfile, Messages.YouHaveNoPermissionsToUseThisCommand,GetUserMenuKeyboard(),cancellationToken);
    }
    #endregion




}


