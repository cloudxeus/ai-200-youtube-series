using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var vaultUrl="";
var secretName="dbpassword";

var client =
    new SecretClient(
        new Uri(vaultUrl),
        new DefaultAzureCredential());

var secret =await client.GetSecretAsync(secretName);

Console.WriteLine(secret.Value.Value);
