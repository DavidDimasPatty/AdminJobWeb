using AdminJobWeb.Models.Account;
using AdminJobWeb.Tracelog;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Diagnostics;
using System.Security.Cryptography;

namespace AdminJobWeb.Controllers
{
    public class UserController : Controller
    {
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoDatabase _database;

        private string databaseName;
        private string adminCollectionName;

        private TracelogUser _tracelogUser;

        public UserController(IMongoClient mongoClient, IConfiguration configuration)
        {
            databaseName = configuration.GetValue<string>("MonggoDbSettings:DatabaseName")!;
            adminCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:adminCollection")!;

            _database = mongoClient.GetDatabase(databaseName);
            _adminCollection = _database.GetCollection<admin>(adminCollectionName);

            _tracelogUser = new TracelogUser();
        }

        public IActionResult Index()
        {
            try
            {
                _tracelogUser.WriteLog("UserController Index view called");

                // Retrieve all admin users from the database
                var admins = _adminCollection.Find(_ => true).ToList();

                if (admins.Count == 0)
                {
                    _tracelogUser.WriteLog("No admin users found in the database.");
                    Debug.WriteLine("No admin users found in the database.");
                    return Content("<script>alert('No admin users found in the database.');window.location.href='/Home/Index';</script>", "text/html");
                }

                _tracelogUser.WriteLog($"Retrieved {admins.Count} admin users from the database.");
                Debug.WriteLine($"Retrieved {admins.Count} admin users from the database.");

                return View(admins);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogUser.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Account/Index';</script>", "text/html");
            }
        }
    }
}
