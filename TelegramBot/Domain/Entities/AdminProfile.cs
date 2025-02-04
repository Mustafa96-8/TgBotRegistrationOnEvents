using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Enums;

namespace TelegramBot.Domain.Entities;
public class AdminProfile : Person
{
    public AdminProfile() { }
    public AdminProfile(long Id)
    {
        this.role = Roles.Admin;
        this.Id = Id;
    }
    public AdminStates AdminState { get; private set; } = AdminStates.completed;
    public bool IsNotificationNewUser { get; private set; } = false;

    public void SetNotification(bool notify)
    {
        if (this.role == Roles.Admin)
        {
            IsNotificationNewUser = notify;
            return;
        }
        IsNotificationNewUser = false;
    }

}
