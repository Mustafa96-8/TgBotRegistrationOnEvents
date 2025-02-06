using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Domain.Collections;
using TelegramBot.Domain.Enums;

namespace TelegramBot.Domain.Entities;
public class Person
{
    public Person(long Id)
    {
        this.Id = Id;
    }

    public long Id { get; init; }
    public Roles role { get; protected set; } = Roles.User;
    public int? LastProfileMessageId { get;set; }

    public static Person Create(long id)
    {
        var personTemp = new Person(id);
        if (personTemp.SetAdmin())
        {
            return new AdminProfile(id);
        }
        return new UserProfile(id);
    }

    public bool SetAdmin()
    {
        var adminList = LoadFileEnvironment(".whiteList");
        if (adminList.Contains(this.Id.ToString()))
        {
            this.role = Roles.Admin;
            return true;
        }
        return false;
    }

    private static List<string> LoadFileEnvironment(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The file '{filePath}' does not exist.");

        List<string> adminIdList = new();
        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue; // Skip empty lines and comments

            adminIdList.Add(line);
        }
        return adminIdList;
    }
}
