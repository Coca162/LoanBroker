using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

public class HelpFormatter : DefaultHelpFormatter
{
    public HelpFormatter(CommandContext ctx) : base(ctx) { }

    public override CommandHelpMessage Build()
    {
        //Green
        EmbedBuilder.Color = new DiscordColor("0F7539");
        return base.Build();
    }
}