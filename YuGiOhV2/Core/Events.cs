﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YuGiOhV2.Services;

namespace YuGiOhV2.Core
{
    public class Events
    {

        private DiscordSocketClient _client;
        private CommandService _commands;
        private Database _database;
        private Chat _chat;
        private Cache _cache;
        private IServiceProvider _services;

        private static readonly DiscordSocketConfig ClientConfig = new DiscordSocketConfig()
        {

            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Verbose,
            MessageCacheSize = 1000

        };

        private static readonly CommandServiceConfig CommandConfig = new CommandServiceConfig()
        {

            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Verbose

        };

        public Events()
        {

            Print("Initializing events...");

            _client = new DiscordSocketClient(ClientConfig);
            _commands = new CommandService(CommandConfig);
            _services = new ServiceCollection()
                .AddSingleton<Cache>()
                .BuildServiceProvider();
            _cache = new Cache();

            RegisterLogging();

            Print("Finished initializing events.");

        }

        public async Task GetReadyForBlastOff()
        {

            await RevEngines();
            await LoadDatabase();
            await RegisterCommands();

        }

        private async Task RevEngines()
        {

            var isTest = Environment.GetCommandLineArgs().FirstOrDefault()?.ToLower();
            string token;

            if (isTest == "true")
                token = File.ReadAllText("Files/Bot Tokens/Test.txt");
            else
                token = File.ReadAllText("Files/Bot Tokens/Test.txt");

            Print("Logging in...");
            await _client.LoginAsync(TokenType.Bot, token);
            Print("Logged in.");
            Print("Starting client...");
            await _client.StartAsync();
            Print("ITS UP AND RUNNING BOIIIIIIIIIIIIIIS");

        }

        private async Task LoadDatabase()
        {

            Print("Waiting for guilds to load...");
            await Task.Delay(10000);
            Print("Guilds loaded.");

            Print("Loading database...");
            _database = new Database(_client.Guilds);
            Print("Finished loading database.");

        }

        private async Task RegisterCommands()
        {

            Print("Registering commands...");

            _chat = new Chat(_cache, _database);

            _client.MessageReceived += HandleCommand;
            _client.MessageReceived += _chat.SOMEONEGETTINGACARDBOIS;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Print("Commands registered.");

        }

        private async Task HandleCommand(SocketMessage message)
        {

            if (!(message is SocketUserMessage)
                || message.Author.IsBot
                || string.IsNullOrEmpty(message.Content))
                return;

            ulong id = 1;
            string prefix = "y!";

            if (!(message.Channel is SocketDMChannel))
            {

                id = (message.Channel as SocketTextChannel).Guild.Id;
                prefix = _database.Settings[id].Prefix;

            }

            var possibleCmd = message as SocketUserMessage;
            var argPos = 0;

            if ((possibleCmd.HasStringPrefix(prefix, ref argPos) || possibleCmd.HasMentionPrefix(_client.CurrentUser, ref argPos))
                && possibleCmd.Content.Trim() != prefix)
            {

                var context = new SocketCommandContext(_client, possibleCmd);

                AltConsole.Print("Info", "Command", $"{possibleCmd.Author.Username} from {(possibleCmd.Channel as SocketTextChannel).Guild.Name}");
                AltConsole.Print("Info", "Command", $"{possibleCmd.Content}");

                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                {

                    if (result.ErrorReason.ToLower().Contains("unknown command"))
                        return;

                    await context.Channel.SendMessageAsync("There was an error in the command.");
                    //await context.Channel.SendMessageAsync("https://goo.gl/JieFJM");

                    AltConsole.Print("Error", "Error", result.ErrorReason);
                    //debug purposes
                    //await context.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");

                }

            }

        }

        private void RegisterLogging()
        {

            _client.Log += (message)
                => Task.Run(()
                => AltConsole.Print(message.Severity.ToString(), message.Source, message.Message, message.Exception));
            _commands.Log += (message)
                => Task.Run(()
                => AltConsole.Print(message.Severity.ToString(), message.Source, message.Message, message.Exception));

        }

        private void Print(string message)
            => AltConsole.Print("Info", "Events", message);

    }
}