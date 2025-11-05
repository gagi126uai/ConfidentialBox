namespace ConfidentialBox.Core.Configuration;

public class FileStorageSettings
{
    public bool StoreInDatabase { get; set; } = true;
    public bool StoreOnFileSystem { get; set; } = false;
    public string FileSystemDirectory { get; set; } = "SecureStorage";
}
