using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace AdminJobWeb.Models.Company
{
    public class Company
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string nama { get; set; }
        public string alamat { get; set; }
        public string domain { get; set; }
        public DateTime addTime { get; set; }
        public DateTime? updTime { get; set; }
        public string noTelp { get; set; }
        public DateTime? lastLogin { get; set; }
        public string email { get; set; }

        [RegularExpression("^(Active|Block|Inactive)$", ErrorMessage = "statusAccount hanya Active/Block/Inactive.")]
        public string statusAccount { get; set; }
    }
}
