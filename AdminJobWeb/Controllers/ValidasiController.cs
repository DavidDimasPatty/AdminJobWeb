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
using System.Security.Cryptography;

namespace AdminJobWeb.Controllers
{
    public class ValidasiController : Controller
    {
        private readonly IMongoCollection<surveyers> _surveyerCollection;
        private readonly IMongoCollection<Company> _companyCollection;
        private readonly IMongoCollection<PerusahaanSurvey> _perusahaanSurveyCollection;
        private readonly IMongoDatabase _database;
        private string databaseName;
        private string surveyerCollectionName;
        private string companyCollectionName;
        private string perusahaanSurveyCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;
        private readonly IMemoryCache _cache;
        private TacelogValidasi _tracelogValidasi;
        private GeneralFunction1 generalFunction1;
        public ValidasiController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            this.databaseName = configuration.GetValue<string>("MonggoDbSettings:DatabaseName")!;
            this._database = mongoClient.GetDatabase(databaseName);
            this.surveyerCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:surveyerCollection")!;
            this._surveyerCollection = _database.GetCollection<surveyers>(this.surveyerCollectionName);
            this.companyCollectionName = configuration["MonggoDbSettings:Collections:companiesCollection"]!;
            this._companyCollection = _database.GetCollection<Company>(this.companyCollectionName);
            this.perusahaanSurveyCollectionName = configuration["MonggoDbSettings:Collections:perusahaanSurveyCollection"]!;
            this._perusahaanSurveyCollection = _database.GetCollection<PerusahaanSurvey>(this.perusahaanSurveyCollectionName);
            this.appPass = configuration.GetValue<string>("Email:appPass")!;
            this.emailClient = configuration.GetValue<string>("Email:emailClient")!;
            this.linkSelf = configuration.GetValue<string>("Link:linkSelf")!;
            this._tracelogValidasi = new TacelogValidasi();
            this.generalFunction1 = new GeneralFunction1();
        }

        [HttpGet]
        public async Task<ActionResult> ValidasiPerusahaanSurveyer()
        {
            try
            {
                _tracelogValidasi.WriteLog("UserController Index view called");
                var docs = await _perusahaanSurveyCollection.Aggregate()
                 .Lookup("companies", "idPerusahaan", "_id", "company")
                 .Lookup("Surveyers", "idSurveyer", "_id", "surveyer")    
                 .As<PerusahaanSurvey>()
                 .ToListAsync();

                ViewBag.loginAs = HttpContext.Session.GetString("loginAs");
                return View("ValidasiPerusahaanSurveyer/ValidasiPerusahaanSurveyer", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddSurveyer(ObjectId id, ObjectId idPerusahaan, string namaPerusahaan)
        {
            try
            {
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.namaPerusahaan = namaPerusahaan;
                List<surveyers> surveyer = await _surveyerCollection.Find(_ => true).ToListAsync();
                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreate",surveyer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer)
        {
            try
            {
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.idSurveyer, idSurveyer).Set(p => p.statusSurvey, "Process").Set(p => p.dateSurvey, DateTime.UtcNow).Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Berhasil Add Surveyer');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ApprovalSurveyer(ObjectId id)
        {
            try
            {
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.statusSurvey, "Accept").Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Berhasil Accept Perusahaan');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

       
        [HttpGet]
        public async Task<ActionResult> RejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer)
        {
            try
            {
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.idSurveyer = idSurveyer;
                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreateReject");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> RejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer, string alasanReject)
        {
            try
            {
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.statusSurvey, "Reject").Set(p => p.updTime, DateTime.UtcNow).Set(p=>p.alasanReject,alasanReject);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Berhasil Reject Perusahaan');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> DetailRejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer)
        {
            try
            {
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.idSurveyer = idSurveyer;

                _tracelogValidasi.WriteLog("UserController Index view called");
                List<PerusahaanSurvey> docs = await _perusahaanSurveyCollection.Aggregate()
                  .Match(Builders<PerusahaanSurvey>.Filter.Eq(x => x._id, id))
                 .Lookup("companies", "idPerusahaan", "_id", "company")
                 .Lookup("Surveyers", "idSurveyer", "_id", "surveyer")
                 .As<PerusahaanSurvey>()
                 .ToListAsync();

                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreateDetailReject", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }
    }
}
