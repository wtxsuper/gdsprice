namespace QuantityCalculator
{
    internal class CountedProduct : Product
    {
        public int WarehouseQuantity { get; set; }
        public int SupplierQuantity { get; set; }
        public int Quantity { get; set; }

        public CountedProduct(Product product)
        {
            Id = product.Id;
            Type = product.Type;
            SubProducts = product.SubProducts;
            Suppliers = product.Suppliers;
            Warehouses = product.Warehouses;
            WarehouseQuantity = CountAllWarehouse(product);
            SupplierQuantity = CountAllSupplier(product);
            Quantity = WarehouseQuantity + SupplierQuantity;
        }

        private int CountAllWarehouse(Product product)
        {
            int sum = 0;
            if (product.Type == "product" && product.Warehouses != null)
            {
                foreach (Warehouse w in product.Warehouses)
                {
                    sum += w.Quantity;
                }
            }
            else if (product.SubProducts != null)
            {
                int minSubWh = int.MaxValue; // minimum quantity in warehouses from subproducts for sets or variants
                foreach (Product sub in product.SubProducts)
                {
                    minSubWh = Math.Min(minSubWh, CountAllWarehouse(sub));
                }
                sum = minSubWh;
            }
            return sum;
        }

        private int CountAllSupplier(Product product)
        {
            int sum = 0;
            if (product.Type == "product" && product.Suppliers != null)
            {
                foreach (Supplier s in product.Suppliers)
                {
                    sum += s.Quantity;
                }
            }
            return sum;
        }
    }
}
