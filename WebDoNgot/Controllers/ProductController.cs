using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using WebDoNgot.Models;
using WebDoNgot.Repositories;
using System.IO;

namespace WebDoNgot.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // 1. HIỂN THỊ DANH SÁCH SẢN PHẨM
        public IActionResult Index()
        {
            var products = _productRepository.GetAll();
            return View(products);
        }

        // 2. HIỂN THỊ CHI TIẾT SẢN PHẨM (Sửa lỗi 404)
        [HttpGet]
        public IActionResult Display(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound(); 
            }
            return View(product); 
        }

        // 3. THÊM MỚI SẢN PHẨM
        [HttpGet]
        public IActionResult Add()
        {
            var categories = _categoryRepository.GetAll();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult Add(Product product, IFormFile imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = SaveImage(imageUrl);
                }
                _productRepository.Add(product);
                return RedirectToAction("Index");
            }

            var categories = _categoryRepository.GetAll();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        // 4. CẬP NHẬT SẢN PHẨM
        [HttpGet]
        public IActionResult Update(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        public IActionResult Update(Product product, IFormFile imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = SaveImage(imageUrl);
                }
                _productRepository.Update(product);
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // 5. XÓA SẢN PHẨM
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public IActionResult DeleteConfirmed(int id)
        {
            _productRepository.Delete(id);
            return RedirectToAction("Index");
        }

        // Hàm hỗ trợ sao chép ảnh đồng bộ
        private string SaveImage(IFormFile image)
        {
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.FileName);
            var directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                image.CopyTo(fileStream);
            }
            return "/images/" + image.FileName;
        }
    }
}