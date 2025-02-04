using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Contracts;
public class UserProfileResponse
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
}
