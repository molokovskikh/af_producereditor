using log4net.Config;
using NHibernate;
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
				Assert.That(Assortment.Find(session, assortments[1].Product), Is.EqualTo(5));
			}
		}
	}
}
