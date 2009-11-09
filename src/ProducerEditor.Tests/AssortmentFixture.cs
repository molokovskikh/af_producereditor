using System.Linq;
using log4net.Config;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class AssortmentFixture
	{
		private ISessionFactory sessionFactory;

		[SetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			sessionFactory = Global.InitializeNHibernate();
		}

		[Test]
		public void Load_assortment()
		{
			using (var session = sessionFactory.OpenSession())
				Assortment.Load(session, 0);
		}

		[Test]
		public void Total()
		{
			using (var session = sessionFactory.OpenSession())
				Assert.That(Assortment.TotalPages(session), Is.GreaterThan(0));
		}

		[Test]
		public void Get_page()
		{
			using (var session = sessionFactory.OpenSession())
			{
				var assortments = Assortment.Load(session, 3);
				Assert.That(Assortment.GetPage(session, assortments[3].Id), Is.EqualTo(3));
			}
		}

		[Test]
		public void Search_text()
		{
			using (var session = sessionFactory.OpenSession())
			{
				var assortments = Assortment.Load(session, 5);
				var findedAssortments = Assortment.Find(session, assortments[1].Product, 0);
				Assert.That(findedAssortments.Content.Count, Is.EqualTo(1));
				Assert.That(findedAssortments.Page, Is.EqualTo(0));
				Assert.That(findedAssortments.TotalPages, Is.EqualTo(1));
			}
		}

		[Test]
		public void Get_assortment_for_producer()
		{
			uint producerId;
			using(var session = sessionFactory.OpenSession())
			{
				var testProducer = session.Linq<Producer>().FirstOrDefault(p => p.Name == "test-producer");
				if (testProducer != null)
					session.Delete(testProducer);
				session.Flush();

				var producer = new Producer();
				producer.Name = "test-producer";
				session.Save(producer);

				var assortment = new Assortment(session.Linq<CatalogProduct>().First(), producer);
				session.Save(assortment);
				session.Flush();

				producerId = producer.Id;
			}

			using (var session = sessionFactory.OpenSession())
			{
				var assortments = Assortment.ByProducer(session, producerId, 0);
				Assert.That(assortments.Content.Count, Is.EqualTo(1));
				var assortment = assortments.Content.Single();
				Assert.That(assortment.Producer, Is.EqualTo("test-producer"));
			}
		}
	}
}
