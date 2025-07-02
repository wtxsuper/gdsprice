using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantityCalculator
{
    internal class Supplier
    {
        public required string Id { get; set; }

        public required int Quantity { get; set; }

        public required double Price { get; set; }
    }
}
