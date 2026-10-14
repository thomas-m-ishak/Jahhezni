using Jahhezni.Models;
using Jahhezni.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jahhezni.Controllers
{
    public class PaymentController : Controller
    {
        private readonly JahhezniContext _context;
        private readonly IConfiguration _config;
        private readonly PaymobService _paymobService;

        public PaymentController(JahhezniContext context, IConfiguration config, PaymobService paymobService)
        {
            _context = context;
            _config = config;
            _paymobService = paymobService;
        }

        // ==========================================
        // 1. عرض صفحة اختيار طريقة الدفع
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order == null)
                return NotFound();

            ViewBag.OrderId = orderId;
            ViewBag.Total = order != null ? order.OrderTotal : 0;

            return View("~/Views/Home/Payment.cshtml");
        }

        // ==========================================
        // 2. استقبال الفورم والتحويل لبوابة الدفع المناسبة
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ProcessPaymentForm(int orderId, string PaymentMethod)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (order == null) return NotFound();

            // أ- لو العميل اختار كاش -> بنحوله فوراً لصفحة الفيد باك
            if (PaymentMethod == "Cash")
            {
                order.Status = "Pending Delivery";
                await _context.SaveChangesAsync();

                return RedirectToAction("Create", "Feedback", new { orderId = orderId });
            }

            // ب- لو اختار دفع إلكتروني (فيزا أو محفظة) -> بنكلم باي موب
            try
            {
                // 1. تجيب التوكن بالـ API Key القديم
                var authToken = await _paymobService.GetAuthToken();

                // 2. تفتح أوردر في باي موب بسعر الأوردر الحقيقي ديناميكياً
                var paymobOrderId = await _paymobService.CreateOrder(authToken, order.OrderTotal);

                // 3. تقرأ الـ Config من الـ Key القديم "Paymob"
                int integrationId = int.Parse(_config["Paymob:IntegrationId"]);
                string iframeId = _config["Paymob:IframeId"];

                // 4. توليد الـ Payment Token
                var paymentToken = await _paymobService.GetPaymentKey(authToken, paymobOrderId, order.OrderTotal, integrationId);

                if (PaymentMethod == "Card")
                {
                    string paymobUrl = $"https://accept.paymob.com/api/acceptance/iframes/{iframeId}?payment_token={paymentToken}";
                    return Redirect(paymobUrl);
                }
                else
                {
                    string walletUrl = await _paymobService.GetWalletPaymentUrl(paymentToken);
                    return Redirect(walletUrl);
                }
            }
            catch (Exception ex)
            {
                return Content($"خطأ في الاتصال ببوابة باي موب: {ex.Message}");
            }
        }


        // ============================================================
        // 3. الدالة السحرية (Callback) اللي باي موب هيرجع ينده عليها بعد الدفع
        // ============================================================
        [HttpGet]
        [Route("Payment/Callback")]
        public async Task<IActionResult> Callback()
        {
            var success = Request.Query["success"].ToString();
            var transactionId = Request.Query["id"].ToString();

            var merchantOrderIdStr = Request.Query["merchant_order_id"].ToString();
            var paymobOrderIdStr = Request.Query["order"].ToString();

            // 1. محاولة قراءة الرقم اللي أنت باعتها
            if (!int.TryParse(merchantOrderIdStr, out int orderId))
            {
                int.TryParse(paymobOrderIdStr, out orderId);
            }

            // 2. الحل السحري: لو الرقم مش متطابق مع الداتابيز أو راجع برقم باي موب المليوني
            // هنجيب آخر أوردر العميل ده لسه عامله حالاً عشان نربط عليه وم يقفش
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (order == null)
            {
                order = await _context.Orders.OrderByDescending(o => o.OrderId).FirstOrDefaultAsync();
                if (order != null)
                {
                    orderId = order.OrderId;
                }
            }

            // لو الجدول فاضي خالص (وده مستحيل يحصل وأنت بتجرب)
            if (order == null) return BadRequest("لا يوجد أي أوردر مسجل في النظام");

            // 3. لو الدفع نجح
            if (success == "true" || success.ToLower() == "true")
            {
                order.Status = "paid";

                var log = new ActivityLog
                {
                    UserId = order.UserId,
                    Action = "Payment completed via Paymob",
                    EntityName = "Payments",
                    Details = $"OrderId={order.OrderId}, Amount={order.OrderTotal}, TxnId={transactionId}",
                    Timestamp = DateTime.UtcNow
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                // هيحولك هنا ديركت على صفحة الفيدباك
                return RedirectToAction("Create", "Feedback", new { orderId = orderId });
            }

            // لو الدفع فشل
            order.Status = "failed";
            await _context.SaveChangesAsync();
            return Content("عفواً، لم تكتمل عملية الدفع الإلكتروني.");
        }
        [HttpGet]
        public async Task<IActionResult> PaymentDashboard()
        {
            // 1. جلب قائمة المدفوعات للجدول
            var payments = await _context.Orders
                .OrderByDescending(o => o.OrderId)
                .Select(o => new Jahhezni.ViewModels.PaymentVM
                {
                    OrderId = o.OrderId,
                    Amount = o.OrderTotal,
                    Method = o.Status == "paid" ? "Online" : "Cash",
                    Status = o.Status ?? "Pending",
                    Date = DateTime.Now // استبدله بـ o.CreatedAt لو موجود عندك
                })
                .ToListAsync();

            // 2. حساب الإحصائيات بناءً على الطلبات المدفوعة فقط (paid)
            decimal totalPaidRevenue = await _context.Orders
                .Where(o => o.Status == "paid")
                .SumAsync(o => (decimal?)o.OrderTotal) ?? 0;

            decimal ourCommission = totalPaidRevenue * 0.05m; // الـ 5% بتاعت جاهزني
            decimal vendorsShare = totalPaidRevenue * 0.95m;  // الـ 95% بتاعت البائعين

            // 3. تمرير الحسابات للـ View
            ViewBag.TotalRevenue = totalPaidRevenue;
            ViewBag.OurCommission = ourCommission;
            ViewBag.VendorsShare = vendorsShare;

            return View(payments);
        }
    }
}