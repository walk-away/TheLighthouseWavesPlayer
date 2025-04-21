namespace TheLighthouseWavesPlayerVideoApp.Models;

public class SortOption
{
    public string Name { get; set; }
    public string Property { get; set; }
    public bool IsAscending { get; set; } = true;

    public SortOption(string name, string property, bool isAscending = true)
    {
        Name = name;
        Property = property;
        IsAscending = isAscending;
    }

    public override string ToString() => Name;
}