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

        // 1. Hiển thị danh sách danh mục
        public IActionResult Index()
        {
            var categories = _categoryRepository.GetAll();
            return View(categories);
        }

        // 2. Thêm danh mục (Form)
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Add(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // 3. Cập nhật danh mục (Form)
        [HttpGet]
        public IActionResult Update(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public IActionResult Update(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Update(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // 4. Xóa danh mục
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();
            return View(category); // Hiển thị trang xác nhận xóa
        }

        [HttpPost, ActionName("Delete")] // Đổi ActionName về "Delete"
        public IActionResult DeleteConfirmed(int id)
        {
            _categoryRepository.Delete(id);
            return RedirectToAction("Index");
        }
    }
}