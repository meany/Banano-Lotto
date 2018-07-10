using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dm.Banotto.Data;
using dm.Banotto.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace dm.Banotto
{
    public class Program
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider services;
        private IConfigurationRoot configuration;
        private Config config;
        private AppDbContext db;

        public static void Main(string[] args)
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            configuration = builder.Build();

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 100
            });
            client.Log += Log;

            commands = new CommandService();
            services = new ServiceCollection()
                .Configure<Config>(configuration)
                .AddDatabase<AppDbContext>(configuration.GetConnectionString("Database"))
                .BuildServiceProvider();
            config = services.GetService<IOptions<Config>>().Value;
            db = services.GetService<AppDbContext>();
            DbInitializer.Initialize(db).Wait();

            await Install();
            await Start();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task Install()
        {
            var events = new Events(commands, client, services, config, db);
            client.Connected += events.HandleConnected;
            client.MessageReceived += events.HandleCommand;
            client.ReactionAdded += events.HandleReaction;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task Start()
        {
            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            var item = await db.Rounds
                .Where(x => x.RoundStatus == RoundStatus.Open)
                .Include(x => x.Bets)
                .FirstOrDefaultAsync();
            if (item != null)
            {
                string roundTypeStr = Utils.GetRoundTypeName(item.RoundType);
                await client.SetGameAsync($"{roundTypeStr} LOTTO");
            }
            else
            {
                await client.SetGameAsync("CLOSED");
            }
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase<T>(this IServiceCollection services, string connectionString) where T : DbContext
        {
            services.AddDbContext<T>(options => options.UseSqlite(connectionString));
            return services;
        }
    }
}

