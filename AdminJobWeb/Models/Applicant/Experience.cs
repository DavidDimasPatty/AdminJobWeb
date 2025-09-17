using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AdminJobWeb.Models.Applicant
{
    [BsonIgnoreExtraElements]
    public class Experience
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public ObjectId? _id { get; set; }
        public string? namaPerusahaan { get; set; }
        public string? industri { get; set; }
        public string? lokasi { get; set; }
        public ObjectId? addId { get; set; }
        public DateTime? addTime { get; set; }
        public string? status { get; set; }
    }
}
