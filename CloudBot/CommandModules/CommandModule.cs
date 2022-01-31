﻿using CloudBot.Models;
using CloudBot.Statics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace CloudBot.CommandModules;

public class CommandModule : AbstractCommandModule
{
    private readonly HttpClient httpClient;

    public CommandModule(ILogger<CommandModule> logger) : base(logger)
    {
        httpClient = new HttpClient();
    }

    protected override void BuildCommands(List<SlashCommandDefinition> commands)
    {
        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("status")
            .WithDescription("Get your status")
            .AddOption("address", ApplicationCommandOptionType.String, "The member address", true)
            .Build(),
            GetMember));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("invites")
            .WithDescription("Get the user invites amount")
            .Build(),
            GetInvites));

        commands.Add(new SlashCommandDefinition(
            new SlashCommandBuilder()
            .WithName("rarity-score")
            .WithDescription("Get the rarity score of a Cloudy")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The # number of the Cloudy", true)
            .Build(),
            GetRarityScore));
    }

    private async Task GetInvites(SocketSlashCommand command)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            var invites = await guildUser.Guild.GetInvitesAsync();
            int uses = invites.Where(i => i.Inviter.Id.Equals(guildUser.Id)).Sum(i => i.Uses) ?? 0;
            await command.RespondAsync($"{guildUser.Mention} You invited {uses} Citizens 😎");
        }
    }

    private async Task GetRarityScore(SocketSlashCommand command)
    {
        var id = command.Data.Options.FirstOrDefault(o => o.Name.Equals("id"));

        string endpoint = $"{Endpoints.METADATA}{$"?id={id!.Value}"}";
        var response = await httpClient.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"```json\n{await response.Content.ReadAsStringAsync()}```");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return;
        }

        string res = await response.Content.ReadAsStringAsync();
        var model = JObject.Parse(res);
        if (model == null || !model.TryGetValue("rarityScore", out var token))
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"Wrong backend data");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
        else
        {
            int score = ((int)token);
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Green);
            embedBuilder.AddField($"Neon Cloud #{id!.Value} rarity score", $"🚀 {score}");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
    }

    private async Task GetMember(SocketSlashCommand command)
    {
        var address = command.Data.Options.FirstOrDefault(o => o.Name.Equals("address"));

        string endpoint = $"{Endpoints.MEMBERS}{$"?address={address!.Value}"}";
        var response = await httpClient.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            var jsonElement = JsonConvert.DeserializeObject<JsonElement>(await response.Content.ReadAsStringAsync());
            embedBuilder.AddField("Error", $"```json\n{JsonConvert.SerializeObject(jsonElement, Formatting.Indented)}```");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
            return;
        }

        string res = await response.Content.ReadAsStringAsync();
        var model = JsonConvert.DeserializeObject<MemberModel>(res);
        if (model == null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Red);
            embedBuilder.AddField("Error", $"Wrong backend data");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
        else
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Green);
            embedBuilder.AddField("Result", $"```json\n{JsonConvert.SerializeObject(model, Formatting.Indented)}```");
            await command.RespondAsync(string.Empty, new Embed[] { embedBuilder.Build() });
        }
    }
}