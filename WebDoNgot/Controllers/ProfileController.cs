using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebDoNgot.Models;

namespace WebDoNgot.Controllers
{
    [Authorize] // Chỉ cho phép người dùng đã đăng nhập truy cập
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 1. HIỂN THỊ HỒ SƠ (GET)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải thông tin người dùng.");
            }

            var model = new Profile
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address
            };

            return View(model);
        }

        // 2. XỬ LÝ CẬP NHẬT HỒ SƠ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Profile model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tìm thấy người dùng.");
            }

            // Cập nhật thông tin mở rộng từ ApplicationUser
            user.FullName = model.FullName;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                // Làm mới lại cookie phiên đăng nhập để cập nhật dữ liệu mới nhất
                await _signInManager.RefreshSignInAsync(user);
                TempData["StatusMessage"] = "Hồ sơ cá nhân của bạn đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}