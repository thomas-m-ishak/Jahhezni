using Jahhezni.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Jahhezni.Controllers
{
    public class AccountController : Controller
    {
        private readonly JahhezniContext _context;

        public AccountController(JahhezniContext context)
        {
            _context = context;
        }

        // =========================
        // Login
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null &&
                    BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("UserId", user.UserId.ToString())
                    };

                    var claimsIdentity =
                        new ClaimsIdentity(
                            claims,
                            CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    // Admin
                    if (user.Role == "Admin")
                    {
                        return RedirectToAction(
                            "Index",
                            "AdminDashboard");
                    }

                    // Vendor
                    if (user.Role == "Vendor")
                    {
                        return RedirectToAction(
                            "Index",
                            "VendorDashboard");
                    }

                    // Bride
                    return RedirectToAction(
                        "UserDashboard",
                        "Dashboard");
                }

                ModelState.AddModelError(
                    "",
                    "الإيميل أو كلمة السر خطأ");
            }

            return View(model);
        }

        // =========================
        // Logout
        // =========================

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);

                // ✅ تصليح الكراش: إعطاء قيمة لـ EntityName تمنع الـ NULL في الداتابيز
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    Action = "Logout",
                    EntityName = "Users", // الكلمة السحرية اللي هتمنع خطأ الـ SQL
                    Details = "User logged out successfully",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    DeviceInfo = Request.Headers["User-Agent"].ToString(),
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            // كود مسح الكوكي أو السيشن حسب مشروعك
            // مثلاً: await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Welcome", "Home");
        }

        // =========================
        // Register
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
     User user,
     string? MerchantName,
     string? Category)
        {
            // تثبيت الـ Role
            user.Role = user.Role?.Trim();

            // لو مشتري
            if (user.Role == "Bride")
            {
                MerchantName = null;
                Category = null;
            }

            if (ModelState.IsValid)
            {
                bool emailExists =
                    await _context.Users.AnyAsync(
                        u => u.Email == user.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "هذا البريد مستخدم بالفعل");

                    return View(user);
                }

                user.Password =
                    BCrypt.Net.BCrypt.HashPassword(user.Password);

                user.CreatedAt = DateTime.Now;

                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                // Vendor فقط
                if (user.Role == "Vendor")
                {
                    Merchant merchant = new Merchant()
                    {
                        UserId = user.UserId,
                        StoreName = MerchantName ?? "",
                        Category = Category ?? "",
                        CreatedAt = DateTime.Now,
                        IsStoreActive = true
                    };

                    _context.Merchants.Add(merchant);

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Login");
            }

            // مهم جدًا عشان تشوف الخطأ الحقيقي
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage);
            }

            return View(user);
        }
    }
}