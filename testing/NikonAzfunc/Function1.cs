using System.Net;
using Azure.Core;
using System.Security.Principal;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.Shares.Models;
using System.Security.Cryptography.X509Certificates;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.ComponentModel;
using System.Xml.Linq;

namespace NikonAzfunc
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("test")]
        public async Task<HttpResponseData> TestAll([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("<br> <b> Test Azure KeyVault: </b> </br>");

            var keyVaultName = Environment.GetEnvironmentVariable("keyVaultName", EnvironmentVariableTarget.Process);
            var clientId = Environment.GetEnvironmentVariable("clientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("clientSecret", EnvironmentVariableTarget.Process);
            var tenantId = Environment.GetEnvironmentVariable("tenantId", EnvironmentVariableTarget.Process);

            // MSI
            //var credential = new ManagedIdentityCredential(clientId);
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);

            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
            {
                string secretName = secretProperties.Name;
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                string secretValue = secret.Value;

                response.WriteString($"<br>");
                response.WriteString($"Secret Name: {secretName} - ");
                response.WriteString($"Secret Value: {secretValue}");
                response.WriteString($"</br>");
            }

            return response;

        }

        [Function("keyvault")]
        public async Task<HttpResponseData> TestAKV([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("<br> <b> Test Azure KeyVault: </b> </br>");

            var keyVaultName = Environment.GetEnvironmentVariable("keyVaultName", EnvironmentVariableTarget.Process);
            var clientId = Environment.GetEnvironmentVariable("clientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("clientSecret", EnvironmentVariableTarget.Process);
            var tenantId = Environment.GetEnvironmentVariable("tenantId", EnvironmentVariableTarget.Process);

            // MSI
            //var credential = new ManagedIdentityCredential(clientId);
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);

            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
            {
                string secretName = secretProperties.Name;
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                string secretValue = secret.Value;

                response.WriteString($"<br>");
                response.WriteString($"Secret Name: {secretName} - ");
                response.WriteString($"Secret Value: {secretValue}");
                response.WriteString($"</br>");
            }

            return response;

        }

        [Function("storage-fileshare")]
        public HttpResponseData TestStorageFS([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, ExecutionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("<br> <b> Test File Share: </b> </br>");

            var uri = Environment.GetEnvironmentVariable("uri", EnvironmentVariableTarget.Process);
            var fileShareName = Environment.GetEnvironmentVariable("fileShareName", EnvironmentVariableTarget.Process);
            var connectionStringFs = Environment.GetEnvironmentVariable("connectionStringFs", EnvironmentVariableTarget.Process);
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
                response.WriteString($"<br>");
                response.WriteString($"Files Name: {fileName} - ");
                response.WriteString($"</br>");
            }

            return response;
        }

        [Function("storage-blob")]
        public async Task<HttpResponseData> TestStorageBlob([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, ExecutionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("<br> <b> Test Blob Storage: </b> </br>");

            var connectionString = Environment.GetEnvironmentVariable("connectionStringFs", EnvironmentVariableTarget.Process);
            var containerName = Environment.GetEnvironmentVariable("containerName", EnvironmentVariableTarget.Process);
            var directory = Environment.GetEnvironmentVariable("directory", EnvironmentVariableTarget.Process);

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var blobs = containerClient.GetBlobsAsync();

            await foreach (BlobItem blob in blobs)
            {
                response.WriteString($"<br>");
                response.WriteString($"Blob Name: {blob.Name} - ");
                response.WriteString($"</br>");
            }


            //BlobServiceClient blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=terraformlaba86e;AccountKey=ryz9MKJDqeNtojWa8G0QAIsQWkXACPXeBF+lfW7pCenTYD+TxnQhxWl+iXe5LaNRL5iwnMLfnXfP+AStNPkKEQ==;EndpointSuffix=core.windows.net");
            //BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("nikon-bob");
            //BlobClient blobClient = containerClient.GetBlobClient("hello.txt");
            //if (await blobClient.ExistsAsync())
            //{
            //    var responseFile = await blobClient.DownloadAsync();
            //    using (var streamReader = new StreamReader(responseFile.Value.Content))
            //    {
            //        response.WriteString("<br> <b> Test Blob Storage: True </b> </br>");
            //    }
            //}

            return response;
        }

        [Function("cosmos")]
        public async Task<HttpResponseData> TestCosmos([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, ExecutionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("<br> <b> Test Azure KeyVault: </b> </br>");

            var keyVaultName = Environment.GetEnvironmentVariable("keyVaultName", EnvironmentVariableTarget.Process);
            var clientId = Environment.GetEnvironmentVariable("clientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("clientSecret", EnvironmentVariableTarget.Process);
            var tenantId = Environment.GetEnvironmentVariable("tenantId", EnvironmentVariableTarget.Process);

            // MSI
            //var credential = new ManagedIdentityCredential(clientId);
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);

            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
            {
                string secretName = secretProperties.Name;
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                string secretValue = secret.Value;

                response.WriteString($"<br>");
                response.WriteString($"Secret Name: {secretName} - ");
                response.WriteString($"Secret Value: {secretValue}");
                response.WriteString($"</br>");
            }

            return response;
        }
    }
}
