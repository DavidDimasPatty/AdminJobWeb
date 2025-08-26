using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace AdminJobWeb.Models.Company
{
    public class PerusahaanAdmin
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public ObjectId _id { get; set; }
        [BsonElement("idPerusahaanSurvey")]
        public ObjectId? idPerusahaanSurvey { get; set; }
        [BsonElement("idAdmin")]
        public ObjectId? idAdmin { get; set; }

        [RegularExpression("^(Accept|Reject|Pending)$", ErrorMessage = "Status Unknown")]
        public string? status { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime? statusDate { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime addTime { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime? updTime { get; set; }
    }
}
