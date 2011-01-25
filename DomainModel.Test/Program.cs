
using System.Diagnostics;
using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Cfg;
using DomainModel.Entities;
using System;
using FluentNHibernate.Cfg.Db;
using NUnit.Framework;

namespace DomainModel.Test
{
    [TestFixture]
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var sessionFactory = CreateSessionFactory();
                using (var session = sessionFactory.OpenSession())
                {
                    using (session.BeginTransaction())
                    {
                        var gateCommands = session.CreateCriteria<GateCmd>().List<GateCmd>();
                        foreach (var gateCmd in gateCommands)
                        {
                            Console.WriteLine(gateCmd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                .Database(MySQLConfiguration.Standard.ConnectionString(c => c.FromAppSetting("ConnStr")))
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<GateCmd>())
                .BuildSessionFactory();
        }

        [Test]
        public void CanGenerateSchema()
        {
            //var cfg = new Configuration();
            //cfg.Configure();
            //cfg.AddAssembly(
        }

        private static void ShouldReadGateCmd()
        {
            Debug.Assert(false);
        }
    }
}
