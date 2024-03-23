using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace NikonAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {

        private readonly ILogger<TestController> _logger;
        private readonly IConfiguration _configuration;

        public TestController(ILogger<TestController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet()]
        public async Task<List<string>> Test()
        {
            try
            {
                List<string> output = new List<string>();
                output = await TestAKVFunc(output);
                output = TestStorageFSFunc(output);
                output = await TestStorageBlobFunc(output);
                output = await TestCosmosFunc(output);
                output = TestRoutingFunc(output);
                return output;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        [HttpGet("Keyvault")]
        public async Task<List<string>> TestAKV()
        {
            List<string> output = new List<string>();
            return await TestAKVFunc(output);
        }

        private async Task<List<string>> TestAKVFunc(List<string> output)
        {
            output.Add("Test Azure KeyVault: ");

            var keyVaultName = _configuration["keyVaultName"];
            var clientId = _configuration["clientId"];
            var clientSecret = _configuration["clientSecret"];
            var tenantId = _configuration["tenantId"];

            // MSI
            //var credential = new ManagedIdentityCredential(clientId);
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);

            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
            {
                string secretName = secretProperties.Name;
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                string secretValue = secret.Value;
                output.Add($"Secret Name: {secretName} - {secretValue} ");
            }
            output.Add("");

            return output;
        }

        [HttpGet("Storage-FileShare")]
        public List<string> TestStorageFS()
        {
            List<string> output = new List<string>();
            return TestStorageFSFunc(output);
        }
        private List<string> TestStorageFSFunc(List<string> output)
        {
            output.Add("Test File Share:");

            var uri = _configuration["uri"];
            var fileShareName = _configuration["fileShareName"];
            var connectionStringFs = _configuration["connectionStringFs"];

            //var clientId = Environment.GetEnvironmentVariable("clientId", EnvironmentVariableTarget.Process);
            //var clientSecret = Environment.GetEnvironmentVariable("clientSecret", EnvironmentVariableTarget.Process);
            //var tenantId = Environment.GetEnvironmentVariable("tenantId", EnvironmentVariableTarget.Process);
            var dir = Environment.GetEnvironmentVariable("dir", EnvironmentVariableTarget.Process);

            // MSI
            //var credential = new ManagedIdentityCredential(clientId);
            //var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var clientOptions = new ShareClientOptions
            {
                ShareTokenIntent = ShareTokenIntent.Backup
            };
            ShareClient shareClient = new ShareClient(connectionStringFs, fileShareName);
            //var shareClient = new ShareClient(new Uri($"{uri}/{fileShareName}"), credential, clientOptions);

            Azure.Pageable<ShareFileItem> files = shareClient.GetDirectoryClient(dir).GetFilesAndDirectories();

            List<string> fileNames = files.Select(f => f.Name).ToList();

            foreach (var fileName in fileNames)
            {
                output.Add($"Files Name: {fileName} - ");
            }
            output.Add("");
            return output;
        }

        [HttpGet("Storage-Blob")]
        public async Task<List<string>> TestStorageBlob()
        {
            List<string> output = new List<string>();
            return await TestStorageBlobFunc(output);
        }
        private async Task<List<string>> TestStorageBlobFunc(List<string> output)
        {
            output.Add("Test Blob Storage:");

            var connectionString = _configuration["connectionStringFs"];
            var containerName = _configuration["containerName"];
            var directory = _configuration["directory"];

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var blobs = containerClient.GetBlobsAsync();

            await foreach (BlobItem blob in blobs)
            {
                output.Add($"Blob Name: {blob.Name} - ");
            }
            output.Add("");
            return output;
        }

        [HttpGet("Cosmos")]
        public async Task<List<string>> TestCosmos()
        {
            List<string> output = new List<string>();
            return await TestCosmosFunc(output);
        }

        private async Task<List<string>> TestCosmosFunc(List<string> output)
        {
            //string connectionString = "AccountEndpoint=https://scarpe.documents.azure.com:443/;AccountKey=aztymVdg89A5JhnYkLWGj58d7ZxaPTxm5YYKeTgY1rTmsi90cJOHyHcQtF2soGB70VbsRH6sc6RyACDbX55Q1g==;";
            output.Add("Test Cosmos connection:");

            //CosmosClient cosmosClient = new CosmosClient(connectionString);

            var EndpointUri = _configuration["EndpointUri"];
            var PrimaryKey = _configuration["PrimaryKey"];

            CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            FeedIterator<DatabaseProperties> databasesIterator = cosmosClient.GetDatabaseQueryIterator<DatabaseProperties>();
            while (databasesIterator.HasMoreResults)
            {
                FeedResponse<DatabaseProperties> currentResultSet = await databasesIterator.ReadNextAsync();
                foreach (DatabaseProperties databaseProperties in currentResultSet)
                {
                    output.Add($"Database Name: {databaseProperties.Id}");
                }
            }
            output.Add("");
            return output;
        }
        [HttpGet("routing")]
        public List<string> TestRouting() {
            List<string> output = new List<string>();
            return TestRoutingFunc(output);
        }
        private List<string> TestRoutingFunc(List<string> output)
        {
            output.Add("Test Routing");
            string computerName = Environment.MachineName;
            output.Add("Computer Name: " + computerName);

            string ipAddress = GetLocalIPAddress();
            output.Add("IP Address: " + ipAddress);
            return output;
        }

        public static string GetLocalIPAddress()
        {
            string ipAddress = string.Empty;
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress address in hostEntry.AddressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = address.ToString();
                    break;
                }
            }

            return ipAddress;
        }
    }
}