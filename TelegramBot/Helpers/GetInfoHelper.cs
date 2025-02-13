using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Extensions;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Entities;
using TelegramBot.Extensions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TelegramBot.Helpers;
public static class GetInfoHelper
{
    public static string GetUserProfileInfo(UserProfile profile)
    {
        return $"{Emoji.Smile} Имя: {profile.Name}\n" +
               $"{Emoji.Iphone} Телефон: {profile.PhoneNumber}\n";
    }
    public static string GetUserProfileInfo(IEnumerable<UserProfile> profiles)
    {
        StringBuilder listAllUsers = new StringBuilder();
        int i = 0;
        foreach(var profile in profiles)
        {
            i++;
            listAllUsers.Append($"#{i} {Emoji.Smile} Имя: {profile.Name} {Emoji.Iphone} Телефон: {profile.PhoneNumber}\n");
        }
        return listAllUsers.ToString();
    }
    public static string GetEventsString(IEnumerable<Event> events,string message = "Мероприятия",int page = 0)
    {
        string eventString =$" {(page+1).AddUnicodeSymbols("\uFE0F\u20E3")} {message}:\n\n";
        int i = ApplicationConstants.numberOfObjectsPerPage*page;
        foreach (var x in events)
        {
            i++;
            eventString += $" {i.AddUnicodeSymbols("\uFE0F\u20E3")} {x.Name}\n  {Emoji.Date} Дата: {x.Date.ToString("D")}\n  {Emoji.Clock2} Время: {x.Date.ToString("t")} \n\n";
        }
        if (i == 0)
        {
            return "Мероприятий не найдено";
        }
        return eventString;
    }
}
