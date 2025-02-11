using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Entities;

namespace TelegramBot.Helpers;
public static class GetInfoHelper
{
    public static string GetUserProfileInfo(UserProfile profile)
    {
        return $"Имя: {profile.Name}\n" +
               $"Телефон: {profile.PhoneNumber}\n";
    }
    public static string GetEventsString(IEnumerable<Event> events, string message = "Мероприятия")
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
}
