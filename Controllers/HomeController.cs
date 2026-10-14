using Microsoft.AspNetCore.Mvc;
using Jahhezni.Models;
using Jahhezni.ViewModels;
using Microsoft.EntityFrameworkCore;
using Jahhezni.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jahhezni.Controllers
{
    public class HomeController : Controller
    {
        private readonly JahhezniContext _context;

        public HomeController(JahhezniContext context)
        {
            _context = context;
        }

        // Welcome Page
        public IActionResult Welcome()
        {
            return View();
        }

        // Home Page
        public IActionResult HomePage()
        {
            var viewModel = new HomeViewModel
            {
                Categories = _context.Categories.AsNoTracking().ToList(),
                Merchants = _context.Users
                    .Where(u => u.Role == "Merchant")
                    .Take(5)
                    .ToList(),

                Products = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .ToList()
            };

            return View(viewModel);
        }

        // ================= SEARCH =================
        [HttpGet]
        public IActionResult Search(string searchWord)
        {
            if (string.IsNullOrWhiteSpace(searchWord))
            {
                return RedirectToAction("HomePage");
            }

            string cleanWord = searchWord.Trim().ToLower();

            var results = _context.Products
                .Where(p => p.IsActive &&
                    ((p.Name != null && p.Name.ToLower().Contains(cleanWord)) ||
                     (p.Description != null && p.Description.ToLower().Contains(cleanWord)))
                )
                .ToList();

            ViewBag.SearchTerm = searchWord;
            return View("SearchResult", results);
        }

        // ================= ADD TO CART =================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Json(new { success = false, message = "يجب تسجيل الدخول أولاً يا هندسة" });

            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null)
            {
                list = new PreparationList { UserId = userId, Title = "Cart", CreatedAt = DateTime.UtcNow, TotalBudget = 0 };
                _context.PreparationLists.Add(list);
                await _context.SaveChangesAsync();
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return Json(new { success = false });

            var item = await _context.PreparationItems.FirstOrDefaultAsync(x => x.ListId == list.ListId && x.ProductId == productId);
            bool isAdded;

            if (item != null)
            {
                _context.PreparationItems.Remove(item);
                isAdded = false;
            }
            else
            {
                _context.PreparationItems.Add(new PreparationItem { ListId = list.ListId, ProductId = productId, DesiredQty = 1, EstimatedPrice = product.Price });
                isAdded = true;
            }
            await _context.SaveChangesAsync();

            list.TotalBudget = await _context.PreparationItems.Where(x => x.ListId == list.ListId).SumAsync(x => x.DesiredQty * x.EstimatedPrice);
            await _context.SaveChangesAsync();

            var count = await _context.PreparationItems.Where(x => x.ListId == list.ListId).SumAsync(x => x.DesiredQty);

            return Json(new { success = true, cartCount = count, isAdded = isAdded });
        }

        // ================= CART PAGE =================
        public async Task<IActionResult> CartPage()
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Welcome");

            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null || !list.PreparationItems.Any())
            {
                ViewBag.Total = 0;
                return View(new List<PreparationItem>());
            }

            ViewBag.Total = list.PreparationItems.Sum(x => x.DesiredQty * x.EstimatedPrice);
            return View(list.PreparationItems);
        }

        // ================= PACKAGE =================
        [HttpPost]
        public IActionResult GeneratePackage(decimal budget)
        {
            var allProducts = _context.Products
                .Where(p => p.IsActive && p.Price != null && p.Category != null)
                .ToList();

            var selectedPackage = new List<Product>();
            decimal currentTotal = 0;

            var productsByCategory = allProducts
                .GroupBy(p => p.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Items = g.OrderBy(x => Guid.NewGuid()).ToList()
                })
                .ToList();

            bool addedAny;

            do
            {
                addedAny = false;

                foreach (var group in productsByCategory)
                {
                    var product = group.Items
                        .FirstOrDefault(p => !selectedPackage.Contains(p));

                    if (product != null && (currentTotal + product.Price <= budget))
                    {
                        selectedPackage.Add(product);
                        currentTotal += product.Price;
                        addedAny = true;
                    }
                }

            } while (addedAny);

            ViewBag.Budget = budget;
            ViewBag.TotalAmount = currentTotal;

            return View("Package", selectedPackage);
        }

        // ================= CATEGORY =================
        public IActionResult Category(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("HomePage");
            }

            var products = _context.Products
                .Where(p => p.IsActive && p.Category == id)
                .ToList();

            ViewBag.CategoryName = id switch
            {
                "Furniture" => "قسم الأثاث المتميز 🛋️",
                "Electronic devices" => "قسم الأجهزة الكهربائية 📺",
                "Appliances" => "قسم الأدوات المنزلية 🍽️",
                "Kitchen" => "قسم المطابخ العصرية 🍳",
                "Clothing" => "قسم الملابس والأناقة 👗",
                _ => id
            };

            return View("CategoryPage", products);
        }

        // ================= CHECKOUT & PROCESS =================

        // 1. صفحة الـ ConfirmOrder (مراجعة السلة وملء بيانات الشحن)
        [HttpGet]
        public async Task<IActionResult> ConfirmOrder()
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Welcome");

            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null || !list.PreparationItems.Any()) return RedirectToAction("CartPage");

            ViewBag.TotalAmount = list.PreparationItems.Sum(x => x.DesiredQty * x.EstimatedPrice);
            return View(list.PreparationItems);
        }

        // 2. معالجة البيانات وتحويل اليوزر لصفحة الـ Payment (جوه مجلد الهوم) فوراً
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(string FullName, string Phone, string City, string Address, string PaymentMethod)
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Welcome");

            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.PreparationLists
                .Include(x => x.PreparationItems).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (list == null || !list.PreparationItems.Any()) return RedirectToAction("CartPage");

            // كود حفظ الأوردر في الداتابيز
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

            // تنظيف السلة
            _context.PreparationItems.RemoveRange(list.PreparationItems);
            list.TotalBudget = 0;
            await _context.SaveChangesAsync();
            // التوجيه لصفحة الدفع في الكنترولر الجديد مع تمرير الـ ID الصح للاوردر
            return RedirectToAction("Payment", "Payment", new { orderId = order.OrderId });
        }
    }
}