using AdminJobWeb.AidFunction;
using AdminJobWeb.Models.Account;
using AdminJobWeb.Models.Applicant;
using AdminJobWeb.Models.Company;
using AdminJobWeb.Tracelog;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;

namespace AdminJobWeb.Controllers
{
    public class UserController : Controller
    {
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoCollection<KeyGenerate> _keyGenerateCollection;
        private readonly IMongoCollection<surveyers> _surveyerCollection;
        private readonly IMongoCollection<Applicant> _applicantCollection;
        private readonly IMongoCollection<Company> _companyCollection;
        private readonly IMongoDatabase _database;

        private string databaseName;
        private string adminCollectionName;
        private string keyGenerateCollectionName;
        private string surveyerCollectionName;
        private string applicantCollectionName;
        private string companyCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;

        private TracelogUser _tracelogUser;
        private GeneralFunction1 _func;

        public UserController(IMongoClient mongoClient, IConfiguration configuration)
        {
            databaseName = configuration.GetValue<string>("MonggoDbSettings:DatabaseName")!;
            adminCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:adminCollection")!;
            keyGenerateCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:keyGenerateCollection")!;
            surveyerCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:surveyerCollection")!;
            applicantCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:usersCollection")!;
            companyCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:companiesCollection")!;

            _database = mongoClient.GetDatabase(databaseName);
            _adminCollection = _database.GetCollection<admin>(adminCollectionName);
            _keyGenerateCollection = _database.GetCollection<KeyGenerate>(keyGenerateCollectionName);
            _surveyerCollection = _database.GetCollection<surveyers>(surveyerCollectionName);
            _applicantCollection = _database.GetCollection<Applicant>(applicantCollectionName);
            _companyCollection = _database.GetCollection<Company>(companyCollectionName);

            appPass = configuration.GetValue<string>("Email:appPass")!;
            emailClient = configuration.GetValue<string>("Email:emailClient")!;
            linkSelf = configuration.GetValue<string>("Link:linkSelf")!;

            _tracelogUser = new TracelogUser();
            _func = new GeneralFunction1();
        }

        public IActionResult Index()
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
                _tracelogUser.WriteLog("UserController Index view called");

                var admins = _adminCollection.Find(_ => true).ToList();

                if (admins.Count == 0)
                {
                    _tracelogUser.WriteLog("No admin users found in the database.");
                      TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "No admin users found in the database.";
                    return RedirectToAction("Index","Home");
                }

