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
        }

        //Aplicant

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                List<Applicant?> applicants = await _applicantCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {applicants.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link =HttpContext.Request.Path;
                return View(applicants);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> BlockApplicant(ObjectId id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _applicantCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    // return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Block Applicant";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Applicant Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Block Applicant";
                return RedirectToAction("Index");
                //return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Block Applicant";
                TempData["icon"] = "error";
                TempData["text"] =e.Message;
                return RedirectToAction("Index");
                // Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");

            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateApplicant(ObjectId id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _applicantCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
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
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
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
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteApplicant(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Applicant";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var result = await _applicantCollection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    // return Content("<script>alert('Gagal Delete Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Delete Applicant";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Applicant Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //return Content("<script>alert('Berhasil Delete Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Applicant";
                return RedirectToAction("Index");

            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
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
            if (HttpContext.Session.GetInt32("role") != 2)
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                List<Experience?> experiences = await _experienceCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {experiences.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                return View("Experience/Experience", experiences);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddExperience(string link)
        {
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
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Experience/AddExperience");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditExperience(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
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
                Experience experience = await _experienceCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                Debug.WriteLine($"Retrieved {experience.ToString()} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Experience/EditExperience", experience);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> AddExperience(Experience data,string link)
        {
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
                var username = HttpContext.Session.GetString("username");
                var checkEdu = await _experienceCollection
                         .Find(Builders<Experience>.Filter.Or(
                             Builders<Experience>.Filter.Eq(p => p.namaPerusahaan, data.namaPerusahaan)))
                        .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    TempData["titlePopUp"] = "Gagal Add Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Experience Sudah Ada";
                    return RedirectToAction("Experience");
                    //return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
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
                return RedirectToAction("Experience");
                //return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                TempData["titlePopUp"] = "Gagal Add Experience";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Experience");

                //  Debug.WriteLine(ex.Message);
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditExperience(Experience data,string link)
        {
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
                var checkEdu = await _experienceCollection
                        .Find(Builders<Experience>.Filter.And(
                            Builders<Experience>.Filter.Eq(p => p.namaPerusahaan, data.namaPerusahaan),
                            Builders<Experience>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    TempData["titlePopUp"] = "Gagal Edit Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Experience Sudah Ada";
                    return RedirectToAction("Experience");

                    //return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Experience>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Experience>.Update.Set(p => p.namaPerusahaan, data.namaPerusahaan).Set(p => p.lokasi, data.lokasi).Set(p => p.industri, data.industri);
                await _experienceCollection.UpdateOneAsync(filter, update);
                //return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Experience";
                return RedirectToAction("Experience");
            }
            catch (Exception ex)
            {
                TempData["titlePopUp"] = "Gagal Edit Experience";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Experience");
                //Debug.WriteLine(ex.Message);
                // return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteExperience(ObjectId id, string link)
        {
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
                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var result = await _experienceCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Delete Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Experience");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Experience";
                return RedirectToAction("Experience");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Delete Experience";
                TempData["icon"] = "error";
                TempData["text"] = "Gagal Delete Experience";
                return RedirectToAction("Experience");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveExperience(ObjectId id, string link)
        {
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

                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var update = Builders<Experience>.Update.Set(p => p.status, "Inactive");

                var result = await _experienceCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Inactive Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Experience");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Experience";
                return RedirectToAction("Experience");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Inactive Experience";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Experience");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateExperience(ObjectId id, string link)
        {
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

                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var update = Builders<Experience>.Update.Set(p => p.status, "Active");

                var result = await _experienceCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Activate Experience";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Experience");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Experience";
                return RedirectToAction("Experience");
            }
            catch (Exception e)
            {
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
            if (HttpContext.Session.GetInt32("role") != 2)
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                List<Education?> educations = await _educationCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {educations.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link=HttpContext.Request.Path;
                return View("Education/Education", educations);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddEducation(string link)
        {
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
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Education/AddEducation");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditEducation(ObjectId id, string link)
        {
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
                Education education = await _educationCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                Debug.WriteLine($"Retrieved {education.ToString()} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Education/EditEducation", education);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> AddEducation(Education data, string link)
        {
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
                var username = HttpContext.Session.GetString("username");
                var checkEdu = await _educationCollection
                         .Find(Builders<Education>.Filter.Or(
                             Builders<Education>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    // return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
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
                return RedirectToAction("Education");
            }
            catch (Exception ex)
            {
                TempData["titlePopUp"] = "Gagal Add Education";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Education");
                //Debug.WriteLine(ex.Message);
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditEducation(Education data, string link)
        {
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
                var checkEdu = await _educationCollection
                        .Find(Builders<Education>.Filter.And(
                            Builders<Education>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Education>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    //return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
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
                return RedirectToAction("Education");
            }
            catch (Exception ex)
            {
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
        public async Task<ActionResult> DeleteEducation(ObjectId id, string link)
        {

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
                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var result = await _educationCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    // return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Delete Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Education");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Education";
                return RedirectToAction("Education");
                // return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                //  Debug.WriteLine(e.Message);
                // return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Add Education";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Education");
            }
        }


        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveEducation(ObjectId id, string link)
        {
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

                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var update = Builders<Education>.Update.Set(p => p.status, "Inactive");

                var result = await _educationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    // return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Inactive Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Education");

                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Education";
                return RedirectToAction("Education");
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Inactive Education";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Education");
                // Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ActivateEducation(ObjectId id, string link)
        {
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

                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var update = Builders<Education>.Update.Set(p => p.status, "Active");

                var result = await _educationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Activate Education";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Education");
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Education";
                return RedirectToAction("Education");
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                // return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Activate Education";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Education");
                //   Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }



        //Skill
        [HttpGet]
        public async Task<ActionResult> Skill()
        {
            if (HttpContext.Session.GetInt32("role") != 2)
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                List<Skill?> skills = await _skillCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {skills.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                return View("Skill/Skill", skills);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddSkill(string link)
        {
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
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Skill/AddSkill");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditSkill(ObjectId id, string link)
        {
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
                Skill skill = await _skillCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                Debug.WriteLine($"Retrieved {skill.ToString()} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Skill/EditSkill", skill);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> AddSkill(Skill data , string link)
        {
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
                var username = HttpContext.Session.GetString("username");
                var checkSkill = await _skillCollection
                         .Find(Builders<Skill>.Filter.Or(
                             Builders<Skill>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkSkill > 0)
                {
                    //   return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Skill';</script>", "text/html");
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
                //  return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Skill';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Skill";
                return RedirectToAction("Skill");
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Skill';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Add Skill";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Skill");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditSkill(Skill data, string link)
        {
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
                var checkSkill = await _skillCollection
                        .Find(Builders<Skill>.Filter.And(
                            Builders<Skill>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Skill>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkSkill > 0)
                {
                    // return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Edit Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Skill Sudah Ada";
                    return RedirectToAction("Skill");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Skill>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Skill>.Update.Set(p => p.nama, data.nama);
                await _skillCollection.UpdateOneAsync(filter, update);
                //  return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Skill";
                return RedirectToAction("Skill");
            }
            catch (Exception ex)
            {
                // Debug.WriteLine(ex.Message);
                // return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Edit Skill";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Skill");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteSkill(ObjectId id, string link)
        {
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
                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var result = await _skillCollection.DeleteOneAsync(filter);
                if (result.DeletedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Delete Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Skill");
                    //return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Skill";
                return RedirectToAction("Skill");
                //return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Delete Skill";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Skill");
                // Debug.WriteLine(e.Message);
                //return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }


        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveSkill(ObjectId id, string link)
        {
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

                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var update = Builders<Skill>.Update.Set(p => p.status, "Inactive");

                var result = await _skillCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Inactive Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Skill");
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                // return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Skill";
                return RedirectToAction("Skill");
            }
            catch (Exception e)
            {
                // Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Inactive Skill";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Skill");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateSkill(ObjectId id, string link)
        {
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

                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var update = Builders<Skill>.Update.Set(p => p.status, "Active");

                var result = await _skillCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Activate Skill";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Skill");
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    // return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Skill";
                return RedirectToAction("Skill");
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                // return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Activate Skill";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Skill");
                // Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }


        //Organization
        [HttpGet]
        public async Task<ActionResult> Organization()
        {
            if (HttpContext.Session.GetInt32("role") != 2)
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                List<Organization?> organizations = await _organizationCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {organizations.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                return View("Organization/Organization", organizations);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddOrganization(string link)
        {
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
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Organization/AddOrganization");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditOrganization(ObjectId id, string link)
        {
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
                Organization organization = await _organizationCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                Debug.WriteLine($"Retrieved {organization.ToString()} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Organization/EditOrganization", organization);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> AddOrganization(Organization data, string link)
        {
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
                var username = HttpContext.Session.GetString("username");
                var checkOrganization = await _organizationCollection
                         .Find(Builders<Organization>.Filter.Or(
                             Builders<Organization>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkOrganization > 0)
                {
                    TempData["titlePopUp"] = "Gagal Add Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Organization Sudah Ada";
                    return RedirectToAction("Organization");

                    // return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Skill';</script>", "text/html");
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
                // return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Skill';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception ex)
            {
                //  Debug.WriteLine(ex.Message);
                // return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Skill';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Add Organization";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Organization");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditOrganization(Organization data, string link)
        {
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
                var checkOrganization = await _organizationCollection
                        .Find(Builders<Organization>.Filter.And(
                            Builders<Organization>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Organization>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkOrganization > 0)
                {
                    TempData["titlePopUp"] = "Gagal Edit Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Organization Sudah Ada";
                    return RedirectToAction("Organization");
                    //  return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Organization>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Organization>.Update.Set(p => p.nama, data.nama).Set(p => p.lokasi, data.lokasi);
                await _organizationCollection.UpdateOneAsync(filter, update);
                // return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception ex)
            {
                // Debug.WriteLine(ex.Message);
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Edit Organization";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Organization");

            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteOrganization(ObjectId id, string link)
        {

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
                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var result = await _organizationCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Delete Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Organization");
                    //     return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Organization";
                return RedirectToAction("Organization");
                //  return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                //   Debug.WriteLine(e.Message);
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Delete Organization";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Organization");

            }
        }

        [HttpPost]
        public async Task<ActionResult> InactiveOrganization(ObjectId id, string link)
        {
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

                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var update = Builders<Organization>.Update.Set(p => p.status, "Inactive");

                var result = await _organizationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Inactive Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Organization");
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //  return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }


                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Organization";
                return RedirectToAction("Organization");
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Inactive Organization";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Organization");
                //  Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");

            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateOrganization(ObjectId id, string link)
        {
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

                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var update = Builders<Organization>.Update.Set(p => p.status, "Active");

                var result = await _organizationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");

                    TempData["titlePopUp"] = "Gagal Activate Organization";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Organization");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                // return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Organization";
                return RedirectToAction("Organization");
            }
            catch (Exception e)
            {
                TempData["titlePopUp"] = "Gagal Activate Organization";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Organization");
                //Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }


        //Certificate
        [HttpGet]
        public async Task<ActionResult> Certificate()
        {
            if (HttpContext.Session.GetInt32("role") != 2)
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                List<Certificate?> certificates = await _certificateCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {certificates.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                return View("Certificate/Certificate", certificates);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddCertificate(string link)
        {
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
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Certificate/AddCertificate");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditCertificate(ObjectId id, string link)
        {
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
                Certificate certificate = await _certificateCollection.Find(v => v._id == id).FirstOrDefaultAsync();
                Debug.WriteLine($"Retrieved {certificate.ToString()} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Certificate/EditCertificate", certificate);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> AddCertificate(Certificate data, string link)
        {
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
                var username = HttpContext.Session.GetString("username");
                var checkCertificate = await _certificateCollection
                         .Find(Builders<Certificate>.Filter.Or(
                             Builders<Certificate>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkCertificate > 0)
                {
                    TempData["titlePopUp"] = "Gagal Add Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Certificate Sudah Ada";
                    return RedirectToAction("Certificate");
                    // return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Skill';</script>", "text/html");
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
                //    return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Skill';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Add Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Skill';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Add Certificate";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditCertificate(Certificate data, string link)
        {
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
                var checkCertificate = await _certificateCollection
                        .Find(Builders<Certificate>.Filter.And(
                            Builders<Certificate>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Certificate>.Filter.Eq(p => p.publisher, data.publisher),
                            Builders<Certificate>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkCertificate > 0)
                {
                    //return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Edit Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Nama Certificate Sudah Ada";
                    return RedirectToAction("Certificate");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Certificate>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Certificate>.Update.Set(p => p.nama, data.nama).Set(p => p.publisher, data.publisher);
                await _certificateCollection.UpdateOneAsync(filter, update);
                //    return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception ex)
            {
                //  Debug.WriteLine(ex.Message);
                // return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Add Certificate";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteCertificate(ObjectId id, string link)
        {
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
                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var result = await _certificateCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    TempData["titlePopUp"] = "Gagal Delete Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Certificate Tidak Ditemukan";
                    return RedirectToAction("Certificate");
                    // return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                // return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception e)
            {
                //   Debug.WriteLine(e.Message);
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Delete Certificate";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveCertificate(ObjectId id, string link)
        {
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

                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var update = Builders<Certificate>.Update.Set(p => p.status, "Inactive");

                var result = await _certificateCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Inactive Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Certificate Tidak Ditemukan";
                    return RedirectToAction("Certificate");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Inactive Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Inactive Certificate";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Certificate");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateCertificate(ObjectId id, string link)
        {
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

                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var update = Builders<Certificate>.Update.Set(p => p.status, "Active");

                var result = await _certificateCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    // return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Active Certificate";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Certificate Tidak Ditemukan";
                    return RedirectToAction("Certificate");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //   return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Active Certificate";
                return RedirectToAction("Certificate");
            }
            catch (Exception e)
            {
                //   Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Active Certificate";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Certificate");
            }
        }

    }
}
