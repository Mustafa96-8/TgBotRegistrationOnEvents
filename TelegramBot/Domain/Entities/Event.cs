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

    public IEnumerable<UserProfile> UserProfiles { get; set; }

    public bool SetDate(DateTime date)
    {
        if (date >= DateTime.Now)
        {
            this.Date = date;
            return true; 
        }
        return false;
    }
}
