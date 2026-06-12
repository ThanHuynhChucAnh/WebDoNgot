using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDoNgot.Models;
using WebDoNgot.Repositories;

namespace WebDoNgot.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // 1. HIỂN THỊ DANH SÁCH DANH MỤC
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        // 2. THÊM DANH MỤC (FORM GET)
        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Add()
        {
            return View();
        }

        // XỬ LÝ THÊM DANH MỤC (POST)
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Add(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepository.AddAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 3. CẬP NHẬT DANH MỤC (FORM GET)
        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Update(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // XỬ LÝ CẬP NHẬT DANH MỤC (POST)
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Update(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepository.UpdateAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 4. XÓA DANH MỤC (FORM XÁC NHẬN GET)
        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category); // Hiển thị trang xác nhận xóa
        }

        // XỬ LÝ XÓA DANH MỤC (POST)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}