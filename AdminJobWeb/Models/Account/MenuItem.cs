using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Account
{
    public class MenuItem
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string menu { get; set; }
        public string subMenu { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public string icon { get; set; }
    }
}
