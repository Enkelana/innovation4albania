namespace Innovation4Albania.Domain.Entities;

public sealed class SyncStatus
{
    public required string Source { get; set; }
    public required string Mode { get; set; }
    public required DateTime LastSyncUtc { get; set; }
    public required DateTime NextSyncUtc { get; set; }
    public required string Health { get; set; }
}
