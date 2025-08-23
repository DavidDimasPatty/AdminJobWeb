using AdminJobWeb.Models.Account;
using AdminJobWeb.Tracelog;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Diagnostics;

namespace AdminJobWeb.Controllers
{
    public class SurveyerController : Controller
    {
        private readonly IMongoCollection<surveyers> _surveyerCollection;
        private readonly IMongoCollection<KeyGenerate> _keyGenerateCollection;
        private readonly IMongoDatabase _database;
        private string databaseName;
        private string surveyerCollectionName;
        private string keyGenerateCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;

        private TracelogSurveyer _tracelogSurveyer;
        public SurveyerController(IMongoClient mongoClient, IConfiguration configuration)
        {
            databaseName = configuration.GetValue<string>("MonggoDbSettings:DatabaseName")!;
            surveyerCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:surveyerCollection")!;
            keyGenerateCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:keyGenerateCollection")!;
            _database = mongoClient.GetDatabase(databaseName);
            _surveyerCollection = _database.GetCollection<surveyers>(this.surveyerCollectionName);
            _keyGenerateCollection = _database.GetCollection<KeyGenerate>(this.keyGenerateCollectionName);
            appPass = configuration.GetValue<string>("Email:appPass")!;
            emailClient = configuration.GetValue<string>("Email:emailClient")!;
            linkSelf = configuration.GetValue<string>("Link:linkSelf")!;
            _tracelogSurveyer = new TracelogSurveyer();
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                _tracelogSurveyer.WriteLog("UserController Index view called");

                // Retrieve all admin users from the database
                List<surveyers> surveyer = await _surveyerCollection.Find(_ => true).ToListAsync();

                if (surveyer.Count == 0)
                {
                    _tracelogSurveyer.WriteLog("No admin users found in the database.");
                    Debug.WriteLine("No admin users found in the database.");
                    return Content("<script>alert('No admin users found in the database.');window.location.href='/Home/Index';</script>", "text/html");
                }

                _tracelogSurveyer.WriteLog($"Retrieved {surveyer.Count} admin users from the database.");
                Debug.WriteLine($"Retrieved {surveyer.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View(surveyer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogSurveyer.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

    }
}
