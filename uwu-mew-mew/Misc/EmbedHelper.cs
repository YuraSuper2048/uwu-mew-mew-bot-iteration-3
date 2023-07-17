using Discord;
using Color = Discord.Color;

namespace uwu_mew_mew.Misc;

public static class EmbedHelper
{
    public static EmbedAuthorBuilder GetEmbedAuthorFromUser(IUser user)
    {
        return new EmbedAuthorBuilder().WithName(user.GlobalName).WithIconUrl(user.GetAvatarUrl());
    }
    
    public static Embed GetEmbed(string? title = null, string? description = null, Color? color = null, IUser? author = null, string? iconUrl = null, string? imageUrl = null)
    {
        var builder = new EmbedBuilder();
        if(title is not null) builder.Title = title;
        if(description is not null) builder.Description = description;
        if(color is not null) builder.Color = color;
        if(author is not null) builder.Author = GetEmbedAuthorFromUser(author);
        if(imageUrl is not null) builder.ImageUrl = imageUrl;
        if(iconUrl is not null) builder.ThumbnailUrl = iconUrl;
        return builder.WithCurrentTimestamp().Build();
    }
}