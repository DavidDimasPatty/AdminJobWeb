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
using AdminJobWeb.AidFunction;
using System.Security.Cryptography;
using System.Text;

namespace AdminJobWeb.Controllers
{
    public class SurveyerController : Controller
    {
        private readonly IMongoCollection<surveyers> _surveyerCollection;
        private readonly IMongoCollection<KeyGenerate> _keyGenerateCollection;
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoCollection<Applicant> _applicantCollection;
        private readonly IMongoCollection<Company> _companyCollection;
        private readonly IMongoDatabase _database;
        private string databaseName;
        private string surveyerCollectionName;
        private string keyGenerateCollectionName;
        private string adminCollectionName;
        private string applicantCollectionName;
        private string companyCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;
        private readonly IMemoryCache _cache;
        private TracelogSurveyer _tracelogSurveyer;
        private GeneralFunction1 generalFunction1;
        public SurveyerController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            databaseName = configuration.GetValue<string>("MonggoDbSettings:DatabaseName")!;
            surveyerCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:surveyerCollection")!;
            keyGenerateCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:keyGenerateCollection")!;
            adminCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:adminCollection")!;
            _database = mongoClient.GetDatabase(databaseName);
            _surveyerCollection = _database.GetCollection<surveyers>(this.surveyerCollectionName);
            _keyGenerateCollection = _database.GetCollection<KeyGenerate>(this.keyGenerateCollectionName);
            _adminCollection = _database.GetCollection<admin>(adminCollectionName);
            this.applicantCollectionName = configuration["MonggoDbSettings:Collections:usersCollection"]!;
            this._applicantCollection = _database.GetCollection<Applicant>(this.applicantCollectionName);
            this.companyCollectionName = configuration["MonggoDbSettings:Collections:companiesCollection"]!;
            this._companyCollection = _database.GetCollection<Company>(this.companyCollectionName);
            appPass = configuration.GetValue<string>("Email:appPass")!;
            emailClient = configuration.GetValue<string>("Email:emailClient")!;
            linkSelf = configuration.GetValue<string>("Link:linkSelf")!;
            _tracelogSurveyer = new TracelogSurveyer();
            this.generalFunction1 = new GeneralFunction1();
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            try
            {
                _tracelogSurveyer.WriteLog("UserController Index view called");
                List<surveyers> surveyer = await _surveyerCollection.Find(_ => true).ToListAsync();

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
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");

                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult AddSurveyer()
        {
            try
            {
                return View("_Partials/_ModalCreate");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogSurveyer.WriteLog("Error in UserController Index: " + ex.Message);
                //  return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateSurveyer(surveyers data)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var keyCreateSurveyer = $"{username}_createSurveyer";
                if (_cache.TryGetValue(keyCreateSurveyer, out _))
                {
                    //return Content("<script>alert('Harap tunggu sebentar untuk create surveyer!');window.location.href='/Account/Index';</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "Harap tunggu sebentar untuk create surveyer!";
                    return RedirectToAction("Index", "Home");
                }

                var admin = await _adminCollection
                         .Find(Builders<admin>.Filter.Or(
                             Builders<admin>.Filter.Eq(p => p.username, data.username),
                             Builders<admin>.Filter.Eq(p => p.email, data.email)))
                        .CountDocumentsAsync();

                var surveyer = await _surveyerCollection
                      .Find(Builders<surveyers>.Filter.Or(
                          Builders<surveyers>.Filter.Eq(p => p.username, data.username),
                          Builders<surveyers>.Filter.Eq(p => p.email, data.email)))
                     .CountDocumentsAsync();

                var applicant = await _applicantCollection
                      .Find(Builders<Applicant>.Filter.Or(
                          Builders<Applicant>.Filter.Eq(p => p.email, data.email)))
                      .CountDocumentsAsync();

                var company = await _companyCollection
                      .Find(Builders<Company>.Filter.Or(
                          Builders<Company>.Filter.Eq(p => p.email, data.email)))
                      .CountDocumentsAsync();

                if (admin + surveyer + applicant + company > 0)
                {

                    // return Content($"<script>alert('Username atau Email Sudah Terdaftar');window.location.href='/Surveyer/Index';</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Username atau Email Sudah Terdaftar";
                    return RedirectToAction("Index");

                }

                var key = generalFunction1.GenerateRandomKey();
                string subject = $"Pembuatan Akun Surveyer";
                string usernameEmail = $"<b>Username</b> : {data.username}";
                string body = @$"<html>
                <header>
                    <h3>Link Untuk Pengisian Data Surveyer</h3>
                </header>
                <body>
                    <div>
                        Berikut merupakan link untuk pengisian data surveyer baru dengan akun:
                    <div>
                    <br/>
                    <br/>
                    <div>
                       {usernameEmail}
                    </div>
                    <br/>
                     <div>
                        <b>Link</b> : <a href='{linkSelf}/Surveyer/CreateForm?username={data.username}&key={key}'>{linkSelf}/Surveyer/CreateForm?username={data.username}&key={key}</a>
                    </div>
                    <br/>
                    <br/>
                    <div>
                        Terima Kasih,
                    </div>
                    <div>
                        IT Dev Ikodora
                    </div>
                </body>

                </html>";
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(emailClient, appPass)
                };

                using (var message = new MailMessage(emailClient, data.email)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                var keyGenerate = new KeyGenerate
                {
                    _id = ObjectId.GenerateNewId().ToString(),
                    key = key,
                    username = data.username,
                    addTime = DateTime.UtcNow,
                    used = "N"
                };

                await _keyGenerateCollection.InsertOneAsync(keyGenerate);

                var surveyerInsert = new surveyers
                {
                    _id = ObjectId.GenerateNewId(),
                    username = data.username,
                    email = data.email,
                    addTime = DateTime.UtcNow,
                    approvalTime = null,
                    lastLogin = null,
                    loginCount = 0,
                    nama = null,
                    password = null,
                    passwordExpired = null,
                    passwordLama = null,
                    saltHash = null,
                    saltHashLama = null,
                    statusAccount = "Active",
                    statusEnrole = false,
                    updateTime = DateTime.UtcNow
                };

                await _surveyerCollection.InsertOneAsync(surveyerInsert);
                _cache.Set(keyCreateSurveyer, true, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                    Size = 1
                });
                //return Content($"<script>alert('Success Add Surveyer');window.location.href='/Surveyer/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Create Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
               // Debug.WriteLine(ex.Message);
                _tracelogSurveyer.WriteLog("Error in UserController Index: " + ex.Message);
                //return Content($"<script>alert('{ex.Message}');window.location.href='/Surveyer/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Create Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<ActionResult> CreateForm(string username, string key)
        {
            var admin = await _keyGenerateCollection
           .Find(Builders<KeyGenerate>.Filter.And(
               Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
               Builders<KeyGenerate>.Filter.Eq(p => p.key, key),
                Builders<KeyGenerate>.Filter.Eq(p => p.used, "N")
           )).FirstOrDefaultAsync();

            if (admin == null)
            {
                //  return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "User Tidak Ditemukan!";
                return RedirectToAction("Index","Account");
            }

            if (admin.addTime.AddMinutes(15) < DateTime.UtcNow)
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Link Expired!";
                return RedirectToAction("Index", "Account");
            }
                //return Content("<script>alert('Link Expired!');window.location.href='/Account/Index'</script>", "text/html");

            ViewBag.username = username;
            ViewBag.key = key;
            return View("CreateNewPassword");
        }

        [HttpPost]
        public async Task<ActionResult> CreateNewPassword(string username, string nama, string password, string passwordRet, string key)
        {
            try
            {
                if (password != passwordRet)
                {
                    //return Content($"<script>alert('Password Tidak Sama!');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "Password Tidak Sama!";
                    return RedirectToAction("CreateForm", new { username=username,key=key});
                }
                surveyers surveyer = new surveyers();

                surveyer = await _surveyerCollection
                            .Find(Builders<surveyers>.Filter.Eq(p => p.username, username))
                            .FirstOrDefaultAsync();

                //admin

                if (surveyer == null)
                {
                    _tracelogSurveyer.WriteLog($"User : {username}, Failed Login, Reason: User Tidak Ditemukan");
                    //return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/LogOut'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "User Tidak Ditemukan!";
                    return RedirectToAction("CreateForm", new { username = username, key = key });
                }


                byte[] passwordSalt = [];
                byte[] passwordHash = [];
                using (var hmac = new HMACSHA512())
                {
                    passwordSalt = hmac.Key;
                    passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                }

                var filterKey = Builders<KeyGenerate>.Filter.And(
                    Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
                    Builders<KeyGenerate>.Filter.Eq(p => p.key, key)
                    );
                var updateKey = Builders<KeyGenerate>.Update.Set(p => p.used, "Y");
                var resultKey = await _keyGenerateCollection.UpdateOneAsync(filterKey, updateKey);

                var filter = Builders<surveyers>.Filter.Eq(p => p.username, username);
                var update = Builders<surveyers>.Update.
                    Set(p=>p.nama,nama).
                    Set(p => p.password, passwordHash).
                    Set(p => p.loginCount, 0).
                    Set(p => p.saltHash, passwordSalt).
                    Set(p => p.passwordExpired, DateTime.UtcNow.AddMonths(3));
                var result = await _surveyerCollection.UpdateOneAsync(filter, update);

                //return Content("<script>alert('Berhasil Create Surveyer');window.location.href='/Account/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Berhasil Create Password";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Create Surveyer!";
                return RedirectToAction("Index","Account");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                //return Content($"<script>alert('{e.Message}');window.location.href='/Surveyer/CreateForm?username={username}&key={key}'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Create Password";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("CreateForm", new { username = username, key = key });
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ApprovalNewSurveyer(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                // return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Start Approval New Surveyer");

                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var update = Builders<surveyers>.Update.Set(p => p.statusEnrole, true).Set(p => p.approvalTime, DateTime.UtcNow);

                var result = await _surveyerCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Approval New Surveyer");
                    //return Content("<script>alert('Gagal Approval New Surveyer!');window.location.href='/User/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Approve Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");

                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Approval New Surveyer");
                // return Content("<script>alert('Berhasil Approval New Surveyer!');window.location.href='/User/Index'</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Approve Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
               // Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Approval New Surveyer, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Approve Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> RejectNewSurveyer(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                // return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Start Reject New Surveyer");

                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var update = Builders<surveyers>.Update.Set(p => p.statusEnrole, false).Set(p => p.approvalTime, DateTime.UtcNow);

                var result = await _surveyerCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Reject New Surveyer");
                    // return Content("<script>alert('Gagal Reject New Surveyer!');window.location.href='/User/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Reject Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Reject New Surveyer");
                //return Content("<script>alert('Berhasil Reject New Surveyer!');window.location.href='/User/Index'</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Reject Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Reject New Surveyer, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Reject Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> BlockSurveyer(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var update = Builders<surveyers>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updateTime, DateTime.UtcNow);

                var result = await _surveyerCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    //  return Content("<script>alert('Gagal Block Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Block Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                //return Content("<script>alert('Berhasil Block Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Block Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/Surveyer/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Block Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateSurveyer(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {
                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var update = Builders<surveyers>.Update.Set(p => p.statusAccount, "Active").Set(p => p.loginCount, 0).Set(p => p.updateTime, DateTime.UtcNow);

                var result = await _surveyerCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Activate Surveyer");
                    // return Content("<script>alert('Gagal Activate Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Activate Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Activate Surveyer");
                //return Content("<script>alert('Berhasil Activate Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Activate Surveyer, Reason : {e.Message}");
                //   return Content($"<script>alert('{e.Message}');window.location.href='/Surveyer/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Activate Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }

        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteSurveyer(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {
                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var result = await _surveyerCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    _tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Delete Surveyer");
                    // return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Delete Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Delete Surveyer");
                //  return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Delete Surveyer, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Delete Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }


    }

}

