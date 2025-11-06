namespace ConfidentialBox.Core.Configuration;

public class FileStorageSettings
{
    public bool StoreInDatabase { get; set; } = true;
    public bool StoreOnFileSystem { get; set; } = false;
    public string FileSystemDirectory { get; set; } = "SecureStorage";

    /// <summary>
    ///     When enabled each user's files are stored inside an isolated directory using the user identifier
    ///     to avoid collisions and to simplify quota calculations.
    /// </summary>
    public bool UseUserScopedDirectories { get; set; } = true;

    /// <summary>
    ///     Maximum amount of storage (in gigabytes) that the whole instance can use. "0" disables the limit.
    /// </summary>
    public int TotalStorageLimitGb { get; set; }

    /// <summary>
    ///     Maximum amount of storage (in gigabytes) that a single user can consume. "0" disables the limit.
    /// </summary>
    public int PerUserStorageLimitGb { get; set; }

    public long GetTotalStorageLimitBytes()
        => TotalStorageLimitGb <= 0 ? 0 : TotalStorageLimitGb * 1024L * 1024L * 1024L;

    public long GetPerUserStorageLimitBytes()
        => PerUserStorageLimitGb <= 0 ? 0 : PerUserStorageLimitGb * 1024L * 1024L * 1024L;
}
