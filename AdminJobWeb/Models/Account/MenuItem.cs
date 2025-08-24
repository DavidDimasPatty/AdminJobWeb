using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Account
{
    public class MenuItem
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string _id { get; set; }
        public string menu { get; set; }
        public string subMenu { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public string icon { get; set; }
    }
}
