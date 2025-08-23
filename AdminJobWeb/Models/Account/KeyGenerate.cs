using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Account
{
    public class KeyGenerate
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string _id { get; set; }
        public string username { get; set; }
        public string key { get; set; }
        public DateTime addTime { get; set; }
        public string used { get; set; }
    }
}
