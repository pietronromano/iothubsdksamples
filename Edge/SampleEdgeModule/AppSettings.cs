//Application Settings
//SEE https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration

public sealed class AppSettings
{
    public required string DeviceConnectionString { get; set; }
    public required int MessageDelay { get; set; }
    public required int MessageCount { get; set; }
}

