using System.Security.Cryptography;
using System.Text;

namespace ConfidentialBox.Infrastructure.Services;

public class ShareLinkGenerator : IShareLinkGenerator
{
    public string GenerateUniqueLink()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}