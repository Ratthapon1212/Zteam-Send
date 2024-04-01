using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Zteam.Data;
using Zteam.Models;
using Zteam.ViewModels;

namespace Zteam.Controllers
{
    public class HomeController : Controller
    {
        public readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db) { _db = db; }


        //private readonly ILogger<HomeController> _logger;
        //public HomeController(ILogger<HomeController> logger)
        //{
        //    _logger = logger;
        //}





        public IActionResult Index()
        {
            IEnumerable<Game> game = _db.Game;
            return View(game);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public async Task<IActionResult> Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string userName, string userPass)
        {
            var cus = from c in _db.Customer
                      where c.CusName.Equals(userName)
                      && c.CusPass.Equals(userPass)
                      select c;

            if (cus.ToList().Count() == 0)
            {
                TempData["ErrorMessage"] = "ËÒ¢éÍÁÙÅäÁè¾º";
                return RedirectToAction("Index");
            }

            int CusId;
            string CusName;

            foreach (var item in cus)
            {
                CusId = item.CusId;
                CusName = item.CusName;

                HttpContext.Session.SetString("CusId", CusId.ToString());
                HttpContext.Session.SetString("CusName", CusName);

                var theRecord = _db.Customer.Find(CusId);
                theRecord.LastLogin = DateOnly.FromDateTime(DateTime.Now);

                _db.Entry(theRecord).State = EntityState.Modified;
            }

            _db.SaveChanges();

            return RedirectToAction("Index", "Home");

        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Customer model)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                if (_db.Customer.Any(c => c.CusName == model.CusName))
                {
                    ModelState.AddModelError("CusName", "Username already exists.");
                }

                if (_db.Customer.Any(c => c.CusEmail == model.CusEmail))
                {
                    ModelState.AddModelError("CusEmail", "Email address already exists.");
                }

                // If no duplicate username or email found, proceed with registration
                if (!ModelState.IsValid)
                {
                    // Redisplay the form with validation errors
                    return View(model);
                }

                var customer = new Customer
                {
                    CusName = model.CusName,
                    CusPass = model.CusPass,
                    CusEmail = model.CusEmail,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                _db.Customer.Add(customer);
                await _db.SaveChangesAsync(); // Use async version
                TempData["SuccessMessage"] = "Registration successful. You can now login.";
                return RedirectToAction("Login", "Home");
            }

            // If ModelState is not valid, redisplay the form with validation errors
            return View(model);
        }
        public IActionResult Show(string id)
        {
            //ตรวจสอบว่ามี id ส่งมาหรือไม่
            if (id == null)
            {
                TempData["ErrorMessage"] = "ต้องระบุ id";
                return RedirectToAction("Index");
            }
            // หาข้อมูลของ Customer.CusId จาก id ที่ส่งมา
            var obj = _db.Customer.Find(id);
            if (obj == null)
            {
                TempData["ErrorMessage"] = "ไม่พบ id ที่ระบุ";
                return RedirectToAction("Index");
            }
            //ตั้งชื่อ File img ของ Customer เป็น <รหัสผู้ใช้>.jpg
            var fileName = id.ToString() + ".jpg";
            // กำหนด Path - Directory ที่เก็บรูป -> imgcus 
            // แล้วทำมาต่อ Path อ้างอิ่งกับตำแหน่งที่ทำงานปัจจุบัน
            var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\imgcus");
            // เอา Path และ ชื่อ File มาต่อกัน
            var filePath = Path.Combine(imgPath, fileName);

            //ตรวจสอบว่ามี File อยู่ตาม Path ที่กำหนดหรือไม่
            //ถ้ามีก็ส่ง Path ไปให้ View ผ่าน ViewBag
            //ถ้าไม่มี ก็ให้เรียกรูปภาพ Default ที่สร้างไว้
            if (System.IO.File.Exists(filePath))
                ViewBag.ImgFile = "/imgcus/" + id + ".jpg";
            else
                ViewBag.ImgFile = "/image/login.png";

            //ถ้าหา id เจอส่ง obj ที่ได้จาก Query ไปให้ View
            return View(obj);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImgUpload(IFormFile imgfiles, string theid)
        {
            // กำหนดตัวแปรชื่อ File , Extension ของ File
            // รวมกันเป็นชื่อ File ที่ต้องการ Save
            var FileName = theid;
            var FileExtension = Path.GetExtension(imgfiles.FileName);
            var SaveFileName = FileName + FileExtension;
            // กำหนดตำแหน่งที่จะ Save File
            var SavePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\imgcus");
            // รวมชื่อและตำแหน่งที่จะ Save File
            var SaveFilePath = Path.Combine(SavePath, SaveFileName);
            //สั่งให้อ่าน File มาเป็น Stream และ Save ลงตำแหน่งที่กำหนด
            using (FileStream fs = System.IO.File.Create(SaveFilePath))
            {
                imgfiles.CopyTo(fs);
                fs.Flush();
            }
            // ย้ายไปทำงานที่ Action Show โดยกำหนดตัวแปร id จากตัวแปร theid
            return RedirectToAction("Show", new { id = theid });
        }

        public IActionResult ImgDelete(string id)
        {
            var DeleteFileName = id + ".jpg";
            var DeletePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\imgcus");
            var DeleteFilePath = Path.Combine(DeletePath, DeleteFileName);
            if (System.IO.File.Exists(DeleteFilePath))
            {
                System.IO.File.Delete(DeleteFilePath);
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่มีรูปที่ระบุ";
            }
            return RedirectToAction("Show", new { id = id });
        }
    }
}
