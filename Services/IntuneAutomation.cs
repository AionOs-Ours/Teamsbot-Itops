//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Azure.Core;
//using Azure.Identity;
//using Microsoft.Graph.Beta;
//using Microsoft.Graph.Beta.Models;
//using Microsoft.Graph.Beta.Models.ManagedTenants;
//using Microsoft.Graph.Beta.Models.ODataErrors;
//using Microsoft.Kiota.Abstractions.Authentication;
//using Microsoft.Kiota.Authentication.Azure;
//using TeamsBot.Models;

//namespace TeamsBot.Services
//{
//    public class IntuneAutomation
//    {
//        public static string vsCodeId = "";
//        private GraphServiceClient _graphClient;
//        string tenantId = "13345921-c174-438f-9a21-1a76064a1a11";
//        string clientId = "0b0dd3a1-d1da-4cea-b4a9-6f1ac5584454";
//        string appId = "cdb5b3e7-d85a-4033-ac55-b764c519ef0f";           // Intune app ID (Win32LobApp/MSI/etc.)
//        string clientSecret = "p648Q~9DGG32R_Q2TLE0gyn_AmtkeXzKQl8ARdij";
//        public IntuneAutomation()
//        {
//        }
//        private async Task<GraphServiceClient> GetGraphClientAsync()
//        {


//            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
//            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

//            using var httpClient = new HttpClient();
//            httpClient.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", token.Token);
//            var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

//            return graphClient;
//        }

//        // 1️⃣ Create an Azure AD group
//        public async Task<Group> CreateGroupAsync(string groupName)
//        {
//            try
//            {


//                var group = new Group
//                {
//                    DisplayName = groupName,
//                    MailEnabled = false,
//                    MailNickname = Guid.NewGuid().ToString(),
//                    SecurityEnabled = true,
//                    GroupTypes = new List<string>() // ensure static group
//                };
//                Group existingGroup = null;

//                var existingGroups = await _graphClient.Groups
//                    .GetAsync(req =>
//                    {
//                        req.QueryParameters.Filter = $"startsWith(displayName,'{groupName}')";
//                    });

//                foreach (var item in existingGroups?.Value)
//                {
//                    await _graphClient.Groups[item.Id]
//                          .DeleteAsync();
//                }
//                var created = await _graphClient.Groups.PostAsync(group);
//                Console.WriteLine($"✅ Group created: {created.Id}");
//                return created;
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }
//        public async Task DeleteGroupAsync(string groupName)
//        {
//            //var existingGroups = await _graphClient.Groups
//            //        .GetAsync(req =>
//            //        {
//            //            req.QueryParameters.Filter = $"startsWith(displayName,'{groupName}')";
//            //        });

//            //if (existingGroups?.Value?.Count > 0)
//            //{
//            //    //existingGroup = existingGroups.Value[0];
//            //    //Console.WriteLine($"🔄 Reusing existing group: {existingGroup.DisplayName}");
//            //}
//            //var url = $"https://graph.microsoft.com/v1.0/groups/{groupId}";
//            //var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
//            //var token = await credential.GetTokenAsync(
//            //    new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

//            //using var httpClient = new HttpClient();
//            //httpClient.DefaultRequestHeaders.Authorization =
//            //    new AuthenticationHeaderValue("Bearer", token.Token);

//            //var response = await httpClient.DeleteAsync(url);

//            //if (response.IsSuccessStatusCode)
//            //{
//            //    Console.WriteLine($"Group {groupId} deleted successfully.");
//            //}
//            //else
//            //{
//            //    var error = await response.Content.ReadAsStringAsync();
//            //    Console.WriteLine($"Failed to delete group: {response.StatusCode}\n{error}");
//            //}
//        }

//        // 2️⃣ Create Intune PowerShell script
//        public async Task<DeviceManagementScript> CreateDeviceScriptAsync(string scriptName, string scriptContent)
//        {
//            try
//            {
//                var scripts = await _graphClient.DeviceManagement.DeviceManagementScripts
//               .GetAsync();

//                var existingScript = scripts?.Value?
//                    .FirstOrDefault(s => s.DisplayName == scriptName);

//                if (existingScript != null)
//                {
//                    Console.WriteLine($"⚠️ Script already exists with ID: {existingScript.Id}. Rewriting...");

//                    // Delete the old one to enforce rewrite
//                    await _graphClient.DeviceManagement.DeviceManagementScripts[existingScript.Id]
//                        .DeleteAsync();

//                    Console.WriteLine($"🗑️ Deleted existing script: {existingScript.Id}");
//                }
//                var script = new DeviceManagementScript
//                {
//                    DisplayName = "My Intune Script",
//                    Description = "This script configures settings",
//                    ScriptContent = System.Text.Encoding.UTF8.GetBytes(scriptContent),
//                    RunAsAccount = RunAsAccountType.System,
//                    EnforceSignatureCheck = false,
//                    FileName = scriptName
//                };

