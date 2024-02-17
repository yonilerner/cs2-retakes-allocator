// Stolen from https://github.com/B3none/cs2-retakes/blob/014663222fa95bb9f506284814ae62205630416c/Modules/Translator.cs

using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RetakesAllocatorCore;

public class Translator
{
    private static Translator? Instance;

    public static Translator Initialize(IStringLocalizer localizer)
    {
        Instance = new(localizer);
        return Instance;
    }

    public static bool IsInitialized => Instance is not null;

    public static Translator GetInstance()
    {
        return Instance ?? throw new Exception("Translator is not initialized.");
    }
    
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

        var translation = localizedString.Value;

        return translation
            .Replace("[GREEN]", isCenter ? "" : ChatColors.Green.ToString())
            .Replace("[RED]", isCenter ? "" : ChatColors.Red.ToString())
            .Replace("[YELLOW]", isCenter ? "" : ChatColors.Yellow.ToString())
            .Replace("[BLUE]", isCenter ? "" : ChatColors.Blue.ToString())
            .Replace("[PURPLE]", isCenter ? "" : ChatColors.Purple.ToString())
            .Replace("[ORANGE]", isCenter ? "" : ChatColors.Orange.ToString())
            .Replace("[WHITE]", isCenter ? "" : ChatColors.White.ToString())
            .Replace("[NORMAL]", isCenter ? "" : ChatColors.White.ToString())
            .Replace("[GREY]", isCenter ? "" : ChatColors.Grey.ToString())
            .Replace("[LIGHT_RED]", isCenter ? "" : ChatColors.LightRed.ToString())
            .Replace("[LIGHT_BLUE]", isCenter ? "" : ChatColors.LightBlue.ToString())
            .Replace("[LIGHT_PURPLE]", isCenter ? "" : ChatColors.LightPurple.ToString())
            .Replace("[LIGHT_YELLOW]", isCenter ? "" : ChatColors.LightYellow.ToString())
            .Replace("[DARK_RED]", isCenter ? "" : ChatColors.DarkRed.ToString())
            .Replace("[DARK_BLUE]", isCenter ? "" : ChatColors.DarkBlue.ToString())
            .Replace("[BLUE_GREY]", isCenter ? "" : ChatColors.BlueGrey.ToString())
            .Replace("[OLIVE]", isCenter ? "" : ChatColors.Olive.ToString())
            .Replace("[LIME]", isCenter ? "" : ChatColors.Lime.ToString())
            .Replace("[GOLD]", isCenter ? "" : ChatColors.Gold.ToString())
            .Replace("[SILVER]", isCenter ? "" : ChatColors.Silver.ToString())
            .Replace("[MAGENTA]", isCenter ? "" : ChatColors.Magenta.ToString());
    }
}
