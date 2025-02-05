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

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));
        services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<UnitOfWork>();
/*        services.AddScoped<IGenericRepository<Person>, PersonRepository>();
        services.AddScoped<IGenericRepository<UserProfile>, UserProfileRepository>();
        services.AddScoped<IGenericRepository<AdminProfile>, AdminProfileRepository>();*/
        services.AddScoped<UserProfileService>();
        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    var botConfiguration = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
                    if (string.IsNullOrWhiteSpace(botConfiguration.BotToken))
                    {
                        throw new InvalidOperationException("Bot token is not configured.");
                    }

                    TelegramBotClientOptions options = new(botConfiguration.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();

await host.RunAsync();
