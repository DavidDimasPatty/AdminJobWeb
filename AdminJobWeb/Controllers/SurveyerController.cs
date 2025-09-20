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
using System.Text.RegularExpressions;

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
        private GeneralFunction1 aid;
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
            this.aid = new GeneralFunction1();
        }

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
                _tracelogSurveyer.WriteLog($"User {adminLogin} start akses {pathUrl}");
                List<surveyers> surveyer = await _surveyerCollection.Find(_ => true).ToListAsync();
                _tracelogSurveyer.WriteLog($"User {adminLogin} success get data surveyer :{surveyer.Count}, from : {pathUrl}");
                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                TempData["link"] = HttpContext.Request.Path.ToString();
                ViewBag.link = TempData["link"];
                _tracelogSurveyer.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View(surveyer);
            }
            catch (Exception ex)
            {
                _tracelogSurveyer.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult AddSurveyer(string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            try
            {
                _tracelogSurveyer.WriteLog($"User {adminLogin} start akses {pathUrl}");
                string linkTemp = "/Surveyer";
                if (!aid.checkPrivilegeSession(HttpContext.Session.GetString("username"), linkTemp, link))
                {
                    TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "Anda Tidak Memiliki Akses!";
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.link = link;
                _tracelogSurveyer.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("_Partials/_ModalCreate");
            }
            catch (Exception ex)
            {
                _tracelogSurveyer.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateSurveyer(surveyers data, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            try
            {
                string linkTemp = "/Surveyer";
                if (!aid.checkPrivilegeSession(HttpContext.Session.GetString("username"), linkTemp, link))
                {
                    TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "Anda Tidak Memiliki Akses!";
                    return RedirectToAction("Index", "Home");
                }

                var regex1 = new Regex(
                pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(1)
              );

                var regex2 = new Regex(
                  @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
                  RegexOptions.None,
                  TimeSpan.FromSeconds(1) 
              );

                if (!regex1.IsMatch(data.username ?? string.Empty))
                {
                    _tracelogSurveyer.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : username Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "username Tidak Valid";
                    return RedirectToAction("Index");
                }

                if (!regex2.IsMatch(data.email ?? string.Empty))
                {
                    _tracelogSurveyer.WriteLog($"User {adminLogin} failed validation data {data.ToString()} error : email Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "email Tidak Valid";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User {adminLogin} start Create Surveyer, {pathUrl} with data : {data.ToString()}");
                var username = HttpContext.Session.GetString("username");
                var keyCreateSurveyer = $"{username}_createSurveyer";
                if (_cache.TryGetValue(keyCreateSurveyer, out _))
                {
                    _tracelogSurveyer.WriteLog($"User {adminLogin} failed Create Surveyer {data.ToString()} error : Kena Throttle, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "Harap tunggu sebentar untuk create surveyer!";
                    return RedirectToAction("Index", "Home");
                }

                _tracelogSurveyer.WriteLog($"User {adminLogin} start Count Data Exist email and username for Surveyer, {pathUrl} with data : {data.ToString()}");
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
                _tracelogSurveyer.WriteLog($"User {adminLogin} succes Count Data Exist email and username for Surveyer, total = {admin + surveyer + applicant + company}, {pathUrl} with data : {data.ToString()}");

                if (admin + surveyer + applicant + company > 0)
                {
                    _tracelogSurveyer.WriteLog($"User {adminLogin} failed  Create Surveyer {data.ToString()} error : Username atau Email Sudah Terdaftar, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Create Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Username atau Email Sudah Terdaftar";
                    return RedirectToAction("Index");

                }
                _tracelogSurveyer.WriteLog($"User {adminLogin} start generate email, {pathUrl} with data : {data.ToString()}");
                var key = aid.GenerateRandomKey();
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
                _tracelogSurveyer.WriteLog($"User {adminLogin} success generate email, {pathUrl} with data : {data.ToString()}");
                _tracelogSurveyer.WriteLog($"User {adminLogin} start send email, {pathUrl} with data : {data.ToString()}");
                using (var message = new MailMessage(emailClient, data.email)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                _tracelogSurveyer.WriteLog($"User {adminLogin} success send email, {pathUrl} with data : {data.ToString()}");
                _tracelogSurveyer.WriteLog($"User {adminLogin} start insert key generate, {pathUrl} with data : {data.ToString()}");
                var keyGenerate = new KeyGenerate
                {
                    _id = ObjectId.GenerateNewId().ToString(),
                    key = key,
                    username = data.username,
                    addTime = DateTime.UtcNow,
                    used = "N"
                };
                await _keyGenerateCollection.InsertOneAsync(keyGenerate);
                _tracelogSurveyer.WriteLog($"User {adminLogin} success insert key generate, {pathUrl} with data : {data.ToString()}");
                _tracelogSurveyer.WriteLog($"User {adminLogin} start insert surveyer, {pathUrl} with data : {data.ToString()}");
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

                _tracelogSurveyer.WriteLog($"User {adminLogin} success insert surveyer, {pathUrl} with data : {data.ToString()}");
                await _surveyerCollection.InsertOneAsync(surveyerInsert);
                _cache.Set(keyCreateSurveyer, true, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                    Size = 1
                });
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Create Surveyer";
                _tracelogSurveyer.WriteLog($"User {adminLogin}, {pathUrl} success Create Surveyer");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _tracelogSurveyer.WriteLog($"User {adminLogin} failed Create Surveyer, {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Create Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<ActionResult> CreateForm(string username, string key)
        {
            string pathUrl = HttpContext.Request.Path;
            try
            {
                _tracelogSurveyer.WriteLog($"User {username} start akses {pathUrl}, with key = {key}");
                var admin = await _keyGenerateCollection
               .Find(Builders<KeyGenerate>.Filter.And(
                   Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
                   Builders<KeyGenerate>.Filter.Eq(p => p.key, key),
                    Builders<KeyGenerate>.Filter.Eq(p => p.used, "N")
               )).FirstOrDefaultAsync();

                if (admin == null)
                {
                    _tracelogSurveyer.WriteLog($"User {username} failed akses {pathUrl}, with key = {key}, error = User Tidak Ditemukan!");
                    TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "User Tidak Ditemukan!";
                    return RedirectToAction("Index", "Account");
                }

                if (admin.addTime.AddMinutes(15) < DateTime.UtcNow)
                {
                    _tracelogSurveyer.WriteLog($"User {username} failed akses {pathUrl}, with key = {key}, error = Link Expired!");
                    TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "Link Expired!";
                    return RedirectToAction("Index", "Account");
                }
                _tracelogSurveyer.WriteLog($"User {username} success akses {pathUrl}, with key = {key}");
                ViewBag.username = username;
                ViewBag.key = key;
                return View("CreateNewPassword");
            }
            catch (Exception e)
            {
                _tracelogSurveyer.WriteLog($"User {username} failed Create Surveyer, {pathUrl} error : {e.Message}");
                TempData["titlePopUp"] = "Gagal Akses Form";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateNewPassword(string username, string nama, string password, string passwordRet, string key)
        {
            string pathUrl = HttpContext.Request.Path;
            try
            {
                _tracelogSurveyer.WriteLog($"User {username} start akses {pathUrl}, with key = {key}");
                if (password != passwordRet)
                {
                    _tracelogSurveyer.WriteLog($"User {username} failed akses {pathUrl}, with key = {key}, error = Password Tidak Sama!");
                    TempData["titlePopUp"] = "Gagal Create Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "Password Tidak Sama!";
                    return RedirectToAction("CreateForm", new { username = username, key = key });
                }
                surveyers surveyer = new surveyers();
                surveyer = await _surveyerCollection
                            .Find(Builders<surveyers>.Filter.Eq(p => p.username, username))
                            .FirstOrDefaultAsync();

                if (surveyer == null)
                {
                    _tracelogSurveyer.WriteLog($"User {username} failed akses {pathUrl}, with key = {key}, error = User Tidak Ditemukan!");
                    TempData["titlePopUp"] = "Gagal Create Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "User Tidak Ditemukan!";
                    return RedirectToAction("CreateForm", new { username = username, key = key });
                }
                _tracelogSurveyer.WriteLog($"User {username} start Hash Password,{pathUrl}, with key = {key}");
                byte[] passwordSalt = [];
                byte[] passwordHash = [];
                using (var hmac = new HMACSHA512())
                {
                    passwordSalt = hmac.Key;
                    passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                }
                _tracelogSurveyer.WriteLog($"User {username} success Hash Password,{pathUrl}, with key = {key}");

                _tracelogSurveyer.WriteLog($"User {username} start update key generate,{pathUrl}, with key = {key}");
                var filterKey = Builders<KeyGenerate>.Filter.And(
                    Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
                    Builders<KeyGenerate>.Filter.Eq(p => p.key, key)
                    );
                var updateKey = Builders<KeyGenerate>.Update.Set(p => p.used, "Y");
                var resultKey = await _keyGenerateCollection.UpdateOneAsync(filterKey, updateKey);
                _tracelogSurveyer.WriteLog($"User {username} success update key generate,{pathUrl}, with key = {key}");

                _tracelogSurveyer.WriteLog($"User {username} start update surveyer,{pathUrl}, with key = {key}");
                var filter = Builders<surveyers>.Filter.Eq(p => p.username, username);
                var update = Builders<surveyers>.Update.
                    Set(p => p.nama, nama).
                    Set(p => p.password, passwordHash).
                    Set(p => p.loginCount, 0).
                    Set(p => p.saltHash, passwordSalt).
                    Set(p => p.passwordExpired, DateTime.UtcNow.AddMonths(3));
                var result = await _surveyerCollection.UpdateOneAsync(filter, update);
                _tracelogSurveyer.WriteLog($"User {username} success update surveyer,{pathUrl}, with key = {key}");

                TempData["titlePopUp"] = "Berhasil Create Password";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Create Surveyer!";
                _tracelogSurveyer.WriteLog($"User {username} Sukses Creeate New Password,{pathUrl}, with key = {key}");
                return RedirectToAction("Index", "Account");
            }
            catch (Exception e)
            {
                _tracelogSurveyer.WriteLog($"User {username} failed Create New Password, {pathUrl} error : {e.Message}");
                Debug.WriteLine(e.Message);
                TempData["titlePopUp"] = "Gagal Create Password";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("CreateForm", new { username = username, key = key });
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ApprovalNewSurveyer(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Surveyer";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
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
                    TempData["titlePopUp"] = "Gagal Approve Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");

                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Approval New Surveyer");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Approve Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                // Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Approval New Surveyer, Reason : {e.Message}");
                TempData["titlePopUp"] = "Gagal Approve Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RejectNewSurveyer(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Surveyer";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
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
                    TempData["titlePopUp"] = "Gagal Reject Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Reject New Surveyer");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Reject Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Reject New Surveyer, Reason : {e.Message}");
                TempData["titlePopUp"] = "Gagal Reject Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BlockSurveyer(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Surveyer";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Start Block Surveyer");

                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var update = Builders<surveyers>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updateTime, DateTime.UtcNow);

                var result = await _surveyerCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    TempData["titlePopUp"] = "Gagal Block Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");

                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Block Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                TempData["titlePopUp"] = "Gagal Block Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActivateSurveyer(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Surveyer";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Start Activate Surveyer");
                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var update = Builders<surveyers>.Update.Set(p => p.statusAccount, "Active").Set(p => p.loginCount, 0).Set(p => p.updateTime, DateTime.UtcNow);

                var result = await _surveyerCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Activate Surveyer");
                    TempData["titlePopUp"] = "Gagal Activate Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Activate Surveyer");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Activate Surveyer, Reason : {e.Message}");
                TempData["titlePopUp"] = "Gagal Activate Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }

        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSurveyer(ObjectId id, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Surveyer";
            if (!aid.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Start Delete Surveyer");
                var filter = Builders<surveyers>.Filter.Eq(p => p._id, id);
                var result = await _surveyerCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    _tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Delete Surveyer");
                    TempData["titlePopUp"] = "Gagal Delete Surveyer";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Ditemukan";
                    return RedirectToAction("Index");
                }

                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Delete Surveyer");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Surveyer";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Delete Surveyer, Reason : {e.Message}");
                TempData["titlePopUp"] = "Gagal Delete Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }


    }

}