                _tracelogUser.WriteLog($"Retrieved {admins.Count} admin users from the database.");
                Debug.WriteLine($"Retrieved {admins.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                ViewBag.link = HttpContext.Request.Path;
                return View(admins);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogUser.WriteLog("Error in UserController Index: " + ex.Message);
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult SendFormAdmin()
        {

            return PartialView("_Partials/_ModalCreate");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendFormAdmin(admin objData,string link)
        {
            string pathUrl = HttpContext.Request.Path;

            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/User";
            if (!_func.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {

                var regex2 = new Regex(
                  @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
                RegexOptions.None,
                  TimeSpan.FromSeconds(1)
                );


                if (!regex2.IsMatch(objData.email ?? string.Empty))
                {
                    _tracelogUser.WriteLog($"User {adminLogin} failed validation data {objData.ToString()} error : email Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "email Tidak Valid";
                    return RedirectToAction("Index");
                }
                var existingEmailAdmin = await _adminCollection
                    .Find(Builders<admin>.Filter.Eq(p => p.email, objData.email))
                    .CountDocumentsAsync();

                var existingEmailSurveyor = await _surveyerCollection
                    .Find(Builders<surveyers>.Filter.Eq(p => p.email, objData.email))
                    .CountDocumentsAsync();

                var existingEmailApplicant = await _applicantCollection
                    .Find(Builders<Applicant>.Filter.Eq(p => p.email, objData.email))
                    .CountDocumentsAsync();

                var existingEmailCompany = await _companyCollection
                    .Find(Builders<Company>.Filter.Eq(p => p.email, objData.email))
                    .CountDocumentsAsync();

                if (existingEmailAdmin + existingEmailSurveyor + existingEmailApplicant + existingEmailCompany > 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Email sudah memiliki akun!");
                    TempData["titlePopUp"] = "Gagal Send Form Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Email sudah memiliki akun!";
                    return RedirectToAction("Index");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Start Send Form to Create Admin Account to {objData.email}");

                var key = _func.GenerateRandomKey();
                string subject = "Form Create Akun Admin";
                string body = @$"<html>
                    <header>
                        <h3>Link Form Create Akun Admin</h3>
                    </header>
                    <body>
                        <div>
                            Berikut merupakan link untuk form create akun admin:
                        <div>
                        <br/>
                        <br/>
                         <div>
                            <b>Link</b> : <a href='{linkSelf}/User/CreateAdmin?username={objData.email}&key={key}'>{linkSelf}/username={objData.email}&key={key}</a>
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

                using (var message = new MailMessage(emailClient, objData.email)
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
                    username = objData.email,
                    addTime = DateTime.UtcNow,
                    used = "N"
                };

                await _keyGenerateCollection.InsertOneAsync(keyGenerate);

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil mengirimkan form untuk pembuatan Admin baru ke {objData.email}");
                // return Content("<script>alert('Berhasil mengirimkan link untuk pembuatan Admin baru. Silahkan hubungi user terkait untuk mengecek email!');window.location.href='/User/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil mengirimkan link untuk pembuatan Admin baru. Silahkan hubungi user terkait untuk mengecek email!";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed send form to create new admin to {objData.email}, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Send Form Admin";
                TempData["icon"] = "error";
                TempData["text"] =e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<ActionResult> CreateAdmin(string username, string key)
        {
            var admin = await _keyGenerateCollection
           .Find(Builders<KeyGenerate>.Filter.And(
               Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
               Builders<KeyGenerate>.Filter.Eq(p => p.key, key)
           )).FirstOrDefaultAsync();

            if (admin == null)
            {
                _tracelogUser.WriteLog($"User : {username}, Key Tidak Ditemukan!");
                // return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "User Tidak Ditemukan!";
                return RedirectToAction("Index","Account");
            }

            if (admin.addTime.AddMinutes(15) < DateTime.UtcNow)
            {
                _tracelogUser.WriteLog($"User : {username}, Link Expired!");
                // return Content("<script>alert('Link Expired!');window.location.href='/Account/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Link Expired!";
                return RedirectToAction("Index", "Account");
            }

            if (admin.used == "Y")
            {
                _tracelogUser.WriteLog($"User : {username}, Link Sudah Digunakan!");
                //  return Content("<script>alert('Link Sudah Digunakan!');window.location.href='/Account/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Link Sudah Digunakan!";
                return RedirectToAction("Index", "Account");
            }

            ViewBag.email = username;
            ViewBag.key = key;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> CreateAdmin([FromForm] admin dataObj, string key, string password, string passwordRet)
        {
            try
            {
                _tracelogUser.WriteLog($"User : {dataObj.email}, Start Create New Admin");
                _tracelogUser.WriteLog($"User : {dataObj.email}, Start Hit Database Admin");

                var existingUsernameAdmin = await _adminCollection
                    .Find(Builders<admin>.Filter.Eq(p => p.username, dataObj.username))
                    .CountDocumentsAsync();

                var existingEmailAdmin = await _adminCollection
                    .Find(Builders<admin>.Filter.Eq(p => p.email, dataObj.email))
                    .CountDocumentsAsync();

                var existingUsernameSurveyor = await _surveyerCollection
                    .Find(Builders<surveyers>.Filter.Eq(p => p.username, dataObj.username))
                    .CountDocumentsAsync();

                var existingEmailSurveyor = await _surveyerCollection
                    .Find(Builders<surveyers>.Filter.Eq(p => p.email, dataObj.email))
                    .CountDocumentsAsync();

                var existingEmailApplicant = await _applicantCollection
                    .Find(Builders<Applicant>.Filter.Eq(p => p.email, dataObj.email))
                    .CountDocumentsAsync();

                var existingEmailCompany = await _companyCollection
                    .Find(Builders<Company>.Filter.Eq(p => p.email, dataObj.email))
                    .CountDocumentsAsync();

                // Input validation
                if (string.IsNullOrEmpty(dataObj.username) || string.IsNullOrEmpty(dataObj.email))
                {
                    _tracelogUser.WriteLog($"User : {dataObj.email}, Data Tidak Boleh Kosong!");
                   // return Content($"<script>alert('Data Tidak Boleh Kosong!');window.location.href='/User/CreateAdmin?username={dataObj.email}&key={key}'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Data Tidak Boleh Kosong!";
                    return RedirectToAction("CreateAdmin", new { username= dataObj.email ,key=key});
                }

                if (existingUsernameAdmin + existingUsernameSurveyor > 0)
                {
                    _tracelogUser.WriteLog($"User : {dataObj.email}, Username Sudah Ada!");
                    // return Content($"<script>alert('Username Sudah Ada!');window.location.href='/User/CreateAdmin?username={dataObj.email}&key={key}'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Username Sudah Ada!";
                    return RedirectToAction("CreateAdmin", new { username = dataObj.email, key = key });
                }

                if (!string.IsNullOrEmpty(dataObj.email) && !dataObj.email.Contains("@"))
                {
                    _tracelogUser.WriteLog($"User : {dataObj.email}, Email Tidak Valid!");
                    // return Content($"<script>alert('Email Tidak Valid!');window.location.href='/User/CreateAdmin?username={dataObj.email}&key={key}'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Email Tidak Valid!";
                    return RedirectToAction("CreateAdmin", new { username = dataObj.email, key = key });
                }

                if (existingEmailAdmin + existingEmailSurveyor + existingEmailApplicant + existingEmailCompany > 0)
                {
                    _tracelogUser.WriteLog($"User : {dataObj.email}, Email Sudah Ada!");
                    //return Content($"<script>alert('Email Sudah Ada!');window.location.href='/User/CreateAdmin?username={dataObj.email}&key={key}'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Email Sudah Ada!";
                    return RedirectToAction("CreateAdmin", new { username = dataObj.email, key = key });
                }

                if (password != passwordRet)
                {
                    _tracelogUser.WriteLog($"User : {dataObj.email}, Password Tidak Sama!");
                    // return Content($"<script>alert('Password Tidak Sama!');window.location.href='/User/CreateAdmin?username={dataObj.email}&key={key}'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Create Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Password Tidak Sama!";
                    return RedirectToAction("CreateAdmin", new { username = dataObj.email, key = key });
                }

                byte[] passwordSalt = [];
                byte[] passwordHash = [];
                using (var hmac = new HMACSHA512())
                {
                    passwordSalt = hmac.Key;
                    passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                }

                dataObj.loginCount = 0;
                dataObj.roleAdmin = 2; // Default Role Admin
                dataObj.statusAccount = "Active";
                dataObj.lastLogin = DateTime.MinValue;
                dataObj.addTime = DateTime.UtcNow;
                dataObj.updateTime = DateTime.UtcNow;
                dataObj.approvalTime = DateTime.MinValue;
                dataObj.statusEnrole = false;
                dataObj.password = passwordHash;
                dataObj.saltHash = passwordSalt;
                dataObj.passwordExpired = DateTime.UtcNow.AddDays(90); // Set password expiration to 90 days from now
                dataObj.passwordLama = passwordHash; // Store the initial password hash
                dataObj.saltHashLama = passwordSalt; // Store the initial salt hash

                await _adminCollection.InsertOneAsync(dataObj);

                // Mark the key as used
                var filter = Builders<KeyGenerate>.Filter.And(
                    Builders<KeyGenerate>.Filter.Eq(p => p.username, dataObj.email),
                    Builders<KeyGenerate>.Filter.Eq(p => p.key, key)
                );
                var update = Builders<KeyGenerate>.Update.Set(p => p.used, "Y");
                await _keyGenerateCollection.UpdateOneAsync(filter, update);

                _tracelogUser.WriteLog($"User : {dataObj.email}, Berhasil Create New Admin {dataObj.username}");
                // return Content("<script>alert('Berhasil membuat admin baru. Mohon hubungi Super Admin untuk approval akun Anda!');window.location.href='/Account/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Berhasil Create Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil membuat admin baru. Mohon hubungi Super Admin untuk approval akun Anda!";
                return RedirectToAction("Index", "Account");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {dataObj.username}, Failed Create New Admin, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/Account/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Create Admin";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index","Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ApprovalNewAdmin(string id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/User";
            if (!_func.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Approval New Admin");

                var filter = Builders<admin>.Filter.Eq(p => p._id, id);
                var update = Builders<admin>.Update.Set(p => p.statusEnrole, true).Set(p => p.approvalTime, DateTime.UtcNow);

                var result = await _adminCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Gagal Approval New Admin, Data tidak ditemukan");
                    // return Content("<script>alert('Gagal Approval New Admin!');window.location.href='/User/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Approve Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Gagal Approval New Admin!";
                    return RedirectToAction("Index");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Approval New Admin");
                //return Content("<script>alert('Berhasil Approval New Admin!');window.location.href='/User/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Berhasil Approve Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Approval New Admin!";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Approval New Admin, Reason : {e.Message}");
                //return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Approve Admin";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> RejectNewAdmin(string id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/User";
            if (!_func.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Reject New Admin");

                var filter = Builders<admin>.Filter.Eq(p => p._id, id);
                var update = Builders<admin>.Update.Set(p => p.statusEnrole, false).Set(p => p.approvalTime, DateTime.UtcNow);

                var result = await _adminCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Gagal Reject New Admin, Data tidak ditemukan");
                    // return Content("<script>alert('Gagal Reject New Admin!');window.location.href='/User/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Reject Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Gagal Reject New Admin!";
                    return RedirectToAction("Index");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Reject New Admin");
                //return Content("<script>alert('Berhasil Reject New Admin!');window.location.href='/User/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Berhasil Reject Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Reject New Admin!";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Reject New Admin, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Reject Admin";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> BlockAdmin(string id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/User";
            if (!_func.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Block Admin");

                var filter = Builders<admin>.Filter.Eq(p => p._id, id);
                var update = Builders<admin>.Update.Set(p => p.statusAccount, "Block");

                var result = await _adminCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Gagal Block Admin, Data tidak ditemukan");
                    //return Content("<script>alert('Gagal Block Admin!');window.location.href='/User/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Block Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Gagal Block Admin!";
                    return RedirectToAction("Index");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Block Admin");
                // return Content("<script>alert('Berhasil Block Admin!');window.location.href='/User/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Berhasil Block Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Block Admin!";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Block Admin, Reason : {e.Message}");
                //  return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Block Admin";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateAdmin(string id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/User";
            if (!_func.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Activate Admin");

                var filter = Builders<admin>.Filter.Eq(p => p._id, id);
                var update = Builders<admin>.Update.Set(p => p.statusAccount, "Active");

                var result = await _adminCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Gagal Activate Admin, Data tidak ditemukan");
                    //return Content("<script>alert('Gagal Activate Admin!');window.location.href='/User/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Activate Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Gagal Activate Admin!";
                    return RedirectToAction("Index");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Activate Admin");
                // return Content("<script>alert('Berhasil Activate Admin!');window.location.href='/User/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Berhasil Activate Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Activate Admin!";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Activate Admin, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Activate Admin";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteAdmin(string id,string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/User";
            if (!_func.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Delete Admin");

                var filter = Builders<admin>.Filter.Eq(p => p._id, id);
                var result = await _adminCollection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Gagal Delete Admin, Data tidak ditemukan");
                    // return Content("<script>alert('Gagal Delete Admin!');window.location.href='/User/Index'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Delete Admin";
                    TempData["icon"] = "error";
                    TempData["text"] = "Gagal Delete Admin!";
                    return RedirectToAction("Index");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Delete Admin");
                //return Content("<script>alert('Berhasil Delete Admin!');window.location.href='/User/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Berhasil Delete Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Delete Admin!";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Delete Admin, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Delete Admin";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
