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
            WarehouseQuantity = 0;
            SupplierQuantity = 0;
            Quantity = 0;
        }
    }
}
