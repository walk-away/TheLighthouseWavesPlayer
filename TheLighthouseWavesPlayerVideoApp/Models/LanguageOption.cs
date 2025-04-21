using System.Globalization;

namespace TheLighthouseWavesPlayerVideoApp.Models;

public class LanguageOption
{
    public string Name { get; set; }
    public CultureInfo Culture { get; set; }

    public override string ToString() => Name;
}