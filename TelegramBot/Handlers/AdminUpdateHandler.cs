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
public class AdminUpdateHandler
{
    private Message msg;
    private string? messageText;
    private AdminProfile adminProfile;
    private readonly ILogger<UpdateHandler> logger;
    private readonly AdminProfileService adminProfileService;
    private readonly UserProfileService userProfileService;
    private readonly EventService eventService;

    private readonly SendingService sendInfoService;


    public AdminUpdateHandler(
        AdminProfileService adminProfileService,
        SendingService sendInfoService,
        UserProfileService userProfileService,
        ILogger<UpdateHandler> logger)
    {
        this.adminProfileService = adminProfileService;
        this.sendInfoService = sendInfoService;
        this.userProfileService = userProfileService;
        this.logger = logger;
    }

    public async Task OnMessage(Message msg,AdminProfile adminProfile,CancellationToken cancellationToken)
    {
        this.msg = msg;
        this.adminProfile = adminProfile;
        messageText = msg.Text;
        Message sentMessage = await (messageText.Split(' ')[0] switch
        {
            "/admin" => GetAdminPanel(cancellationToken),
            "/addAdmin" => HandleAdminCreateNewAdmin(cancellationToken),
            "/menu" => GetAdminPanel(cancellationToken),
            "/getEventDebug" => HandleGetEvent(cancellationToken),
            "/deleteDebug" => DeleteProfileDebug(cancellationToken),
            _ => HandleAdminInput(cancellationToken)
        });
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }

