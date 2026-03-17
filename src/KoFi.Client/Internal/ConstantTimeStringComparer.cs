using System.Security.Cryptography;
using System.Text;

namespace KoFi.Client.Internal;

/// <summary>
/// Provides a constant-time string comparison helper for secrets and verification tokens.
/// </summary>
internal static class ConstantTimeStringComparer
{
    /// <summary>
    /// Compares two strings using a constant-time comparison over their UTF-8 byte representation.
    /// </summary>
    /// <param name="left">The first string to compare.</param>
    /// <param name="right">The second string to compare.</param>
    /// <returns>
    /// <see langword="true"/> when both strings are non-null and their UTF-8 byte sequences
    /// are equal; otherwise <see langword="false"/>.
    /// </returns>
    public static bool Equals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}