//                var createdScript = await _graphClient.DeviceManagement.DeviceManagementScripts
//                    .PostAsync(script);
//                return createdScript;
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }

//        }

//        // 3️⃣ Add device to group (using AAD device objectId)
//        public async Task AddDeviceToGroupAsync(string groupId, string managedDeviceId)
//        {
//            try
//            {
//                //get AAD deviceId from Intune managedDevice
//                var md = await _graphClient.DeviceManagement.ManagedDevices[managedDeviceId].GetAsync();
//                if (md == null)
//                    throw new Exception("Managed device not found.");

//                // find the corresponding Azure AD device object
//                var devices = await _graphClient.Devices.GetAsync(rc =>
//                {
//                    rc.QueryParameters.Filter = $"deviceId eq '{md.AzureADDeviceId}'";
//                });

//                var aadDevice = devices.Value?.FirstOrDefault();
//                if (aadDevice == null)
//                    throw new Exception("Azure AD device not found.");

//                await _graphClient.Groups[groupId].Members.Ref.PostAsync(new ReferenceCreate
//                {
//                    OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{aadDevice.Id}"
//                });

//                Console.WriteLine($"✅ Device added to group {groupId}");
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }

//        // 4️⃣ Assign the script to the group
//        public async Task AssignScriptToGroupAsync(string scriptId, string groupId)
//        {
//            try
//            {
//                var assignments = new DeviceManagementScriptAssignment
//                {
//                    Target = new GroupAssignmentTarget
//                    {
//                        GroupId = groupId
//                    }
//                };
//                var existingAssignments = await _graphClient.DeviceManagement.DeviceManagementScripts[groupId]
//               .Assignments
//               .GetAsync();
//                bool alreadyAssigned = false;

//                if (existingAssignments?.Value != null)
//                {
//                    foreach (var assign in existingAssignments.Value)
//                    {
//                        if (assign.Target is GroupAssignmentTarget groupTarget &&
//                            groupTarget.GroupId == groupId)
//                        {
//                            alreadyAssigned = true;
//                            break;
//                        }
//                    }
//                }

//                if (!alreadyAssigned)
//                {
//                    var assignPayload = new
//                    {
//                        deviceManagementScriptAssignments = new[]
//                        {
//                            new {
//                                    target = new Dictionary<string, object>
//                                        {
//                                            { "@odata.type", "#microsoft.graph.groupAssignmentTarget" },
//                                            { "groupId", groupId }
//                                        }
//                                 }
//                        }
//                    };

//                    var assignContent = new StringContent(JsonSerializer.Serialize(assignPayload), Encoding.UTF8, "application/json");
//                    var response = await assignScript(scriptId, assignContent);
//                }
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }
//        private async Task<string> assignScript(string scriptId, StringContent assignContent)
//        {
//            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
//            var token = await credential.GetTokenAsync(
//                new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

//            using var httpClient = new HttpClient();
//            httpClient.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", token.Token);

//            var assignResponse = await httpClient.PostAsync(
//                        $"https://graph.microsoft.com/beta/deviceManagement/deviceManagementScripts/{scriptId}/assign",
//                        assignContent);

//            if (assignResponse.IsSuccessStatusCode)
//            {
//                Console.WriteLine($"✅ Script assigned to group");
//            }
//            else
//            {
//                Console.WriteLine($"❌ Failed to assign script: {assignResponse.StatusCode}");
//                Console.WriteLine(await assignResponse.Content.ReadAsStringAsync());
//            }
//            return "done";

//        }

//        // 5️⃣ Force Intune sync on the device
//        public async Task ForceSyncAsync(string managedDeviceId)
//        {
//            try
//            {
//                Console.WriteLine($"🔄 Forcing sync on device {managedDeviceId}");
//                await _graphClient.DeviceManagement.ManagedDevices[managedDeviceId].SyncDevice.PostAsync();
//                Console.WriteLine($"✅ Sync triggered successfully — script will execute shortly.");
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }

//        // 6️⃣ Combined flow
//        public async Task RunAutomationAsync(string groupName, string scriptName, string scriptContent, string userId)
//        {
//            try
//            {
//                _graphClient = await GetGraphClientAsync();
//                var Alldevices = await _graphClient.DeviceManagement.ManagedDevices.GetAsync();
//                var device = Alldevices.Value.FirstOrDefault(x => x.UserId == userId && x.DeviceType == DeviceType.WindowsRT);
//                // Step 1: Create group
//                var group = await CreateGroupAsync(groupName);

//                // Step 2: Create script
//                var script = await CreateDeviceScriptAsync(scriptName, scriptContent);

//                // Step 3: Add device to group
//                await AddDeviceToGroupAsync(group.Id, device.Id);

