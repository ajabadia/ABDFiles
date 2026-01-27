using System.Security.Cryptography;
using System.Text;

namespace ABDTools.Core.Crypto;

/// <summary>
/// AES-256-GCM encryption/decryption service
/// </summary>
public class CryptoService
{
    private const int SaltSize = 16;      // 128-bit
    private const int NonceSize = 12;     // 96-bit (GCM standard)
    private const int KeySize = 32;       // AES-256
    private const int Iterations = 100000; // PBKDF2 iterations

    /// <summary>
    /// Encrypts a file using AES-256-GCM
    /// </summary>
    /// <param name="sourcePath">Source file path</param>
    /// <param name="destPath">Destination file path</param>
    /// <param name="password">Encryption password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task EncryptFileAsync(
        string sourcePath, 
        string destPath, 
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        ArgumentException.ThrowIfNullOrEmpty(destPath);
        ArgumentException.ThrowIfNullOrEmpty(password);

        // Read input file
        var plaintext = await File.ReadAllBytesAsync(sourcePath, cancellationToken);

        // Generate random salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Derive key
        var key = DeriveKey(password, salt);

        // Generate random nonce
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);

        // Encrypt
        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        // Write output: [Salt][Nonce][Tag][Ciphertext]
        await using var output = File.Create(destPath);
        await output.WriteAsync(salt, cancellationToken);
        await output.WriteAsync(nonce, cancellationToken);
        await output.WriteAsync(tag, cancellationToken);
        await output.WriteAsync(ciphertext, cancellationToken);
    }

    /// <summary>
    /// Decrypts a file using AES-256-GCM
    /// </summary>
    /// <param name="sourcePath">Encrypted file path</param>
    /// <param name="destPath">Destination file path</param>
    /// <param name="password">Decryption password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DecryptFileAsync(
        string sourcePath,
        string destPath,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        ArgumentException.ThrowIfNullOrEmpty(destPath);
        ArgumentException.ThrowIfNullOrEmpty(password);

        // Read encrypted file
        var data = await File.ReadAllBytesAsync(sourcePath, cancellationToken);

        var minSize = SaltSize + NonceSize + AesGcm.TagByteSizes.MaxSize;
        if (data.Length < minSize)
        {
            throw new CryptographicException(
                "Archivo dañado o formato incorrecto (demasiado corto)");
        }

        // Extract components
        var salt = data[..SaltSize];
        var nonce = data[SaltSize..(SaltSize + NonceSize)];
        var tag = data[(SaltSize + NonceSize)..(SaltSize + NonceSize + AesGcm.TagByteSizes.MaxSize)];
        var ciphertext = data[(SaltSize + NonceSize + AesGcm.TagByteSizes.MaxSize)..];

        // Derive key
        var key = DeriveKey(password, salt);

        // Decrypt
        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException(
                "Error de desencriptación: contraseña incorrecta o archivo modificado");
        }

        // Write output
        await File.WriteAllBytesAsync(destPath, plaintext, cancellationToken);
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(KeySize);
    }
}
