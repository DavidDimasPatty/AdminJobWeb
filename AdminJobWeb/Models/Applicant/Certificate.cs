using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace AdminJobWeb.Models.Applicant
{
    [BsonIgnoreExtraElements]
    public class Certificate
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public ObjectId? _id { get; set; }
        public string? nama { get; set; }
        public string? publisher { get; set; }
        public ObjectId? addId { get; set; }
        public DateTime? addTime { get; set; }
        public string? status { get; set; }
    }
}
