using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Account
{
    public class admin
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string _id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public int roleAdmin { get; set; }
        public int loginCount { get; set; }
        public DateTime lastLogin { get; set; }
        public string statusAccount { get; set; }
        public DateTime addTime { get; set; }
        public DateTime updateTime { get; set; }
    }
}
