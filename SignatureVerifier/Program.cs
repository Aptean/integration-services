using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

Console.WriteLine("AIP Signature Verifier Sample");

string fc = File.ReadAllText(AppContext.BaseDirectory + "\\sampleevent.json");

dynamic jo = JObject.Parse(fc);

var eventData = jo.data;

var eventPayload = JsonConvert.SerializeObject(eventData.payload);

string signature = eventData.signature.Value;

// TO DO: Get the public keys from endpoint (stg or production): https://stg.integration-graph.apteansharedservices.com/v1/public-keys
var primaryKey =
    "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvu3XDHvz0qO8GFURW1LZU0yHDmRm/Uc8\r\n7zBcGNgPnZP4R6X3GEwhpsErBWltMYaTqQFq9vJtrH81lU2SkFVL2wph7ux665e9KaJWb1A9ArwD\r\nD5XSzxGbLgAxZ9PjM+8+Mfcm3Xdtey9hXAU+8UP9XLiPgcRKvd1lcyiy36n4Vy0OaaN64V+MIhoE\r\nrBNO3MhYth6UfmRY+iH0oEmIlaZLjJ7bycQMFsy26RHAdpHU2cdipiVzwC48ggx49xTJKIHXcSiC\r\nCFeMYhpvxpQaJPJtriu8SbNYL9XV5UyCajHhFgN5cmr2rPIPt/KxkaLDwmc9iMK3a9unE6tqlPYH\r\nyqwfyQIDAQAB\n-----END PUBLIC KEY-----\n";

var
    secondaryKey =
    "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwnTEcW2kYFz75CFEDUZejZhFcFF4R8Gw\r\nSUv4g8AsDW8j3Zv//DjPqlhGrP1tTRz9p4KlKOey3PGCxhNBfbkapt2xEwIsDLm2pDUH4DrgtQWQ\r\n2N6Ctf60BZeEV9lp0GzNHP7GBqa2BYMuQph9Rlg88MenECFstkkhkoId1fw/ATO4qqiaZWh3DOXw\r\neraz1nnSVu9bAO2SYFmSZ15/T4YansTF/GHCp8R1wlvgPRe0DZWvSsqlIst2sicMHlmrDaWDETg5\r\n+5aDVtjV57xIXIXdfmXteE2YEISsES2HrH+jJboS9MozoISJeLxlwx/vCoE3gEOKdJ4+XaSC6N4R\r\nJjDBEQIDAQAB\n-----END PUBLIC KEY-----\n";

var signatureBytes = Convert.FromBase64String(signature);
var signatureParsed = JArray.Parse(Encoding.UTF8.GetString(signatureBytes));
if (signatureParsed.Count != 2)
{
  throw new Exception("Invalid signature");
}

var primarySignature = signatureParsed[0]!["signature"]!.Value<string>()!;
var secondarySignature = signatureParsed[1]!["signature"]!.Value<string>()!;

Console.WriteLine($"Valid signature: {VerifySignatureInternal(primaryKey, eventPayload, primarySignature) || VerifySignatureInternal(secondaryKey, eventPayload, secondarySignature)}");


static bool VerifySignatureInternal(string publicKey, string payload, string signature)
{
  var publicKeyProvider = GetPublicKeyProvider(publicKey);
  var publicKeyParameters = publicKeyProvider.ExportParameters(false);

  var hashOfDataToSign = GetHash(payload);

  using var rsa = new RSACryptoServiceProvider(2048);
  rsa.ImportParameters(publicKeyParameters);

  var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
  rsaDeformatter.SetHashAlgorithm("SHA256");

  return rsaDeformatter.VerifySignature(hashOfDataToSign, Convert.FromBase64String(signature));
}

static RSACryptoServiceProvider GetPublicKeyProvider(string publicPem)
{
  using var publicKeyTextReader = new StringReader(publicPem);
  var publicKeyParam = (RsaKeyParameters)new PemReader(publicKeyTextReader).ReadObject();

  var rsaParams = DotNetUtilities.ToRSAParameters(publicKeyParam);

  var csp = new RSACryptoServiceProvider(); // cspParams);
  csp.ImportParameters(rsaParams);

  return csp;
}

static byte[] GetHash(string plaintext)
{
  HashAlgorithm algorithm = SHA256.Create();

  return algorithm.ComputeHash(Encoding.UTF8.GetBytes(plaintext));
}
