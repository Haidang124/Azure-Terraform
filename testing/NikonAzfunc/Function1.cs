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
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.Reflection.Metadata;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace NikonAzfunc
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("Function1")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }

        [Function("test")]
        public async Task<HttpResponseData> TestAll([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {

            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                // Test Keyvault
                response = await TestAKVFunc(req, response);

                // Test File Share
                response = TestStorageFSFunc(req,response);

                // Test Blob Storage 
                response = await TestStorageBlobFunc(req, response);

                // Test Cosmos DB
                response = await TestCosmosFunc(req, response);

                return response;
            }
            catch (Exception ex)
            {
                response.WriteString(ex.ToString());
                return response;
            }

        }

        [Function("keyvault")]
        public async Task<HttpResponseData> TestAKV([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response = await TestAKVFunc(req,response);
            return response;
        }
        public async Task<HttpResponseData> TestAKVFunc([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, HttpResponseData response)
        {
            response.WriteString("<br> <b> Test Azure KeyVault: </b> </br>");

            var keyVaultName = Environment.GetEnvironmentVariable("keyVaultName", EnvironmentVariableTarget.Process);
            var clientId = Environment.GetEnvironmentVariable("clientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("clientSecret", EnvironmentVariableTarget.Process);
            var tenantId = Environment.GetEnvironmentVariable("tenantId", EnvironmentVariableTarget.Process);

            //// MSI
            ////var credential = new ManagedIdentityCredential(clientId);
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
        public HttpResponseData TestStorageFS([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response = TestStorageFSFunc(req, response);
            return response;
        }

        public HttpResponseData TestStorageFSFunc([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, HttpResponseData response)
        {
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
        public async Task<HttpResponseData> TestStorageBlob([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response = await TestStorageBlobFunc(req, response);
            return response;
        }

        public async Task<HttpResponseData> TestStorageBlobFunc([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, HttpResponseData response)
        {
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
        public async Task<HttpResponseData> TestCosmos([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response = await TestCosmosFunc(req, response);
            return response;
        }

        public async Task<HttpResponseData> TestCosmosFunc([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, HttpResponseData response)
        {
            //string connectionString = "AccountEndpoint=https://scarpe.documents.azure.com:443/;AccountKey=aztymVdg89A5JhnYkLWGj58d7ZxaPTxm5YYKeTgY1rTmsi90cJOHyHcQtF2soGB70VbsRH6sc6RyACDbX55Q1g==;";
            response.WriteString("<br> <b> Test Cosmos connection: </b> </br>");

            //CosmosClient cosmosClient = new CosmosClient(connectionString);
            var EndpointUri = Environment.GetEnvironmentVariable("EndpointUri", EnvironmentVariableTarget.Process);
            var PrimaryKey = Environment.GetEnvironmentVariable("PrimaryKey", EnvironmentVariableTarget.Process);

            CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            FeedIterator<DatabaseProperties> databasesIterator = cosmosClient.GetDatabaseQueryIterator<DatabaseProperties>();
            if (databasesIterator != null)
            {
                while (databasesIterator.HasMoreResults)
                {
                    FeedResponse<DatabaseProperties> currentResultSet = await databasesIterator.ReadNextAsync();
                    foreach (DatabaseProperties databaseProperties in currentResultSet)
                    {
                        response.WriteString($"<br>");
                        response.WriteString($"Database Name: {databaseProperties.Id}");
                        response.WriteString($"</br>");
                    }
                }
            }    
           
            return response;
        }
    }
}
