using System;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using TeamsBot.Models;
using TeamsBot.Services.Interfaces;

namespace TeamsBot.Services
{
    public class BlobService: IBlobService
    {
        private readonly IConfiguration _config;
        private readonly string _url;
        private readonly string accountKey;
        public BlobService(IConfiguration config)
        {
            _config = config;
            accountKey = config["Config:AppConfig:AccountKey"];
        }
       
        public async Task<string> GetFileContent(string blobName= "pythonSuite.ps1")
        {
            try
            {
                string accountName = "installerscripts";
                
                string containerName = "teams-bot";

                var credential = new StorageSharedKeyCredential(accountName, accountKey);
                var serviceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), credential);
                var containerClient = serviceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var download = await blobClient.DownloadContentAsync();
                string content = download.Value.Content.ToString();
                return content;
            }
            catch (System.Exception)
            {

                throw;
            }
        }
    }
}
