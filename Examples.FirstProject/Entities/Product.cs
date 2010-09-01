namespace Entities
{
    using System.Collections.Generic;
    using System.Globalization;

    public class Product
    {
        private CultureInfo ci = new CultureInfo("en-US");
        public virtual int Id { get; private set; }
        public virtual string Name { get; set; }
        public virtual double Price { get; set; }
        public virtual IList<Store> StoresStockedIn { get; private set; }

        public Product()
        {
            StoresStockedIn = new List<Store>();
        }

        public override string ToString()
        {
            return string.Format(ci, "Name={0}, Price={1:C2}", Name, Price);
        }
    }
}