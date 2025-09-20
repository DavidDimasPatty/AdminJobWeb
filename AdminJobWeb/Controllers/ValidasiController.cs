using AdminJobWeb.AidFunction;
using AdminJobWeb.Models.Account;
using AdminJobWeb.Models.Applicant;
using AdminJobWeb.Models.Company;
using AdminJobWeb.Tracelog;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;

namespace AdminJobWeb.Controllers
{
    public class ValidasiController : Controller
    {
        private readonly IMongoCollection<surveyers> _surveyerCollection;
        private readonly IMongoCollection<Company> _companyCollection;
        private readonly IMongoCollection<PerusahaanSurvey> _perusahaanSurveyCollection;
        private readonly IMongoCollection<PerusahaanAdmin> _perusahaanAdminCollection;
        private readonly IMongoCollection<admin> _adminCollection;
        private readonly IMongoDatabase _database;
        private string databaseName;
        private string surveyerCollectionName;
        private string companyCollectionName;
        private string perusahaanSurveyCollectionName;
        private string perusahaanAdminCollectionName;
        private string adminCollectionName;
        private string appPass;
        private string emailClient;
        private string linkSelf;
        private readonly IMemoryCache _cache;
        private TacelogValidasi _tracelogValidasi;
        private GeneralFunction1 generalFunction1;
        public ValidasiController(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
        {
            this._cache = cache;
            this.databaseName = configuration.GetValue<string>("MonggoDbSettings:DatabaseName")!;
            this._database = mongoClient.GetDatabase(databaseName);
            this.surveyerCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:surveyerCollection")!;
            this._surveyerCollection = _database.GetCollection<surveyers>(this.surveyerCollectionName);
            this.companyCollectionName = configuration["MonggoDbSettings:Collections:companiesCollection"]!;
            this._companyCollection = _database.GetCollection<Company>(this.companyCollectionName);
            this.perusahaanSurveyCollectionName = configuration["MonggoDbSettings:Collections:perusahaanSurveyCollection"]!;
            this._perusahaanSurveyCollection = _database.GetCollection<PerusahaanSurvey>(this.perusahaanSurveyCollectionName);
            this.perusahaanAdminCollectionName = configuration["MonggoDbSettings:Collections:perusahaanAdminCollection"]!;
            this._perusahaanAdminCollection = _database.GetCollection<PerusahaanAdmin>(this.perusahaanAdminCollectionName);
            this.adminCollectionName = configuration.GetValue<string>("MonggoDbSettings:Collections:adminCollection")!;
            this._adminCollection = _database.GetCollection<admin>(adminCollectionName);
            this.appPass = configuration.GetValue<string>("Email:appPass")!;
            this.emailClient = configuration.GetValue<string>("Email:emailClient")!;
            this.linkSelf = configuration.GetValue<string>("Link:linkSelf")!;
            this._tracelogValidasi = new TacelogValidasi();
            this.generalFunction1 = new GeneralFunction1();
        }

        // Validasi Perusahaan Surveyer
        [HttpGet]
        public async Task<ActionResult> ValidasiPerusahaanSurveyer()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
                {
                    TempData["titlePopUp"] = "Gagal Akses";
                    TempData["icon"] = "error";
                    TempData["text"] = "Anda Tidak Memiliki Akses!";
                    return RedirectToAction("Index", "Home");
                }
                _tracelogValidasi.WriteLog($"User {adminLogin} start akses {pathUrl}");
                string loginAs = HttpContext.Session.GetString("loginAs")!;
                List<PerusahaanSurvey>? docs;
                if (loginAs == "Survey")
                {
                    string idSurveyer = HttpContext.Session.GetString("idUser")!;
                    ObjectId surveyerObjectId = ObjectId.Parse(idSurveyer);
                    docs = await _perusahaanSurveyCollection.Aggregate()
                     .Match(Builders<PerusahaanSurvey>.Filter.Eq(x => x.idSurveyer, surveyerObjectId))
                    .Lookup("companies", "idPerusahaan", "_id", "company")
                    .Lookup("Surveyers", "idSurveyer", "_id", "surveyer")
                    .As<PerusahaanSurvey>()
                    .ToListAsync();
                }
                else
                {
                    docs = await _perusahaanSurveyCollection.Aggregate()
                      .Lookup("companies", "idPerusahaan", "_id", "company")
                      .Lookup("Surveyers", "idSurveyer", "_id", "surveyer")
                      .Match(Builders<BsonDocument>.Filter.Ne("company", new BsonArray()))
                      .As<PerusahaanSurvey>()
                      .ToListAsync();
                }
                _tracelogValidasi.WriteLog($"User {adminLogin} success get data validasi surveyer :{docs.Count}, from : {pathUrl}");

                ViewBag.loginAs = HttpContext.Session.GetString("loginAs");
                ViewBag.link = HttpContext.Request.Path;
                _tracelogValidasi.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("ValidasiPerusahaanSurveyer/ValidasiPerusahaanSurveyer", docs);
            }
            catch (Exception ex)
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                Debug.WriteLine(ex.Message);
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddSurveyer(ObjectId id, ObjectId idPerusahaan, string namaPerusahaan, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            string linkTemp = "/Validasi/ValidasiPerusahaanSurveyer";
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start akses {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.namaPerusahaan = namaPerusahaan;
                List<surveyers> surveyer = await _surveyerCollection.Find(_ => true).ToListAsync();
                _tracelogValidasi.WriteLog($"User {adminLogin} success get data surveyer :{surveyer.Count}, with data : id = {id}, idPerushaan = {idPerusahaan}, from : {pathUrl}");
                _tracelogValidasi.WriteLog($"User {adminLogin} success akses {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreate", surveyer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed akses {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanSurveyer";
            string pathUrl = HttpContext.Request.Path;
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start Add Surveyer {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, idSurveyer = {idSurveyer}");
                surveyers dataSurveyer = await _surveyerCollection
                .Find(p => p._id == idSurveyer)
               .FirstOrDefaultAsync();

                Company dataCompany = await _companyCollection
                  .Find(p => p._id == idPerusahaan)
                 .FirstOrDefaultAsync();
                _tracelogValidasi.WriteLog($"User {adminLogin} start Generate and Send Email {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, idSurveyer = {idSurveyer}");
                string subject = $"Perusahaan {dataCompany.nama} Butuh Survey";
                string body = @$"<html>
                    <header>
                        <h3>Halo {dataSurveyer.nama}, perusahaan {dataCompany.nama} baru saja mendaftar dan butuh survey</h3>
                    </header>
                    <body>
                        <div>
                           Berikut merupakan data-data perusahaan:
                        <div>
                        <br/>
                        <br/>
                        <div>
                            <ul>
                                <li>Nama Perusahaan : {dataCompany.nama}</li>
                                <li>Alamat Perusahaan : {dataCompany.alamat}</li>
                                <li>Domain Perusahaan : {dataCompany.domain}</li>
                                <li>No Telp Perusahaan : {dataCompany.noTelp}</li>
                                <li>ID Survey : {id}</li>
                            </ul>
                        </div>
                        <br/>
                         <div>
                            <b> Segera Melakukan Survey dan Ubah Status Pada </b> : <a href='{linkSelf}'>{linkSelf}</a>
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

                using (var message = new MailMessage(emailClient, dataSurveyer.email)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                _tracelogValidasi.WriteLog($"User {adminLogin} success Generate and Send Email {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, idSurveyer = {idSurveyer}");
                _tracelogValidasi.WriteLog($"User {adminLogin} start update perusahaan survey {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, idSurveyer = {idSurveyer}");
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.idSurveyer, idSurveyer).Set(p => p.statusSurvey, "Pending").Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                _tracelogValidasi.WriteLog($"User {adminLogin} success update perusahaan survey {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, idSurveyer = {idSurveyer}");
                _tracelogValidasi.WriteLog($"User {adminLogin} success add surveyer {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, idSurveyer = {idSurveyer}");
                TempData["titlePopUp"] = "Berhasil Add Surveyer";
                TempData["icon"] = "success";
                TempData["text"] = "Add Surveyer Berhasil";
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed add surveyer {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, idSurveyer = {idSurveyer}, error = {ex.Message}");
                TempData["titlePopUp"] = "Gagal Add Surveyer";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessSurveyer(ObjectId id, ObjectId idSurveyer, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanSurveyer";
            string pathUrl = HttpContext.Request.Path;
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start Process Surveyer, {pathUrl} with data id : {id.ToString()}, idSurveyer : {idSurveyer.ToString()}");
                _tracelogValidasi.WriteLog($"User {adminLogin} start update perusahaan survey, {pathUrl} with data id : {id.ToString()}, idSurveyer : {idSurveyer.ToString()}");
                 var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.idSurveyer, idSurveyer).Set(p => p.statusSurvey, "Process").Set(p => p.dateSurvey, DateTime.UtcNow).Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                _tracelogValidasi.WriteLog($"User {adminLogin} success update perusahaan survey, {pathUrl} with data id : {id.ToString()}, idSurveyer : {idSurveyer.ToString()}");
                _tracelogValidasi.WriteLog($"User {adminLogin} success Process Surveyer, {pathUrl} with data id : {id.ToString()}, idSurveyer : {idSurveyer.ToString()}");
                TempData["titlePopUp"] = "Berhasil Survey Perusahaan";
                TempData["icon"] = "success";
                TempData["text"] = "Survey Dilakukan";
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed Process Surveyer, {pathUrl} with data id : {id.ToString()}, idSurveyer : {idSurveyer.ToString()}, error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Proses Survey";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ApprovalSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanSurveyer";
            string pathUrl = HttpContext.Request.Path;
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start Approval Surveyer, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                _tracelogValidasi.WriteLog($"User {adminLogin} start generate and send email, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                List<admin> data = await _adminCollection
                      .Find(Builders<admin>.Filter.And(
                          Builders<admin>.Filter.Eq(p => p.roleAdmin, 2),
                          Builders<admin>.Filter.Eq(p => p.statusAccount, "Active")))
                     .ToListAsync();

                Company dataCompany = await _companyCollection
                  .Find(p => p._id == idPerusahaan)
                 .FirstOrDefaultAsync();


                surveyers dataSurveyer = await _surveyerCollection
                  .Find(p => p._id == idSurveyer)
                 .FirstOrDefaultAsync();

                foreach (var item in data)
                {
                    string subject = $"Perusahaan {dataCompany.nama} Butuh Validasi";
                    string body = @$"<html>
                    <header>
                        <h3>Perusahaan {dataCompany.nama} Telah di Survey</h3>
                    </header>
                    <body>
                        <div>
                           Berikut merupakan data survey :
                        <div>
                        <br/>
                        <br/>
                        <div>
                            <ul>
                                <li>Nama Perusahaan : {dataCompany.nama}</li>
                                <li>Alamat Perusahaan : {dataCompany.alamat}</li>
                                <li>Domain Perusahaan : {dataCompany.domain}</li>
                                <li>No Telp Perusahaan : {dataCompany.noTelp}</li>
                                <li>Nama Surveyer : {dataSurveyer.nama}</li>
                                <li>ID Survey : {id}</li>
                            </ul>
                        </div>
                        <br/>
                         <div>
                            <b> Segera Melakukan Validasi Pada </b> : <a href='{linkSelf}'>{linkSelf}</a>
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

                    using (var message = new MailMessage(emailClient, item.email)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        smtp.Send(message);
                    }
                }
                _tracelogValidasi.WriteLog($"User {adminLogin} success generate and send email, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");

                _tracelogValidasi.WriteLog($"User {adminLogin} start update perusahaan survey, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.statusSurvey, "Accept").Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                _tracelogValidasi.WriteLog($"User {adminLogin} success update perusahaan survey, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");

                _tracelogValidasi.WriteLog($"User {adminLogin} start insert perusahaan admin, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                var perusahaanAdmin = new PerusahaanAdmin
                {
                    _id = ObjectId.GenerateNewId(),
                    idAdmin = null,
                    idPerusahaanSurvey = id,
                    status = "Pending",
                    statusDate = null,
                    addTime = DateTime.UtcNow,
                    updTime = DateTime.UtcNow
                };

                await _perusahaanAdminCollection.InsertOneAsync(perusahaanAdmin);
                _tracelogValidasi.WriteLog($"User {adminLogin} success insert perusahaan admin, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");

                _tracelogValidasi.WriteLog($"User {adminLogin} success Approval Surveyer, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                TempData["titlePopUp"] = "Berhasil Approve Perusahaan";
                TempData["icon"] = "success";
                TempData["text"] = "Approve Perusahaan Berhasil";
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} Failed Approval Surveyer, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}, error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Approve Perusahaan";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
        }


        [HttpGet]
        public async Task<ActionResult> RejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanSurveyer";
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.link = link;
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start akses {pathUrl}, with data : id = {id}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.idSurveyer = idSurveyer;
                _tracelogValidasi.WriteLog($"User {adminLogin} success akses {pathUrl}, with data : id = {id},  idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreateReject");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed akses {pathUrl}, with data : id = {id},  idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}, error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer, string alasanReject, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanSurveyer";
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start Reject Surveyer, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");

                var regex1 = new Regex(
                pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(1)
              );


                if (!regex1.IsMatch(alasanReject ?? string.Empty))
                {
                    _tracelogValidasi.WriteLog($"User {adminLogin} failed validation data {alasanReject} error : alasan Reject Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "alasan Reject Tidak Valid";
                    return RedirectToAction("Index");
                }

                _tracelogValidasi.WriteLog($"User {adminLogin} start update perusahaan survey, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.statusSurvey, "Reject").Set(p => p.updTime, DateTime.UtcNow).Set(p => p.alasanReject, alasanReject);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                _tracelogValidasi.WriteLog($"User {adminLogin} success update perusahaan survey, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");

                _tracelogValidasi.WriteLog($"User {adminLogin} start generate and send email, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                Company dataCompany = await _companyCollection
                  .Find(p => p._id == idPerusahaan)
                 .FirstOrDefaultAsync();


                surveyers dataSurveyer = await _surveyerCollection
                  .Find(p => p._id == idSurveyer)
                 .FirstOrDefaultAsync();

                string subject = $"Perusahaan {dataCompany.nama} Gagal Validasi";
                string body = @$"<html>
                    <header>
                        <h3>Perusahaan {dataCompany.nama} Gagal Validasi</h3>
                    </header>
                    <body>
                        <div>
                           Dengan berat hati kami sampaikan bahwa perusahaan dengan data sebagai berikut:                            
                        <div>
                        <br/>
                        <br/>
                        <div>
                            <ul>
                                <li>Nama Perusahaan : {dataCompany.nama}</li>
                                <li>Alamat Perusahaan : {dataCompany.alamat}</li>
                                <li>Domain Perusahaan : {dataCompany.domain}</li>
                                <li>No Telp Perusahaan : {dataCompany.noTelp}</li>
                                <li>Nama Surveyer : {dataSurveyer.nama}</li>
                                <li>ID Survey : {id}</li>
                            </ul>
                        </div>
                        <br/>
                         <div>
                           Gagal divalidasi oleh pihak kami, dikarenakan : 
                        </div>
                        <div>
                            {alasanReject}
                        </div>
                        <div>
                            Mohon Coba Perbaiki Perysaratan.
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

                using (var message = new MailMessage(emailClient, dataCompany.email!)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                _tracelogValidasi.WriteLog($"User {adminLogin} success generate and send email, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                TempData["titlePopUp"] = "Berhasil Reject Perusahaan";
                TempData["icon"] = "success";
                TempData["text"] = "Reject Perusahaan Berhasil";
                _tracelogValidasi.WriteLog($"User {adminLogin} success Reject Surveyer, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");

                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed Reject Surveyer, {pathUrl} with data id : {id.ToString()}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}, error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Reject Perusahaan";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
        }

        [HttpGet]
        public async Task<ActionResult> DetailRejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanSurveyer";
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start akses {pathUrl}, with data : id = {id}, idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.idSurveyer = idSurveyer;

                _tracelogValidasi.WriteLog("UserController Index view called");
                List<PerusahaanSurvey> docs = await _perusahaanSurveyCollection.Aggregate()
                  .Match(Builders<PerusahaanSurvey>.Filter.Eq(x => x._id, id))
                 .Lookup("companies", "idPerusahaan", "_id", "company")
                 .Lookup("Surveyers", "idSurveyer", "_id", "surveyer")
                 .As<PerusahaanSurvey>()
                 .ToListAsync();

                _tracelogValidasi.WriteLog($"User {adminLogin} success get data PerusahaanSurvey :{docs.ToString()}, from : {pathUrl}");
                _tracelogValidasi.WriteLog($"User {adminLogin} success akses {pathUrl}, with data : id = {id},  idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}");

                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreateDetailReject", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed akses {pathUrl}, with data : id = {id},  idPerusahaan : {idPerusahaan}, idSurveyer : {idSurveyer.ToString()}, error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanSurveyer");
            }
        }

        // Validasi Perusahaan Admin
        [HttpGet]
        public async Task<ActionResult> ValidasiPerusahaanAdmin()
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string pathUrl = HttpContext.Request.Path;
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("ValidasiPerusahaanAdmin");
            }

            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start akses {pathUrl}");

                List<PerusahaanAdminViewModel>? docs;
                docs = await _perusahaanAdminCollection.Aggregate()
                    .Lookup("PerusahaanSurvey", "idPerusahaanSurvey", "_id", "perusahaanSurvey")
                    .Unwind<PerusahaanAdmin>("perusahaanSurvey")
                    .Lookup("companies", "perusahaanSurvey.idPerusahaan", "_id", "company")
                    .Unwind<PerusahaanAdmin>("company")
                    .Lookup("Surveyers", "perusahaanSurvey.idSurveyer", "_id", "surveyer")
                    .Unwind<PerusahaanAdmin>("surveyer")
                    .Project<PerusahaanAdminViewModel>(new BsonDocument
                    {
                        { "perusahaanAdmin", "$$ROOT" },
                        { "perusahaanSurvey", "$perusahaanSurvey" },
                        { "company", "$company" },
                        { "surveyer", "$surveyer" },
                        { "_id", 0 }
                    })
                    .ToListAsync();

                docs = docs.Where(x => x.perusahaanSurvey.statusSurvey == "Accept").ToList();
                _tracelogValidasi.WriteLog($"User {adminLogin} success get data validasi admin :{docs.Count}, from : {pathUrl}");
                ViewBag.link = HttpContext.Request.Path;
                ViewBag.loginAs = HttpContext.Session.GetString("loginAs");
                _tracelogValidasi.WriteLog($"User {adminLogin} success akses {pathUrl}");
                return View("ValidasiPerusahaanAdmin/ValidasiPerusahaanAdmin", docs);
            }
            catch (Exception ex)
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} failed akses {pathUrl} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanAdmin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ApprovalAdmin(ObjectId id, ObjectId idPerusahaan, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanAdmin";
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                _tracelogValidasi.WriteLog("Unauthorized access attempt to ApprovalAdmin");
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start Approval Admin {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");


                string idAdmin = HttpContext.Session.GetString("idUser")!;
                ObjectId adminObjectId = ObjectId.Parse(idAdmin);
                _tracelogValidasi.WriteLog($"User {adminLogin} start update perusahaan admin {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                var filter = Builders<PerusahaanAdmin>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanAdmin>.Update.Set(p => p.idAdmin, adminObjectId).Set(p => p.status, "Accept").Set(p => p.statusDate, DateTime.UtcNow).Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanAdminCollection.UpdateOneAsync(filter, update);
                _tracelogValidasi.WriteLog($"User {adminLogin} success update perusahaan admin {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");

                _tracelogValidasi.WriteLog($"User {adminLogin} start Generate and Send Email {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                Company dataCompany = await _companyCollection
                  .Find(p => p._id == idPerusahaan)
                  .FirstOrDefaultAsync();

                string subject = $"Perusahaan {dataCompany.nama} Berhasil Validasi";
                string body = @$"<html>
                    <header>
                        <h3>Perusahaan {dataCompany.nama} Berhasil Validasi</h3>
                    </header>
                    <body>
                        <div>
                           Dengan senang hati kami sampaikan bahwa perusahaan dengan data sebagai berikut:                            
                        <div>
                        <br/>
                        <br/>
                        <div>
                            <ul>
                                <li>Nama Perusahaan : {dataCompany.nama}</li>
                                <li>Alamat Perusahaan : {dataCompany.alamat}</li>
                                <li>Domain Perusahaan : {dataCompany.domain}</li>
                                <li>No Telp Perusahaan : {dataCompany.noTelp}</li>
                            </ul>
                        </div>
                        <br/>
                         <div>
                           Telah berhasil divalidasi oleh pihak kami, dan dapat menggunakan layanan Ikodora.
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

                _tracelogValidasi.WriteLog("Preparing to send approval email to company");
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(emailClient, appPass)
                };
                using (var message = new MailMessage(emailClient, dataCompany.email!)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                _tracelogValidasi.WriteLog($"User {adminLogin} success Generate and Send Email {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                _tracelogValidasi.WriteLog($"User {adminLogin} start update perusahaan {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                var filterCompany = Builders<Company>.Filter.Eq(p => p._id, idPerusahaan);
                var updateCompany = Builders<Company>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);
                await _companyCollection.UpdateOneAsync(filterCompany, updateCompany);
                _tracelogValidasi.WriteLog($"User {adminLogin} success update perusahaan, {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                TempData["titlePopUp"] = "Berhasil Approve Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Approve Perusahaan Berhasil";
                _tracelogValidasi.WriteLog($"User {adminLogin} success Approval Admin {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                return RedirectToAction("ValidasiPerusahaanAdmin");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed Approval Admin {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}, error = {ex.Message}");
                TempData["titlePopUp"] = "Gagal Approve Admin";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanAdmin");
            }
        }

        [HttpGet]
        public async Task<ActionResult> RejectAdmin(ObjectId id, ObjectId idPerusahaan, string link)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanAdmin";
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            string pathUrl = HttpContext.Request.Path;
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            if (loginAs != "Admin")
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start akses {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                ViewBag.id = id;
                ViewBag.idCompany = idPerusahaan;
                _tracelogValidasi.WriteLog($"User {adminLogin} success akses {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan}");
                return PartialView("ValidasiPerusahaanAdmin/_Partials/_ModalReject");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed akses {pathUrl}, with data : id = {id}, idPerushaan = {idPerusahaan} error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanAdmin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> RejectAdmin(PerusahaanAdmin objData, ObjectId idPerusahaan, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanAdmin";
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            if (loginAs != "Admin")
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start  Reject Admin {pathUrl}, with data : data = {objData.ToString()}, idPerushaan = {idPerusahaan}");


                var regex1 = new Regex(
                pattern: @"^[A-Za-z0-9 _\-\(\)/\\]{0,150}$",
                options: RegexOptions.None,
                matchTimeout: TimeSpan.FromSeconds(1)
              );


                if (!regex1.IsMatch(objData.alasanReject ?? string.Empty))
                {
                    _tracelogValidasi.WriteLog($"User {adminLogin} failed validation data {objData.alasanReject} error : alasan Reject Tidak Valid, from : {pathUrl}");
                    TempData["titlePopUp"] = "Gagal Add Data";
                    TempData["icon"] = "error";
                    TempData["text"] = "alasan Reject Tidak Valid";
                    return RedirectToAction("Index");
                }



                string idAdmin = HttpContext.Session.GetString("idUser")!;
                ObjectId adminObjectId = ObjectId.Parse(idAdmin);
                _tracelogValidasi.WriteLog($"User {adminLogin} start update perusahaan admin {pathUrl}, with data : data = {objData.ToString()}, idPerushaan = {idPerusahaan}");

                var filter = Builders<PerusahaanAdmin>.Filter.Eq(p => p._id, objData._id);
                var update = Builders<PerusahaanAdmin>.Update.Set(p => p.idAdmin, adminObjectId).Set(p => p.status, "Reject").Set(p => p.statusDate, DateTime.UtcNow).Set(p => p.updTime, DateTime.UtcNow).Set(p => p.alasanReject, objData.alasanReject);
                await _perusahaanAdminCollection.UpdateOneAsync(filter, update);
                _tracelogValidasi.WriteLog($"User {adminLogin} success update perusahaan admin {pathUrl}, with data : data = {objData.ToString()}, idPerushaan = {idPerusahaan}");

                _tracelogValidasi.WriteLog($"User {adminLogin} start Generate and Send Email {pathUrl}, with data : data = {objData.ToString()}, idPerushaan = {idPerusahaan}");
                Company dataCompany = await _companyCollection
                  .Find(p => p._id == idPerusahaan)
                 .FirstOrDefaultAsync();

                string subject = $"Perusahaan {dataCompany.nama} Gagal Validasi";
                string body = @$"<html>
                    <header>
                        <h3>Perusahaan {dataCompany.nama} Gagal Validasi</h3>
                    </header>
                    <body>
                        <div>
                           Dengan berat hati kami sampaikan bahwa perusahaan dengan data sebagai berikut:                            
                        <div>
                        <br/>
                        <br/>
                        <div>
                            <ul>
                                <li>Nama Perusahaan : {dataCompany.nama}</li>
                                <li>Alamat Perusahaan : {dataCompany.alamat}</li>
                                <li>Domain Perusahaan : {dataCompany.domain}</li>
                                <li>No Telp Perusahaan : {dataCompany.noTelp}</li>
                            </ul>
                        </div>
                        <br/>
                         <div>
                           Gagal divalidasi oleh pihak kami, dikarenakan : 
                        </div>
                        <div>
                            {objData.alasanReject}
                        </div>
                        <div>
                            Mohon Coba Perbaiki Perysaratan.
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
                using (var message = new MailMessage(emailClient, dataCompany.email!)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                _tracelogValidasi.WriteLog($"User {adminLogin} success Generate and Send Email {pathUrl}, with data : data = {objData.ToString()}, idPerushaan = {idPerusahaan}");
                _tracelogValidasi.WriteLog($"User {adminLogin} success  Reject Admin {pathUrl}, with data : data = {objData.ToString()}, idPerushaan = {idPerusahaan}");
                TempData["titlePopUp"] = "Berhasil Reject Admin";
                TempData["icon"] = "success";
                TempData["text"] = "Reject Perusahaan Berhasil";
                return RedirectToAction("ValidasiPerusahaanAdmin");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed Reject Admin {pathUrl}, with data : data = {objData.ToString()}, idPerushaan = {idPerusahaan}, error = {ex.Message}");
                TempData["titlePopUp"] = "Gagal Reject Admin";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("ValidasiPerusahaanAdmin");
            }
        }

        [HttpGet]
        public async Task<ActionResult> DetailRejectAdmin(ObjectId id, string link)
        {
            string pathUrl = HttpContext.Request.Path;
            string adminLogin = HttpContext.Session.GetString("username")!;
            string linkTemp = "/Validasi/ValidasiPerusahaanAdmin";
            if (!generalFunction1.checkPrivilegeSession(adminLogin, linkTemp, link))
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = "Anda Tidak Memiliki Akses!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _tracelogValidasi.WriteLog($"User {adminLogin} start akses {pathUrl}, with data : id = {id}");

                PerusahaanAdmin docs = await _perusahaanAdminCollection
                    .Find(Builders<PerusahaanAdmin>.Filter.Eq(x => x._id, id))
                    .FirstOrDefaultAsync();
                _tracelogValidasi.WriteLog($"User {adminLogin} success akses {pathUrl}, with data : id = {id}");
                _tracelogValidasi.WriteLog($"User {adminLogin} success get data perusahaan admin :{docs.ToString()}, with data : id = {id}, from : {pathUrl}");
                return PartialView("ValidasiPerusahaanAdmin/_Partials/_ModalDetailReject", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog($"User {adminLogin} failed akses {pathUrl}, with data : id = {id}, error : {ex.Message}");
                TempData["titlePopUp"] = "Gagal Akses";
                TempData["icon"] = "error";
                TempData["text"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
