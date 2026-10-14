using Jahhezni.Helpers;
using Jahhezni.Models;
using Jahhezni.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jahhezni.Controllers
{
    public class PackageController : Controller
    {
        private readonly JahhezniContext _context;

        public PackageController(JahhezniContext context)
        {
            _context = context;
        }

        // =========================
        // Page: Package
        // =========================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            return View("package", products);
        }

        // =========================
        // Toggle Cart
        // =========================
        [HttpPost]
        public async Task<IActionResult> ToggleCart([FromBody] int productId)
        {
            var userIdClaim = User.FindFirst("UserId");

            if (userIdClaim == null)
                return Json(new { success = false, message = "يجب تسجيل الدخول" });

            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null)
            {
                list = new PreparationList
                {
                    UserId = userId,
                    Title = "Cart",
                    CreatedAt = DateTime.UtcNow,
                    TotalBudget = 0
                };

                _context.PreparationLists.Add(list);
                await _context.SaveChangesAsync();
            }

            var product = await _context.Products.FindAsync(productId);

            if (product == null)
                return Json(new { success = false });

            var item = await _context.PreparationItems
                .FirstOrDefaultAsync(x =>
                    x.ListId == list.ListId &&
                    x.ProductId == productId);

            if (item != null)
            {
                _context.PreparationItems.Remove(item);
            }
            else
            {
                _context.PreparationItems.Add(new PreparationItem
                {
                    ListId = list.ListId,
                    ProductId = productId,
                    DesiredQty = 1,
                    EstimatedPrice = product.Price
                });
            }

            await _context.SaveChangesAsync();

            // ✅ تحديث الإجمالي بشكل صحيح
            list.TotalBudget = await _context.PreparationItems
                .Where(x => x.ListId == list.ListId)
                .SumAsync(x => x.DesiredQty * x.EstimatedPrice);

            await _context.SaveChangesAsync();

            var count = await _context.PreparationItems
                .Where(x => x.ListId == list.ListId)
                .SumAsync(x => x.DesiredQty);

            return Json(new { success = true, count });
        }

        // =========================
        // Cart Page
        // =========================
        public async Task<IActionResult> CartPage()
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null || !list.PreparationItems.Any())
            {
                ViewBag.Total = 0;
                return View("~/Views/Home/CartPage.cshtml", new List<PreparationItem>());
            }

            ViewBag.Total = list.PreparationItems.Sum(x => x.DesiredQty * x.EstimatedPrice);

            return View("~/Views/Home/CartPage.cshtml", list.PreparationItems);
        }

        // =========================
        // Update Qty
        // =========================
        [HttpPost]
        public async Task<IActionResult> UpdateQty([FromBody] UpdateQtyRequest request)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null)
                return Json(new { success = false });

            var item = list.PreparationItems
                .FirstOrDefault(x => x.ItemId == request.ItemId);

            if (item == null)
                return Json(new { success = false });

            item.DesiredQty += request.Change;

            if (item.DesiredQty <= 0)
                _context.PreparationItems.Remove(item);

            await _context.SaveChangesAsync();

            list.TotalBudget = list.PreparationItems
                .Sum(x => x.DesiredQty * x.EstimatedPrice);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // =========================
        // Remove Item
        // =========================
        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null)
                return Json(new { success = false });

            var item = list.PreparationItems
                .FirstOrDefault(x => x.ItemId == request.ItemId);

            if (item != null)
                _context.PreparationItems.Remove(item);

            await _context.SaveChangesAsync();

            list.TotalBudget = list.PreparationItems
                .Sum(x => x.DesiredQty * x.EstimatedPrice);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // =========================
        // Cart State
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetCartState()
        {
            var userIdClaim = User.FindFirst("UserId");

            if (userIdClaim == null)
                return Json(new
                {
                    count = 0,
                    items = new List<int>()
                });

            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null)
                return Json(new
                {
                    count = 0,
                    items = new List<int>()
                });

            var items = list.PreparationItems
                .Select(x => x.ProductId)
                .ToList();

            var count = list.PreparationItems
                .Sum(x => x.DesiredQty);

            return Json(new
            {
                count,
                items
            });
        }
        // =========================
        // Confirm Order
        // =========================
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(string FullName, string Phone, string City, string Address)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null || !list.PreparationItems.Any())
                return RedirectToAction("CartPage");

            var order = new Order
            {
                UserId = userId,
                MerchantId = list.PreparationItems.First().Product.VendorId,
                OrderTotal = list.PreparationItems.Sum(x => x.DesiredQty * x.EstimatedPrice),
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in list.PreparationItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Qty = item.DesiredQty,
                    UnitPrice = item.EstimatedPrice
                });
            }

            _context.PreparationItems.RemoveRange(list.PreparationItems);
            list.TotalBudget = 0;

            await _context.SaveChangesAsync();

            // ✅ التعديل هنا: التوجيه لـ ConfirmOrderPage الاسم الصح للدالة اللي تحت
            return RedirectToAction("PaymentPage", new { orderId = order.OrderId });
        }

        // ✅ ضفنا الـ HttpGet هنا صراحة لضمان الأمان والتوجيه الصح
        [HttpGet]
        public IActionResult ConfirmOrderPage(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View("~/Views/Home/ConfirmOrder.cshtml");
        }
    }
}