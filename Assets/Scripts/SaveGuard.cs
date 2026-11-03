using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SaveGuard
{
    private static string _secret;
    private static readonly string FilePath = Path.Combine(Application.dataPath, "Secrets.json");

    private static string Secret
    {
        get
        {
            if (string.IsNullOrEmpty(_secret))
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var data = JsonUtility.FromJson<SecretConfig>(json);
                    _secret = data.saveGuardSecret;
                }
                else
                {
                    // Generate a random key automatically for your teammate
                    byte[] randomBytes = new byte[32];
                    using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(randomBytes); }
                    _secret = Convert.ToBase64String(randomBytes);

                    // Save it locally so their game works on subsequent launches
                    var newConfig = new SecretConfig { saveGuardSecret = _secret };
                    File.WriteAllText(FilePath, JsonUtility.ToJson(newConfig, true));
                    
                    Debug.LogWarning("[SaveGuard] Secrets.json was missing! Automatically generated a unique local key for this machine.");
                }
            }
            return _secret;
        }
    }

    public static string Sign(string payload)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    [Serializable]
    private class SecretConfig
    {
        public string saveGuardSecret;
    }
}
