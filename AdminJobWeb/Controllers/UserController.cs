using AdminJobWeb.Models.Account;
using AdminJobWeb.Tracelog;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace AdminJobWeb.Controllers
{
    public class UserController : Controller
    {
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoCollection<KeyGenerate> _keyGenerateCollection;
        private readonly IMongoDatabase _database;

        private string databaseName;
        private string adminCollectionName;
        private string keyGenerateCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;

        private TracelogUser _tracelogUser;

        public UserController(IMongoClient mongoClient, IConfiguration configuration)
        {
            databaseName = configuration.GetValue<string>("MonggoDbSettings:DatabaseName")!;
            adminCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:adminCollection")!;
            keyGenerateCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:keyGenerateCollection")!;

            _database = mongoClient.GetDatabase(databaseName);
            _adminCollection = _database.GetCollection<admin>(adminCollectionName);
            _keyGenerateCollection = _database.GetCollection<KeyGenerate>(keyGenerateCollectionName);

            appPass = configuration.GetValue<string>("Email:appPass")!;
            emailClient = configuration.GetValue<string>("Email:emailClient")!;
            linkSelf = configuration.GetValue<string>("Link:linkSelf")!;

            _tracelogUser = new TracelogUser();
        }

        private static string GenerateRandomKey()
        {
            int size = 16;
            var randomNumber = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            string token = Convert.ToBase64String(randomNumber)
                             .Replace("+", "-")
                             .Replace("/", "_")
                             .Replace("=", "");

            return token;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogUser.WriteLog("UserController Index view called");

                // Retrieve all admin users from the database
                var admins = _adminCollection.Find(_ => true).ToList();

                if (admins.Count == 0)
                {
                    _tracelogUser.WriteLog("No admin users found in the database.");
                    Debug.WriteLine("No admin users found in the database.");
                    return Content("<script>alert('No admin users found in the database.');window.location.href='/Home/Index';</script>", "text/html");
                }

                _tracelogUser.WriteLog($"Retrieved {admins.Count} admin users from the database.");
                Debug.WriteLine($"Retrieved {admins.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");

                return View(admins);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogUser.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> SendFormAdmin()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string emailLogin = HttpContext.Session.GetString("email")!;
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Send Form to Create New Admin");

                var key = GenerateRandomKey();
                string subject = "Form Create Akun Admin Baru";
                string body = @$"<html>
                <header>
                    <h3>Link Untuk Form Create Akun Admin Baru</h3>
                </header>
                <body>
                    <div>
                        Berikut merupakan link untuk form create akun admin baru:
                    <div>
                    <br/>
                    <br/>
                    <div>
                        <b>Username</b> : {adminLogin}
                    </div>
                    <br/>
                     <div>
                        <b>Link</b> : <a href='{linkSelf}/User/CreateAdmin?username={adminLogin}&key={key}'>{linkSelf}/username={adminLogin}&key={key}</a>
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

                using (var message = new MailMessage(emailClient, emailLogin)
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
                    username = adminLogin,
                    addTime = DateTime.UtcNow
                };

                await _keyGenerateCollection.InsertOneAsync(keyGenerate);

                return Content("<script>alert('Berhasil mengirimkan link untuk pembuatan Admin baru. Silahkan cek email Anda!');window.location.href='/User/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed send form to create new admin, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
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
                _tracelogUser.WriteLog($"User : {username}, User Tidak Ditemukan!");
                return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>", "text/html");
            }

            if (admin.addTime.AddMinutes(15) < DateTime.UtcNow)
            {
                _tracelogUser.WriteLog($"User : {username}, Link Expired!");
                return Content("<script>alert('Link Expired!');window.location.href='/Account/Index'</script>", "text/html");
            }

            ViewBag.username = username;
            ViewBag.key = key;

            return View();
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> CreateAdmin([FromForm] admin dataObj, string usernameAdmin, string key)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Create New Admin");
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Hit Database Admin");

                var existingAdmin = await _adminCollection
                    .Find(Builders<admin>.Filter.Eq(p => p.username, dataObj.username))
                    .FirstOrDefaultAsync();

                // Input validation
                if (string.IsNullOrEmpty(dataObj.username) || string.IsNullOrEmpty(dataObj.email))
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Data Tidak Boleh Kosong!");
                    return Content($"<script>alert('Data Tidak Boleh Kosong!');window.location.href='/User/CreateAdmin?username={usernameAdmin}&key={key}'</script>", "text/html");
                }

                if (existingAdmin != null)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Username Sudah Ada!");
                    return Content($"<script>alert('Username Sudah Ada!');window.location.href='/User/CreateAdmin?username={usernameAdmin}&key={key}'</script>", "text/html");
                }

                if (!string.IsNullOrEmpty(dataObj.email) && !dataObj.email.Contains("@"))
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Email Tidak Valid!");
                    return Content($"<script>alert('Email Tidak Valid!');window.location.href='/User/CreateAdmin?username={usernameAdmin}&key={key}'</script>", "text/html");
                }

                dataObj.loginCount = 0;
                dataObj.roleAdmin = 2; // Default Role Admin
                dataObj.statusAccount = "Active";
                dataObj.lastLogin = DateTime.MinValue;
                dataObj.addTime = DateTime.UtcNow;
                dataObj.updateTime = DateTime.UtcNow;
                dataObj.approvalTime = DateTime.MinValue;
                dataObj.statusEnrole = false;

                await _adminCollection.InsertOneAsync(dataObj);

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Create New User {dataObj.username}");
                
                // -----------------

                _tracelogUser.WriteLog($"User : {adminLogin}, Start Send Create Password for User {dataObj.username}");

                var keyPass = GenerateRandomKey();
                string subject = "Create Password Akun Admin";
                string body = @$"<html>
                <header>
                    <h3>Link Untuk Create Password</h3>
                </header>
                <body>
                    <div>
                        Berikut merupakan link untuk create password dengan akun:
                    <div>
                    <br/>
                    <br/>
                    <div>
                        <b>Username</b> : {dataObj.username}
                    </div>
                    <br/>
                     <div>
                        <b>Link</b> : <a href='{linkSelf}/User/CreateNewPassword?username={dataObj.username}&key={keyPass}'>{linkSelf}/username={dataObj.username}&key={keyPass}</a>
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

                using (var message = new MailMessage(emailClient, dataObj.email)
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
                    key = keyPass,
                    username = dataObj.username,
                    addTime = DateTime.UtcNow
                };

                await _keyGenerateCollection.InsertOneAsync(keyGenerate);

                _tracelogUser.WriteLog($"User : {adminLogin}, Successfully send email for User {dataObj.username}");
                return Content("<script>alert('Berhasil membuat admin baru. Mohon hubungi user terkait untuk mengecek email dan membuat password!');window.location.href='/User/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Create New User, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> CreateNewPassword(string username, string key)
        {
            var admin = await _keyGenerateCollection
           .Find(Builders<KeyGenerate>.Filter.And(
               Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
               Builders<KeyGenerate>.Filter.Eq(p => p.key, key)
           )).FirstOrDefaultAsync();

            if (admin == null)
            {
                _tracelogUser.WriteLog($"User : {username}, User Tidak Ditemukan!");
                return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>", "text/html");
            }

            if (admin.addTime.AddMinutes(15) < DateTime.UtcNow)
            {
                _tracelogUser.WriteLog($"User : {username}, Link Expired!");
                return Content("<script>alert('Link Expired!');window.location.href='/Account/Index'</script>", "text/html");
            }

            ViewBag.username = username;
            ViewBag.key = key;

            return View();
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> CreateNewPassword([FromForm] string username, string password, string passwordRet, string key)
        {
            if (password != passwordRet)
            {
                _tracelogUser.WriteLog($"User : {username}, Password Tidak Sama!");
                return Content($"<script>alert('Password Tidak Sama!');window.location.href='/User/CreateNewPassword?username={username}&key={key}'</script>", "text/html");
            }

            var admin = await _adminCollection
                        .Find(Builders<admin>.Filter.Eq(p => p.username, username))
                        .FirstOrDefaultAsync();

            if (admin == null)
            {
                _tracelogUser.WriteLog($"User : {username}, User Tidak Ditemukan!");
                return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>", "text/html");
            }

            byte[] passwordSalt = [];
            byte[] passwordHash = [];
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }

            var filter = Builders<admin>.Filter.Eq(p => p.username, username);
            var update = Builders<admin>.Update.Set(p => p.password, passwordHash).Set(p => p.loginCount, 0).Set(p => p.saltHash, passwordSalt).Set(p => p.statusAccount, "Active");
            var result = await _adminCollection.UpdateOneAsync(filter, update);

            _tracelogUser.WriteLog($"User : {username}, Berhasil Create Password");
            return Content("<script>alert('Berhasil Create Password!');window.location.href='/Account/Index'</script>", "text/html");
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ApprovalNewAdmin(string id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Approval New Admin");

                var filter = Builders<admin>.Filter.Eq(p => p._id, id);
                var update = Builders<admin>.Update.Set(p => p.statusEnrole, true).Set(p => p.approvalTime, DateTime.UtcNow);

                var result = await _adminCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Gagal Approval New Admin");
                    return Content("<script>alert('Gagal Approval New Admin!');window.location.href='/User/Index'</script>", "text/html");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Approval New Admin");
                return Content("<script>alert('Berhasil Approval New Admin!');window.location.href='/User/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Approval New Admin, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> RejectNewAdmin(string id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            if (HttpContext.Session.GetInt32("role") != 1)
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }
            try
            {
                _tracelogUser.WriteLog($"User : {adminLogin}, Start Reject New Admin");

                var filter = Builders<admin>.Filter.Eq(p => p._id, id);
                var update = Builders<admin>.Update.Set(p => p.statusEnrole, false).Set(p => p.approvalTime, DateTime.UtcNow);

                var result = await _adminCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    _tracelogUser.WriteLog($"User : {adminLogin}, Gagal Reject New Admin");
                    return Content("<script>alert('Gagal Reject New Admin!');window.location.href='/User/Index'</script>", "text/html");
                }

                _tracelogUser.WriteLog($"User : {adminLogin}, Berhasil Reject New Admin");
                return Content("<script>alert('Berhasil Reject New Admin!');window.location.href='/User/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _tracelogUser.WriteLog($"User : {adminLogin}, Failed Reject New Admin, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/User/Index';</script>", "text/html");
            }
        }
    }
}
