using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using AdminJobWeb.Models.Account;

namespace AdminJobWeb.Models.Company
{
    public class PerusahaanSurvey
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public ObjectId _id { get; set; }
        [BsonElement("idPerusahaan")]
        public ObjectId idPerusahaan { get; set; }
        [BsonElement("idSurveyer")]
        public ObjectId idSurveyer { get; set; }

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

        public List<Company>? company { get; set; }
        public List<surveyers>? surveyer { get; set; }

        public string? alasanReject { get; set; }
    }
}
