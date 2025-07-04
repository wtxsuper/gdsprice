using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class PricedProduct : CountedProduct
    {
        public bool Test { get; set; } = false;

        public PricedProduct(CountedProduct counted) : base(counted)
        {
            this.Test = true;
        }
    }
}
