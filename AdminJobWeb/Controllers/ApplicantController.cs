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
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;

namespace AdminJobWeb.Controllers
{
    public class ApplicantController : Controller
    {
        private readonly IMongoCollection<Applicant> _applicantCollection;
        private readonly IMongoCollection<Experience> _experienceCollection;
        private readonly IMongoCollection<Education> _educationCollection;
        private readonly IMongoCollection<Skill> _skillCollection;
        private readonly IMongoCollection<Certificate> _certificateCollection;
        private readonly IMongoCollection<Organization> _organizationCollection;
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        private readonly IMemoryCache _cache;
        private string databaseName;
        private string applicantCollectionName;
        private string experienceCollectionName;
        private string educationCollectionName;
        private string skillCollectionName;
        private string certificateCollectionName;
        private string organizationCollectionName;
        private GeneralFunction1 aid;
        private TracelogApplicant trace;

        public ApplicantController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            this.congfiguration = configuration;
            this.databaseName = configuration["MonggoDbSettings:DatabaseName"]!;
            this._database = mongoClient.GetDatabase(this.databaseName);
            this.applicantCollectionName = configuration["MonggoDbSettings:Collections:usersCollection"]!;
            this._applicantCollection = _database.GetCollection<Applicant>(this.applicantCollectionName);

            this.experienceCollectionName = configuration["MonggoDbSettings:Collections:experienceCollection"]!;
            this._experienceCollection = _database.GetCollection<Experience>(this.experienceCollectionName);

            this.educationCollectionName = configuration["MonggoDbSettings:Collections:educationCollection"]!;
            this._educationCollection = _database.GetCollection<Education>(this.educationCollectionName);

            this.skillCollectionName = configuration["MonggoDbSettings:Collections:skillCollection"]!;
            this._skillCollection = _database.GetCollection<Skill>(this.skillCollectionName);

            this.certificateCollectionName = configuration["MonggoDbSettings:Collections:certificateCollection"]!;
            this._certificateCollection = _database.GetCollection<Certificate>(this.certificateCollectionName);

            this.organizationCollectionName = configuration["MonggoDbSettings:Collections:organizationCollection"]!;
            this._organizationCollection = _database.GetCollection<Organization>(this.organizationCollectionName);

            this.aid = new GeneralFunction1();
            this.trace = new TracelogApplicant();
        }

        //Aplicant

