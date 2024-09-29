using System.Text;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc.Other;

public static class Helper
{
    public static Guid Sha256ToGuid(string s)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return new Guid(sha256.ComputeHash(Encoding.UTF8.GetBytes(s)).Take(16).ToArray());
    }
}