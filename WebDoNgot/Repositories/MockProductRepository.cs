using WebDoNgot.Models;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace WebDoNgot.Repositories
{
    public class MockProductRepository : IProductRepository
    {
        private readonly List<Product> _products;
        public MockProductRepository()
        {
            // Tạo một số dữ liệu mẫu
            _products = new List<Product>
        {
                new Product { Id = 1, Name = "Banh Croissant Bo Toi", Price = 17000, Description = "Thom, Beo", ImageUrl = "/images/croissant.jpg" },
                new Product { Id = 2, Name = "Keo Dynamite", Price = 3000, Description = "Bung No", ImageUrl = "/images/dynamite.jpg" },
                new Product { Id = 3, Name = "Cookie New York", Price= 15000, Description="Đỉnh của chóp", ImageUrl="/images/cookie.jpg"}
            };
        }
        public IEnumerable<Product> GetAll()
        {
            return _products;
        }
        public Product GetById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }
        public void Add(Product product)
        {
            product.Id = _products.Max(p => p.Id) + 1;
            _products.Add(product);
        }
        public void Update(Product product)
        {
            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index != -1)
            {
                _products[index] = product;
            }
        }
        public void Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _products.Remove(product);
            }
        }
    }
}
