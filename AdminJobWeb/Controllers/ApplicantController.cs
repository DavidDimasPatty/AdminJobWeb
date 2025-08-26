using AdminJobWeb.AidFunction;
using AdminJobWeb.Models.Account;
using AdminJobWeb.Models.Applicant;
using AdminJobWeb.Models.Company;
using AdminJobWeb.Tracelog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace AdminJobWeb.Controllers
{
    public class ApplicantController : Controller
    {
        private readonly IMongoCollection<Applicant> _applicantCollection;
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        private readonly IMemoryCache _cache;
        private string databaseName;
        private string applicantCollectionName;

        public ApplicantController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            this.congfiguration = configuration;
            this.databaseName = configuration["MonggoDbSettings:DatabaseName"]!;
            this._database = mongoClient.GetDatabase(this.databaseName);
            this.applicantCollectionName = configuration["MonggoDbSettings:Collections:usersCollection"]!;
            this._applicantCollection = _database.GetCollection<Applicant>(this.applicantCollectionName);
        }


        [HttpGet]
        public async Task<ActionResult> Index()
        {
            try
            {
                List<Applicant> applicants = await _applicantCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {applicants.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View(applicants);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> BlockApplicant(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _applicantCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ActivateApplicant(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _applicantCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteApplicant(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var result = await _applicantCollection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Delete Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Delete Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

    }
}
