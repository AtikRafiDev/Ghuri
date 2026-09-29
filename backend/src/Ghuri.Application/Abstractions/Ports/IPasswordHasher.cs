namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// Turns a password into a one-way hash, and checks a password against a
/// stored hash. Application only knows THAT passwords are hashed; HOW
/// (PBKDF2, iteration count, salt) is Infrastructure's business.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>A new salted hash - hashing the same password twice gives two different strings.</summary>
    string Hash(string password);

    /// <summary>True if password is the one that produced passwordHash.</summary>
    bool Verify(string passwordHash, string password);
}
