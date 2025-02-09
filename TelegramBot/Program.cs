using Microsoft.Extensions.Options;
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
using 

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlite(Environment.GetEnvironmentVariable("CONNECTIONSTRING")));
        services.AddScoped<IUnitOfWork,UnitOfWork>();
        services.AddScoped<UserProfileService>();
        services.AddScoped<AdminProfileService>();
        services.AddScoped<PersonService>();
        services.AddScoped<EventService>();
        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    var botConfiguration = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
                    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BOTTOKEN")))
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
