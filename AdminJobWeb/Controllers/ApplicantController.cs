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
        }

        //Aplicant

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            try
            {
                List<Applicant?> applicants = await _applicantCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {applicants.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
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
        public async Task<ActionResult> BlockApplicant(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Block").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _applicantCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateApplicant(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var update = Builders<Applicant>.Update.Set(p => p.statusAccount, "Active").Set(p => p.updTime, DateTime.UtcNow);

                var result = await _applicantCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteApplicant(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Applicant>.Filter.Eq(p => p._id, id);
                var result = await _applicantCollection.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Delete Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Delete Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }


        //Experience
        [HttpGet]
        public async Task<ActionResult> Experience()
        {
            try
            {
                List<Experience?> experiences = await _experienceCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {experiences.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Experience/Experience",experiences);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddExperience()
        {
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
        public async Task<ActionResult> EditExperience(ObjectId id)
        {
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
        public async Task<ActionResult> AddExperience(Experience data)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var checkEdu = await _experienceCollection
                         .Find(Builders<Experience>.Filter.Or(
                             Builders<Experience>.Filter.Eq(p => p.namaPerusahaan, data.namaPerusahaan)))
                        .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var experienceInsert = new Experience
                {
                    _id = ObjectId.GenerateNewId(),
                    addId = null,
                    addTime = DateTime.Now,
                    lokasi = data.lokasi,
                    namaPerusahaan = data.namaPerusahaan,
                    industri=data.industri,
                    status = "Active"
                };

                await _experienceCollection.InsertOneAsync(experienceInsert);
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditExperience(Experience data)
        {
            try
            {
                var checkEdu = await _experienceCollection
                        .Find(Builders<Experience>.Filter.And(
                            Builders<Experience>.Filter.Eq(p => p.namaPerusahaan, data.namaPerusahaan),
                            Builders<Experience>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Experience>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Experience>.Update.Set(p => p.namaPerusahaan, data.namaPerusahaan).Set(p => p.lokasi, data.lokasi).Set(p=>p.industri,data.industri);
                await _experienceCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteExperience(ObjectId id)
        {

            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {
                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var result = await _experienceCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveExperience(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var update = Builders<Experience>.Update.Set(p => p.status, "Inactive");

                var result = await _experienceCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateExperience(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Experience>.Filter.Eq(p => p._id, id);
                var update = Builders<Experience>.Update.Set(p => p.status, "Active");

                var result = await _experienceCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }


        //Education
        [HttpGet]
        public async Task<ActionResult> Education()
        {
            try
            {
                List<Education?> educations = await _educationCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {educations.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Education/Education", educations);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddEducation()
        {
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
        public async Task<ActionResult> EditEducation(ObjectId id)
        {
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
        public async Task<ActionResult> AddEducation(Education data)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var checkEdu = await _educationCollection
                         .Find(Builders<Education>.Filter.Or(
                             Builders<Education>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
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
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditEducation(Education data)
        {
            try
            {
                var checkEdu = await _educationCollection
                        .Find(Builders<Education>.Filter.And(
                            Builders<Education>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Education>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Education>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Education>.Update.Set(p => p.nama, data.nama).Set(p => p.lokasi, data.lokasi);
                await _educationCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteEducation(ObjectId id)
        {

            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {
                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var result = await _educationCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }


        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveEducation(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var update = Builders<Education>.Update.Set(p => p.status, "Inactive");

                var result = await _educationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ActivateEducation(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Education>.Filter.Eq(p => p._id, id);
                var update = Builders<Education>.Update.Set(p => p.status, "Active");

                var result = await _educationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }



        //Skill
        [HttpGet]
        public async Task<ActionResult> Skill()
        {
            try
            {
                List<Skill?> skills = await _skillCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {skills.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Skill/Skill",skills);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddSkill()
        {
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
        public async Task<ActionResult> EditSkill(ObjectId id)
        {
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
        public async Task<ActionResult> AddSkill(Skill data)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var checkSkill = await _skillCollection
                         .Find(Builders<Skill>.Filter.Or(
                             Builders<Skill>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkSkill > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Skill';</script>", "text/html");
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
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Skill';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Skill';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditSkill(Skill data)
        {
            try
            {
                var checkEdu = await _skillCollection
                        .Find(Builders<Skill>.Filter.And(
                            Builders<Skill>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Skill>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkEdu > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Skill>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Skill>.Update.Set(p => p.nama, data.nama);
                await _skillCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteSkill(ObjectId id)
        {

            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {
                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var result = await _skillCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }


        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveSkill(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var update = Builders<Skill>.Update.Set(p => p.status, "Inactive");

                var result = await _skillCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateSkill(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Skill>.Filter.Eq(p => p._id, id);
                var update = Builders<Skill>.Update.Set(p => p.status, "Active");

                var result = await _skillCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }


        //Organization
        [HttpGet]
        public async Task<ActionResult> Organization()
        {
            try
            {
                List<Organization?> organizations = await _organizationCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {organizations.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Organization/Organization",organizations);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddOrganization()
        {
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
        public async Task<ActionResult> EditOrganization(ObjectId id)
        {
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
        public async Task<ActionResult> AddOrganization(Organization data)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var checkOrganization = await _organizationCollection
                         .Find(Builders<Organization>.Filter.Or(
                             Builders<Organization>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkOrganization > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Skill';</script>", "text/html");
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
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Skill';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Skill';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditOrganization(Organization data)
        {
            try
            {
                var checkOrganization = await _organizationCollection
                        .Find(Builders<Organization>.Filter.And(
                            Builders<Organization>.Filter.Eq(p => p.nama, data.nama),
                            Builders<Organization>.Filter.Ne(p => p._id, data._id)))
                       .CountDocumentsAsync();

                if (checkOrganization > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Organization>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Organization>.Update.Set(p => p.nama, data.nama).Set(p => p.lokasi, data.lokasi);
                await _organizationCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteOrganization(ObjectId id)
        {

            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {
                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var result = await _organizationCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        public async Task<ActionResult> InactiveOrganization(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var update = Builders<Organization>.Update.Set(p => p.status, "Inactive");

                var result = await _organizationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateOrganization(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Organization>.Filter.Eq(p => p._id, id);
                var update = Builders<Organization>.Update.Set(p => p.status, "Active");

                var result = await _organizationCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }


        //Certificate
        [HttpGet]
        public async Task<ActionResult> Certificate()
        {
            try
            {
                List<Certificate?> certificates = await _certificateCollection.Find(_ => true).ToListAsync();
                Debug.WriteLine($"Retrieved {certificates.Count} admin users from the database.");

                ViewBag.username = HttpContext.Session.GetInt32("username");
                ViewBag.role = HttpContext.Session.GetInt32("role");
                return View("Certificate/Certificate",certificates);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Home/Index';</script>", "text/html");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AddCertificate()
        {
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
        public async Task<ActionResult> EditCertificate(ObjectId id)
        {
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
        public async Task<ActionResult> AddCertificate(Certificate data)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var checkCertificate = await _certificateCollection
                         .Find(Builders<Certificate>.Filter.Or(
                             Builders<Certificate>.Filter.Eq(p => p.nama, data.nama)))
                        .CountDocumentsAsync();

                if (checkCertificate > 0)
                {
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Skill';</script>", "text/html");
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
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Skill';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Skill';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> EditCertificate(Certificate data)
        {
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
                    return Content($"<script>alert('Nama Education Sudah Ada');window.location.href='/Applicant/Education';</script>", "text/html");
                }

                var username = HttpContext.Session.GetString("username");
                var filter = Builders<Certificate>.Filter.Eq(p => p._id, data._id);
                var update = Builders<Certificate>.Update.Set(p => p.nama, data.nama).Set(p => p.publisher, data.publisher);
                await _certificateCollection.UpdateOneAsync(filter, update);
                return Content($"<script>alert('Success Add Education');window.location.href='/Applicant/Education';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Content($"<script>alert('{ex.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> DeleteCertificate(ObjectId id)
        {

            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {
                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var result = await _certificateCollection.DeleteOneAsync(filter);


                if (result.DeletedCount == 0)
                {
                    return Content("<script>alert('Gagal Delete Surveyer!');window.location.href='/Surveyer/Index'</script>", "text/html");
                }

                return Content("<script>alert('Berhasil Delete Surveyer!');window.location.href='/Applicant/Education'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Education';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> InactiveCertificate(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var update = Builders<Certificate>.Update.Set(p => p.status, "Inactive");

                var result = await _certificateCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Block Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> ActivateCertificate(ObjectId id)
        {
            string adminLogin = HttpContext.Session.GetString("username")!;
            try
            {

                var filter = Builders<Certificate>.Filter.Eq(p => p._id, id);
                var update = Builders<Certificate>.Update.Set(p => p.status, "Active");

                var result = await _certificateCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Gagal Block Surveyer");
                    return Content("<script>alert('Gagal Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
                }

                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Berhasil Block Surveyer");
                return Content("<script>alert('Berhasil Activate Applicant!');window.location.href='/Applicant/Index'</script>", "text/html");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                //_tracelogSurveyer.WriteLog($"User : {adminLogin}, Failed Block Surveyer, Reason : {e.Message}");
                return Content($"<script>alert('{e.Message}');window.location.href='/Applicant/Index';</script>", "text/html");
            }
        }

    }
}
