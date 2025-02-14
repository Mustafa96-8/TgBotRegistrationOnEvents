using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Enums;

namespace TelegramBot.Domain.Entities;
public class AdminProfile : Person
{
    public AdminProfile(long Id):base(Id) 
    {
        role=Roles.Admin;
    }
    public AdminStates AdminState { get; private set; } = AdminStates.completed;
    public ICollection<Event> Events { get; set; } = new List<Event>();


    public Guid? CurrentEvent { get; private set; } = null;

    public void StartCreatingEvent()
    {
        AdminState = AdminStates.create_event;
    }

    public bool SetCurrentEvent(Guid id)
    {
        if (CurrentEvent == null)
        {
            AdminState = AdminStates.awaiting_eventName;
            CurrentEvent = id;
            return true;
        }
        return false;
    }

    public void SetAdminState(AdminStates state)
    {
        AdminState = state;
    }


    public void ResetCurrentEvent()
    {
        AdminState = AdminStates.completed;
        CurrentEvent = null;
    }

}
