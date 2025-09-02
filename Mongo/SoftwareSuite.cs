using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace TeamsBot.Mongo
{
    public class SoftwareSuite
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("SuiteName")]
        public string SuiteName { get; set; }

        [BsonElement("ScriptName")]
        public string ScriptName { get; set; }

        [BsonElement("Category")]
        public string Category { get; set; } // e.g., "Python", "DevOps", "DataScience"

        [BsonElement("Softwares")]
        public List<SoftwareComponent> Softwares { get; set; } = new();
    }

    public class SoftwareComponent
    {
        [BsonElement("Name")]
        public string Name { get; set; } // e.g., "Python 3.12", "Anaconda", "uv"

        [BsonElement("Version")]
        public string Version { get; set; }

        [BsonElement("InstallScript")]
        public string InstallScript { get; set; } // PowerShell script as string

        [BsonElement("Dependencies")]
        public List<string> Dependencies { get; set; } = new(); // Optional
    }
}
