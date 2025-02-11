using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Domain.Collections;
public static class Commands
{

    public static string GetMenu => "/menu";

    public static class Admin
    {
        public static string GetAdminPanel => "/admin";
        public static string AddNewAdmin => "/addAdmin";
    }

    public static class User
    {

    }
}
