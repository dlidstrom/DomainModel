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

		public override int GetHashCode()
		{
			int seed = (int)(Empty >> 32);
			seed ^= (int)Empty + (seed << 6) + (seed >> 2);
			seed ^= (int)(Mover >> 32) + (seed << 6) + (seed >> 2);
			seed ^= (int)Mover + (seed << 6) + (seed >> 2);
			return seed;
		}

		public override bool Equals(object obj)
		{
			var board = obj as Board;
			if (board == null)
			{
				return false;
			}

			return board.Empty == Empty && board.Mover == Mover;
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
					c.Properties.Add("generate_statistics", "true");
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
				var set = new C5.HashSet<Board>();

				// only flush session when we commit
				// this will improve performance
				session.FlushMode = FlushMode.Commit;
				int currentChild = 0;
				for (int i = 0; i < 100; i++)
				{
					if ((i % 10) == 0)
					{
						Console.Write(".");
					}

					var parent = new Board { Empty = i, Mover = i };
					set.FindOrAdd(ref parent);

					currentChild = 0;
					for (int j = 0; j >= -10; j--)
					{
						var child = new Board { Empty = currentChild--, Mover = currentChild-- };
						set.FindOrAdd(ref child);
						// fetch child from store, if it exists
						child = session.CreateQuery("from Board b where b.Empty = :empty and b.Mover = :mover")
							.SetInt64("empty", child.Empty)
							.SetInt64("mover", child.Mover)
							.UniqueResult<Board>() ?? child;
						parent.AddSuccessor(child);
						child.AddParent(parent);
					}

					session.Save(parent);
				}

				Console.WriteLine();
				tx.Commit();

				Console.WriteLine("CloseStatementCount.............: {0}", sessionFactory.Statistics.CloseStatementCount);
				Console.WriteLine("CollectionFetchCount............: {0}", sessionFactory.Statistics.CollectionFetchCount);
				Console.WriteLine("CollectionLoadCount.............: {0}", sessionFactory.Statistics.CollectionLoadCount);
				Console.WriteLine("CollectionRecreateCount.........: {0}", sessionFactory.Statistics.CollectionRecreateCount);
				Console.WriteLine("CollectionRemoveCount...........: {0}", sessionFactory.Statistics.CollectionRemoveCount);
				Console.WriteLine("CollectionRoleNames:");
				foreach (var r in sessionFactory.Statistics.CollectionRoleNames)
				{
					Console.WriteLine("Role name.......................: {0} ", r);
				}
				Console.WriteLine("CollectionUpdateCount...........: {0}", sessionFactory.Statistics.CollectionUpdateCount);
				Console.WriteLine("ConnectCount....................: {0}", sessionFactory.Statistics.ConnectCount);
				Console.WriteLine("EntityDeleteCount...............: {0}", sessionFactory.Statistics.EntityDeleteCount);
				Console.WriteLine("EntityFetchCount................: {0}", sessionFactory.Statistics.EntityFetchCount);
				Console.WriteLine("EntityInsertCount...............: {0}", sessionFactory.Statistics.EntityInsertCount);
				Console.WriteLine("EntityLoadCount.................: {0}", sessionFactory.Statistics.EntityLoadCount);
				Console.WriteLine("EntityNames:");
				foreach (var r in sessionFactory.Statistics.EntityNames)
				{
					Console.WriteLine("Entity name:....................: {0} ", r);
				}
				Console.WriteLine("EntityUpdateCount...............: {0}", sessionFactory.Statistics.EntityUpdateCount);
				Console.WriteLine("FlushCount......................: {0}", sessionFactory.Statistics.FlushCount);
				Console.WriteLine("OperationThreshold..............: {0} ms", sessionFactory.Statistics.OperationThreshold.Milliseconds);
				Console.WriteLine("OptimisticFailureCount..........: {0}", sessionFactory.Statistics.OptimisticFailureCount);
				Console.WriteLine("PrepareStatementCount...........: {0}", sessionFactory.Statistics.PrepareStatementCount);
				Console.WriteLine("Queries:");
				foreach (var r in sessionFactory.Statistics.Queries)
				{
					Console.WriteLine("Query...........................: \"{0}\" ", r);
				}
				Console.WriteLine("QueryCacheHitCount..............: {0}", sessionFactory.Statistics.QueryCacheHitCount);
				Console.WriteLine("QueryCacheMissCount.............: {0}", sessionFactory.Statistics.QueryCacheMissCount);
				Console.WriteLine("QueryCachePutCount..............: {0}", sessionFactory.Statistics.QueryCachePutCount);
				Console.WriteLine("QueryExecutionCount.............: {0}", sessionFactory.Statistics.QueryExecutionCount);
				Console.WriteLine("QueryExecutionMaxTime...........: {0} ms", sessionFactory.Statistics.QueryExecutionMaxTime.Milliseconds);
				Console.WriteLine("QueryExecutionMaxTimeQueryString: \"{0}\"", sessionFactory.Statistics.QueryExecutionMaxTimeQueryString);
				Console.WriteLine("SecondLevelCacheHitCount........: {0}", sessionFactory.Statistics.SecondLevelCacheHitCount);
				Console.WriteLine("SecondLevelCacheMissCount.......: {0}", sessionFactory.Statistics.SecondLevelCacheMissCount);
				Console.WriteLine("SecondLevelCachePutCount:.......: {0} ", sessionFactory.Statistics.SecondLevelCachePutCount);
				Console.WriteLine("SecondLevelCacheRegionNames:");
				foreach (var r in sessionFactory.Statistics.SecondLevelCacheRegionNames)
				{
					Console.WriteLine("Cache Region Name: {0} ", r);
				}
				Console.WriteLine("SessionCloseCount...............: {0}", sessionFactory.Statistics.SessionCloseCount);
				Console.WriteLine("SessionOpenCount................: {0}", sessionFactory.Statistics.SessionOpenCount);
				Console.WriteLine("StartTime.......................: {0}", sessionFactory.Statistics.StartTime.ToString("s"));
				Console.WriteLine("SuccessfulTransactionCount......: {0}", sessionFactory.Statistics.SuccessfulTransactionCount);
				Console.WriteLine("TransactionCount................: {0}", sessionFactory.Statistics.TransactionCount);

				//Console.WriteLine(session.Statistics.CollectionCount);
				//Console.WriteLine(session.Statistics.CollectionKeys);
				//Console.WriteLine(session.Statistics.EntityCount);
				//Console.WriteLine(session.Statistics.EntityKeys);
			}

			return;

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
