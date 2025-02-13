using Telegram.Bot.Types;
using TelegramBot.Domain.Entities;
using TelegramBot.Domain.Enums;
using TelegramBot.Contracts;
using TelegramBot.Services;
using static TelegramBot.Domain.Collections.Keyboards;
using static TelegramBot.Helpers.GetInfoHelper;
using Microsoft.Extensions.Logging;
using TelegramBot.Helpers;

namespace TelegramBot.Handlers;
public class AdminUpdateHandler
{
    private Message? msg;
    private AdminProfile? adminProfile;
    private readonly ILogger<UpdateHandler> logger;
    private readonly AdminProfileService adminProfileService;
    private readonly UserProfileService userProfileService;
    private readonly EventService eventService;
    private readonly PersonService personService;
    private readonly SendingService sendInfoService;
    private readonly CancellationToken cancellationToken;


    public AdminUpdateHandler(
        AdminProfileService adminProfileService,
        SendingService sendInfoService,
        UserProfileService userProfileService,
        EventService eventService,
        PersonService personService,
        ILogger<UpdateHandler> logger,
        CancellationToken cancellationToken
        )
    {
        this.adminProfileService = adminProfileService;
        this.sendInfoService = sendInfoService;
        this.userProfileService = userProfileService;
        this.eventService = eventService;
        this.personService = personService;
        this.logger = logger;
        this.cancellationToken = cancellationToken;
    }

    public async Task OnMessage(Message msg,AdminProfile adminProfile )
    {
        this.msg = msg;
        this.adminProfile = adminProfile;
        var messageText = msg.Text??"";
        Message sentMessage = await (messageText.Split(' ')[0] switch
        {
            "/cancel" => DropCommand(),
            "/deleteprofile" => DeleteProfile(),
            "/admin" => GetAdminPanel(),
            "/addAdmin" => HandleAdminCreateNewAdmin(),
            "/deleteperson" => HandleAdminDeletePerson(),
            "/menu" => GetAdminPanel(),
            "/getevents" => PrintEventsOnPage("g"),
            _ => HandleAdminInput()
        });

        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }

    public async Task OnCallbackQuery(Message msg, AdminProfile adminProfile, char command,Guid eventId )
    {
        this.msg = msg;
        this.adminProfile = adminProfile;
        Message sentMessage = await (command switch
        {
            'a' => AdminGetAllRegistratedUsers(eventId ),
            's' => SwitchNotificationNewUsers(eventId ),
            'd' => AdminDeleteEvent(eventId),
            'g' => GetEventInfo(eventId),
            _ => GetAdminPanel()
        });
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }
    public async Task OnCallbackQuery(Message msg, AdminProfile adminProfile, CallbackQuery callbackQuery)
    {
        this.msg = msg;
        this.adminProfile = adminProfile;
        var callbackQueryDataArgs = (callbackQuery.Data ?? " ").Split('|');
        Message sentMessage = await (callbackQueryDataArgs[0] switch
        {
            "/cancel" =>DropCommand(),
            "/addAdmin" => HandleAdminCreateNewAdmin(),
            "/deleteperson" => HandleAdminDeletePerson(),
            "/getMenu" => GetAdminPanel(),
            "/getevents" => PrintEventsOnPage("g"),
            "/getUsers" => PrintEventsOnPage("a"),
            "/switchNotification" => PrintEventsOnPage("s"),
            "/deleteEvent" => PrintEventsOnPage("d"),
            "/createEvent" => HandleCreateEvent(),
            "->" => PrintEventsOnPage(callbackQueryDataArgs[1], int.Parse(callbackQueryDataArgs[2])+1),
            "<-" => PrintEventsOnPage(callbackQueryDataArgs[1], int.Parse(callbackQueryDataArgs[2])-1),
            _ => HandleAdminInput()
        });
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }


