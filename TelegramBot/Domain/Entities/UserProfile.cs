using PhoneNumbers;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Enums;
using TelegramBot.Extensions;
using System.Configuration;
using System.IO;
using Telegram.Bot.Types;

namespace TelegramBot.Domain.Entities;

public class UserProfile : Person
{
    public UserProfile(long Id):base(Id)
    {
        this.Id = Id;
        this.role=Roles.User;
    }

    public string? Name { get; private set; }
    public string? PhoneNumber { get; private set; } = "+7 999 999 99 99";
    public UserStates UserState { get; private set; } = UserStates.awaiting_registration;
    public bool HeIsEighteen {  get; private set; } = false;
    public bool IsRegistered { get; private set; } = false;
    public IEnumerable<Event> Events { get; set; }= new List<Event>();

    public bool ChekUserIsEighteen(string answer,UserStates userState = UserStates.awaiting_name)
    {
        string cheking = answer.ToLower().Trim();
        if (cheking == "yes" || cheking == "да" || cheking == "есть")
        {
            this.UserState = userState;
            return true;
        }
        this.UserState = UserStates.completed;
        return false;
    }

    public bool StartRegistration(UserStates userState = UserStates.completed)
    {
        if (IsRegistered) 
        {
            this.UserState = userState; 
            return false; 
        }
        UserState = UserStates.awaiting_Eighteen;
        return true;
    }

    public bool ToPreviousState() 
    {
        this.UserState = UserState.Previous();
        return true;
    }

    /// <summary>
    /// Устанавливает значение Name для профиля пользователя и переключает состояние.
    /// </summary>
    /// <param name="name">Имя пользователя</param>
    /// <param name="nextState">Следующее состояние</param>
    /// <returns>Если имя изменено успешно - <see langword="true"/>, иначе <see langword="false"/>.</returns>
    public bool SetName(string? name, UserStates nextState = UserStates.awaiting_phone)
    {
        if (string.IsNullOrWhiteSpace(name)) { return false; }
        this.Name = name;
        this.UserState = nextState;
        return true;
    }

    public bool SetPhoneNumber(string? phoneNumber, UserStates nextState = UserStates.completed)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber) && ValidatePhoneNumber(phoneNumber))
        {
            this.PhoneNumber = phoneNumber;
            this.UserState = nextState;
            this.IsRegistered = true;
            return true;
        }
        return false;
    }

    public bool ValidatePhoneNumber(string phoneNumber)
    {
        PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
        try
        {
            PhoneNumber parsedPhoneNumber = phoneNumberUtil.Parse(phoneNumber, null);
            return phoneNumberUtil.IsValidNumber(parsedPhoneNumber);
        }
        catch (NumberParseException)
        {
            return false;
        }
    }
}