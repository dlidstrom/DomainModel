namespace NhTest
{
	using System;
	using System.IO;
	using System.Linq;
	using FluentNHibernate.Cfg;
	using FluentNHibernate.Cfg.Db;
	using FluentNHibernate.Mapping;
	using Iesi.Collections.Generic;
	using NHibernate;
	using NHibernate.Tool.hbm2ddl;

	public class Board
	{
		private readonly ISet<Board> successors = new HashedSet<Board>();
		private readonly ISet<Board> parents = new HashedSet<Board>();

		public virtual Guid Id
		{
			get;
			set;
		}

		public virtual Int64 Empty
		{
			get;
			set;
		}

		public virtual Int64 Mover
		{
			get;
			set;
		}

		public virtual System.Collections.Generic.IEnumerable<Board> Successors
		{
			get { return successors; }
		}

		public virtual void AddSuccessor(Board successor)
		{
			successors.Add(successor);
		}

		public virtual System.Collections.Generic.IEnumerable<Board> Parents
		{
			get { return parents; }
		}

		public virtual void AddParent(Board parent)
		{
			parents.Add(parent);
		}

		public override string ToString()
		{
			return "{" + string.Format("0x{0:X} 0x{1:X}", Empty, Mover) + "}";
		}

		public class Map : ClassMap<Board>
		{
			public Map()
			{
				Id(x => x.Id);
				NaturalId().Property(x => x.Empty).Property(x => x.Mover);
				HasManyToMany(x => x.Successors)
					.Access.ReadOnlyPropertyThroughLowerCaseField()
					.ParentKeyColumn("ParentId")
					.ChildKeyColumn("ChildId")
					.Cascade.All()
					.AsSet()
					.Table("Successors");
				HasManyToMany(x => x.Parents)
					.Access.ReadOnlyPropertyThroughLowerCaseField()
					.ParentKeyColumn("ChildId")
					.ChildKeyColumn("ParentId")
					.Cascade.All() // test
					.AsSet()
					.Table("Parents");
			}
		}
	}

	class Program
	{
		static ISessionFactory CreateSessionFactory()
		{
			return Fluently
				.Configure()
				.Database(SQLiteConfiguration.Standard.UsingFile("positions.db"))
				.Mappings(m => m.FluentMappings.AddFromAssemblyOf<Board>())
				.ExposeConfiguration(c =>
				{
					if (File.Exists("positions.db"))
						File.Delete("positions.db");
					new SchemaExport(c).Create(true, true);
				})
				.BuildSessionFactory();
		}

		static void Main(string[] args)
		{
			try
			{
				new Program().Run2();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		void Run2()
		{
			var sessionFactory = CreateSessionFactory();
			using (var session = sessionFactory.OpenSession())
			using (var tx = session.BeginTransaction())
			{
				int currentChild = 0;
				for (int i = 0; i < 1000; i++)
				{
					if ((i % 10) == 0)
					{
						Console.Write(".");
					}

					var parent = new Board { Empty = i, Mover = i };

					for (int j = 0; j >= -10; j--)
					{
						var child = new Board { Empty = currentChild--, Mover = currentChild-- };
#if false
						// fetch child from store, if it exists
						child = session.CreateQuery("from Board b where b.Empty = :empty and b.Mover = :mover")
							.SetInt64("empty", child.Empty)
							.SetInt64("mover", child.Mover)
							.UniqueResult<Board>() ?? child;
#endif
						parent.AddSuccessor(child);
						child.AddParent(parent);
					}

					session.Save(parent);
				}

				Console.WriteLine();
				tx.Commit();
			}

			using (var session = sessionFactory.OpenSession())
			using (var tx = session.BeginTransaction())
			{
				foreach (var pos in session.CreateQuery("from Board").Enumerable<Board>().Take(10))
				{
					Console.WriteLine("Position: {0}", pos);
					Console.WriteLine("Successors:");
					foreach (var successor in pos.Successors)
					{
						Console.WriteLine("\t{0}, parent = {1}", successor, string.Join(",", successor.Parents));
					}
				}

				tx.Commit();
			}
		}
	}
}

#if false
namespace Examples.FirstProject
{
    using Entities;
    using NHibernate;
    using System;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using System.Diagnostics;
    using NHibernate.Cfg;
    using NHibernate.Tool.hbm2ddl;
    using System.IO;

    class Program
    {
        private static string DbFile = "store.db";

        static void Main(string[] args)
        {
            try
            {
                test();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadKey();
        }

        private static void test()
        {
            var sessionFactory = CreateSessionFactory();

            using (var session = sessionFactory.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var barginBasin = new Store { Name = "Bargin Basin" };
                    var superMart = new Store { Name = "SuperMart" };

                    var potatoes = new Product { Name = "Potatoes", Price = 3.60 };
                    var fish = new Product { Name = "Fish", Price = 4.49 };
                    var milk = new Product { Name = "Milk", Price = 0.79 };
                    var bread = new Product { Name = "Bread", Price = 1.29 };
                    var cheese = new Product { Name = "Cheese", Price = 2.10 };
                    var waffles = new Product { Name = "Waffles", Price = 2.41 };

                    var daisy = new Employee { FirstName = "Daisy", LastName = "Harrison" };
                    var jack = new Employee { FirstName = "Jack", LastName = "Torrance" };
                    var sue = new Employee { FirstName = "Sue", LastName = "Walkters" };
                    var bill = new Employee { FirstName = "Bill", LastName = "Taft" };
                    var joan = new Employee { FirstName = "Joan", LastName = "Pope" };

                    // add products to the stores, there's some crossover in the products in each
                    // store, because the store-product relationship is many-to-many
                    AddProductsToStore(barginBasin, potatoes, fish, milk, bread, cheese);
                    AddProductsToStore(superMart, bread, cheese, waffles);

                    // add employees to the stores, this relationship is a one-to-many, so one
                    // employee can only work at one store at a time
                    AddEmployeesToStore(barginBasin, daisy, jack, sue);
                    AddEmployeesToStore(superMart, bill, joan);

                    // save both stores, this saves everything else via cascading
                    session.SaveOrUpdate(barginBasin);
                    session.SaveOrUpdate(superMart);

                    transaction.Commit();
                }

                // retreive all stores and display them
                using (session.BeginTransaction())
                {
                    var stores = session.CreateCriteria<Store>().List<Store>();

                    foreach (var store in stores)
                    {
                        Console.WriteLine(store);
                    }
                }
            }
        }

        private static void AddEmployeesToStore(Store store, params Employee[] employees)
        {
            foreach (var employee in employees)
            {
                store.AddEmployee(employee);
            }
        }

        private static void AddProductsToStore(Store store, params Product[] products)
        {
            foreach (var product in products)
            {
                store.AddProduct(product);
            }
        }

        private static ISessionFactory CreateSessionFactory()
        {
            var cfg = new Configuration();
            cfg.Properties.Add("show_sql", "true");
            return Fluently.Configure(cfg)
                .Database(SQLiteConfiguration.Standard.UsingFile(DbFile))
				//.Database(SQLiteConfiguration.Standard.InMemory())
				//.Database(SQLiteConfiguration.Standard.UsingFile(":memory:"))
				.Mappings(m => m.FluentMappings.AddFromAssemblyOf<Store>())
                .ExposeConfiguration(BuildSchema)
                .BuildSessionFactory();
        }

        private static void BuildSchema(Configuration config)
        {
            if (File.Exists(DbFile))
            {
                File.Delete(DbFile);
            }

            new SchemaExport(config).Create(false, true);
        }
    }
}
#endif
