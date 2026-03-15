namespace Community.PowerToys.Run.Plugin.OutlastTrials;

public record ShockDefinition
{
    public static readonly ShockDefinition Toxic = new()
    {
        Name = "toxic",
        DisplayName = "Toxic Shock",
        IconPath = "Images/toxic-shock.png",
        PlayingTime = 60 + 30,
        HidingTime = 30,
    };
    public static readonly ShockDefinition Cold = new()
    {
        Name = "cold",
        DisplayName = "Cold Snap",
        IconPath = "Images/cold-snap.png",
        PlayingTime = 60 * 2,
        HidingTime = 30,
    };

    public string Name { get; init; }
    public string DisplayName { get; init; }
    public string IconPath { get; init; }
    public int PlayingTime { get; init; }
    public int HidingTime { get; init; }
}
