using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Account
{
    public class Privilege
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public int roleId { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string menuId { get; set; }
        public string loginAs { get; set; }
    }
}
