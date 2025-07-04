namespace ClassLibrary
{
    public class CountedProduct : Product
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

            int whQty = 0;
            int supQty = 0;

            // Calculate warehouse quantity
            if (Type == "product" && Warehouses != null && Warehouses.Count > 0)
            {
                whQty = Warehouses.Sum(w => w.Quantity);
            }
            else if (SubProducts != null && SubProducts.Count > 0)
            {
                whQty = SubProducts.Min(sp => sp.Warehouses?.Sum(w => w.Quantity) ?? 0);
            }

            // Calculate supplier quantity
            if (Type == "product" && Suppliers != null && Suppliers.Count > 0)
            {
                supQty = Suppliers.Sum(s => s.Quantity);
            }

            WarehouseQuantity = whQty;
            SupplierQuantity = supQty;
            Quantity = whQty + supQty;
        }

        public CountedProduct()
        {
        }
    }
}
