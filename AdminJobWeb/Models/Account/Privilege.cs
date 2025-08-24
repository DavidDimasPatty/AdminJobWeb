using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Account
{
    public class Privilege
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string _id { get; set; }
        public int roleId { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string menuId { get; set; }
    }
}
