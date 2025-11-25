using System.Security.Cryptography;

namespace MovieDatabase.Api.Core.Utils;

public static class HashUtils
{
    public static string ComputeHash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA512.HashData(bytes);

        return Convert.ToBase64String(hashBytes);
    }
}