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
            try
            {
                _tracelogValidasi.WriteLog("UserController Index view called");
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
                      .As<PerusahaanSurvey>()
                      .ToListAsync();
                }

                ViewBag.loginAs = HttpContext.Session.GetString("loginAs");
                return View("ValidasiPerusahaanSurveyer/ValidasiPerusahaanSurveyer", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddSurveyer(ObjectId id, ObjectId idPerusahaan, string namaPerusahaan)
        {
            try
            {
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.namaPerusahaan = namaPerusahaan;
                List<surveyers> surveyer = await _surveyerCollection.Find(_ => true).ToListAsync();
                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreate", surveyer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer)
        {
            try
            {
                surveyers dataSurveyer = await _surveyerCollection
                .Find(p => p._id == idSurveyer)
               .FirstOrDefaultAsync();

                Company dataCompany = await _companyCollection
                  .Find(p => p._id == idPerusahaan)
                 .FirstOrDefaultAsync();

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


                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.idSurveyer, idSurveyer).Set(p => p.statusSurvey, "Pending").Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Berhasil Add Surveyer');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ProcessSurveyer(ObjectId id, ObjectId idSurveyer)
        {
            try
            {
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.idSurveyer, idSurveyer).Set(p => p.statusSurvey, "Process").Set(p => p.dateSurvey, DateTime.UtcNow).Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Survey Dilakukan!');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ApprovalSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer)
        {
            try
            {
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

                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.statusSurvey, "Accept").Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);

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
                return Content($"<script>alert('Berhasil Accept Perusahaan');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }


        [HttpGet]
        public async Task<ActionResult> RejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer)
        {
            try
            {
                ViewBag.id = id;
                ViewBag.idPerusahaan = idPerusahaan;
                ViewBag.idSurveyer = idSurveyer;
                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreateReject");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> RejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer, string alasanReject)
        {
            try
            {
                var filter = Builders<PerusahaanSurvey>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanSurvey>.Update.Set(p => p.statusSurvey, "Reject").Set(p => p.updTime, DateTime.UtcNow).Set(p => p.alasanReject, alasanReject);
                await _perusahaanSurveyCollection.UpdateOneAsync(filter, update);

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

                return Content($"<script>alert('Berhasil Reject Perusahaan');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> DetailRejectSurveyer(ObjectId id, ObjectId idPerusahaan, ObjectId idSurveyer)
        {
            try
            {
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

                return View("ValidasiPerusahaanSurveyer/_Partials/_ModalCreateDetailReject", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in UserController Index: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanSurveyer';</script>", "text/html");
            }
        }

        // Validasi Perusahaan Admin
        [HttpGet]
        public async Task<ActionResult> ValidasiPerusahaanAdmin()
        {
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogValidasi.WriteLog("ValidasiController ValidasiPerusahaanAdmin view called");

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

                // Filter hanya yang sudah di Approve oleh Surveyer
                docs = docs.Where(x => x.perusahaanSurvey.statusSurvey == "Accept").ToList();

                ViewBag.loginAs = HttpContext.Session.GetString("loginAs");
                return View("ValidasiPerusahaanAdmin/ValidasiPerusahaanAdmin", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in ValidasiController ValidasiPerusahaanAdmin: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ApprovalAdmin(ObjectId id, ObjectId idPerusahaan)
        {
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                _tracelogValidasi.WriteLog("Unauthorized access attempt to ApprovalAdmin");
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogValidasi.WriteLog("ValidasiController ApprovalAdmin called");

                string idAdmin = HttpContext.Session.GetString("idUser")!;
                ObjectId adminObjectId = ObjectId.Parse(idAdmin);

                var filter = Builders<PerusahaanAdmin>.Filter.Eq(p => p._id, id);
                var update = Builders<PerusahaanAdmin>.Update.Set(p => p.idAdmin, adminObjectId).Set(p => p.status, "Accept").Set(p => p.statusDate, DateTime.UtcNow).Set(p => p.updTime, DateTime.UtcNow);
                await _perusahaanAdminCollection.UpdateOneAsync(filter, update);

                // Send Email to Company
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
                _tracelogValidasi.WriteLog("Approval email sent successfully");

                // Update company status
                var filterCompany = Builders<Company>.Filter.Eq(p => p._id, idPerusahaan);
                var updateCompany = Builders<Company>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);
                await _companyCollection.UpdateOneAsync(filterCompany, updateCompany);

                _tracelogValidasi.WriteLog("ValidasiController ApprovalAdmin completed successfully");
                return Content($"<script>alert('Approval Perusahaan Berhasil');window.location.href='/Validasi/ValidasiPerusahaanAdmin';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in ValidasiController ApprovalAdmin: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanAdmin';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> RejectAdmin(ObjectId id, ObjectId idPerusahaan)
        {
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                ViewBag.id = id;
                ViewBag.idCompany = idPerusahaan;

                return PartialView("ValidasiPerusahaanAdmin/_Partials/_ModalReject");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in ValidasiController RejectAdmin: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanAdmin';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> RejectAdmin(PerusahaanAdmin objData, ObjectId idPerusahaan)
        {
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }
            try
            {
                _tracelogValidasi.WriteLog("ValidasiController RejectAdmin called");

                string idAdmin = HttpContext.Session.GetString("idUser")!;
                ObjectId adminObjectId = ObjectId.Parse(idAdmin);

                var filter = Builders<PerusahaanAdmin>.Filter.Eq(p => p._id, objData._id);
                var update = Builders<PerusahaanAdmin>.Update.Set(p => p.idAdmin, adminObjectId).Set(p => p.status, "Reject").Set(p => p.statusDate, DateTime.UtcNow).Set(p => p.updTime, DateTime.UtcNow).Set(p => p.alasanReject, objData.alasanReject);
                await _perusahaanAdminCollection.UpdateOneAsync(filter, update);

                // Send Email to Company
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

                _tracelogValidasi.WriteLog("Preparing to send rejection email to company");
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
                _tracelogValidasi.WriteLog("Rejection email sent successfully");

                _tracelogValidasi.WriteLog("ValidasiController RejectAdmin completed successfully");
                return Content($"<script>alert('Reject Perusahaan Berhasil');window.location.href='/Validasi/ValidasiPerusahaanAdmin';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in ValidasiController RejectAdmin: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanAdmin';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> DetailRejectAdmin(ObjectId id)
        {
            string loginAs = HttpContext.Session.GetString("loginAs")!;
            if (loginAs != "Admin")
            {
                return Content("<script>alert('Anda Tidak Memiliki Akses!');window.location.href='/Home/Index'</script>", "text/html");
            }

            try
            {
                _tracelogValidasi.WriteLog("ValidasiController DetailRejectAdmin view called");

                PerusahaanAdmin docs = await _perusahaanAdminCollection
                    .Find(Builders<PerusahaanAdmin>.Filter.Eq(x => x._id, id))
                    .FirstOrDefaultAsync();

                return PartialView("ValidasiPerusahaanAdmin/_Partials/_ModalDetailReject", docs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _tracelogValidasi.WriteLog("Error in ValidasiController DetailRejectAdmin: " + ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Validasi/ValidasiPerusahaanAdmin';</script>", "text/html");
            }
        }
    }
}
