﻿using Microsoft.Extensions.Options;
using Telegram.Bot;
using TelegramBot.Services;
using TelegramBot;
using TelegramBot.Domain;
using TelegramBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using TelegramBot.Domain.Repositories.IRepositories;
using TelegramBot.Domain.Entities;
using TelegramBot.Services.TelegramServices;
using TelegramBot.Handlers;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;


CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING")
             ?? context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlite(connectionString ?? throw new InvalidOperationException("Database connection string is not configured.")));
        services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<UserProfileService>();
        services.AddScoped<AdminProfileService>();
        services.AddScoped<PersonService>();
        services.AddScoped<SendingService>();
        services.AddScoped<EventService>();
        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    string? botToken = Environment.GetEnvironmentVariable("BOTTOKEN");
                    if(string.IsNullOrWhiteSpace(botToken))
                    {
                        var botConfiguration = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
                        botToken = botConfiguration.BotToken;
                    }
                    if(string.IsNullOrWhiteSpace(botToken))
                    {
                        throw new InvalidOperationException("Bot token is not configured.");
                    }

                    TelegramBotClientOptions options = new(botToken);
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("ru-RU")
            };
            options.DefaultRequestCulture = new RequestCulture("ru-RU");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });
    })
    .Build();
await host.RunAsync();