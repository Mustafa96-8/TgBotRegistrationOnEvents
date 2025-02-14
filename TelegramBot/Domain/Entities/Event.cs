using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Collections;
using TelegramBot.Extensions;

namespace TelegramBot.Domain.Entities;
public class Event
{
    public Event() { }
    public Guid Id { get; init; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; private set;}

    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
    public ICollection<AdminProfile> AdminProfiles { get; set; } = new List<AdminProfile>();

    public string GetDescription() => $"{Emoji.Page_Facing_Up} {Description} \n";
    public override string ToString()
    {
        return $"\n<b>{Name} </b>\n{Emoji.Calendar} Дата: { Date.ToString("D")}\n{Emoji.Clock2} Время: {Date.ToString("t")}";
    }    
    public bool SetDate(string text)
    {
        DateTime date;
        if (DateTime.TryParse(text, out date) && date >= DateTime.Now)
        {
            Date = date;
            return true; 
        }
        return false;
    }
}
