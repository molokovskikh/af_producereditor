using System.Linq;
using Common.Tools;
using log4net.Config;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ProducerEditor.Service;
using ProducerEditor.Service.Models;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ModelsFixture
	{
		private ISessionFactory sessionFactory;

		[SetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			sessionFactory = Global.InitializeNHibernate();
		}

		[Test]
		public void Search_suspicious()
		{
			using(var session = sessionFactory.OpenSession())
			{
				var all = session.Query<SuspiciousProducerSynonym>().ToList();
				all.Each(session.Delete);
				var testSynonym =  session.Query<ProducerSynonym>().FirstOrDefault(s => s.Name == "test");
				if (testSynonym != null)
					session.Delete(testSynonym);
				session.Flush();

				var synonym = new ProducerSynonym();
				synonym.Producer = session.Query<Producer>().First();
				synonym.Price = session.Query<Price>().First();
				synonym.Name = "test";
				session.Save(synonym);
				session.Flush();

				session.Save(new SuspiciousProducerSynonym(synonym));
				session.Flush();
			}

			using(var session = sessionFactory.OpenSession())
			{
				var suspisioses = SynonymReportItem.Suspicious(session);
				Assert.That(suspisioses.Count, Is.EqualTo(1));
			}
		}
	}
}