        [HttpGet]
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
                List<Applicant?> applicants = await _applicantCollection.Find(_ => true).ToListAsync();
                trace.WriteLog($"User {adminLogin} success get data applicant :{applicants.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View(applicants);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> BlockApplicant(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant";
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
                trace.WriteLog($"User {adminLogin} start Block Applicant, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updTime, DateTime.UtcNow);
                var result = await _applicantCollection.UpdateOneAsync(filter, update);
                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Block Applicant {id.ToString()} error : Data Applicant Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Block Applicant";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Applicant Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                trace.WriteLog($"User {adminLogin} success Block Applicant applicant :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Block Applicant";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Block Applicant {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Block Applicant";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateApplicant(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant";
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
                trace.WriteLog($"User {adminLogin} start Activate Applicant, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);
                var result = await _applicantCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Activate Applicant {id.ToString()} error : Data Applicant Tidak Ditemukan, from : {pathUrl}");
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    // return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Activate Applicant";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Applicant Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Applicant";
                trace.WriteLog($"User {adminLogin} success Activate Applicant :{id.ToString()}, from : {pathUrl}");
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Activate Applicant, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Activate Applicant";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
                // Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteApplicant(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant";
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
                trace.WriteLog($"User {adminLogin} start Delete Applicant, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var result = await _applicantCollection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Delete Applicant {id.ToString()} error : Data Applicant Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Delete Applicant";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Applicant Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Applicant";
                trace.WriteLog($"User {adminLogin} success block Delete Applicant :{id.ToString()}, from : {pathUrl}");
                return RedirectToAction("Index");

            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Delete Applicant, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Delete Applicant";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }


        //Experience
        [HttpGet]
        public async Task<ActionResult> Experience()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            if (HttpContext.Session.GetInt32("role") != 2 || string.IsNullOrEmpty(adminLogin))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                List<Experience?> experiences = await _experienceCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {experiences.Count} admin users from the database.");
                trace.WriteLog($"User {adminLogin} success get data Experience :{experiences.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Experience/Experience", experiences);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddExperience(string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            string linkTemp = "/Applicant/Experience";
            if (!aid.checkPrivilegeSession(HttpContext.Session.GetString("username"), linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Experience/AddExperience");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditExperience(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            string linkTemp = "/Applicant/Experience";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl} with data : {id.ToString()}");
                Experience experience = await _experienceCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                Debug.WriteLine($"Retrieved {experience.ToString()} admin users from the database.");
                trace.WriteLog($"User {adminLogin} success get data experience :{experience.ToString()}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Experience/EditExperience", experience);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddExperience(Experience data, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Experience";
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
                trace.WriteLog($"User {adminLogin} start Add Experience, {pathUrl} with data : {data.ToString()}");

                var regex = new Regex(
                      pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                      options: RegexOptions.None,
                      matchTimeout: TimeSpan.FromSeconds(1)
                  );

                if (!regex.IsMatch(data.namaPerusahaan ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Experience");
                }

                if (!regex.IsMatch(data.lokasi ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : lokasi Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "lokasi Tidak Valid";
                    return RedirectToAction("Experience");
                }

                if (!regex.IsMatch(data.industri ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : industri Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "industri Tidak Valid";
                    return RedirectToAction("Experience");
                }

                var username = HttpContext.Session.GetString("username");
                var checkEdu = await _experienceCollection
                         .Find(Builders<Experience>.Filter.Or(
                             Builders<Experience>.Filter.Eq(p => p.namaPerusahaan, data.namaPerusahaan)))
                        .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Add Experience {data.ToString()} error : Nama Experience Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Experience Sudah Ada";
                    return RedirectToAction("Experience");
                }

                var experienceInsert = new Experience
                {
                    _id = ObjectId.GenerateNewId(),
                    addId = null,
                    addTime = DateTime.Now,
                    lokasi = data.lokasi,
                    namaPerusahaan = data.namaPerusahaan,
                    industri = data.industri,
                    status = "Active"
                };

                await _experienceCollection.InsertOneAsync(experienceInsert);
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Experience";
                trace.WriteLog($"User {adminLogin} success Add Experience :{data.ToString()}, from : {pathUrl}");
                return RedirectToAction("Experience");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Add Experience, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Add Experience";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Experience");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditExperience(Experience data, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Experience";
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
                trace.WriteLog($"User {adminLogin} start Edit Experience, {pathUrl} with data : {data.ToString()}");

                var regex = new Regex(
      pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
      options: RegexOptions.None,
      matchTimeout: TimeSpan.FromSeconds(1)
  );

                if (!regex.IsMatch(data.namaPerusahaan ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Experience");
                }

                if (!regex.IsMatch(data.lokasi ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : lokasi Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "lokasi Tidak Valid";
                    return RedirectToAction("Experience");
                }

                if (!regex.IsMatch(data.industri ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : industri Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "industri Tidak Valid";
                    return RedirectToAction("Experience");
                }

                var checkEdu = await _experienceCollection
                        .Find(Builders<Experience>.Filter.And(
                            Builders<Experience>.Filter.Eq(p => p.namaPerusahaan, data.namaPerusahaan),
                            Builders<Experience>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Edit Experience {data.ToString()} error : Nama Experience Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Edit Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Experience Sudah Ada";
                    return RedirectToAction("Experience");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Experience>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Experience>.Update.Set(p => p.namaPerusahaan, data.namaPerusahaan).Set(p => p.lokasi, data.lokasi).Set(p => p.industri, data.industri);
                await _experienceCollection.UpdateOneAsync(filter, update);
                trace.WriteLog($"User {adminLogin} success Edit Experience :{data.ToString()}, from : {pathUrl}");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Experience";
                return RedirectToAction("Experience");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Edit Experience, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Edit Experience";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Experience");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteExperience(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Experience";
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
                trace.WriteLog($"User {adminLogin} start Delete Experience, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var result = await _experienceCollection.DeleteOneAsync(filter);
                if (result.DeletedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Delete Experience {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Delete Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Experience");
                }
                trace.WriteLog($"User {adminLogin} success Delete Experience :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Experience";
                return RedirectToAction("Experience");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Delete Experience, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Delete Experience";
                TempData["icon"] = "error";
                TempData["text"] = "Gagal Delete Experience";
                return RedirectToAction("Experience");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveExperience(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Experience";
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
                trace.WriteLog($"User {adminLogin} start Inactive Experience, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var update = Builders<Experience>.Update.Set(p => p.status, "Inactive");

                var result = await _experienceCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Inactive Experience {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Inactive Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Experience");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Experience";
                trace.WriteLog($"User {adminLogin} success Inactive Experience :{id.ToString()}, from : {pathUrl}");
                return RedirectToAction("Experience");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Inactive Experience, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Inactive Experience";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Experience");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateExperience(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Experience";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Activate Experience, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var update = Builders<Experience>.Update.Set(p => p.status, "Active");

                var result = await _experienceCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Activate Experience {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");

                    TempData["titlePopUp"] = "Gagal Activate Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Experience");
                }
                trace.WriteLog($"User {adminLogin} success Activate Experience :{id.ToString()}, from : {pathUrl}");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Experience";
                return RedirectToAction("Experience");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Activate Experience, {pathUrl} error : {e.Message}");

                TempData["titlePopUp"] = "Gagal Activate Experience";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Experience");
            }
        }


        //Education
        [HttpGet]
        public async Task<ActionResult> Education()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            if (HttpContext.Session.GetInt32("role") != 2 || string.IsNullOrEmpty(adminLogin))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                List<Education?> educations = await _educationCollection.Find(_ => true).ToListAsync();
                trace.WriteLog($"User {adminLogin} success get data educations :{educations.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Education/Education", educations);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddEducation(string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Education";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Education/AddEducation");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditEducation(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Education";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl} with data : {id.ToString()}");
                Education education = await _educationCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                trace.WriteLog($"User {adminLogin} success get data education :{education.ToString()}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Education/EditEducation", education);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEducation(Education data, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Education";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Add Education, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                  pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                  options: RegexOptions.None,
                  matchTimeout: TimeSpan.FromSeconds(1)
              );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Education");
                }

                if (!regex.IsMatch(data.lokasi ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : lokasi Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "lokasi Tidak Valid";
                    return RedirectToAction("Education");
                }

                var username = HttpContext.Session.GetString("username");
                var checkEdu = await _educationCollection
                         .Find(Builders<Education>.Filter.Or(
                             Builders<Education>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Add Education {data.ToString()} error : Nama  Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Education Sudah Ada";
                    return RedirectToAction("Education");
                }

                var educationInsert = new Education
                {
                    _id = ObjectId.GenerateNewId(),
                    addId = null,
                    addTime = DateTime.Now,
                    lokasi = data.lokasi,
                    nama = data.nama,
                    status = "Active"
                };

                await _educationCollection.InsertOneAsync(educationInsert);
                // return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Education";
                trace.WriteLog($"User {adminLogin} success Add Education :{data.ToString()}, from : {pathUrl}");

                return RedirectToAction("Education");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Add Education, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Add Education";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Education");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditEducation(Education data, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Education";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Edit Education, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                      pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                      options: RegexOptions.None,
                      matchTimeout: TimeSpan.FromSeconds(1)
                    );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Education");
                }

                if (!regex.IsMatch(data.lokasi ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : lokasi Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "lokasi Tidak Valid";
                    return RedirectToAction("Education");
                }
                var checkEdu = await _educationCollection
                        .Find(Builders<Education>.Filter.And(
                            Builders<Education>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Education>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Edit Education {data.ToString()} error : Nama  Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Edit Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Education Sudah Ada";
                    return RedirectToAction("Education");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Education>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Education>.Update.Set(p => p.nama, data.nama).Set(p => p.lokasi, data.lokasi);
                await _educationCollection.UpdateOneAsync(filter, update);
                //return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Education";
                trace.WriteLog($"User {adminLogin} success Edit Education :{data.ToString()}, from : {pathUrl}");

                return RedirectToAction("Education");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Edit Education, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Edit Education";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Education");
                //Debug.WriteLine(ex.Message);
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteEducation(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Education";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Delete Education, {pathUrl} with data : {id.ToString()}");

                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var result = await _educationCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Delete Education {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Delete Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Education");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Education";
                trace.WriteLog($"User {adminLogin} success Delete Education :{id.ToString()}, from : {pathUrl}");

                return RedirectToAction("Education");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Delete Education, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Add Education";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Education");
            }
        }


        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InactiveEducation(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Education";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Inactive Education, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var update = Builders<Education>.Update.Set(p => p.status, "Inactive");

                var result = await _educationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Inactive Education {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Inactive Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Education");

                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Education";
                trace.WriteLog($"User {adminLogin} success Inactive Education :{id.ToString()}, from : {pathUrl}");

                return RedirectToAction("Education");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Inactive Education, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Inactive Education";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Education");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateEducation(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Education";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Activate Education, {pathUrl} with data : {id.ToString()}");

                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var update = Builders<Education>.Update.Set(p => p.status, "Active");

                var result = await _educationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Activate Education {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Activate Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Education");
                }
                trace.WriteLog($"User {adminLogin} success Activate Education :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Education";
                return RedirectToAction("Education");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Activate Education, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Activate Education";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Education");
            }
        }



        //Skill
        [HttpGet]
        public async Task<ActionResult> Skill()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            if (HttpContext.Session.GetInt32("role") != 2 || string.IsNullOrEmpty(adminLogin))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                List<Skill?> skills = await _skillCollection.Find(_ => true).ToListAsync();
                trace.WriteLog($"User {adminLogin} success get data skills :{skills.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Skill/Skill", skills);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddSkill(string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Skill";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Skill/AddSkill");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditSkill(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Skill";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl} with data : {id.ToString()}");
                Skill skill = await _skillCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                trace.WriteLog($"User {adminLogin} success get data skill :{skill.ToString()}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Skill/EditSkill", skill);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> AddSkill(Skill data, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Skill";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Add Skill, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                  pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                  options: RegexOptions.None,
                  matchTimeout: TimeSpan.FromSeconds(1)
                );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Skill");
                }


                var username = HttpContext.Session.GetString("username");
                var checkSkill = await _skillCollection
                         .Find(Builders<Skill>.Filter.Or(
                             Builders<Skill>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkSkill > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Add Skill {data.ToString()} error : Nama Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Skill Sudah Ada";
                    return RedirectToAction("Skill");
                }

                var SkillInsert = new Skill
                {
                    _id = ObjectId.GenerateNewId(),
                    addId = null,
                    addTime = DateTime.Now,
                    nama = data.nama,
                    status = "Active"
                };

                await _skillCollection.InsertOneAsync(SkillInsert);
                trace.WriteLog($"User {adminLogin} success Add Skill :{data.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Skill";
                return RedirectToAction("Skill");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Add Skill, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Add Skill";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Skill");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditSkill(Skill data, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Skill";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Edit Skill, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                  pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                  options: RegexOptions.None,
                  matchTimeout: TimeSpan.FromSeconds(1)
                );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Skill");
                }



                var checkSkill = await _skillCollection
                        .Find(Builders<Skill>.Filter.And(
                            Builders<Skill>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Skill>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkSkill > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Edit Skill {data.ToString()} error : Nama Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Edit Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Skill Sudah Ada";
                    return RedirectToAction("Skill");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Skill>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Skill>.Update.Set(p => p.nama, data.nama);
                await _skillCollection.UpdateOneAsync(filter, update);
                trace.WriteLog($"User {adminLogin} success Edit Skill :{data.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Skill";
                return RedirectToAction("Skill");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Edit Skill, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Edit Skill";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Skill");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSkill(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Skill";
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
                trace.WriteLog($"User {adminLogin} start Delete Skill, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var result = await _skillCollection.DeleteOneAsync(filter);
                if (result.DeletedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Delete Skill {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Delete Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Skill");
                }

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Skill";
                trace.WriteLog($"User {adminLogin} success Delete Skill :{id.ToString()}, from : {pathUrl}");
                return RedirectToAction("Skill");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Delete Skill, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Delete Skill";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Skill");
            }
        }


        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InactiveSkill(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Skill";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Inactive Skill, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var update = Builders<Skill>.Update.Set(p => p.status, "Inactive");

                var result = await _skillCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Inactive Skill {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Inactive Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Skill");
                }
                trace.WriteLog($"User {adminLogin} success Inactive Skill :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Skill";
                return RedirectToAction("Skill");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Inactive Skill, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Inactive Skill";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Skill");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateSkill(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Skill";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Activate Skill, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var update = Builders<Skill>.Update.Set(p => p.status, "Active");
                var result = await _skillCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Activate Skill {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Activate Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Skill");
                }
                trace.WriteLog($"User {adminLogin} success Activate Skill :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Skill";
                return RedirectToAction("Skill");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Activate Skill, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Activate Skill";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Skill");
            }
        }


        //Organization
        [HttpGet]
        public async Task<ActionResult> Organization()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            if (HttpContext.Session.GetInt32("role") != 2 || string.IsNullOrEmpty(adminLogin))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                List<Organization?> organizations = await _organizationCollection.Find(_ => true).ToListAsync();
                trace.WriteLog($"User {adminLogin} success get data organizations :{organizations.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Organization/Organization", organizations);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddOrganization(string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Organization";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Organization/AddOrganization");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditOrganization(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Organization";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl} with data : {id.ToString()}");
                Organization organization = await _organizationCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                trace.WriteLog($"User {adminLogin} success get data organization :{organization.ToString()}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Organization/EditOrganization", organization);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddOrganization(Organization data, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Organization";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Add Organization, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                  pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                  options: RegexOptions.None,
                  matchTimeout: TimeSpan.FromSeconds(1)
                );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Organization");
                }

                if (!regex.IsMatch(data.lokasi ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : lokasi Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "lokasi Tidak Valid";
                    return RedirectToAction("Organization");
                }

                var username = HttpContext.Session.GetString("username");
                var checkOrganization = await _organizationCollection
                         .Find(Builders<Organization>.Filter.Or(
                             Builders<Organization>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkOrganization > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Add Organization {data.ToString()} error : Nama Sudah Ada, from : {pathUrl}");

                    TempData["titlePopUp"] = "Gagal Add Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Organization Sudah Ada";
                    return RedirectToAction("Organization");
                }

                var OrganizationInsert = new Organization
                {
                    _id = ObjectId.GenerateNewId(),
                    addId = null,
                    addTime = DateTime.Now,
                    nama = data.nama,
                    lokasi = data.lokasi,
                    status = "Active"

                };

                await _organizationCollection.InsertOneAsync(OrganizationInsert);
                trace.WriteLog($"User {adminLogin} success Add Organization :{data.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Add Organization, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Add Organization";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Organization");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditOrganization(Organization data, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Organization";
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
                trace.WriteLog($"User {adminLogin} start Edit Organization, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                  pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                  options: RegexOptions.None,
                  matchTimeout: TimeSpan.FromSeconds(1)
                );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Organization");
                }

                if (!regex.IsMatch(data.lokasi ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : lokasi Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "lokasi Tidak Valid";
                    return RedirectToAction("Organization");
                }

                var checkOrganization = await _organizationCollection
                        .Find(Builders<Organization>.Filter.And(
                            Builders<Organization>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Organization>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkOrganization > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Edit Organization {data.ToString()} error : Nama Sudah Ada, from : {pathUrl}");

                    TempData["titlePopUp"] = "Gagal Edit Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Organization Sudah Ada";
                    return RedirectToAction("Organization");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Organization>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Organization>.Update.Set(p => p.nama, data.nama).Set(p => p.lokasi, data.lokasi);
                await _organizationCollection.UpdateOneAsync(filter, update);
                trace.WriteLog($"User {adminLogin} success Edit Organization :{data.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Edit Organization, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Edit Organization";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Organization");

            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteOrganization(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Organization";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Delete Organization, {pathUrl} with data : {id.ToString()}");

                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var result = await _organizationCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Delete Organization {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");

                    TempData["titlePopUp"] = "Gagal Delete Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Organization");
                }
                trace.WriteLog($"User {adminLogin} success Delete Organization :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Delete Organization, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Delete Organization";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Organization");

            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InactiveOrganization(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Organization";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Inactive Organization, {pathUrl} with data : {id.ToString()}");

                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var update = Builders<Organization>.Update.Set(p => p.status, "Inactive");

                var result = await _organizationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Inactive Organization {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");

                    TempData["titlePopUp"] = "Gagal Inactive Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Organization");
                }
                trace.WriteLog($"User {adminLogin} success Inactive Organization :{id.ToString()}, from : {pathUrl}");


                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Inactive Organization, {pathUrl} error : {e.Message}");

                TempData["titlePopUp"] = "Gagal Inactive Organization";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Organization");

            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateOrganization(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;

            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Organization";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Activate Organization, {pathUrl} with data : {id.ToString()}");

                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var update = Builders<Organization>.Update.Set(p => p.status, "Active");

                var result = await _organizationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Activate Organization {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");

                    TempData["titlePopUp"] = "Gagal Activate Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Organization");
                }
                trace.WriteLog($"User {adminLogin} success Activate Organization :{id.ToString()}, from : {pathUrl}");


                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Activate Organization, {pathUrl} error : {e.Message}");


                TempData["titlePopUp"] = "Gagal Activate Organization";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Organization");
            }
        }


        //Certificate
        [HttpGet]
        public async Task<ActionResult> Certificate()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            if (HttpContext.Session.GetInt32("role") != 2 || string.IsNullOrEmpty(adminLogin))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");
                List<Certificate?> certificates = await _certificateCollection.Find(_ => true).ToListAsync();
                trace.WriteLog($"User {adminLogin} success get data certificates :{certificates.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Certificate/Certificate", certificates);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddCertificate(string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Certificate";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl}");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Certificate/AddCertificate");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditCertificate(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Certificate";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                trace.WriteLog($"User {adminLogin} start akses {pathUrl} with data : {id.ToString()}");
                Certificate certificate = await _certificateCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                trace.WriteLog($"User {adminLogin} success get data certificate :{certificate.ToString()}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                trace.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("Certificate/EditCertificate", certificate);
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddCertificate(Certificate data, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Certificate";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Add Certificate, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                  pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                  options: RegexOptions.None,
                  matchTimeout: TimeSpan.FromSeconds(1)
                );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Certificate");
                }

                if (!regex.IsMatch(data.publisher ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : publisher Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "publisher Tidak Valid";
                    return RedirectToAction("Certificate");
                }
                var username = HttpContext.Session.GetString("username");
                var checkCertificate = await _certificateCollection
                         .Find(Builders<Certificate>.Filter.And(
                             Builders<Certificate>.Filter.Eq(p => p.nama, data.nama),
                             Builders<Certificate>.Filter.Eq(p => p.publisher, data.publisher)))
                        .CountDocumentsAsync();

                if (checkCertificate > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Add Certificate {data.ToString()} error : Nama dan publisher Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Certificate Sudah Ada";
                    return RedirectToAction("Certificate");
                }

                var CertificateInsert = new Certificate
                {
                    _id = ObjectId.GenerateNewId(),
                    addId = null,
                    addTime = DateTime.Now,
                    nama = data.nama,
                    status = "Active",
                    publisher = data.publisher

                };

                await _certificateCollection.InsertOneAsync(CertificateInsert);
                trace.WriteLog($"User {adminLogin} success Add Certificate :{data.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception ex)
            {
                trace.WriteLog($"User {adminLogin} failed Add Certificate, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Add Certificate";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditCertificate(Certificate data, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Certificate";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Edit Certificate, {pathUrl} with data : {data.ToString()}");
                var regex = new Regex(
                pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(1)
              );

                if (!regex.IsMatch(data.nama ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : Nama Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Tidak Valid";
                    return RedirectToAction("Certificate");
                }

                if (!regex.IsMatch(data.publisher ?? string.Empty))
                {
                    trace.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : publisher Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "publisher Tidak Valid";
                    return RedirectToAction("Certificate");
                }

                var checkCertificate = await _certificateCollection
                        .Find(Builders<Certificate>.Filter.And(
                            Builders<Certificate>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Certificate>.Filter.Eq(p => p.publisher, data.publisher),
                            Builders<Certificate>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkCertificate > 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Edit Certificate {data.ToString()} error : Nama dan publisher Sudah Ada, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Edit Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Certificate Sudah Ada";
                    return RedirectToAction("Certificate");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Certificate>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Certificate>.Update.Set(p => p.nama, data.nama).Set(p => p.publisher, data.publisher);
                await _certificateCollection.UpdateOneAsync(filter, update);
                trace.WriteLog($"User {adminLogin} success Edit Certificate :{data.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception ex)
            {

                trace.WriteLog($"User {adminLogin} failed Edit Certificate, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Edit Certificate";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCertificate(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Certificate";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start Delete Certificate, {pathUrl} with data : {id.ToString()}");

                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var result = await _certificateCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Delete Certificate {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Delete Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Certificate Tidak Ditemukan";
                    return RedirectToAction("Certificate");
                }

                trace.WriteLog($"User {adminLogin} success Delete Certificate :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Delete Certificate, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Delete Certificate";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InactiveCertificate(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Certificate";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                trace.WriteLog($"User {adminLogin} start  Inactive Certificate, {pathUrl} with data : {id.ToString()}");
                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var update = Builders<Certificate>.Update.Set(p => p.status, "Inactive");

                var result = await _certificateCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Inactive Certificate {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Inactive Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Certificate Tidak Ditemukan";
                    return RedirectToAction("Certificate");
                }
                trace.WriteLog($"User {adminLogin} success Inactive Certificate :{id.ToString()}, from : {pathUrl}");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Inactive Certificate, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Inactive Certificate";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateCertificate(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant/Certificate";
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
                trace.WriteLog($"User {adminLogin} start  Activate Certificate, {pathUrl} with data : {id.ToString()}");

                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var update = Builders<Certificate>.Update.Set(p => p.status, "Active");

                var result = await _certificateCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    trace.WriteLog($"User {adminLogin} failed Activate Certificate {id.ToString()} error : Data Tidak Ditemukan, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Active Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Certificate Tidak Ditemukan";
                    return RedirectToAction("Certificate");
                }
                trace.WriteLog($"User {adminLogin} success Activate Certificate :{id.ToString()}, from : {pathUrl}");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Active Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception e)
            {
                trace.WriteLog($"User {adminLogin} failed Activate Certificate, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Active Certificate";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Certificate");
            }
        }

    }
}
