using Jahhezni.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Security.Claims;

namespace Jahhezni.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly JahhezniContext _context;

        public AdminDashboardController(JahhezniContext context)
        {
            _context = context;
        }

        // =========================
        // Dashboard الرئيسية
        // =========================
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var users = await _context.Users.ToListAsync();

            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            // 🔍 Search by OrderId or User Name
            if (!string.IsNullOrEmpty(search))
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.User.Name.Contains(search) ||
                    o.OrderId.ToString().Contains(search));
            }

            // 🎯 Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var orders = await ordersQuery.ToListAsync();

            // ===== Stats =====
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();

            // حساب الإيرادات من الطلبات التي تم تسليمها فقط (أو إجمالي المبيعات حسب رغبتك)
            // يفضل Delivered لضمان الحساب الفعلي، وهنا سنمشي على طلبك لحساب الإجمالي المتاح
            decimal totalSales = await _context.Orders.SumAsync(o => o.OrderTotal);
            ViewBag.TotalSales = totalSales;

            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "pending");
            ViewBag.DeliveredOrders = await _context.Orders.CountAsync(o => o.Status == "delivered");

            // 🔥 [الحل المالي الحاسم]: حساب الأرباح والمستحقات المصفّرة
            ViewBag.PlatformProfit = totalSales * 0.05m; // 5% أرباح المنصة
            ViewBag.VendorMoney = totalSales * 0.95m;   // 95% مستحقات البائعين

            // 🔔 [الإشعارات]: حساب عدد المنتجات غير النشطة (المعلقة) للكارت الجديد
            ViewBag.PendingProductsCount = await _context.Products.CountAsync(p => !p.IsActive);

            ViewBag.Orders = orders;
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(users);
        }

        // ===================================
        // صفحة عرض المنتجات المعلقة (GET)
        // ===================================
        [HttpGet]
        public async Task<IActionResult> PendingProducts()
        {
            var pendingProducts = await _context.Products
                .Where(p => !p.IsActive)
                .ToListAsync();

            return View(pendingProducts);
        }

        // ===================================
        // تنفيذ الموافقة وتفعيل المنتج (POST)
        // ===================================
        [HttpPost]
        public async Task<IActionResult> ApproveProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return NotFound();

            product.IsActive = true; // تفعيل المنتج ليظهر في الويب ودواشبرد التاجر
            _context.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("PendingProducts"); // يرجعه لصفحة المنتجات المعلقة
        }

        // =========================
        // تغيير حالة الطلب
        // =========================
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int orderId, string status)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { success = false });

            var oldStatus = order.Status;
            order.Status = status;

            // =========================
            // Activity Log
            // =========================
            var log = new ActivityLog
            {
                UserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0"),
                Action = "Change Order Status",
                EntityName = "Orders",
                Details = $"Order #{order.OrderId} changed from {oldStatus} to {status}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                DeviceInfo = Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // =========================
        // حذف مستخدم
        // =========================
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // =========================
        // حذف طلب
        // =========================
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order != null)
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            var logs = await _context.ActivityLogs
                .Where(x => x.EntityName == "Orders" && x.Details.Contains(id.ToString()))
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            ViewBag.Logs = logs;

            return View(order);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            MemoryStream stream = new MemoryStream();
            Document doc = new Document();
            PdfWriter.GetInstance(doc, stream);

            doc.Open();

            doc.Add(new Paragraph("Jahhezni Invoice"));
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph($"Order ID: {order.OrderId}"));
            doc.Add(new Paragraph($"Customer: {order.User.Name}"));
            doc.Add(new Paragraph($"Date: {order.CreatedAt}"));
            doc.Add(new Paragraph($"Status: {order.Status}"));
            doc.Add(new Paragraph(" "));

            doc.Add(new Paragraph("Items:"));
            doc.Add(new Paragraph(" "));

            foreach (var item in order.OrderItems)
            {
                string line = $"{item.Product.Name} - Qty: {item.Qty} - Price: {item.UnitPrice}";
                doc.Add(new Paragraph(line));
            }

            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph($"Total: {order.OrderTotal} EGP"));

            doc.Close();

            byte[] bytes = stream.ToArray();
            stream.Close();

            return File(bytes, "application/pdf", $"Invoice_{order.OrderId}.pdf");
        }

        public IActionResult HomePage()
        {
            return View();
        }
        // ===================================
        // تنفيذ رفض وحذف المنتج المعلق (POST)
        // ===================================
        [HttpPost]
        public async Task<IActionResult> RejectProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return NotFound();

            // بنشيل المنتج نهائي طالما مرفوض ومفتشيش عليه ومظهرش في الموقع قبل كدة
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("PendingProducts"); // يرجعه لصفحة المنتجات المعلقة
        }
    }
}