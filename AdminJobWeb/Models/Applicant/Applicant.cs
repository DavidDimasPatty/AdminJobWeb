using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace AdminJobWeb.Models.Applicant
{
    [BsonIgnoreExtraElements]
    public class Applicant
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public ObjectId _id { get; set; }
        public string nama { get; set; }
        public string email { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime tanggalLahir { get; set; }
        public string tempatLahir { get; set; }

        [RegularExpression("^(L|P)$", ErrorMessage = "jenisKelamin hanya L atau P.")]
        public string jenisKelamin { get; set; }
        public DateTime? lastLogin { get; set; }

        [RegularExpression("^(Active|Block)$", ErrorMessage = "statusAccount hanya Active/Block.")]
        public string statusAccount { get; set; }
        public DateTime addTime { get; set; }
        public DateTime? updTime { get; set; }
        public string noTelp { get; set; }
    }
}

