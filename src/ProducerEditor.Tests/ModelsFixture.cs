using System.Linq;
using Common.Tools;
using Test.Support.Suppliers;
using log4net.Config;
using NHibernate.Linq;
using NUnit.Framework;
using ProducerEditor.Service;
using ProducerEditor.Service.Models;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ModelsFixture : BaseFixture
	{
		private Price price;

		[SetUp]
		public void Setup()
		{
			var supplier = TestSupplier.Create();
			price = session.Load<Price>(supplier.Prices[0].Id);
		}

		[Test]
		public void Search_suspicious()
		{
			var all = session.Query<SuspiciousProducerSynonym>().ToList();
			all.Each(session.Delete);
			var testSynonym = session.Query<ProducerSynonym>().FirstOrDefault(s => s.Name == "test");
			if (testSynonym != null)
				session.Delete(testSynonym);
			session.Flush();

			var synonym = new ProducerSynonym();
			synonym.Producer = session.Query<Producer>().First();
			synonym.Price = price;
			synonym.Name = "test";
			session.Save(synonym);
			session.Flush();

			session.Save(new SuspiciousProducerSynonym(synonym));

			session.Flush();
			session.Clear();

			var suspisioses = SynonymReportQuery.Suspicious(session);
			Assert.That(suspisioses.Count, Is.EqualTo(1));
		}
	}
}