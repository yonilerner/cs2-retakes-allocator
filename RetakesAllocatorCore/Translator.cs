// Stolen from https://github.com/B3none/cs2-retakes/blob/014663222fa95bb9f506284814ae62205630416c/Modules/Translator.cs

using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RetakesAllocatorCore;

public class Translator
{
    private static Translator? _instance;

    public static Translator Initialize(IStringLocalizer localizer)
    {
        _instance = new(localizer);
        return _instance;
    }

    public static bool IsInitialized => _instance is not null;

    public static Translator Instance => _instance ?? throw new Exception("Translator is not initialized.");
    
    private IStringLocalizer _stringLocalizerImplementation;

    public Translator(IStringLocalizer localizer)
    {
        _stringLocalizerImplementation = localizer;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _stringLocalizerImplementation.GetAllStrings(includeParentCultures);
    }

    public string this[string name] => Translate(name);

    public string this[string name, params object[] arguments] => Translate(name, arguments);

    private string Translate(string key, params object[] arguments)
    {
        var isCenter = key.StartsWith("center.");
        key = key.Replace("center.", "");
        
        var localizedString = _stringLocalizerImplementation[key, arguments];

        if (localizedString == null || localizedString.ResourceNotFound)
        {
            return key;
        }

        return isCenter ? localizedString.Value : Color(localizedString.Value);
    }

    public static string Color(string text)
    {
        return text
            .Replace("[GREEN]", ChatColors.Green.ToString())
            .Replace("[RED]", ChatColors.Red.ToString())
            .Replace("[YELLOW]", ChatColors.Yellow.ToString())
            .Replace("[BLUE]", ChatColors.Blue.ToString())
            .Replace("[PURPLE]", ChatColors.Purple.ToString())
            .Replace("[ORANGE]", ChatColors.Orange.ToString())
            .Replace("[WHITE]", ChatColors.White.ToString())
            .Replace("[NORMAL]", ChatColors.White.ToString())
            .Replace("[GREY]", ChatColors.Grey.ToString())
            .Replace("[LIGHT_RED]", ChatColors.LightRed.ToString())
            .Replace("[LIGHT_BLUE]", ChatColors.LightBlue.ToString())
            .Replace("[LIGHT_PURPLE]", ChatColors.LightPurple.ToString())
            .Replace("[LIGHT_YELLOW]", ChatColors.LightYellow.ToString())
            .Replace("[DARK_RED]", ChatColors.DarkRed.ToString())
            .Replace("[DARK_BLUE]", ChatColors.DarkBlue.ToString())
            .Replace("[BLUE_GREY]", ChatColors.BlueGrey.ToString())
            .Replace("[OLIVE]", ChatColors.Olive.ToString())
            .Replace("[LIME]", ChatColors.Lime.ToString())
            .Replace("[GOLD]", ChatColors.Gold.ToString())
            .Replace("[SILVER]", ChatColors.Silver.ToString())
            .Replace("[MAGENTA]", ChatColors.Magenta.ToString());
    }
}
