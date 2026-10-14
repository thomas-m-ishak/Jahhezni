using Jahhezni.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Jahhezni.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly JahhezniContext _context;

        public FeedbackController(JahhezniContext context)
        {
            _context = context;
        }

        // =========================
        // صفحة التقييم (Feedback/Create)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order == null)
                return NotFound();

            ViewBag.OrderId = orderId;

            return View(order);
        }

        // =========================
        // حفظ التقييم والـ Submit
        // =========================
        [HttpPost]
        public async Task<IActionResult> Submit(int orderId, int rating, string comment)
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order == null)
                return NotFound();

            // تقييم عام للطلب والتاجر
            var review = new Review
            {
                UserId = userId,
                MerchantId = order.MerchantId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Activity Log
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = "Add Feedback",
                EntityName = "Reviews",
                Details = $"User added feedback for Order #{orderId}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                DeviceInfo = Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow
            });

            // Notification
            _context.Notifications.Add(new Notification
            {
                UserId = order.MerchantId,
                Title = "تقييم جديد",
                Message = "لديك تقييم جديد من أحد العملاء",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            return RedirectToAction("ThankYou");
        }

        // =========================
        // صفحة الشكر (Feedback/ThankYou)
        // =========================
        public IActionResult ThankYou()
        {
            return View();
        }
    }
}