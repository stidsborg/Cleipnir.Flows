using System.Text;
using System.Text.Json;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

public static class Utils
{
    public static async Task<string> ToPrettyString<TParam, TReturn>(this BaseControlPanel<TParam, TReturn> controlPanel)
    {
        var effects = controlPanel.Effects;
        var effectIds = (await effects.AllIds).OrderBy(effectId => effectId.Serialize()).ToList();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Effects:");
        foreach (var effectId in effectIds)
            stringBuilder.AppendLine(new
                {
                    Id = effectId.Context == "" ? effectId.Id : effectId.Serialize(),
                    Result = (await effects.GetResultBytes(effectId))?.ToStringFromUtf8Bytes() ?? "[EMPTY]"
                }.ToString()
            );
        if (!effectIds.Any())
            stringBuilder.AppendLine("[None]");
            
        var messages = await controlPanel.Messages.AsObjects;
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("Messages:");
        foreach (var message in messages)
            stringBuilder.AppendLine(JsonSerializer.Serialize(message));
        if (!messages.Any())
            stringBuilder.AppendLine("[None]");
        
        return stringBuilder.ToString();    
    }
}