    public async Task OnCallbackQuery(Message msg, AdminProfile adminProfile, char command,Guid eventId, CancellationToken cancellationToken)
    {
        this.msg = msg;
        this.adminProfile = adminProfile;
        messageText = msg.Text;
        Message sentMessage = await (command switch
        {
            'a' => AdminGetAllRegistratedUsers(eventId, cancellationToken),
            's' => SwitchNotificationNewUsers(eventId, cancellationToken),
            'd' => AdminDeleteEvent(eventId, cancellationToken)
        });
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);

    }
    public async Task OnCallbackQuery(Message msg, AdminProfile adminProfile, CallbackQuery callbackQuery,CancellationToken cancellationToken)
    {
        this.msg = callbackQuery.Message;
        this.adminProfile = adminProfile;
        messageText = msg.Text;
        Message sentMessage = await (callbackQuery.Data switch
        {
            "/addAdmin" => HandleAdminCreateNewAdmin(cancellationToken),
            "/getMenu" => GetAdminPanel(cancellationToken),
            "/getUsers" => HandleAdminGetAllRegistratedUsers(cancellationToken),
            "/switchNotification" => HandleSwitchNotificationNewUsers(cancellationToken),
            "/deleteEvent" => HandleAdminDeleteEvent(cancellationToken),
            "/createEvent" => HandleCreateEvent(cancellationToken),
            _ => HandleAdminInput(cancellationToken)
        });
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);
    }

    private async Task<Message> HandleSwitchNotificationNewUsers(CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, GetEventsString(events), GetEventKeyboard(events, 's'), cancellationToken);
    }
    private async Task<Message> SwitchNotificationNewUsers(Guid eventId, CancellationToken cancellationToken)
    {
        Event myEvent = await eventService.Get(eventId, cancellationToken);
        adminProfile.ChangeNotification(myEvent);
        await adminProfileService.Update(adminProfile, cancellationToken);
        if (adminProfile.IsNotification(myEvent))
        {
            return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.YouWillReceiveNotifications, GetAdminKeyboard(), cancellationToken);
        }
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.YouWillNotReceiveNotifications, GetAdminKeyboard(), cancellationToken);
    }

    private async Task<Message> HandleAdminDeleteEvent(CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, GetEventsString(events), GetEventKeyboard(events, 'd'), cancellationToken);
    }
    private async Task<Message> AdminDeleteEvent(Guid eventId, CancellationToken cancellationToken)
    {
        Event deleteEvent = await eventService.Get(eventId, cancellationToken);
        logger.LogInformation(await eventService.Delete(deleteEvent, cancellationToken));
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Event.EventWasDeleted, GetAdminKeyboard(), cancellationToken);
    }


    private async Task<Message> HandleAdminGetAllRegistratedUsers(CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, GetEventsString(events), GetEventKeyboard(events, 'a'), cancellationToken);

    }
    private async Task<Message> AdminGetAllRegistratedUsers(Guid eventId, CancellationToken cancellationToken)
    {
        Event? myEvent = await eventService.Get(eventId, cancellationToken);
        if (myEvent == null)
        {
            return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Event.EventNotFound, GetAdminKeyboard(), cancellationToken: cancellationToken);
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
            Message excelFileMessage = await sendInfoService.SendFile(msg.Chat.Id, users, myEvent.Name + " " + DateTime.Now);
            logger.LogInformation("The message with CSV file send with Id: {SentMessageId}", excelFileMessage?.Id);
        }
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, listAllUsers ??= Messages.Admin.UsersNotFound, GetAdminKeyboard(), cancellationToken: cancellationToken);
    }

    private async Task<Message> HandleAdminCreateNewAdmin(CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        adminProfile.SetAdminState(AdminStates.awaiting_newAdminId);
        logger.LogInformation(await adminProfileService.Update(adminProfile, cancellationToken));
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.PrintNewAdminId, null, cancellationToken);
    }
    private async Task<Message> AdminCreateNewAdmin(CancellationToken cancellationToken)
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
            await Task.WhenAll(
                adminProfileService.Create(newAdmin, cancellationToken),
                adminProfileService.Update(adminProfile, cancellationToken));
        }
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.NewAdminAdded, GetAdminKeyboard(), cancellationToken);
    }
    private async Task<Message> HandleAdminInput(CancellationToken cancellationToken)
    {

        return await (adminProfile.AdminState switch
        {
            AdminStates.create_event => HandleCreateEvent(cancellationToken),
            AdminStates.awaiting_eventName => HandleEventName(cancellationToken),
            AdminStates.awaiting_eventDateTime => HandleEventDate(cancellationToken),
            AdminStates.awaiting_eventDescription => HandleEventDescription(cancellationToken),
            AdminStates.awaiting_newAdminId => AdminCreateNewAdmin(cancellationToken),
            _ => GetAdminPanel(cancellationToken),
        });
    }

    private async Task<Message> GetAdminPanel(CancellationToken cancellationToken)
    {
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.Menu, GetAdminKeyboard(), cancellationToken: cancellationToken);
    }

    #region Процесс создания мероприятия

    private async Task<Message> HandleCreateEvent(CancellationToken cancellationToken)
    {
        if (adminProfile.CurrentEvent != null)
        {
            return await sendInfoService.SendMessage(msg.Chat.Id, Messages.Admin.YouAlreadyOperatingWithEvent, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }
        Event newEvent = new();
        await eventService.Create(newEvent, cancellationToken);
        adminProfile.SetCurrentEvent(newEvent.Id);
        await adminProfileService.Update(adminProfile, cancellationToken);

        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.PrintEventName, null, cancellationToken);
    }


    private async Task<Message> HandleEventName(CancellationToken cancellationToken)
    {
        if (adminProfile.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await sendInfoService.SendMessage(msg,adminProfile, Messages.SomethingWentWrong, , replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }

        Event newEvent = await eventService.Get(adminProfile.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            adminProfile.ResetCurrentEvent();
            await adminProfileService.Update(adminProfile, cancellationToken);
            return await sendInfoService.SendMessage(msg.Chat.Id, Messages.Admin.EventNotFound, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }
        newEvent.Name = msg.Text;
        adminProfile.SetAdminState(AdminStates.awaiting_eventDateTime);
        await Task.WhenAll(
            adminProfileService.Update(adminProfile, cancellationToken),
            eventService.Update(newEvent, cancellationToken)
            );
        return await sendInfoService.SendMessage(msg.Chat.Id, Messages.Admin.PrintEventDateTime, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }


    private async Task<Message> HandleEventDate(CancellationToken cancellationToken)
    {
        if (adminProfile.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await sendInfoService.SendMessage(msg.Chat.Id, Messages.SomethingWentWrong, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        Event newEvent = await eventService.Get(adminProfile.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            adminProfile.ResetCurrentEvent();
            await adminProfileService.Update(adminProfile, cancellationToken);
            return await sendInfoService.SendMessage(msg.Chat.Id, Messages.Admin.EventNotFound, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }

        if (!newEvent.SetDate(msg.Text))
        {
            return await sendInfoService.SendMessage(msg.Chat.Id, Messages.Admin.WrongDateTimeFormat, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        adminProfile.SetAdminState(AdminStates.awaiting_eventDescription);
        await Task.WhenAll(
            adminProfileService.Update(adminProfile, cancellationToken),
            eventService.Update(newEvent, cancellationToken)
            );
        return await sendInfoService.SendMessage(msg.Chat.Id, Messages.Admin.PrintEventDescription, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }
    private async Task<Message> HandleEventDescription(CancellationToken cancellationToken)
    {
        if (adminProfile.CurrentEvent == null || string.IsNullOrEmpty(msg.Text))
        {
            return await sendInfoService.SendMessage(msg.Chat.Id, Messages.SomethingWentWrong, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }

        Event newEvent = await eventService.Get(adminProfile.CurrentEvent, cancellationToken);
        if (newEvent == null)
        {
            adminProfile.ResetCurrentEvent();
            await adminProfileService.Update(adminProfile, cancellationToken);
            return await sendInfoService.SendMessage(msg.Chat.Id, Messages.Admin.EventNotFound, parseMode: ParseMode.Html, replyMarkup: GetAdminKeyboard(), cancellationToken: cancellationToken);
        }
        newEvent.Description = msg.Text;
        adminProfile.ResetCurrentEvent();
        adminProfile.SetAdminState(AdminStates.completed);
        await Task.WhenAll(
            adminProfileService.Update(adminProfile, cancellationToken),
            eventService.Update(newEvent, cancellationToken)
            );
        return await sendInfoService.EditOrSendMessage(msg, adminProfile, Messages.Admin.EventsuccessfullyCreated, GetAdminKeyboard(), cancellationToken);
    }


    private async Task<Message> HandleGetEvent(CancellationToken cancellationToken)
    {
        var chatId = msg?.Chat?.Id;
        var events = await eventService.GetAll(cancellationToken);
        return await sendInfoService.SendMessage(msg, GetEventsString(events),);
    }
    #endregion
    private async Task<Message> DeleteProfileDebug(CancellationToken cancellationToken)
    {
        logger.LogInformation(await adminProfileService.Delete(adminProfile, cancellationToken));
        return await sendInfoService.SendMessage(msg.Chat.Id, Messages.ProfileWasDeleted);
    }
}
