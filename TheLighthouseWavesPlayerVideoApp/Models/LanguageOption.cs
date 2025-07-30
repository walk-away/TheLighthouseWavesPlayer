using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Models;

public class LanguageOption
{
    public string Name { get; set; } = string.Empty;
    public CultureInfo? Culture { get; set; }

    public override string? ToString() => Name;
}