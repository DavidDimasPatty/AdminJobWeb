using AdminJobWeb.AidFunction;
using AdminJobWeb.Models.Account;
using AdminJobWeb.Models.Applicant;
using AdminJobWeb.Models.Company;
using AdminJobWeb.Tracelog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace AdminJobWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoCollection<KeyGenerate> _keyGenerateCollection;
        private readonly IMongoCollection<surveyers> _surveyerCollection;
        private readonly IMongoCollection<Applicant> _applicantCollection;
        private readonly IMongoCollection<Company> _companyCollection;
        private readonly IMongoCollection<MenuItem> _menuCollection;
        private readonly IMongoCollection<Privilege> _privilegeCollection;
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        private readonly IMemoryCache _cache;
        private string databaseName;
        private string adminCollectionName;
        private string keyGenerateCollectionName;
        private string surveyerCollectionName;
        private string applicantCollectionName;
        private string companyCollectionName;
        private string menuCollectionName;
        private string privilegeCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;
        private TracelogAccount tracelog;
        public GeneralFunction1 generalFunction1;
        public AccountController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            this.congfiguration = configuration;
            this.databaseName = configuration["MonggoDbSettings:DatabaseName"]!;
            this._database = mongoClient.GetDatabase(this.databaseName);
            this.adminCollectionName = configuration["MonggoDbSettings:Collections:adminCollection"]!;
            this._adminCollection = _database.GetCollection<admin>(this.adminCollectionName);
            this.keyGenerateCollectionName = configuration["MonggoDbSettings:Collections:keyGenerateCollection"]!;
            this._keyGenerateCollection = _database.GetCollection<KeyGenerate>(this.keyGenerateCollectionName);
            this.surveyerCollectionName = configuration["MonggoDbSettings:Collections:surveyerCollection"]!;
            this._surveyerCollection = _database.GetCollection<surveyers>(this.surveyerCollectionName);
            this.applicantCollectionName = configuration["MonggoDbSettings:Collections:usersCollection"]!;
            this._applicantCollection = _database.GetCollection<Applicant>(this.applicantCollectionName);
            this.companyCollectionName = configuration["MonggoDbSettings:Collections:companiesCollection"]!;
            this._companyCollection = _database.GetCollection<Company>(this.companyCollectionName);
            this.menuCollectionName = configuration["MonggoDbSettings:Collections:menuCollection"]!;
            this._menuCollection = _database.GetCollection<MenuItem>(this.menuCollectionName);
            this.privilegeCollectionName = configuration["MonggoDbSettings:Collections:privilegeCollection"]!;
            this._privilegeCollection = _database.GetCollection<Privilege>(this.privilegeCollectionName);
            this.appPass = configuration["Email:appPass"]!;
            this.emailClient = configuration["Email:emailClient"]!;
            this.linkSelf = configuration["Link:linkSelf"]!;
            this.tracelog = new TracelogAccount();
            this.generalFunction1 = new GeneralFunction1();
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("username") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            int? loginCount = HttpContext.Session.GetInt32("loginCount");
            var waiting = HttpContext.Session.GetString("waiting");
            if (loginCount != 0 && loginCount != null)
            {
                ViewBag.loginCount = loginCount;
                if (!string.IsNullOrEmpty(waiting))
                {
                    string? dateWrong = HttpContext.Session.GetString("dateWrong");
                    int? diffMin = HttpContext.Session.GetInt32("diffMin");
                    int? diffSec = HttpContext.Session.GetInt32("diffSec");
                    TimeSpan diff = DateTime.Parse(dateWrong!).AddMinutes((double)diffMin!).AddSeconds((double)diffSec!) - DateTime.UtcNow;
                    int diffMinRemain = diff.Minutes;
                    int diffSecRemain = diff.Seconds;
                    ViewBag.waiting = waiting;
                    ViewBag.diffMin = diffMinRemain;
                    ViewBag.diffSec = diffSecRemain;
                }
            }
            ViewBag.titlePopUp = TempData["titlePopUp"];
            ViewBag.icon = TempData["icon"];
            ViewBag.text = TempData["text"];
            return View("Login");
        }

        [HttpPost]
        public async Task<ActionResult> Login(string username, string password)
        {
            try
            {
                tracelog.WriteLog($"User : {username}, Start Login");
                tracelog.WriteLog($"User : {username}, Start Hit Database Admin");
                var admin = await _adminCollection
                             .Find(Builders<admin>.Filter.And(
                                 Builders<admin>.Filter.Eq(p => p.username, username),
                                 Builders<admin>.Filter.Eq(p => p.statusEnrole, true),
                                 Builders<admin>.Filter.Eq(p => p.statusAccount, "Active")))
                             .FirstOrDefaultAsync();

                if (admin != null)
                {

                    if (admin.loginCount == 4 && admin.lastLogin.AddMinutes(5) > DateTime.UtcNow)
                    {
                        TimeSpan diff = admin.lastLogin.AddMinutes(5) - DateTime.UtcNow;
                        int diffMin = diff.Minutes;
                        int diffSec = diff.Seconds;
                        tracelog.WriteLog($"User : {username}, Failed Login, Reason: User masih pending {diffMin} Menit dan {diffSec} detik");
                        //return Content($"<script>alert('Anda masih harus menunggu {diffMin} Menit dan {diffSec} detik!');window.location.href='/Account/Index'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Login";
                        TempData["icon"] = "error";
                        TempData["text"] = $"Anda masih harus menunggu {diffMin} Menit dan {diffSec} detik!";
                        return RedirectToAction("Index");
                    }

                    tracelog.WriteLog($"User : {username}, Success Hit Database Admin");

                    var filter = Builders<admin>.Filter.Eq(p => p.username, username);
                    tracelog.WriteLog($"User : {username}, Start Hash Password");
                    using (var hmac = new HMACSHA512(admin.saltHash))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != admin.password[i])
                            {
                                var updateWrongPassword = Builders<admin>.Update.Set(p => p.loginCount, admin.loginCount + 1).Set(p => p.lastLogin, DateTime.UtcNow);
                                await _adminCollection.UpdateOneAsync(filter, updateWrongPassword);
                                if (admin.loginCount < 5)
                                {
                                    if (admin.loginCount == 3)
                                    {
                                        HttpContext.Session.SetString("waiting", "True");
                                        TimeSpan diff = DateTime.UtcNow.AddMinutes(5) - DateTime.UtcNow;
                                        int diffMin = diff.Minutes;
                                        int diffSec = diff.Seconds;
                                        HttpContext.Session.SetInt32("diffMin", diffMin);
                                        HttpContext.Session.SetInt32("diffSec", diffSec);
                                        HttpContext.Session.SetString("dateWrong", DateTime.UtcNow.ToString());
                                    }
                                    HttpContext.Session.SetInt32("loginCount", admin.loginCount + 1);
                                    tracelog.WriteLog($"User : {username}, Failed Login, Reason: Password Salah");
                                    // return Content("<script>alert('Password Salah!');window.location.href='/Account/Index'</script>", "text/html");
                                    TempData["titlePopUp"] = "Gagal Login";
                                    TempData["icon"] = "error";
                                    TempData["text"] = "Password Salah!";
                                    return RedirectToAction("Index");

                                }
                                else
                                {
                                    var updateBlock = Builders<admin>.Update.Set(p => p.statusAccount, "Block");
                                    await _adminCollection.UpdateOneAsync(filter, updateBlock);
                                    tracelog.WriteLog($"User : {username}, Failed Login, Reason: Password Salah");
                                    //return Content("<script>alert('Akun Anda Di Block!');window.location.href='/Account/Index'</script>", "text/html");
                                    TempData["titlePopUp"] = "Gagal Login";
                                    TempData["icon"] = "error";
                                    TempData["text"] = "Akun Anda Di Block!";
                                    return RedirectToAction("Index");
                                }
                            }

                        }
                    }

                    tracelog.WriteLog($"User : {username}, Success Hash Password");

                    var updateSuccress = Builders<admin>.Update.Set(p => p.loginCount, 0).Set(p => p.lastLogin, DateTime.UtcNow);
                    await _adminCollection.UpdateOneAsync(filter, updateSuccress);
                    HttpContext.Session.SetString("loginAs", "Admin");
                    HttpContext.Session.SetString("username", admin.username);
                    HttpContext.Session.SetString("email", admin.email);
                    HttpContext.Session.SetInt32("role", admin.roleAdmin);
                    HttpContext.Session.SetString("idUser", admin._id);
                    if (admin.passwordExpired.AddDays(-7) < DateTime.UtcNow)
                    {
                        int daysExp = admin.passwordExpired.Day - DateTime.UtcNow.Day;
                        tracelog.WriteLog($"User : {username}, Success Login but the password near expired date, remaining days : {daysExp}");
                        HttpContext.Session.SetInt32("passExpired", daysExp);
                    }
                    // Get all privileges for based on role
                    tracelog.WriteLog($"User : {username}, Start Get Menu Items");

                    var privileges = await _privilegeCollection
                        .Find(p => p.roleId == admin.roleAdmin && p.loginAs == "Admin")
                        .ToListAsync();
                    var menuIds = privileges.Select(p => p.menuId).ToList();
                    var menuItems = await _menuCollection
                        .Find(m => menuIds.Contains(m._id))
                        .ToListAsync();

                    TempData["menuItems"] = System.Text.Json.JsonSerializer.Serialize(menuItems);

                    tracelog.WriteLog($"User : {username}, Success Get Menu Items");

                    tracelog.WriteLog($"User : {username}, Success Login");
                    //return RedirectToAction("Index", "Home");
                    string script = @"
                    <html>
                        <head>
                            <script src='https://cdn.jsdelivr.net/npm/sweetalert2@11'></script>
                        </head>
                        <body>
                           <script>
                                Swal.fire({
                                    title: 'Login Berhasil',
                                    text: 'Selamat datang kembali!',
                                    icon: 'success',
                                    showConfirmButton: false,
                                    timer: 2000,
                                    timerProgressBar: true,
                                    willClose: () => {
                                        window.location.href = '/Home/Index';
                                    }
                                });
                            </script>
                        </body>
                    </html>";

                    return Content(script, "text/html");
                }
                else
                {
                    var surveyer = await _surveyerCollection
                           .Find(Builders<surveyers>.Filter.And(
                               Builders<surveyers>.Filter.Eq(p => p.username, username),
                               Builders<surveyers>.Filter.Eq(p => p.statusEnrole, true),
                               Builders<surveyers>.Filter.Eq(p => p.statusAccount, "Active")))
                           .FirstOrDefaultAsync();

                    if (surveyer == null)
                    {
                        tracelog.WriteLog($"User : {username}, Failed Login, Reason: User Tidak Ditemukan");
                        //return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/LogOut'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Login";
                        TempData["icon"] = "error";
                        TempData["text"] = "User Tidak Ditemukan!";
                        return RedirectToAction("Index");

                    }


                    if (surveyer.loginCount == 4 && surveyer.lastLogin?.AddMinutes(5) > DateTime.UtcNow)
                    {
                        TimeSpan? diff = surveyer.lastLogin?.AddMinutes(5) - DateTime.UtcNow;
                        int? diffMin = diff?.Minutes;
                        int? diffSec = diff?.Seconds;
                        tracelog.WriteLog($"User : {username}, Failed Login, Reason: User masih pending {diffMin} Menit dan {diffSec} detik");
                        //return Content($"<script>alert('Anda masih harus menunggu {diffMin} Menit dan {diffSec} detik!');window.location.href='/Account/Index'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Login";
                        TempData["icon"] = "error";
                        TempData["text"] = $"Anda masih harus menunggu {diffMin} Menit dan {diffSec} detik!";
                        return RedirectToAction("Index");
                    }

                    tracelog.WriteLog($"User : {username}, Success Hit Database Admin");

                    var filter = Builders<surveyers>.Filter.Eq(p => p.username, username);
                    tracelog.WriteLog($"User : {username}, Start Hash Password");
                    using (var hmac = new HMACSHA512(surveyer.saltHash))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != surveyer.password[i])
                            {
                                var updateWrongPassword = Builders<surveyers>.Update.Set(p => p.loginCount, surveyer.loginCount + 1).Set(p => p.lastLogin, DateTime.UtcNow);
                                await _surveyerCollection.UpdateOneAsync(filter, updateWrongPassword);
                                if (surveyer.loginCount < 5)
                                {
                                    if (surveyer.loginCount == 3)
                                    {
                                        HttpContext.Session.SetString("waiting", "True");
                                        TimeSpan diff = DateTime.UtcNow.AddMinutes(5) - DateTime.UtcNow;
                                        int diffMin = diff.Minutes;
                                        int diffSec = diff.Seconds;
                                        HttpContext.Session.SetInt32("diffMin", diffMin);
                                        HttpContext.Session.SetInt32("diffSec", diffSec);
                                        HttpContext.Session.SetString("dateWrong", DateTime.UtcNow.ToString());
                                    }
                                    HttpContext.Session.SetInt32("loginCount", surveyer.loginCount + 1);
                                    tracelog.WriteLog($"User : {username}, Failed Login, Reason: Password Salah");
                                    //return Content("<script>alert('Password Salah!');window.location.href='/Account/Index'</script>", "text/html");
                                    TempData["titlePopUp"] = "Gagal Login";
                                    TempData["icon"] = "error";
                                    TempData["text"] = "Password Salah!";
                                    return RedirectToAction("Index");

                                }
                                else
                                {
                                    var updateBlock = Builders<surveyers>.Update.Set(p => p.statusAccount, "Block");
                                    await _surveyerCollection.UpdateOneAsync(filter, updateBlock);
                                    tracelog.WriteLog($"User : {username}, Failed Login, Reason: Password Salah");
                                    // return Content("<script>alert('Akun Anda Di Block!');window.location.href='/Account/Index'</script>", "text/html");
                                    TempData["titlePopUp"] = "Gagal Login";
                                    TempData["icon"] = "error";
                                    TempData["text"] = "Akun Anda Di Block!";
                                    return RedirectToAction("Index");

                                }
                            }

                        }
                    }

                    tracelog.WriteLog($"User : {username}, Success Hash Password");

                    var updateSuccress = Builders<surveyers>.Update.Set(p => p.loginCount, 0).Set(p => p.lastLogin, DateTime.UtcNow);
                    await _surveyerCollection.UpdateOneAsync(filter, updateSuccress);
                    HttpContext.Session.SetString("loginAs", "Survey");
                    HttpContext.Session.SetString("username", surveyer.username);
                    HttpContext.Session.SetString("email", surveyer.email);
                    HttpContext.Session.SetString("idUser", surveyer._id.ToString());
                    if (surveyer.passwordExpired?.AddDays(-7) < DateTime.UtcNow)
                    {
                        int? daysExp = surveyer.passwordExpired?.Day - DateTime.UtcNow.Day;
                        tracelog.WriteLog($"User : {username}, Success Login but the password near expired date, remaining days : {daysExp}");
                        HttpContext.Session.SetInt32("passExpired", (int)daysExp!);
                    }

                    var privileges = await _privilegeCollection
                      .Find(p => p.loginAs == "Survey")
                      .ToListAsync();
                    var menuIds = privileges.Select(p => p.menuId).ToList();
                    var menuItems = await _menuCollection
                        .Find(m => menuIds.Contains(m._id))
                        .ToListAsync();

                    TempData["menuItems"] = System.Text.Json.JsonSerializer.Serialize(menuItems);
                    tracelog.WriteLog($"User : {username}, Success Get Menu Items");

                    tracelog.WriteLog($"User : {username}, Success Login");
                    //return RedirectToAction("Index", "Home");
                    string script = @"
                    <html>
                        <head>
                            <script src='https://cdn.jsdelivr.net/npm/sweetalert2@11'></script>
                        </head>
                        <body>
                           <script>
                                Swal.fire({
                                    title: 'Login Berhasil',
                                    text: 'Selamat datang kembali!',
                                    icon: 'success',
                                    showConfirmButton: false,
                                    timer: 2000,
                                    timerProgressBar: true,
                                    willClose: () => {
                                        window.location.href = '/Home/Index';
                                    }
                                });
                            </script>
                        </body>
                    </html>";

                    return Content(script, "text/html");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                tracelog.WriteLog($"User : {username}, Failed Login, Reason : {e.Message}");
                // return Content($"<script>alert('{e.Message}');window.location.href='/Account/LogOut';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Login";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("Index");

            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {

            return View("ResetPassword");
        }

        [HttpPost]
        public async Task<ActionResult> ResetPassword(string username, string email)
        {
            try
            {
                var keyReset = $"{username}_resetPassword";
                if (_cache.TryGetValue(keyReset, out _))
                {
                    //return Content("<script>alert('Harap tunggu sebentar untuk reset password!');window.location.href='/Account/Index';</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Reset Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "Harap tunggu sebentar untuk reset password!";
                    return RedirectToAction("ResetPassword");
                }
                tracelog.WriteLog($"User : {username}, Start Reset Password");
                tracelog.WriteLog($"User : {username}, Start Hit Database Admin");
                surveyers surveyer = new surveyers();
                admin admin = new admin();

                admin = await _adminCollection
                .Find(Builders<admin>.Filter.And(
                    Builders<admin>.Filter.Eq(p => p.username, username),
                    Builders<admin>.Filter.Eq(p => p.email, email)
                )).FirstOrDefaultAsync();

                if (admin == null)
                {
                    surveyer = await _surveyerCollection
                          .Find(Builders<surveyers>.Filter.And(
                              Builders<surveyers>.Filter.Eq(p => p.username, username),
                              Builders<surveyers>.Filter.Eq(p => p.statusEnrole, true),
                              Builders<surveyers>.Filter.Eq(p => p.statusAccount, "Active")))
                          .FirstOrDefaultAsync();

                    if (surveyer == null)
                    {
                        tracelog.WriteLog($"User : {username}, Failed Login, Reason: User Tidak Ditemukan");
                        // return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/LogOut'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Reset Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "User Tidak Ditemukan!";
                        return RedirectToAction("ResetPassword");
                    }
                }

                var key = generalFunction1.GenerateRandomKey();
                string resetFor = admin != null ? "Admin" : "Surveyer";
                string subject = $"Reset Password Akun {resetFor}";
                string usernameEmail = admin != null ? $"<b>Username</b> : {admin.username}" : $"<b>Username</b> : {surveyer.username}";
                string body = @$"<html>
                <header>
                    <h3>Link Untuk Reset Password {resetFor}</h3>
                </header>
                <body>
                    <div>
                        Berikut merupakan link untuk reset password {resetFor} dengan akun:
                    <div>
                    <br/>
                    <br/>
                    <div>
                       {usernameEmail}
                    </div>
                    <br/>
                     <div>
                        <b>Link</b> : <a href='{linkSelf}/Account/CreateResetPassword?username={username}&key={key}'>{linkSelf}/Account/CreateResetPassword?username={username}&key={key}</a>
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

                using (var message = new MailMessage(emailClient, admin != null ? admin.email : surveyer.email)
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
                    username = username,
                    addTime = DateTime.UtcNow,
                    used = "N"
                };

                await _keyGenerateCollection.InsertOneAsync(keyGenerate);
                _cache.Set(keyReset, true, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                    Size = 1
                });
                //return Content("<script>alert('Mohon Cek Email!');window.location.href='/Account/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Mohon Check Email!";
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                tracelog.WriteLog($"User : {username}, Failed Reset Password, Reason : {e.Message}");
                //  return Content($"<script>alert('{e.Message}');window.location.href='/Account/Index';</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Reset Password";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("ResetPassword");
            }
        }

        [HttpGet]
        public async Task<ActionResult> CreateResetPassword(string username, string key)
        {
            var admin = await _keyGenerateCollection
           .Find(Builders<KeyGenerate>.Filter.And(
               Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
               Builders<KeyGenerate>.Filter.Eq(p => p.key, key),
                Builders<KeyGenerate>.Filter.Eq(p => p.used, "N")
           )).FirstOrDefaultAsync();

            if (admin == null)
            {
                // return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Reset Password";
                TempData["icon"] = "error";
                TempData["text"] = "User Tidak Ditemukan!";
                return RedirectToAction("Index");
            }

            if (admin.addTime.AddMinutes(15) < DateTime.UtcNow)
            {
                TempData["titlePopUp"] = "Gagal Reset Password";
                TempData["icon"] = "error";
                TempData["text"] = "Link Expired!";
                return RedirectToAction("Index");
            }
               // return Content("<script>alert('Link Expired!');window.location.href='/Account/Index'</script>", "text/html");

            ViewBag.username = username;
            ViewBag.key = key;
            return View("CreateResetPassword");
        }

        [HttpPost]
        public async Task<ActionResult> CreateResetPassword(string username, string password, string passwordRet, string key)
        {
            try
            {
                if (password != passwordRet)
                {
                    TempData["titlePopUp"] = "Gagal Reset Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "Password Tidak Sama!";
                    return RedirectToAction("CreateResetPassword", new { username = username, key = key });
                    //return Content($"<script>alert('Password Tidak Sama!');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>", "text/html");
                }
                surveyers surveyer = new surveyers();
                admin admin = new admin();

                admin = await _adminCollection
                            .Find(Builders<admin>.Filter.Eq(p => p.username, username))
                            .FirstOrDefaultAsync();

                //admin
                if (admin == null)
                {
                    surveyer = await _surveyerCollection
                          .Find(Builders<surveyers>.Filter.And(
                              Builders<surveyers>.Filter.Eq(p => p.username, username),
                              Builders<surveyers>.Filter.Eq(p => p.statusEnrole, true),
                              Builders<surveyers>.Filter.Eq(p => p.statusAccount, "Active")))
                          .FirstOrDefaultAsync();

                    if (surveyer == null)
                    {
                        tracelog.WriteLog($"User : {username}, Failed Login, Reason: User Tidak Ditemukan");
                        //return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/LogOut'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Reset Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "User Tidak Ditemukan!";
                        return RedirectToAction("CreateResetPassword", new { username = username, key = key });
                    }
                }

                if (admin != null)
                {
                    bool checkPassNow = true;
                    bool checkPassOld = true;
                    using (var hmac = new HMACSHA512(admin.saltHash))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != admin.password[i])
                            {
                                checkPassNow = false;
                                break;
                            }
                        }
                    }

                    if (checkPassNow == true)
                    {
                        // return Content($"<script>alert('Password Tidak Boleh Sama dengan Password Sekarang!');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Reset Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "Password Tidak Boleh Sama dengan Password Sekarang!";
                        return RedirectToAction("CreateResetPassword", new { username = username, key = key });
                    }


                    using (var hmac = new HMACSHA512(admin.saltHashLama))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != admin.passwordLama[i])
                            {
                                checkPassOld = false;
                                break;
                            }
                        }
                    }

                    if (checkPassOld == true)
                    {
                        //return Content($"<script>alert('Password Tidak Boleh Sama dengan Password Lama!');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Reset Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "Password Tidak Boleh Sama dengan Password Lama!";
                        return RedirectToAction("CreateResetPassword", new { username = username, key = key });
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

                    var filter = Builders<admin>.Filter.Eq(p => p.username, username);
                    var update = Builders<admin>.Update.
                        Set(p => p.password, passwordHash).
                        Set(p => p.loginCount, 0).
                        Set(p => p.saltHash, passwordSalt).
                        Set(p => p.saltHashLama, admin.saltHash).
                        Set(p => p.passwordLama, admin.password).
                        Set(p => p.passwordExpired, DateTime.UtcNow.AddMonths(3));
                    var result = await _adminCollection.UpdateOneAsync(filter, update);
                }
                //surveyer
                else
                {
                    bool checkPassNow = true;
                    bool checkPassOld = true;
                    using (var hmac = new HMACSHA512(surveyer.saltHash))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != surveyer.password[i])
                            {
                                checkPassNow = false;
                                break;
                            }
                        }
                    }

                    if (checkPassNow == true)
                    {
                        // return Content($"<script>alert('Password Tidak Boleh Sama dengan Password Sekarang!');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Reset Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "Password Tidak Boleh Sama dengan Password Sekarang!";
                        return RedirectToAction("CreateResetPassword", new { username = username, key = key });
                    }


                    using (var hmac = new HMACSHA512(surveyer.saltHashLama))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != surveyer.passwordLama[i])
                            {
                                checkPassOld = false;
                                break;
                            }
                        }
                    }

                    if (checkPassOld == true)
                    {
                        // return Content($"<script>alert('Password Tidak Boleh Sama dengan Password Lama!');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Reset Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "Password Tidak Boleh Sama dengan Password Lama!";
                        return RedirectToAction("CreateResetPassword", new { username = username, key = key });
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
                        Set(p => p.password, passwordHash).
                        Set(p => p.loginCount, 0).
                        Set(p => p.saltHash, passwordSalt).
                        Set(p => p.saltHashLama, surveyer.saltHash).
                        Set(p => p.passwordLama, surveyer.password).
                        Set(p => p.passwordExpired, DateTime.UtcNow.AddMonths(3));
                    var result = await _surveyerCollection.UpdateOneAsync(filter, update);
                }
                // return Content("<script>alert('Berhasil Reset Password!');window.location.href='/Account/Index'</script>", "text/html");
                string script = @"
                    <html>
                        <head>
                            <script src='https://cdn.jsdelivr.net/npm/sweetalert2@11'></script>
                        </head>
                        <body>
                           <script>
                                Swal.fire({
                                    title: 'Reset Berhasil',
                                    text: 'Success Reset Password!',
                                    icon: 'success',
                                    showConfirmButton: false,
                                    timer: 2000,
                                    timerProgressBar: true,
                                    willClose: () => {
                                        window.location.href = '/Account/Index';
                                    Account
                                });
                            </script>
                        </body>
                    </html>";
                return Content(script, "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //return Content($"<script>alert('{e.Message}');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Reset Password";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("CreateResetPassword", new { username=username,key=key});
            }
        }



        [HttpGet]
        public ActionResult EditPassword()
        {
            return View("EditPassword");
        }

        [HttpPost]
        public async Task<ActionResult> EditPassword(string passwordNow, string password, string passwordRet)
        {
            try
            {

                string role = HttpContext.Session.GetString("loginAs")!;
                string username = HttpContext.Session.GetString("username")!;
                if (password == passwordNow)
                {
                    //return Content($"<script>alert('Password Tidak Boleh Sama dengan Password Sekarang!');window.location.href='/Account/EditPassword'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Edit Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "Password Tidak Boleh Sama dengan Password Sekarang!";
                    return RedirectToAction("EditPassword");
                }

                if (password != passwordRet)
                {
                    //return Content($"<script>alert('Password Baru Tidak Sama!');window.location.href='/Account/EditPassword'</script>", "text/html");
                    TempData["titlePopUp"] = "Gagal Edit Password";
                    TempData["icon"] = "error";
                    TempData["text"] = "Password Baru Tidak Sama!";
                    return RedirectToAction("EditPassword");
                }

                surveyers surveyer = new surveyers();
                admin admin = new admin();
                bool checkPassOld = true;

                if (role == "Admin")
                {
                    admin = await _adminCollection
                          .Find(Builders<admin>.Filter.Eq(p => p.username, username))
                          .FirstOrDefaultAsync();

                    if (admin == null)
                    {
                        //return Content($"<script>alert('User Tidak Ditemukan!');window.location.href='/Account/EditPassword'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Edit Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "User Tidak Ditemukan!";
                        return RedirectToAction("EditPassword");
                    }

                    using (var hmac = new HMACSHA512(admin.saltHash))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(passwordNow));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != admin.password[i])
                            {
                                // return Content("<script>alert('Password Sekarang Salah!');window.location.href='/Account/EditPassword'</script>", "text/html");
                                TempData["titlePopUp"] = "Gagal Edit Password";
                                TempData["icon"] = "error";
                                TempData["text"] = "Password Sekarang Salah!";
                                return RedirectToAction("EditPassword");
                            }

                        }
                    }

                    using (var hmac = new HMACSHA512(admin.saltHashLama))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != admin.passwordLama[i])
                            {
                                checkPassOld = false;
                                break;
                            }
                        }
                    }

                    if (checkPassOld == true)
                    {
                        //   return Content($"<script>alert('Password Tidak Boleh Sama dengan Password Lama!');window.location.href='/Account/EditPassword'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Edit Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "Password Tidak Boleh Sama dengan Password Lama!";
                        return RedirectToAction("EditPassword");
                    }

                    byte[] passwordSalt = [];
                    byte[] passwordHash = [];
                    using (var hmac = new HMACSHA512())
                    {
                        passwordSalt = hmac.Key;
                        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                    }

                    var filter = Builders<admin>.Filter.Eq(p => p.username, username);
                    var update = Builders<admin>.Update.
                        Set(p => p.password, passwordHash).
                        Set(p => p.loginCount, 0).
                        Set(p => p.saltHash, passwordSalt).
                        Set(p => p.saltHashLama, admin.saltHash).
                        Set(p => p.passwordLama, admin.password).
                        Set(p => p.passwordExpired, DateTime.UtcNow.AddMonths(3));
                    var result = await _adminCollection.UpdateOneAsync(filter, update);
                }
                else
                {
                    surveyer = await _surveyerCollection
                      .Find(Builders<surveyers>.Filter.And(
                          Builders<surveyers>.Filter.Eq(p => p.username, username)))
                      .FirstOrDefaultAsync();

                    if (surveyer == null)
                    {
                        //return Content($"<script>alert('User Tidak Ditemukan!');window.location.href='/Account/EditPassword'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Edit Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "User Tidak Ditemukan!";
                        return RedirectToAction("EditPassword");
                    }

                    using (var hmac = new HMACSHA512(surveyer.saltHash))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(passwordNow));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != surveyer.password[i])
                            {
                                // return Content("<script>alert('Password Sekarang Salah!');window.location.href='/Account/EditPassword'</script>", "text/html");
                                TempData["titlePopUp"] = "Gagal Edit Password";
                                TempData["icon"] = "error";
                                TempData["text"] = "Password Sekarang Salah!";
                                return RedirectToAction("EditPassword");
                            }

                        }
                    }

                    using (var hmac = new HMACSHA512(surveyer.saltHashLama))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                        for (int i = 0; i < computedHash.Length; i++)
                        {

                            if (computedHash[i] != surveyer.passwordLama[i])
                            {
                                checkPassOld = false;
                                break;
                            }
                        }
                    }

                    if (checkPassOld == true)
                    {
                        //  return Content($"<script>alert('Password Tidak Boleh Sama dengan Password Lama!');window.location.href='/Account/EditPassword'</script>", "text/html");
                        TempData["titlePopUp"] = "Gagal Edit Password";
                        TempData["icon"] = "error";
                        TempData["text"] = "Password Tidak Boleh Sama dengan Password Lama!";
                        return RedirectToAction("EditPassword");
                    }

                    byte[] passwordSalt = [];
                    byte[] passwordHash = [];
                    using (var hmac = new HMACSHA512())
                    {
                        passwordSalt = hmac.Key;
                        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                    }

                    var filter = Builders<surveyers>.Filter.Eq(p => p.username, username);
                    var update = Builders<surveyers>.Update.
                        Set(p => p.password, passwordHash).
                        Set(p => p.loginCount, 0).
                        Set(p => p.saltHash, passwordSalt).
                        Set(p => p.saltHashLama, surveyer.saltHash).
                        Set(p => p.passwordLama, surveyer.password).
                        Set(p => p.passwordExpired, DateTime.UtcNow.AddMonths(3));
                    var result = await _surveyerCollection.UpdateOneAsync(filter, update);
                }
                //return Content("<script>alert('Berhasil Edit Password!');window.location.href='/Account/EditPassword'</script>", "text/html");
                TempData["titlePopUp"] = "Success";
                TempData["icon"] = "success";
                TempData["text"] = "Berhasil Edit Password";
                return RedirectToAction("EditPassword");

            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
                //return Content($"<script>alert('{e.Message}');window.location.href='/Account/EditPassword'</script>", "text/html");
                TempData["titlePopUp"] = "Gagal Edit Password";
                TempData["icon"] = "error";
                TempData["text"] = e.Message;
                return RedirectToAction("EditPassword");
            }
        }

        [HttpGet]
        public ActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return View("Login");
        }
    }
}
