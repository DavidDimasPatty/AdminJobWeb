using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using AdminJobWeb.Models.Account;

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
        public string? alasanReject { get; set; }
        // Attribute to ignore unknown fields during deserialization
        [BsonExtraElements]
        public BsonDocument ExtraElements { get; set; }
    }

    public class PerusahaanAdminViewModel
    {
        public PerusahaanAdmin perusahaanAdmin { get; set; }
        public PerusahaanSurvey perusahaanSurvey { get; set; }
        public Company company { get; set; }
        public surveyers surveyer { get; set; }
    }
}
