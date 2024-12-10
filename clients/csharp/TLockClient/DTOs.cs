namespace TLockClient;

public class EncryptDto
{
    public string Text { get; set; }
}

public class DecryptDto
{
    public string Encrypted { get; set; }
}

public class EncryptResponseDto
{
    public string Encrypted { get; set; }
}

public class DecryptResponseDto
{
    public string Text { get; set; }
}
