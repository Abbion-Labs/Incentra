namespace VariableCompensation.Application.Abstractions.Security;

public interface ISensitiveDataEncryptionService
{
    byte[] EncryptDecimal(decimal value);

    decimal DecryptDecimal(byte[] encryptedPayload);
}
