using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Domain.Entities;
public class Event
{
    public Event() { }
    public Guid Id { get; init; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; private set;}

    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();

    public override string ToString()
    {
        return Name+" Дата:"+Date.ToString();
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
