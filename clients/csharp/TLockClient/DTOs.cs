namespace TLockClient;

public class EncryptDto
{
    /// <summary>
    /// The UTF8 text to be encrypted.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// The specific round to use to encrypt the message. Cannot be used with <see cref="Duration"/>.
    /// </summary>
    public UInt64 Round { get; set; }

    /// <summary>
    /// How long to wait before the message can be decrypted. When specified, expects a number followed by one of these units:
    /// "s", "m", "h", "d", "M", "y".
    /// </summary>
    public string Duration { get; set; }
}

public class DecryptDto
{
    /// <summary>
    /// The encrypted data in base64 format.
    /// </summary>
    public string Encrypted { get; set; }
}

public class EncryptResponseDto
{
    /// <summary>
    /// The encrypted data in base64 format.
    /// </summary>
    public string Encrypted { get; set; }
    /// <summary>
    /// The encrypted data in PEM format which can be used in https://timevault.drand.love/.
    /// </summary>
    public string Pem { get; set; }
}

public class DecryptResponseDto
{
    /// <summary>
    /// The decrypted UTF8 text.
    /// </summary>
    public string Text { get; set; }
}