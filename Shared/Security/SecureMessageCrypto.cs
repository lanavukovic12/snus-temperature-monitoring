using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Dtos;

namespace Shared.Security;

public static class SecureMessageCrypto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static SecureIngestRequest Protect(
        IngestReadingRequest request,
        long messageId,
        SecureMessagingOptions options)
    {
        ValidateOptions(options, requirePrivateKey: true);

        var sentAt = DateTime.UtcNow;
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions);
        var aesKey = Convert.FromBase64String(options.AesKeyBase64);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var secureRequest = new SecureIngestRequest
        {
            SensorId = request.SensorId,
            MessageId = messageId,
            SentAt = sentAt,
            Iv = Convert.ToBase64String(aes.IV),
            Ciphertext = Convert.ToBase64String(ciphertext)
        };

        secureRequest.Signature = Sign(secureRequest, options.Rsa);
        return secureRequest;
    }

    public static bool TryUnprotect(
        SecureIngestRequest secureRequest,
        SecureMessagingOptions options,
        out IngestReadingRequest? request,
        out string error)
    {
        request = null;
        error = string.Empty;

        try
        {
            ValidateOptions(options, requirePrivateKey: false);

            if (!VerifySignature(secureRequest, options.Rsa))
            {
                error = "Invalid digital signature.";
                return false;
            }

            var aesKey = Convert.FromBase64String(options.AesKeyBase64);
            var iv = Convert.FromBase64String(secureRequest.Iv);
            var ciphertext = Convert.FromBase64String(secureRequest.Ciphertext);

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            request = JsonSerializer.Deserialize<IngestReadingRequest>(plaintext, JsonOptions);

            if (request is null)
            {
                error = "Encrypted payload is empty.";
                return false;
            }

            if (!string.Equals(request.SensorId, secureRequest.SensorId, StringComparison.Ordinal))
            {
                error = "Envelope SensorId does not match encrypted payload SensorId.";
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string Sign(SecureIngestRequest request, RsaKeyOptions keyOptions)
    {
        using var rsa = RSA.Create();
        rsa.ImportParameters(ToParameters(keyOptions, includePrivate: true));
        var bytesToSign = Encoding.UTF8.GetBytes(BuildCanonicalString(request));
        var signature = rsa.SignData(bytesToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    private static bool VerifySignature(SecureIngestRequest request, RsaKeyOptions keyOptions)
    {
        using var rsa = RSA.Create();
        rsa.ImportParameters(ToParameters(keyOptions, includePrivate: false));
        var bytesToVerify = Encoding.UTF8.GetBytes(BuildCanonicalString(request));
        var signature = Convert.FromBase64String(request.Signature);
        return rsa.VerifyData(bytesToVerify, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private static string BuildCanonicalString(SecureIngestRequest request)
    {
        return string.Join('\n',
            request.SensorId,
            request.MessageId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.SentAt.ToUniversalTime().ToString("O"),
            request.Iv,
            request.Ciphertext);
    }

    private static RSAParameters ToParameters(RsaKeyOptions options, bool includePrivate)
    {
        var parameters = new RSAParameters
        {
            Modulus = Decode(options.Modulus),
            Exponent = Decode(options.Exponent)
        };

        if (!includePrivate)
        {
            return parameters;
        }

        parameters.D = Decode(options.D);
        parameters.P = Decode(options.P);
        parameters.Q = Decode(options.Q);
        parameters.DP = Decode(options.DP);
        parameters.DQ = Decode(options.DQ);
        parameters.InverseQ = Decode(options.InverseQ);
        return parameters;
    }

    private static byte[] Decode(string value) => Convert.FromBase64String(value);

    private static void ValidateOptions(SecureMessagingOptions options, bool requirePrivateKey)
    {
        if (string.IsNullOrWhiteSpace(options.AesKeyBase64))
        {
            throw new CryptographicException("SecureMessaging:AesKeyBase64 is required.");
        }

        _ = Convert.FromBase64String(options.AesKeyBase64);

        if (string.IsNullOrWhiteSpace(options.Rsa.Modulus) ||
            string.IsNullOrWhiteSpace(options.Rsa.Exponent))
        {
            throw new CryptographicException("RSA public key is required.");
        }

        if (requirePrivateKey &&
            (string.IsNullOrWhiteSpace(options.Rsa.D) ||
             string.IsNullOrWhiteSpace(options.Rsa.P) ||
             string.IsNullOrWhiteSpace(options.Rsa.Q) ||
             string.IsNullOrWhiteSpace(options.Rsa.DP) ||
             string.IsNullOrWhiteSpace(options.Rsa.DQ) ||
             string.IsNullOrWhiteSpace(options.Rsa.InverseQ)))
        {
            throw new CryptographicException("RSA private key is required for signing.");
        }
    }
}