    private async Task<Message> GetEventInfo(Guid eventId)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if(myEvent == null)
        {
            return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.SomethingWentWrong, GetAdminKeyboard(), cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, $"{myEvent.ToString()}\n{myEvent.GetDescription()}", GetAdminKeyboard(), cancellationToken);
    }

    private async Task<Message> DropCommand()
    {
        if(adminProfile.CurrentEvent != null)
        {
            Event deleteEvent = await eventService.Get(adminProfile.CurrentEvent, cancellationToken);
            logger.LogInformation(await eventService.Delete(deleteEvent, cancellationToken));
            adminProfile.ResetCurrentEvent();
        }
        adminProfile.SetAdminState(AdminStates.completed);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.Menu, GetAdminKeyboard(), cancellationToken: cancellationToken);
    }

    private async Task<Message> PrintEventsOnPage(string command ,int page=0)
    {
        page = page < 0 ? 0 : page;
        var events = await eventService.GetWithPagination(cancellationToken,page);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, GetEventsString(events,page:page), GetEventKeyboard(events, command,page), cancellationToken);
    }

    private async Task<Message> SwitchNotificationNewUsers(Guid eventId )
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        adminProfile.ChangeNotification(myEvent);
        if (adminProfile.IsNotification(myEvent))
        {
            return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.YouWillReceiveNotifications, GetAdminKeyboard(), cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.YouWillNotReceiveNotifications, GetAdminKeyboard(), cancellationToken);
    }

    private async Task<Message> AdminDeleteEvent(Guid eventId )
    {
        Event deleteEvent = await eventService.Get(eventId, cancellationToken);
        logger.LogInformation(await eventService.Delete(deleteEvent, cancellationToken));
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Event.EventWasDeleted, GetAdminKeyboard(), cancellationToken);
    }


    private async Task<Message> AdminGetAllRegistratedUsers(Guid eventId)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if (myEvent == null)
        {
            return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Event.EventNotFound, GetAdminKeyboard(), cancellationToken: cancellationToken);
        }
        var result = await userProfileService.GetAllByEvent(myEvent, cancellationToken);
        string listAllUsers = GetUserProfileInfo(result);
        if (!string.IsNullOrWhiteSpace(listAllUsers))
        {
            IEnumerable<UserProfileResponse> users = result.Select(u => new UserProfileResponse { Id = u.Id, Name = u.Name, PhoneNumber = u.PhoneNumber });
            Message excelFileMessage = await sendInfoService.SendFile(msg.Chat.Id, users, myEvent.Name + " " + DateTime.Now.ToString("g"));
            logger.LogInformation("The message with excel file send with Id: {SentMessageId}", excelFileMessage?.Id);
            return await sendInfoService.SendMessage( adminProfile, listAllUsers ??= Messages.Admin.UsersNotFound, GetAdminKeyboard(),cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.UsersNotFound, GetAdminKeyboard(),cancellationToken);
    }

    private async Task<Message> HandleAdminCreateNewAdmin()
    {
        adminProfile.SetAdminState(AdminStates.awaiting_newAdminId);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.PrintNewAdminId, GetKeyBoardCancel(), cancellationToken);
    }
    private async Task<Message> HandleAdminDeletePerson()
    {
        adminProfile.SetAdminState(AdminStates.awaiting_deletePersonId);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.PrintDeletePersonId, GetKeyBoardCancel(), cancellationToken);
    }
    private async Task<Message> AdminCreateNewAdmin()
    {
        long newAdminId;
        if (long.TryParse(msg.Text.Split(' ')[0], out newAdminId))
        {
            UserProfile? user = await userProfileService.Get(newAdminId, cancellationToken);
            if (user != null)
            {
                await userProfileService.Delete(user, cancellationToken);
            }
            AdminProfile newAdmin = new(newAdminId);
            adminProfile.SetAdminState(AdminStates.completed);
            await adminProfileService.Create(newAdmin, cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.NewAdminAdded, GetAdminKeyboard(), cancellationToken);
    }
    private async Task<Message> HandleAdminInput()
    {

        return await (adminProfile.AdminState switch
        {
            AdminStates.create_event => HandleCreateEvent(),
            AdminStates.awaiting_eventName => HandleEventName(),
            AdminStates.awaiting_eventDateTime => HandleEventDate(),
            AdminStates.awaiting_eventDescription => HandleEventDescription(),
            AdminStates.awaiting_newAdminId => AdminCreateNewAdmin(),
            AdminStates.awaiting_deletePersonId => AdminDeletePerson(),
            _ => GetAdminPanel(),
        });
    }

    private async Task<Message> AdminDeletePerson()
    {
        long deletePersonId; 
        if(long.TryParse(msg.Text.Split(' ')[0], out deletePersonId))
        {
            Person person = await personService.Get(deletePersonId, cancellationToken);
            if(person == null)
            {
                return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.UsersNotFound, GetKeyBoardCancel(), cancellationToken);
            }
            string result;
            if(person.role == Domain.Collections.Roles.User)
            {
                result = await userProfileService.Delete((UserProfile)person, cancellationToken);
            }
            else
            {
                result = await adminProfileService.Delete((AdminProfile)person, cancellationToken);
            }
            adminProfile.SetAdminState(AdminStates.completed);
            logger.LogInformation(result);
            return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.PersonWasDeleted, GetAdminKeyboard(), cancellationToken);
        }
        return await sendInfoService.SendMessage(adminProfile, Messages.Admin.WrongTelegramId, GetKeyBoardCancel(), cancellationToken);
    }

    private async Task<Message> GetAdminPanel()
    {
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.Menu, GetAdminKeyboard(), cancellationToken);
    }

    #region Процесс создания мероприятия

    private async Task<Message> HandleCreateEvent()
    {
        if (adminProfile.CurrentEvent != null)
        {
            return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.YouAlreadyOperatingWithEvent, GetKeyBoardCancel(), cancellationToken);
        }
        Event newEvent = new();
        await eventService.Create(newEvent, cancellationToken);
        adminProfile.SetCurrentEvent(newEvent.Id);

        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.PrintEventName, GetKeyBoardCancel(), cancellationToken);
    }


    private async Task<Message> HandleEventName()
    {
        if (adminProfile.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await sendInfoService.SendMessage(adminProfile ,Messages.SomethingWentWrong, GetKeyBoardCancel(), cancellationToken);
        }

        Event newEvent = await eventService.Get(adminProfile.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            adminProfile.ResetCurrentEvent();
            return await sendInfoService.SendMessage(adminProfile ,Messages.Admin.EventNotFound,GetAdminKeyboard(), cancellationToken);
        }
        newEvent.Name = msg.Text;
        adminProfile.SetAdminState(AdminStates.awaiting_eventDateTime);
        await eventService.Update(newEvent, cancellationToken);
        return await sendInfoService.SendMessage(adminProfile, Messages.Admin.PrintEventDateTime, GetKeyBoardCancel(), cancellationToken);
    }


    private async Task<Message> HandleEventDate()
    {
        if (adminProfile.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await sendInfoService.SendMessage( adminProfile,Messages.SomethingWentWrong, GetKeyBoardCancel(), cancellationToken: cancellationToken);
        }

        Event newEvent = await eventService.Get(adminProfile.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            adminProfile.ResetCurrentEvent();
            return await sendInfoService.SendMessage(adminProfile, Messages.Admin.EventNotFound,GetAdminKeyboard(), cancellationToken: cancellationToken);
        }

        if (!newEvent.SetDate(msg.Text))
        {
            return await sendInfoService.SendMessage(adminProfile, Messages.Admin.WrongDateTimeFormat, GetAdminKeyboard(), cancellationToken);
        }

        adminProfile.SetAdminState(AdminStates.awaiting_eventDescription);
        await eventService.Update(newEvent, cancellationToken);
        return await sendInfoService.SendMessage(adminProfile, Messages.Admin.PrintEventDescription,GetKeyBoardCancel(),cancellationToken);
    }
    private async Task<Message> HandleEventDescription()
    {
        if (adminProfile.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await sendInfoService.SendMessage(adminProfile, Messages.SomethingWentWrong, GetKeyBoardCancel(), cancellationToken);
        }

        Event newEvent = await eventService.Get(adminProfile.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            adminProfile.ResetCurrentEvent();
            return await sendInfoService.SendMessage(adminProfile, Messages.Admin.EventNotFound, GetAdminKeyboard(),cancellationToken);
        }
        newEvent.Description = msg.Text;
        adminProfile.ResetCurrentEvent();
        adminProfile.SetAdminState(AdminStates.completed);
        await eventService.Update(newEvent, cancellationToken);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.EventsuccessfullyCreated, GetAdminKeyboard(), cancellationToken);
    }

    #endregion
    private async Task<Message> DeleteProfile()
    {
        logger.LogInformation(await adminProfileService.Delete(adminProfile, cancellationToken));
        return await sendInfoService.SendSimpleMessage(msg, Messages.ProfileWasDeleted,null,cancellationToken);
    }
}
