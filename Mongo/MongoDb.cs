using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema.Teams;
 using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Beta.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Supabase.Gotrue;
using TeamsBot.Services;

namespace TeamsBot.Mongo
{
    public class MongoDb
    {
        const string connectionUri = "mongodb+srv://aionosBot:woHChuYnUbHlyT5t@cluster0.jnrxkfx.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
        public static IMongoDatabase database { get; set; }
        public MongoDb()
        {

            // TODO:
            // Replace the placeholder connection string below with your
            // Altas cluster specifics. Be sure it includes
            // a valid username and password! Note that in a production environment,
            // you do not want to store your password in plain-text here.

            var settings = MongoClientSettings.FromConnectionString(connectionUri);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            var client = new MongoClient(settings);
            database = client.GetDatabase("aionoS");
            // The IMongoClient is the object that defines the connection to our
            // datastore (Atlas, for example)
            try
            {
                var result = database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
                database.CreateCollection("conversations");

                var usersCollection = database.GetCollection<Conversations>("conversations");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public IMongoCollection<Conversations> GetConversationsCollection()
        {
            return database.GetCollection<Conversations>("conversations");
        }
        public IMongoCollection<ServiceRequest> GetServiceRequestCollection()
        {
            return database.GetCollection<ServiceRequest>("serviceRequests");
        }
        public async Task<JArray> GetSoftwareSuiteCollection()
        {
            var _gitService = new GitService();
            var collection = await _gitService.GetGitJsonFile();
            return (JArray)collection["software"];
        }
        public async Task CreateConversationAsync(Conversations conversation)
        {
            var collection = GetConversationsCollection();
            await collection.InsertOneAsync(conversation);
        }
        public async Task<Conversations> FindConversationAsync(string TeamsUserId)
        {
            var collection = GetConversationsCollection();
            return await collection.FindAsync(Builders<Conversations>.Filter.Eq("TeamsUserId", TeamsUserId)).Result.FirstOrDefaultAsync();
        }
        public async Task CreateServiceRequestAsync(ServiceRequest serviceRequest)
        {
            var collection = GetServiceRequestCollection();
            await collection.InsertOneAsync(serviceRequest);
        }
        public async Task<ServiceRequest> FindServiceRequestAsync(string TicketNumber)
        {
            var collection = GetServiceRequestCollection();
            return await collection.FindAsync(Builders<ServiceRequest>.Filter.Eq("TicketNumber", TicketNumber)).Result.FirstOrDefaultAsync();
        }
        public async Task<List<JObject>> FindSoftwareSuiteAsync(string Id)
        {
            var _gitService = new GitService();
            var collection = await _gitService.GetGitJsonFile();
            JArray softwares = (JArray)collection["software"];
            return softwares
            .Children<JObject>()
            .Where(u => (string)u["_id"] == Id)
            .ToList();
        }
    }
}