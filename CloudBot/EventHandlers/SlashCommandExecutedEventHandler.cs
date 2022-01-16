﻿using CloudBot.CommandModules;
using Discord.WebSocket;

namespace CloudBot.EventHandlers;

public class SlashCommandExecutedEventHandler : IDiscordClientEventHandler
{
    private readonly IEnumerable<ISlashCommandModule> slashCommandModules;
    private readonly ILogger logger;

    public SlashCommandExecutedEventHandler(ILoggerFactory loggerFactory, IEnumerable<ISlashCommandModule> slashCommandModules)
    {
        this.slashCommandModules = slashCommandModules;
        logger = loggerFactory.CreateLogger($"{GetType().Name}");
    }

    public void RegisterHandlers(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += async (command) => await SlashCommandExecuted(client, command);
    }

    private async Task SlashCommandExecuted(DiscordSocketClient client, SocketSlashCommand slashCommand)
    {
        foreach (var module in slashCommandModules)
        {
            var selected = module.GetOrDefault(slashCommand.CommandName);
            if (selected is not null)
            {
                await selected.Delegate(slashCommand);
                break;
            }
        }
    }
}