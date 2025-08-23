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
        public async Task<ActionResult> Index()
        {
            try
            {
                _tracelogValidasi.WriteLog("UserController Index view called");
                var result = await _perusahaanSurveyCollection.Aggregate()
                          .Lookup("projects", "projectId", "_id", @as: "project")
                          .Unwind("project")
                          .Lookup("customers", "project.customerId", "_id", @as: "customer")
                          .Unwind("customer")
                          .Project(new BsonDocument
                          {
                            { "_id", 1 },
                            { "name", 1 },
                            { "projectName", "$project.projectName" },
                            { "customerName", "$customer.customerName" }
                          })
                          .ToListAsync();
                return View(surveyer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }
    }
}
