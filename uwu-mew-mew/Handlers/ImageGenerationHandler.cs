using System.Numerics;
using Discord;
using Discord.WebSocket;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Misc;
using uwu_mew_mew.Openai;
using Color = SixLabors.ImageSharp.Color;

namespace uwu_mew_mew.Handlers;

public class ImageGenerationHandler : ISlashCommandHandler
{
    public async Task RegisterSlashCommands()
    {
        var image = new SlashCommandBuilder();
        image.Name = "image";
        image.Description = "Generate an image. NSFW allowed in dm. All images are public.";
        image.AddOption("prompt", ApplicationCommandOptionType.String, 
            "Prompt to generate the image from", isRequired: true);
        image.AddOption("cfg", ApplicationCommandOptionType.Number, 
            "Classifier Free Guidance scale, how much the model should respect your prompt. Default is 5,5.");
        image.AddOption("steps", ApplicationCommandOptionType.Integer, 
            "Number of sampling steps. The more the better, but longer. Default is 80. ");
        image.AddOption("seed", ApplicationCommandOptionType.Number, 
            "Seed for the sampler. Only useful if you already know a seed");

        var commands = await Bot.Client.GetGlobalApplicationCommandsAsync();
        if (!commands.Any(c => c.Name == image.Name))
            await Bot.Client.CreateGlobalApplicationCommandAsync(image.Build());
    }

    public async Task HandleSlashCommandAsync(SocketSlashCommand arg)
    {
        if (arg.Data.Name == "image")
        {
            GenerateImage(arg);
        }
    }

    private async Task GenerateImage(SocketSlashCommand arg)
    {
        var args = arg.Data.Options;
        var prompt = (string)args.First(a => a.Name == "prompt").Value;
        var cfgScale = args.Any(a => a.Name == "cfg_scale")
            ? (double)args.First(a => a.Name == "cfg_scale").Value
            : StableDiffusion.DefaultCfg;
        var samplingSteps = args.Any(a => a.Name == "steps")
            ? (int)(long)args.First(a => a.Name == "steps").Value
            : StableDiffusion.DefaultSteps;
        var seed = args.Any(a => a.Name == "seed")
            ? (int)(long)args.First(a => a.Name == "seed").Value
            : -1;
        
        var message = await arg.FollowupAsync(embed: generationEmbed.Build());
        
        var (image, newSeed) = await StableDiffusion.GenerateImage(prompt, cfgScale, samplingSteps, seed);

        var link = StableDiffusion.Upload(image);

        await message.ModifyAsync(m => m.Embed = GetFinalEmbed(arg.User, link, image, newSeed).Build());
    }

    private readonly EmbedBuilder generationEmbed = new EmbedBuilder()
        .WithColor(255, 192, 203)
        .WithTitle("Hold on mastew~ uwu")
        .WithDescription("Im wowrking as hawd as i can... :cat: mew")
        .WithCurrentTimestamp();
    
    public static Vector3 RgbaToHsv(Rgba32 rgba)
    {
        float r = rgba.R / 255f, g = rgba.G / 255f, b = rgba.B / 255f;
        float max = Math.Max(Math.Max(r, g), b), min = Math.Min(Math.Min(r, g), b), diff = max - min;
        float h = max == min ? 0 : max == r ? (60 * ((g - b) / diff + 6)) % 360 : max == g ? 60 * ((b - r) / diff + 2) : 60 * ((r - g) / diff + 4);
        return new Vector3(h, max == 0 ? 0 : diff / max, max);
    }


    private static EmbedBuilder GetFinalEmbed(IUser user, string link, byte[] image, long seed)
    {
        using var memoryStream = new MemoryStream(image);
        var decodedImage = PngDecoder.Instance.Decode<Rgba32>(new DecoderOptions(), memoryStream);
        var heat = new Dictionary<Rgba32, int>();
        decodedImage.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var x = 0; x < pixelRow.Length; x++)
                {
                    if(!heat.ContainsKey(pixelRow[x]))
                        heat.Add(pixelRow[x], 1);
                    else
                        heat[pixelRow[x]] += 1;
                }
            }
        });

        var bestColor = new Rgba32();
        var thresholdBrightness = 0.6f; // Example threshold
        var thresholdSaturation = 0.6f; // Example threshold
        int maxFrequency = 0;

        foreach (var color in heat.Keys)
        {
            var hsv = RgbaToHsv(color);

            if (hsv.Y < thresholdSaturation || hsv.Z < thresholdBrightness) continue;
            if (heat[color] <= maxFrequency) continue;
            maxFrequency = heat[color];
            bestColor = color;
        }
        
        return new EmbedBuilder()
            .WithAuthor(EmbedHelper.GetEmbedAuthorFromUser(user))
            .WithTitle("Done uwu!")
            .WithDescription($"[Download]({link})\nSeed:{seed}")
            .WithImageUrl(link)
            .WithColor(bestColor.R, bestColor.G, bestColor.B);
    }
}