using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace AdminJobWeb.Controllers
{    
    public class AccountController : Controller
    {
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        public AccountController(IMongoClient mongoClient,IConfiguration configuration)
        {
            this.congfiguration= configuration;
            var databaseName = configuration["MonggoDbSettings:DatabaseName"];
            this._database = mongoClient.GetDatabase(databaseName);
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
