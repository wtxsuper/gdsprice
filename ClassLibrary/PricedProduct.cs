namespace ClassLibrary
{
    public class PricedProduct : CountedProduct
    {
        public double MinPrice { get; set; }

        public PricedProduct(CountedProduct counted) : base(counted)
        {
            double whPrice = 0;
            double supPrice = 0;

            // Calculte warehouse price
            if (Type == "product" && Warehouses != null && Warehouses.Count > 0)
            {
                whPrice = Warehouses.Average(w => w.Price);
            }
            else if (Type == "set" && SubProducts != null && SubProducts.Count > 0)
            {
                whPrice = SubProducts.Sum(sp => sp.Warehouses?.Average(w => w.Price * w.Quantity) ?? 0);
            }
            else if (Type == "variant" && SubProducts != null && SubProducts.Count > 0)
            {
                whPrice = SubProducts.Min(sp => sp.Warehouses?.Average(w => w.Price) ?? 0);
            }

            // Calculate supplier price
            if (Type == "product" && Suppliers != null && Suppliers.Count > 0)
            {
                supPrice = Suppliers.Min(s => s.Price);
            }

            // Determine minimum price
            if (whPrice > 0 && supPrice > 0)
            {
                MinPrice = Math.Min(whPrice, supPrice);
            }
            else if (whPrice > 0)
            {
                MinPrice = whPrice;
            }
            else if (supPrice > 0)
            {
                MinPrice = supPrice;
            }
            else
            {
                MinPrice = 0; // No price available
            }
        }
    }
}
