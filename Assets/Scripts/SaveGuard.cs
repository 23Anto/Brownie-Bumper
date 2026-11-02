using System; using System.Security.Cryptography; using System.Text;

public static class SaveGuard
{
    const string Secret = "";

    public static string Sign(string payload)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}