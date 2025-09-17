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
    public class CompanyController : Controller
    {

        private readonly IMongoCollection<Company> _companyCollection;
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        private readonly IMemoryCache _cache;
        private string databaseName;
        private string companyCollectionName;

        public CompanyController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            this.congfiguration = configuration;
            this.databaseName = configuration["MonggoDbSettings:DatabaseName"]!;
            this._database = mongoClient.GetDatabase(this.databaseName);
            this.companyCollectionName = configuration["MonggoDbSettings:Collections:companiesCollection"]!;
            this._companyCollection = _database.GetCollection<Company>(this.companyCollectionName);
        }


        public async Task<ActionResult> Index()
        {
            try
            {
                List<Company> companies = await _companyCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {companies.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View(companies);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> BlockCompany(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Company>.Filter.Eq(p => p._id, id);
                var update = Builders<Company>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _companyCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    // return Content("<script>alert('Gagal Block Company!');window.location.href='/Company/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Block Company";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Company Tidak Ditemukan";
                    return RedirectToAction("Index");

                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                // return Content("<script>alert('Berhasil Block Company!');window.location.href='/Company/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Block Company";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                //   Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Company/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Block Company";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ActivateCompany(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Company>.Filter.Eq(p => p._id, id);
                var update = Builders<Company>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _companyCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //   return Content("<script>alert('Gagal Activate Company!');window.location.href='/Company/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Activate Company";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Activate Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                // return Content("<script>alert('Berhasil Activate Company!');window.location.href='/Company/Index'</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Company";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Company/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Activate Company";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteCompany(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Company>.Filter.Eq(p => p._id, id);
                var result = await _companyCollection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //   return Content("<script>alert('Gagal Delete Company!');window.location.href='/Surveyer/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Delete Company";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Company Tidak Ditemukan";
                    return RedirectToAction("Index");

                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //  return Content("<script>alert('Berhasil Delete Company!');window.location.href='/Surveyer/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Company";
                return RedirectToAction("Index");

            }
            catch (Exception e)
            {
                //   Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Company/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Delete Company";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