//                // Step 4: Assign script to group
//                await AssignScriptToGroupAsync(script.Id, group.Id);

//                // Step 5: Force sync so script runs immediately
//                await ForceSyncAsync(device.Id);

//                Console.WriteLine("🎯 Full Intune automation completed successfully.");
//            }
//            catch (ODataError ex)
//            {
//                Console.WriteLine($"Graph API Error: {ex.Error?.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Unhandled Exception: {ex.Message}");
//            }
//        }

//        public static async Task<string> CreateWin32AppAsync(GraphServiceClient graphClient)
//        {
//            var app = new Win32LobApp
//            {
//                DisplayName = "Visual Studio Code",
//                Description = "Installs VS Code silently via PowerShell",
//                Publisher = "Microsoft",
//                InstallCommandLine = "powershell.exe -ExecutionPolicy Bypass -File Install-VSCode.ps1",
//                UninstallCommandLine = "powershell.exe -ExecutionPolicy Bypass -File Uninstall-VSCode.ps1",
//                InstallExperience = new Win32LobAppInstallExperience
//                {
//                    RunAsAccount = RunAsAccountType.System
//                },
//                DetectionRules = new List<Win32LobAppDetection>()
//        {
//            new Win32LobAppFileSystemDetection
//            {
//                DetectionType = Win32LobAppFileSystemDetectionType.Exists,
//                FileOrFolderName = "Code.exe",
//                Path = @"C:\Program Files\Microsoft VS Code"
//            }
//        },
//                MinimumSupportedOperatingSystem = new WindowsMinimumOperatingSystem
//                {
//                    V101809 = true
//                }
//            };

//            var result = await graphClient.DeviceAppManagement.MobileApps.PostAsync(app);
//            Console.WriteLine($"App Created: {result.Id}");
//            return result.Id;
//        }

//        public static async Task UploadIntuneWinFileAsync(GraphServiceClient graphClient, string appId, string filePath)
//        {
//            Console.WriteLine("Creating upload session...");
//            var uploadSession = await graphClient
//                .DeviceAppManagement
//                .MobileApps[appId]
//                .GraphWin32LobApp
//                .ContentVersions
//                .PostAsync(new MobileAppContent());

//            var contentVersionId = uploadSession.Id;
//            Console.WriteLine($"Content Version ID: {contentVersionId}");

//            var fileName = Path.GetFileName(filePath);
//            var fileInfo = new FileInfo(filePath);

//            // Create upload request
//            var fileObj = new MobileAppContentFile
//            {
//                Name = fileName,
//                Size = fileInfo.Length,
//                Manifest = "",
//                IsDependency = false
//            };

//            var fileContent = await graphClient.DeviceAppManagement.MobileApps[appId].GraphWin32LobApp
//                .ContentVersions[contentVersionId]
//                .Files
//                .PostAsync(fileObj);

//            var uploadUrl = fileContent.UploadState.ToString();
//            var uploadUri = new Uri(uploadUrl);

//            Console.WriteLine("Uploading file in chunks...");
//            await UploadFileInChunks(uploadUri, filePath);

//            // Commit upload
//            await graphClient.DeviceAppManagement.MobileApps[appId].GraphWin32LobApp
//                .ContentVersions[contentVersionId]
//                .Files[fileContent.Id]
//                .Commit
//                .PostAsync(new MobileAppContentFileCommitParameterSet());

//            Console.WriteLine("Upload complete and committed!");
//        }

//        private static async Task UploadFileInChunks(Uri uploadUrl, string filePath, int chunkSize = 5 * 1024 * 1024)
//        {
//            using var httpClient = new HttpClient();
//            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

//            long totalBytes = fileStream.Length;
//            long bytesSent = 0;
//            byte[] buffer = new byte[chunkSize];

//            while (bytesSent < totalBytes)
//            {
//                int bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
//                var rangeStart = bytesSent;
//                var rangeEnd = bytesSent + bytesRead - 1;

//                using var content = new ByteArrayContent(buffer, 0, bytesRead);
//                content.Headers.ContentRange = new ContentRangeHeaderValue(rangeStart, rangeEnd, totalBytes);
//                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

//                var response = await httpClient.PutAsync(uploadUrl, content);
//                response.EnsureSuccessStatusCode();

//                bytesSent += bytesRead;
//                Console.WriteLine($"Uploaded {bytesSent}/{totalBytes} bytes...");
//            }
//        }
//        public static async Task AssignToGroupAsync(GraphServiceClient graphClient, string appId, string groupId)
//        {
//            var assignment = new MobileAppAssignment
//            {
//                Intent = InstallIntent.Required,
//                Target = new GroupAssignmentTarget { GroupId = groupId }
//            };

//            await graphClient.DeviceAppManagement.MobileApps[appId].Assignments.Request().AddAsync(assignment);
//            Console.WriteLine("App assigned to group successfully!");
//        }
//    }




//}
