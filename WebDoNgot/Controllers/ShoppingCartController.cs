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
        [AllowAnonymous]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.RemoveItem(productId);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        // =============================================
        // 4. CẬP NHẬT SỐ LƯỢNG (Tải lại trang)
        // =============================================
        [AllowAnonymous]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

            // Khống chế số lượng tối thiểu tránh lỗi
            if (quantity < 1) quantity = 1;

            cart.UpdateQuantity(productId, quantity);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index");
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
            order.TotalPrice = cart.GetTotal();
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
    }
}