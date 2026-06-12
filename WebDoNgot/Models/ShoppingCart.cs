using System.Collections.Generic;
using System.Linq;

namespace WebDoNgot.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public void AddItem(CartItem item)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                Items.Add(item);
            }
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var existing = Items.FirstOrDefault(i => i.ProductId == productId);
            if (existing == null) return;

            if (quantity <= 0)
            {
                Items.Remove(existing);
            }
            else
            {
                existing.Quantity = quantity;
            }
        }

        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }

        public void Clear() => Items.Clear();

        public decimal GetTotal() => Items.Sum(i => i.Price * i.Quantity);

        public int GetTotalQuantity() => Items.Sum(i => i.Quantity);
    }
}
