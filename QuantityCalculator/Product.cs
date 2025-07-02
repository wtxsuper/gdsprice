using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantityCalculator
{
    internal class Product
    {
        public required string Id { get; set; }
        public required string Type { get; set; }
        public List<Supplier>? Suppliers { get; set; }
        public List<Product>? SubProducts { get; set; }
        public List<Warehouse>? Warehouses { get; set; }
        public int? WarehouseQuantity { get; set; }
        public int? SupplierQuantity { get; set; }
        public int? Quantity { get; set; }
    }
}
