using WebDoNgot.Models;

namespace WebDoNgot.Repositories
{
    public class MockCategoryRepository : ICategoryRepository
    {
        private static List<Category> _categoryList;

        public MockCategoryRepository()
        {
            if (_categoryList == null)
            {
                _categoryList = new List<Category>
                {
                    new Category { Id = 1, Name = "Banh" },
                    new Category { Id = 2, Name = "Keo" },
                };
            }
        }

        public IEnumerable<Category> GetAll() => _categoryList;

        public Category GetById(int id)
        {
            return _categoryList.FirstOrDefault(c => c.Id == id);
        }

        public void Add(Category category)
        {
            category.Id = _categoryList.Any()
                ? _categoryList.Max(c => c.Id) + 1
                : 1;

            _categoryList.Add(category);
        }

        public void Update(Category category)
        {
            var existingCategory = GetById(category.Id);

            if (existingCategory != null)
            {
                existingCategory.Name = category.Name;
            }
        }

        public void Delete(int id)
        {
            var category = GetById(id);

            if (category != null)
            {
                _categoryList.Remove(category);
            }
        }
    }
}