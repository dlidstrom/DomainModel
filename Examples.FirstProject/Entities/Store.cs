namespace Entities
{
    using System.Collections.Generic;
    using System.Text;

    public class Store
    {
        public virtual int Id { get; private set; }
        public virtual string Name { get; set; }
        public virtual IList<Product> Products { get; set; }
        public virtual IList<Employee> Staff { get; set; }

        public Store()
        {
            Products = new List<Product>();
            Staff = new List<Employee>();
        }

        public virtual void AddProduct(Product product)
        {
            product.StoresStockedIn.Add(this);
            Products.Add(product);
        }

        public virtual void AddEmployee(Employee employee)
        {
            employee.Store = this;
            Staff.Add(employee);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Format("{0}\n{1}", Name, string.Empty.PadLeft(Name.Length, '*')));
            foreach (var product in Products)
            {
                builder.AppendLine("Product: " + product);
            }
            foreach (var staff in Staff)
            {
                builder.AppendLine("Employee: " + staff);
            }

            return builder.ToString().Trim();
        }
    }
}
