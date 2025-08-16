using AdminJobWeb.Models.Account;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace AdminJobWeb.Controllers
{    
    public class AccountController : Controller
    {
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        private string databaseName;
        private string adminCollectionName;
        public AccountController(IMongoClient mongoClient,IConfiguration configuration)
        {
            this.congfiguration= configuration;
            this.databaseName = configuration["MonggoDbSettings:DatabaseName"]!;
            this._database = mongoClient.GetDatabase(this.databaseName);
            this.adminCollectionName = configuration["MonggoDbSettings:Collections:adminCollection"];
            this._adminCollection = _database.GetCollection<admin>(this.adminCollectionName);
        }

        [HttpGet]
        public IActionResult Index()
        {

            return View("Login");
        }

        [HttpPost]
        public async Task<ActionResult> Login(string username,string password)
        {
            return RedirectToAction("Index", "Home");
        }
        
    }
}
