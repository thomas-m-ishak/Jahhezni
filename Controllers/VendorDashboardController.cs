using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Jahhezni.Models; 

public class VendorDashboardController : Controller
{
    private readonly JahhezniContext _context;

    public VendorDashboardController(JahhezniContext context)
    {
        _context = context;
    }

    // 1. عرض منتجات التاجر الحالي فقط
    public async Task<IActionResult> Index()
    {
        var userIdString = HttpContext.Session.GetString("UserId")
            ?? User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userIdString))
            return RedirectToAction("Login", "Account");

        var userId = int.Parse(userIdString);

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (merchant == null)
            return RedirectToAction("Login", "Account");

        // منتجات التاجر فقط
        var products = await _context.Products
            .Where(p => p.VendorId == merchant.UserId && p.IsActive)
            .ToListAsync();



        // ===============================
        // الطلبات الخاصة بمنتجات التاجر
        // ===============================

        var orders = await _context.Orders
            .Include(o => o.User)
            .Where(o => o.OrderItems
            .Any(i => i.Product.VendorId == merchant.UserId))
            .ToListAsync();



        // ===============================
        // الإحصائيات
        // ===============================

        ViewBag.TotalProducts = products.Count();

        ViewBag.TotalOrders = orders.Count();

        ViewBag.LowStock = products.Count(p => p.Stock < 5);

        ViewBag.TotalSales = orders
            .Where(o => o.Status == "delivered")
            .Sum(o => o.OrderTotal);

        ViewBag.Orders = orders;

        return View(products);
    }

    // 2. صفحة إضافة منتج (GET)
    [HttpGet]
    public IActionResult AddProduct()
    {
        return View();
    }

    // 3. تنفيذ إضافة منتج (POST)
    [HttpPost]
    public async Task<IActionResult> AddProduct(Product product, IFormFile imageFile)
    {
        var userIdString = HttpContext.Session.GetString("UserId")
            ?? User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userIdString))
            return RedirectToAction("Login", "Account");

        var userId = int.Parse(userIdString);
        var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.UserId == userId);

        if (merchant == null)
            return RedirectToAction("Login", "Account");

        // =========================
        // 🔥 رفع الصورة + منع التكرار
        // =========================

        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileExtension = Path.GetExtension(imageFile.FileName);

            // اسم فريد قوي (منع التكرار بنسبة شبه 100%)
            var uniqueName = Guid.NewGuid().ToString() + fileExtension;

            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            product.Images = uniqueName;
        }

        // ربط التاجر بالـ UserId الصح زي ما قفلناها سوى
        product.VendorId = merchant.UserId;

        // 🔒 التعديل هنا: خليناها false عشان ميتعرضش في الموقع علطول لحين موافقتك
        product.IsActive = false;
        product.CreatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // ==========================================
        // 🔔 إرسال الإشعار التلقائي لك في جدول Notifications
        // ==========================================
        try
        {
            // افترضنا إن الـ UserId بتاعك كأدمن هو 1 (عدله حسب الـ ID بتاعك في الداتابيز)
            var adminId = 1;

            var adminNotification = new Notification
            {
                UserId = adminId,
                Title = "منتج جديد في انتظار موافقتك",
                Message = $"قام التاجر ({merchant.StoreName}) بإضافة منتج جديد: ({product.Name}). برجاء مراجعته وتفعيله.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(adminNotification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // سطر احتياطي عشان لو حصل أي حاجة في الإشعار ميعطلش عملية حفظ المنتج الأساسية
            System.Diagnostics.Debug.WriteLine($"Notification Error: {ex.Message}");
        }

        // تقدر تستخدم الـ TempData دي في الـ View عشان تظهر رسالة تأكيد للتاجر إن منتجه قيد المراجعة
        TempData["Message"] = "تم حفظ المنتج بنجاح، وهو قيد المراجعة حالياً من قبل الإدارة.";

        return RedirectToAction(nameof(Index));
    }
    // 4. صفحة تعديل منتج (GET)
    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        return View(product);
    }

    // 5. تنفيذ تعديل منتج (POST)
    [HttpPost]
    public async Task<IActionResult> EditProduct(Product model, IFormFile imageFile)
    {
        var existingProduct = await _context.Products.FindAsync(model.ProductId);

        if (existingProduct == null) return NotFound();

        existingProduct.Name = model.Name;
        existingProduct.Price = model.Price;
        existingProduct.Description = model.Description;
        existingProduct.Category = model.Category;

        // لو المستخدم رفع صورة جديدة
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");

            var fileExtension = Path.GetExtension(imageFile.FileName);
            var uniqueName = Guid.NewGuid().ToString() + fileExtension;

            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            existingProduct.Images = uniqueName;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    // 1. صفحة تأكيد الحذف (GET)
    [HttpGet]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        return View(product);
    }

    // 2. تنفيذ الحذف الفعلي (POST)
    [HttpPost, ActionName("DeleteProduct")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            // بدل ما نمسحه نهائي، بنخليه غير نشط عشان الداتابيز متضربش لو مربوط بطلبات
            product.IsActive = false;
            _context.Update(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}