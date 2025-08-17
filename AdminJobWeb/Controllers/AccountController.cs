using AdminJobWeb.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using Microsoft.AspNetCore.Http;

namespace AdminJobWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoCollection<KeyGenerate> _keyGenerateCollection;
        private readonly IMongoDatabase _database;
        private IConfiguration congfiguration;
        private string databaseName;
        private string adminCollectionName;
        private string keyGenerateCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;
        public AccountController(IMongoClient mongoClient, IConfiguration configuration)
        {
            this.congfiguration = configuration;
            this.databaseName = configuration["MonggoDbSettings:DatabaseName"]!;
            this._database = mongoClient.GetDatabase(this.databaseName);
            this.adminCollectionName = configuration["MonggoDbSettings:Collections:adminCollection"]!;
            this._adminCollection = _database.GetCollection<admin>(this.adminCollectionName);
            this.keyGenerateCollectionName = configuration["MonggoDbSettings:Collections:keyGenerateCollection"]!;
            this._keyGenerateCollection = _database.GetCollection<KeyGenerate>(this.keyGenerateCollectionName);
            this.appPass = configuration["Email:appPass"]!;
            this.emailClient = configuration["Email:emailClient"]!;
            this.linkSelf = configuration["Link:linkSelf"]!;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("username") != null){
                return RedirectToAction("Index", "Home");
            }

            return View("Login");
        }

        [HttpPost]
        public async Task<ActionResult> Login(string username, string password)
        {
            var admin = await _adminCollection
                         .Find(Builders<admin>.Filter.Eq(p => p.username, username))
                         .FirstOrDefaultAsync();

            if (admin == null)
            {
                return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>");
            }

            var filter = Builders<admin>.Filter.Eq(p => p.username, username);

            using (var hmac = new HMACSHA512(admin.saltHash))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != admin.password[i])
                    {
                        var updateWrongPassword = Builders<admin>.Update.Set(p => p.loginCount, admin.loginCount + 1);
                        await _adminCollection.UpdateOneAsync(filter, updateWrongPassword);
                        return Content("<script>alert('Password Salah!');window.location.href='/Account/Index'</script>");
                    }
                }
            }

            var updateSuccress = Builders<admin>.Update.Set(p => p.loginCount,0).Set(p=>p.lastLogin,DateTime.Now);
            await _adminCollection.UpdateOneAsync(filter, updateSuccress);
            HttpContext.Session.SetString("username",admin.username);
            HttpContext.Session.SetString("email", admin.email);
            HttpContext.Session.SetInt32("role", admin.roleAdmin);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {

            return View("ResetPassword");
        }

        [HttpPost]
        public async Task<ActionResult> ResetPassword(string username, string email)
        {
            var admin = await _adminCollection
             .Find(Builders<admin>.Filter.And(
                 Builders<admin>.Filter.Eq(p => p.username, username),
                 Builders<admin>.Filter.Eq(p => p.email, email)
             )).FirstOrDefaultAsync();

            if (admin == null)
            {
                return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>");
            }

            var key = GenerateRandomKey();
            string subject = "Reset Password Akun Admin";
            string body = @$"<html>
                <header>
                    <h3>Link Untuk Reset Password</h3>
                </header>
                <body>
                    <div>
                        Berikut merupakan link untuk reset password dengan akun:
                    <div>
                    <br/>
                    <br/>
                    <div>
                        <b>Username</b> : {admin.username}
                    </div>
                    <br/>
                     <div>
                        <b>Link</b> : <a href='{linkSelf}/Account/CreateResetPassword?username={username}&key={key}'>{linkSelf}/username={username}&key={key}</a>
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

            using (var message = new MailMessage("AdminYukKerja", admin.email)
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
                key=key,
                username=username,
                addTime=DateTime.Now
            };

            await _keyGenerateCollection.InsertOneAsync(keyGenerate);

            return Content("<script>alert('Mohon Cek Email!');window.location.href='/Account/Index';</script>","text/html");
        }

        [HttpGet]
        public async Task<ActionResult> CreateResetPassword(string username, string key)
        {
            var admin = await _keyGenerateCollection
           .Find(Builders<KeyGenerate>.Filter.And(
               Builders<KeyGenerate>.Filter.Eq(p => p.username, username),
               Builders<KeyGenerate>.Filter.Eq(p => p.key, key)
           )).FirstOrDefaultAsync();

            if (admin == null)
            {
                return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>");
            }

            ViewBag.username = username;
            ViewBag.key = key;
            return View("CreateResetPassword");
        }

        [HttpPost]
        public async Task<ActionResult> CreateResetPassword(string username, string password, string passwordRet, string key)
        {
            if (password != passwordRet)
            {
                return Content($"<script>alert('Password Tidak Sama!');window.location.href='/Account/CreateResetPassword?username={username}&key={key}'</script>");
            }

            var admin = await _adminCollection
                        .Find(Builders<admin>.Filter.Eq(p => p.username, username))
                        .FirstOrDefaultAsync();

            if (admin == null)
            {
                return Content("<script>alert('User Tidak Ditemukan!');window.location.href='/Account/Index'</script>");
            }

            byte[] passwordSalt = [];
            byte[] passwordHash = [];
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }

            var filter = Builders<admin>.Filter.Eq(p => p.username, username);
            var update = Builders<admin>.Update.Set(p => p.password, passwordHash).Set(p => p.loginCount, 0).Set(p => p.saltHash,passwordSalt);
            var result = await _adminCollection.UpdateOneAsync(filter, update);

            return Content("<script>alert('Berhasil Reset Password!');window.location.href='/Account/Index'</script>","text/html");
        }

        public static string GenerateRandomKey()
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


    }
}
