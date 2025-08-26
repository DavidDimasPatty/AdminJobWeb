using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Account
{
    public class surveyers
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public ObjectId _id { get; set; }
        public string? nama { get; set; }
        public string username { get; set; }
        public byte[]? password { get; set; }
        public string email { get; set; }
        public int loginCount { get; set; }
        public DateTime? lastLogin { get; set; }
        public string statusAccount { get; set; }
        public byte[]? saltHash { get; set; }
        public DateTime? addTime { get; set; }
        public DateTime? updateTime { get; set; }
        public bool statusEnrole { get; set; }
        public DateTime? approvalTime { get; set; }
        public DateTime? passwordExpired { get; set; }
        public byte[]? passwordLama { get; set; }
        public byte[]? saltHashLama { get; set; }
    }
}
