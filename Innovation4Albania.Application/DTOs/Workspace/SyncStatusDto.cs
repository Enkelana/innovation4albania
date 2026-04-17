namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record SyncStatusDto(
    string Source,
    string Mode,
    string Health,
    string LastSync,
    string NextSync);
