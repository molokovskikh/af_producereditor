using System.Linq;
using System.Reflection;
using log4net.Config;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ProducerEditor.Service;
using System;
using Castle.Windsor.Installer;
using ProducerEditor.Service.Helpers;
using Rhino.Mocks;

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

		private Producer CreateTestProducer(ISession session)
		{
			const string ProducerName = "test-producer";

			var testProducer = session.Linq<Producer>().FirstOrDefault(p => p.Name == ProducerName);
			if (testProducer != null)
				session.Delete(testProducer);
			session.Flush();

			var producer = new Producer();
			producer.Name = ProducerName;
			session.Save(producer);
			return producer;
		}

		[Test]
		public void DeleteAssortmentPosition()
		{
			uint assortmentId;
			uint countOffers = 0;
			uint startOfferId = 118375;
			using (var session = sessionFactory.OpenSession())
			{
				var producer = CreateTestProducer(session);
				// Создаем позицию в ассортименте
				var assortment = new Assortment(session.Linq<CatalogProduct>().First(), producer);
				session.Save(assortment);
				session.Flush();
				var producerId = assortment.Producer.Id;				
				var catalogId = assortment.CatalogProduct.Id;
				assortmentId = assortment.Id;

				var queryGetProductId = @"
select Id 
from Catalogs.Products
where CatalogId = :CatalogId
";
				// Получаем идентификаторы продуктов по Id каталога
				var productsIds = session.CreateSQLQuery(queryGetProductId)
					.SetParameter("CatalogId", catalogId).List();

				// Создаем запись предложений (записи в Core0)
				var queryInsertOffer = @"
insert into farm.Core0
values(:OfferId, 307, :ProductId, :CodeFirmCr, 1808805, 9832, :EmptyString, 
	:EmptyString, :EmptyString, :EmptyString, :EmptyString, :EmptyString, 
	:EmptyString, :EmptyString, 0, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, now(), now())";

				var offerId = startOfferId;
				foreach (var productId in productsIds)
				{
					session.CreateSQLQuery("delete from farm.Core0 where Id = :OfferId")
						.SetParameter("OfferId", offerId)
						.ExecuteUpdate();

					session.CreateSQLQuery(queryInsertOffer)
							.SetParameter("OfferId", offerId)
							.SetParameter("ProductId", productId)
							.SetParameter("CodeFirmCr", producerId)
							.SetParameter("EmptyString", "")
							.ExecuteUpdate();
					offerId++;
					countOffers++;
				}
				session.Flush();				
			}

			var service = new ProducerService(Global.InitializeNHibernate(), new Mailer());
			service.DeleteAssortment(assortmentId);

			using (var session = sessionFactory.OpenSession())
			{				
				var querySelect = @"
select Id
from farm.Core0
where Id = :OfferId
";
				var offerId = startOfferId;
				for (var i = 0; i < countOffers; i++)
				{
					var count = session.CreateSQLQuery(querySelect)
						.SetParameter("OfferId", offerId++)
						.List().Count;
					session.Flush();
					Assert.IsTrue(count == 0, "Для данной записи в ассортименте не все предложения были удалены");
				}
			}
		}

		private delegate void TestDelegate(Action<ISession> action);

		private static MySqlException GetMySqlException(int errorCode, string message)
		{
			return (MySqlException)typeof(MySqlException)
				.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(int) }, null)
				.Invoke(new object[] { message, errorCode });
		}

		[Test]
		public void Delete_assortment_with_deadlock()
		{
			var _sessionFactory = Global.InitializeNHibernate();
			var count = 0;
			try
			{
				var repository = new MockRepository();
				var callback = new TestDelegate(action => {
					count++;
					throw GetMySqlException(1205, "test");
				});
				var executor = (Executor) repository.StrictMock(typeof (Executor), _sessionFactory);
				var service = new ProducerService(_sessionFactory, new Mailer(), executor);
				executor.Stub((ex) => ex.WithTransaction(s => { })).IgnoreArguments().Do(callback);
				repository.ReplayAll();

				service.DeleteAssortment(1);
			}
			catch (Exception)
			{
				Assert.That(count, Is.EqualTo(50));
			}
		}
	}
}
