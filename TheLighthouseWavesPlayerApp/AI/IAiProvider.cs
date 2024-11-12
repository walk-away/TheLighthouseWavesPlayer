namespace TheLighthouseWavesPlayerApp.AI;

public interface IAiProvider
{
    Task<string?> GetResponse(string request);
}