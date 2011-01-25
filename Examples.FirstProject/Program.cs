namespace NhTest
{
	using System;
	using System.IO;
	using FluentNHibernate.Cfg;
	using FluentNHibernate.Cfg.Db;
	using FluentNHibernate.Mapping;
	using Iesi.Collections.Generic;
	using NHibernate;
	using NHibernate.Tool.hbm2ddl;

	public class Board
	{
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

		public virtual ISet<Board> Successors
		{
			get;
			set;
		}

		public virtual ISet<Board> Parents
		{
			get;
			set;
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
					.ParentKeyColumn("ParentId")
					.ChildKeyColumn("ChildId")
					.Cascade.All()
					.AsSet()
					.Table("Successors");
				HasManyToMany(x => x.Parents)
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
				new Program().Run();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		void Run()
		{
			var sessionFactory = CreateSessionFactory();
			// Arrange
			Guid id = Guid.Empty;
			using (var session = sessionFactory.OpenSession())
			using (var tx = session.BeginTransaction())
			{
				id = (Guid)session.Save(new Board
				{
					Empty = 8446743970227683327,
					Mover = 34628173824,
					Successors = new HashedSet<Board>(),
					Parents = new HashedSet<Board>()
				});
				tx.Commit();
			}

			// Act
			using (var session = sessionFactory.OpenSession())
			using (var tx = session.BeginTransaction())
			{
				var successor = new Board
				{
					Empty = -103481868289,
					Mover = 34628173824,
					Successors = new HashedSet<Board>(),
					Parents = new HashedSet<Board>()
				};
				var pos = session.Get<Board>(id);
				pos.Successors.Add(successor);
				successor.Parents.Add(pos);
				session.Update(pos);
				tx.Commit();
			}

			// Assert
			using (var session = sessionFactory.OpenSession())
			using (var tx = session.BeginTransaction())
			{
				var pos = session.Get<Board>(id);
				Console.WriteLine("Position: {0}", pos);
				Console.WriteLine("Successors:");
				foreach (var successor in pos.Successors)
				{
					Console.WriteLine("\t{0}, parent = {1}", successor, string.Join(",", successor.Parents));
				}
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
