using System;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using TeamsBot.Services.Interfaces;

namespace TeamsBot.Services
{
    public class BlobService: IBlobService
    {
        private readonly string _url;
        public async Task<string> GetFileContent(string blobName= "pythonSuite.ps1")
        {
            try
            {
                string accountName = "installerscripts";
                string accountKey = "oJAl7PbysDRjuVNl6VSh2yQ+RB93tGKBsIQIf/7k5hT/2PZVFhL+P79ecFmjlba23elq76Gth62J+AStHbtIIA==";
                string containerName = "teams-bot";

                var credential = new StorageSharedKeyCredential(accountName, accountKey);
                var serviceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), credential);
                var containerClient = serviceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var download = await blobClient.DownloadContentAsync();
                string content = download.Value.Content.ToString();
                return content;
                Console.WriteLine("Blob content:");
                Console.WriteLine(content);
            }
            catch (System.Exception)
            {

                throw;
            }
        }
    }
}
