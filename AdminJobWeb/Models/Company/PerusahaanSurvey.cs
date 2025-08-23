using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace AdminJobWeb.Models.Company
{
    public class PerusahaanSurvey
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public ObjectId _id { get; set; }
        public ObjectId _idPerusahaan { get; set; }
        public ObjectId _idSurveyer { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime? dateSurvey { get; set; }

        [RegularExpression("^(Accept|Reject|Pending|Process)$", ErrorMessage = "Status Unknown")]
        public string? statusSurvey { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime? statusDate { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime addTime { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime? updTime { get; set; }
    }
}
