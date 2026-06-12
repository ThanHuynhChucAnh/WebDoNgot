using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebDoNgot.Extensions;
using WebDoNgot.Models;
using WebDoNgot.Repositories;

namespace WebDoNgot.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        // =============================================
        // 1. HIỂN THỊ GIỎ HÀNG
        // =============================================
        [AllowAnonymous]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            return View(cart);
        }

        // =============================================
        // 2. THÊM SẢN PHẨM VÀO GIỎ HÀNG
        // =============================================
        [AllowAnonymous]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Product");
            }

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

            var cartItem = new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            };

            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        // =============================================
        // 3. XÓA SẢN PHẨM KHỎI GIỎ HÀNG
        // =============================================
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.RemoveItem(productId);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return Json(new
            {
                success = true,
                cartTotal = cart.GetTotal().ToString("N0") + " đ"
            });
        }

        // =============================================
        // 4. CẬP NHẬT SỐ LƯỢNG (Tải lại trang)
        // =============================================
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            if (quantity < 1) quantity = 1;

            cart.UpdateQuantity(productId, quantity);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return Json(new
            {
                success = true,
                lineTotal = (cart.Items.First(i => i.ProductId == productId).Price * quantity).ToString("N0") + " đ",
                cartTotal = cart.GetTotal().ToString("N0") + " đ"
            });
        }

        // =============================================
        // 5. XÓA TOÀN BỘ GIỎ HÀNG
        // =============================================
        [AllowAnonymous]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            TempData["Success"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction("Index");
        }

        // =============================================
        // 6. TRANG THANH TOÁN (GET)
        // =============================================
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            // Điền sẵn địa chỉ từ thông tin user
            var user = await _userManager.GetUserAsync(User);
            var order = new Order
            {
                ShippingAddress = user?.Address ?? ""
            };

            ViewBag.Cart = cart;
            return View(order);
        }

        // =============================================
        // 7. XỬ LÝ THANH TOÁN (POST)
        // =============================================
        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.ShippingFee = cart.ShippingFee;
            order.Discount = cart.Discount;
            order.TotalPrice = cart.GetGrandTotal();
            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            return View("OrderCompleted", order.Id);
        }

        // =============================================
        // 8. LỊCH SỬ MUA HÀNG (PHÂN QUYỀN ADMIN / USER)
        // =============================================
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            // Lấy ClaimsIdentity của User hiện tại để trích xuất UserId
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var userId = claim.Value;

            // Khởi tạo truy vấn gốc (IQueryable) từ bảng Orders
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser); // Nạp thêm thông tin User để hiển thị tên khách hàng nếu cần

            List<Order> orders;

            // KIỂM TRA PHÂN QUYỀN: Nếu là Admin thì lấy hết, nếu là User thường thì chỉ lấy đơn hàng của họ
            if (User.IsInRole(SD.Role_Admin))
            {
                orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else
            {
                orders = await query
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            return View(orders);
        }

        // =============================================
        // 9. CHI TIẾT ĐƠN HÀNG (CẬP NHẬT CHO ADMIN)
        // =============================================
        [Authorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var userId = claim.Value;

            // Tạo truy vấn lấy thông tin đơn hàng
            var orderQuery = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(o => o.ApplicationUser)
                .AsQueryable();

            Order? order;

            // Nếu là Admin thì có quyền xem chi tiết đơn hàng bất kỳ dựa vào ID đơn hàng
            if (User.IsInRole(SD.Role_Admin))
            {
                order = await orderQuery.FirstOrDefaultAsync(o => o.Id == id);
            }
            else // Nếu là User thường thì bắt buộc phải đúng ID đơn hàng và đúng UserId của chính họ
            {
                order = await orderQuery.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            }

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        // =============================================
        // 10. MÃ GIẢM GIÁ
        // ============================================

        [HttpPost]
        public IActionResult ApplyCoupon(string couponCode)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (couponCode == "GIAMGIA10")
            {
                cart.Discount = cart.GetTotal() * 0.1m;
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                return Json(new { success = true, discount = cart.Discount, grandTotal = cart.GetGrandTotal() });
            }
            return Json(new { success = false, message = "Mã không hợp lệ" });
        }

        // =============================================
        // 11. ADMIN - QUẢN LÝ ĐƠN HÀNG
        // =============================================
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> OrderManagement(string? status)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.AllStatuses = new[]
            {
                SD.OrderStatus_Processing,
                SD.OrderStatus_Shipped,
                SD.OrderStatus_Delivered,
                SD.OrderStatus_Cancelled
            };

            // Thống kê nhanh cho header
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.ProcessingCount = await _context.Orders.CountAsync(o => o.Status == SD.OrderStatus_Processing);
            ViewBag.ShippedCount = await _context.Orders.CountAsync(o => o.Status == SD.OrderStatus_Shipped);
            ViewBag.DeliveredCount = await _context.Orders.CountAsync(o => o.Status == SD.OrderStatus_Delivered);
            ViewBag.CancelledCount = await _context.Orders.CountAsync(o => o.Status == SD.OrderStatus_Cancelled);

            return View(orders);
        }

        // =============================================
        // 12. ADMIN - CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        // =============================================
        [Authorize(Roles = SD.Role_Admin)]
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus });
        }

        // =============================================
        // 13. ADMIN - DASHBOARD DOANH THU
        // =============================================
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> RevenueDashboard()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .ToListAsync();

            var deliveredOrders = orders.Where(o => o.Status == SD.OrderStatus_Delivered).ToList();

            // KPI
            var totalRevenue = deliveredOrders.Sum(o => o.TotalPrice);
            var totalOrders = orders.Count;
            var deliveredCount = deliveredOrders.Count;
            var aov = deliveredCount > 0 ? totalRevenue / deliveredCount : 0;
            var conversionRate = totalOrders > 0
                ? Math.Round((double)deliveredCount / totalOrders * 100, 1)
                : 0;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.AOV = aov;
            ViewBag.ConversionRate = conversionRate;

            // Biểu đồ doanh thu 6 tháng gần nhất (chỉ đơn Đã giao)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var revenueByMonth = deliveredOrders
                .Where(o => o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                    Revenue = g.Sum(o => o.TotalPrice),
                    Count = g.Count()
                })
                .OrderBy(x => x.Label)
                .ToList();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(revenueByMonth.Select(x => x.Label));
            ViewBag.ChartRevenue = System.Text.Json.JsonSerializer.Serialize(revenueByMonth.Select(x => x.Revenue));
            ViewBag.ChartOrders = System.Text.Json.JsonSerializer.Serialize(revenueByMonth.Select(x => x.Count));

            // Top 5 sản phẩm bán chạy nhất (theo số lượng)
            var topProducts = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => new { od.ProductId, Name = od.Product?.Name ?? "N/A", ImageUrl = od.Product?.ImageUrl })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Key.ImageUrl,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.Price)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToList();

            ViewBag.TopProducts = topProducts;

            // Thống kê trạng thái cho donut chart
            ViewBag.ProcessingCount = orders.Count(o => o.Status == SD.OrderStatus_Processing);
            ViewBag.ShippedCount = orders.Count(o => o.Status == SD.OrderStatus_Shipped);
            ViewBag.DeliveredCount = deliveredCount;
            ViewBag.CancelledCount = orders.Count(o => o.Status == SD.OrderStatus_Cancelled);

            return View();
        }
    }
}