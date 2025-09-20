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
    public class CompanyController : Controller
    {

        private readonly IMongoCollection<Company> _companyCollection;
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        private readonly IMemoryCache _cache;
        private string databaseName;
        private string companyCollectionName;
        private GeneralFunction1 aid;
        TracelogApplicant trace;

        public CompanyController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            this.congfiguration = configuration;
            this.databaseName = configuration["MonggoDbSettings:DatabaseName"]!;
            this._database = mongoClient.GetDatabase(this.databaseName);
            this.companyCollectionName = configuration["MonggoDbSettings:Collections:companiesCollection"]!;
            this._companyCollection = _database.GetCollection<Company>(this.companyCollectionName);
            this.aid= new GeneralFunction1();
            this.trace = new TracelogApplicant();
        }


        public async Task<ActionResult> Index()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            if (HttpContext.Session.GetInt32("role") != 1 || string.IsNullOrEmpty(adminLogin))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                List<Company> companies = await _companyCollection.Find(_ => true).ToListAsync();
                trace.WriteLog($"User {adminLogin} success get data companies :{companies.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View(companies);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BlockCompany(ObjectId id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            string linkTemp = "/Company";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Block Company, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Company>.Filter.Eq(p => p._id, id);
                var update = Builders<Company>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _companyCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Block Company {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Block Company";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Company Tidak Ditemukan";
                    return RedirectToAction("Index");

                }
                trace.WriteLog($"User {adminLogin} success Block Company :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Block Company";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Block Company, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Block Company";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateCompany(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Company"; 
            string pathUrl = HttpContext.Request.Path;
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Activate Company, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Company>.Filter.Eq(p => p._id, id);
                var update = Builders<Company>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _companyCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Activate Company {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Activate Company";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Activate Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                trace.WriteLog($"User {adminLogin} success Activate Company :{id.ToString()}, from : {pathUrl}");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Company";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Activate Company, {pathUrl} error : {e.Message}");
                 TempData["titlePopUp"] = "Gagal Activate Company";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCompany(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Company";
            string pathUrl = HttpContext.Request.Path;
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Delete Company, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Company>.Filter.Eq(p => p._id, id);
                var result = await _companyCollection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Delete Company {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Delete Company";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Company Tidak Ditemukan";
                    return RedirectToAction("Index");

                }
                trace.WriteLog($"User {adminLogin} success Delete Company :{id.ToString()}, from : {pathUrl}");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Company";
                return RedirectToAction("Index");

            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Delete Company, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Delete Company";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
