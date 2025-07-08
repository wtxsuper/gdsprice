namespace ClassLibrary
{
    public class Product
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public List<Supplier>? Suppliers { get; set; }
        public List<Product>? SubProducts { get; set; }
        public List<Warehouse>? Warehouses { get; set; }
    }
}
