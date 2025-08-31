using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Supabase.Gotrue;
using TeamsBot.Services.Interfaces;

namespace TeamsBot.Services
{
    public class IntuneService:IIntuneService
    {
        string tenantId = "13345921-c174-438f-9a21-1a76064a1a11";
        string clientId = "0b0dd3a1-d1da-4cea-b4a9-6f1ac5584454";
        string clientSecret = "p648Q~9DGG32R_Q2TLE0gyn_AmtkeXzKQl8ARdij";
        public IntuneService() {
       
        }
        public async Task<string> GetAccessTokenAsync()
        {

    
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

           return token.Token;
            
        }
        public async Task<string> PushSoftware(string userId)
        {
            try
            {

            
            // 🔹 Replace with your values
            string tenantId = "13345921-c174-438f-9a21-1a76064a1a11";
            string clientId = "0b0dd3a1-d1da-4cea-b4a9-6f1ac5584454";
            string clientSecret = "p648Q~9DGG32R_Q2TLE0gyn_AmtkeXzKQl8ARdij";
            string deviceName = "AINHYDLP4359412";       // device to target
            string appId = "cdb5b3e7-d85a-4033-ac55-b764c519ef0f";           // Intune app ID (Win32LobApp/MSI/etc.)

                // 🔹 Authenticate
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

                var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
                //var token = await GetAccessTokenAsync(); // must return a valid token

                //var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(request =>
                //{
                //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                //    return Task.CompletedTask;
                //}));


                // -------------------------------------------------------
                // 1️⃣ Find the device in Intune
                // -------------------------------------------------------
                var Alldevices = await graphClient.DeviceManagement.ManagedDevices.GetAsync();
                var device = Alldevices.Value.FirstOrDefault(x => x.UserId == userId);
                //var devices = await graphClient.DeviceManagement.ManagedDevices
                //.GetAsync(req =>
                //{
                //    req.QueryParameters.Filter = $"userId eq '{userId}'";
                //});

                if (device is null)
            {
                Console.WriteLine($"❌ Device {deviceName} not found.");
                //return;
            }

           
            Console.WriteLine($"✅ Found device {device.DeviceName}, Id = {device.AzureADDeviceId}");

            // -------------------------------------------------------
            // 2️⃣ Check if group already exists for this device
            // -------------------------------------------------------
            string groupPrefix = $"AppDeployment-{device.DeviceName}";
            Group existingGroup = null;

            var existingGroups = await graphClient.Groups
                .GetAsync(req =>
                {
                    req.QueryParameters.Filter = $"startsWith(displayName,'{groupPrefix}')";
                });

            if (existingGroups?.Value?.Count > 0)
            {
                existingGroup = existingGroups.Value[0];
                Console.WriteLine($"🔄 Reusing existing group: {existingGroup.DisplayName}");
            }
            else
            {
                // -------------------------------------------------------
                // 3️⃣ Create new security group if none exists
                // -------------------------------------------------------
                string groupName = $"{groupPrefix}-{Guid.NewGuid()}";

                var newGroup = new Group
                {
                    DisplayName = groupName,
                    Description = $"Dynamic group for app deployment to {device.DeviceName}",
                    MailEnabled = false,
                    MailNickname = Guid.NewGuid().ToString(),
                    SecurityEnabled = true
                };

                existingGroup = await graphClient.Groups.PostAsync(newGroup);
                Console.WriteLine($"✅ Created new group {existingGroup.DisplayName}, Id = {existingGroup.Id}");
            }

            // -------------------------------------------------------
            // 4️⃣ Add the device as a member (skip if already present)
            // -------------------------------------------------------
            bool alreadyMember = false;
            var members = await graphClient.Groups[existingGroup.Id].Members.GetAsync();

            if (members?.Value != null)
            {
                foreach (var member in members.Value)
                {
                    if (member.Id == device.UserId.ToString())
                    {
                        alreadyMember = true;
                        break;
                    }
                }
            }

            if (!alreadyMember)
            {
                  
                    await graphClient.Groups[existingGroup.Id].Members.Ref
                    .PostAsync(new ReferenceCreate
                    {
                        OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{device.UserId}"
                    });

            }

            // -------------------------------------------------------
            // 5️⃣ Assign the app to the group (skip if already assigned)
            // -------------------------------------------------------
            var assignments = await graphClient.DeviceAppManagement.MobileApps[appId].Assignments.GetAsync();
            bool alreadyAssigned = false;

            if (assignments?.Value != null)
            {
                foreach (var assign in assignments.Value)
                {
                    if (assign.Target is GroupAssignmentTarget groupTarget &&
                        groupTarget.GroupId == existingGroup.Id)
                    {
                        alreadyAssigned = true;
                        break;
                    }
                }
            }

            if (!alreadyAssigned)
            {
                var assignment = new MobileAppAssignment
                {
                    Intent = InstallIntent.Required,
                    Target = new GroupAssignmentTarget
                    {
                        GroupId = existingGroup.Id
                    }
                };

                await graphClient.DeviceAppManagement.MobileApps[appId].Assignments
                    .PostAsync(assignment);

                Console.WriteLine($"✅ Assigned app {appId} to group {existingGroup.DisplayName}");
            }
            else
            {
                Console.WriteLine($"ℹ️ App already assigned to group {existingGroup.DisplayName}");
            }
            return "Done";
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
