using iTextSharp.text;
using iTextSharp.text.pdf;
using Jahhezni.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Jahhezni.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly JahhezniContext _context;

        public DashboardController(JahhezniContext context)
        {
            _context = context;
        }

       
        // لوحة تحكم الأدمن (عرض الكل)
        public async Task<IActionResult> AdminDashboard()
        {
            if (!User.IsInRole("Admin")) return Forbid();
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // --- عمليات الـ CRUD للأدمن ---

        // 1. حذف مستخدم
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AdminDashboard");
        }

        // 2. صفحة تعديل مستخدم (لأدمن يغير لغيره أو المستخدم لنفسه)
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            // التحقق: إما يكون أدمن أو بيعدل بيانات نفسه
            var currentUserId = User.FindFirst("UserId")?.Value;
            if (!User.IsInRole("Admin") && currentUserId != id.ToString()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User updatedUser)
        {
            var userInDb = await _context.Users.FindAsync(updatedUser.UserId);

            if (userInDb == null)
                return View(updatedUser);

            userInDb.Name = updatedUser.Name;
            userInDb.Email = updatedUser.Email;
            userInDb.Phone = updatedUser.Phone;
            userInDb.Address = updatedUser.Address;
            userInDb.Role = updatedUser.Role?.Trim();
            userInDb.ShopName = updatedUser.ShopName;

            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                userInDb.Password = BCrypt.Net.BCrypt.HashPassword(updatedUser.Password);
            }

            await _context.SaveChangesAsync();

            // 🔥 أهم سطر
            var role = userInDb.Role;

            if (role == "Vendor")
                return RedirectToAction("Index", "VendorDashboard");

            if (role == "Admin")
                return RedirectToAction("AdminDashboard", "Dashboard");

            return RedirectToAction("UserDashboard", "Dashboard");
        }
        public async Task<IActionResult> UserDashboard()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (userId == null)
                return RedirectToAction("Login", "Account");

            int id = int.Parse(userId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == id)
                .ToListAsync();

            // 1. عدد الطلبات الكلي
            ViewBag.TotalOrders = orders.Count();

            // 2. إجمالي المصروفات الحقيقي (مجموع كل الطلبات اللي عملتها العروسة في السيستم)
            ViewBag.TotalSpent = orders.Sum(x => x.OrderTotal);

            // 3. إجمالي ما تم دفعه (الطلبات التي تم دفعها + الطلبات التي تم تسليمها بنجاح)
            ViewBag.TotalPaid = orders
                .Where(x => x.Status != null && (
                    x.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                    x.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase)
                ))
                .Sum(x => x.OrderTotal);

            // 4. إجمالي المتبقي (الطلبات التي ما زالت تحت التنفيذ ولم تُدفع بعد)
            ViewBag.TotalPendingAmount = orders
                .Where(x => x.Status != null && (
                    x.Status.Equals("Pending Delivery", StringComparison.OrdinalIgnoreCase) ||
                    x.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                ))
                .Sum(x => x.OrderTotal);

            // 5. عدد الطلبات قيد التنفيذ كـ (عدد)
            ViewBag.PendingOrdersCount = orders
                .Count(x => x.Status != null && (
                    x.Status.Equals("Pending Delivery", StringComparison.OrdinalIgnoreCase) ||
                    x.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                ));

            ViewBag.Orders = orders;

            return View(user);
        }
    }
}