using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Castle.ActiveRecord;
using NUnit.Framework;
using NHibernate;
using NHibernate.Linq;
using ProducerEditor.Contract;
using ProducerEditor.Service;
using ProducerEditor.Service.Helpers;
using ProducerEditor.Service.Models;
using Test.Support;
using Test.Support.Suppliers;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ProducerServiceFixture : IntegrationFixture
	{
		private ISessionFactory sessionFactory;
		private ProducerService service;
		private Mailer mailer;
		private ISession localSession;

		[SetUp]
		public void Setup()
		{
			mailer = new Mailer();
			sessionFactory = FixtureSetup.sessionFactory;
			service = new ProducerService(sessionFactory, mailer);
			localSession = sessionFactory.OpenSession();

			CreateExclude();
		}

		[TearDown]
		public void TearDown()
		{
			if (localSession != null)
				localSession.Close();
		}

		private void CreateExclude()
		{
			using (var session = sessionFactory.OpenSession()) {
				var supplier = TestSupplier.Create();
				var price = session.Load<Price>(supplier.Prices[0].Id);
				var producerSynonym = new ProducerSynonym {
					Name = "Тетовый синоним",
					Price = price,
					Producer = session.Query<Producer>().First()
				};
				var productSynonym = new Synonym {
					Name = "Тетовый синоним",
					Price = price,
					ProductId = session.Query<CatalogProduct>().First().Id
				};
				var exclude = new Exclude {
					CatalogProduct = session.Query<CatalogProduct>().First(),
					Price = price,
					ProducerSynonym = "Тетовый синоним",
					OriginalSynonym = productSynonym,
				};
				session.Save(producerSynonym);
				session.Save(productSynonym);
				session.Save(exclude);
			}
		}

		[Test]
		public void Join_producers_with_identical_synonyms()
		{
			var price = TestSupplier.Create().Prices[0];
			var producer1 = new TestProducer("Тестовый производитель");
			var producer2 = new TestProducer("Тестовый производитель");
			var synonym1 = new TestProducerSynonym("Тестовый синоним", producer1, price);
			var synonym2 = new TestProducerSynonym("Тестовый синоним", producer2, price);
			session.Save(producer1);
			session.Save(producer2);
			session.Save(synonym1);
			session.Save(synonym2);
			session.Transaction.Commit();

			service.DoJoin(new[] { producer1.Id }, producer2.Id);
		}

		[Test]
		public void Check_service()
		{
			foreach (var method in typeof(ProducerService).GetMethods()) {
				if (!method.GetCustomAttributes(false).Any(a => a.GetType() == typeof(OperationContractAttribute)))
					continue;

				if (!method.IsVirtual)
					throw new Exception(String.Format("Метод {0} не виртуальный не будет работать перехватчик который собирает ошибки", method.Name));
			}
		}

		[Test]
		public void Delete_synonym()
		{
			var excludes = service.ShowExcludes();
			var exclude = excludes.Content.First(e => e.OriginalSynonymId != 0);
			service.DeleteSynonym(exclude.OriginalSynonymId);
			Assert.That(mailer.Messages[0].Body, Is.StringContaining(String.Format("Продукт: {0}", exclude.Catalog)));
		}

		[Test]
		public void Get_data_for_exclude()
		{
			var excludes = service.ShowExcludes();
			var data = service.GetExcludeData(excludes.Content[0].Id);
			Assert.That(data.Producers, Is.Not.Null);
			Assert.That(data.Synonyms, Is.Not.Null);
		}

		[Test]
		public void Check_assortment()
		{
			var assortments = service.GetAssortmentPage(0);
			var assortment = assortments.Content[0];
			assortment.Checked = true;
			service.UpdateAssortment(assortment);
		}

		[Test]
		public void Search_exclude()
		{
			var excludes = service.ShowExcludes();
			var result = service.SearchExcludes(excludes.Content[0].ProducerSynonym, false, false, 0, false);
			Assert.That(result.Content.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Create_assortment()
		{
			var excludes = service.ShowExcludes().Content;
			var producers = service.GetProducers();
			var exclude = excludes.First();
			var producer = producers.First();
			service.AddToAssotrment(exclude.Id, producer.Id, exclude.ProducerSynonym);

			excludes = service.ShowExcludes().Content;
			Assert.That(excludes.FirstOrDefault(e => e.Id == exclude.Id), Is.Null, "не удалили исключение");

			var equivalents = service.GetEquivalents(producer.Id);
			Assert.That(equivalents.First(e => e.Name == exclude.ProducerSynonym.ToUpper()), Is.Not.Null, "не создали эквивалент");

			var synonym = service
				.GetSynonyms(producer.Id)
				.FirstOrDefault(s => s.Name == exclude.ProducerSynonym && s.Supplier == exclude.Supplier);
			Assert.That(synonym, Is.Not.Null, "не создали синоним");
		}

		[Test]
		public void CheckMonobrendforExclude()
		{
			var excludes = service.ShowExcludes().Content;
			var exclude = excludes.First();
			uint existsProducerId;
			// устанавливаем для продукта свойство монобренда
			using (var session = sessionFactory.OpenSession()) {
				var product = session.Load<Exclude>(exclude.Id).CatalogProduct;
				product.Monobrend = true;
				session.Save(product);
				var producer = session.Query<Producer>().First();
				var newAssortment = new Assortment {
					CatalogProduct = product,
					Producer = producer
				};
				session.Save(newAssortment);
				session.Flush();
				existsProducerId = producer.Id;
			}
			Assert.That(service.CheckProductIsMonobrend(exclude.Id, 0), Is.True);
			Assert.That(service.CheckProductIsMonobrend(exclude.Id, existsProducerId), Is.False);

			// возвращаем все назад, чтобы не ломать остальные тесты
			using (var session = sessionFactory.OpenSession()) {
				var product = session.Load<Exclude>(exclude.Id).CatalogProduct;
				product.Monobrend = false;
				session.Save(product);
				session.Flush();
			}
		}

		[Test]
		public void Create_equivalent()
		{
			var excludes = service.ShowExcludes().Content;
			var producers = service.GetProducers();
			var exclude = excludes.First();
			var producer = producers.First();
			service.CreateEquivalent(exclude.Id, producer.Id);

			excludes = service.ShowExcludes().Content;
			Assert.That(excludes.FirstOrDefault(e => e.Id == exclude.Id), Is.Null, "не удалили исключение");

			var equivalents = service.GetEquivalents(producer.Id);
			Assert.That(equivalents.First(e => e.Name == exclude.ProducerSynonym.ToUpper()), Is.Not.Null, "не создали эквивалент");

			var synonym = service
				.GetSynonyms(producer.Id)
				.FirstOrDefault(s => s.Name == exclude.ProducerSynonym && s.Supplier == exclude.Supplier);
			Assert.That(synonym, Is.Not.Null, "не создали синоним");
		}

		[Test]
		public void DeleteSynonymProducerWithretransTest()
		{
			var supplier = TestSupplier.Create();
			supplier.Prices[0].Costs[0].PriceItem.Format.PriceFormat = PriceFormatType.NativeDbf;
			var producer = new TestProducer {
				Name = "Тестовый производитель"
			};

			Save(producer);
			var price = service.Slave(s => s.Load<Price>(supplier.Prices[0].Id));
			var producerSynonym = new TestProducerSynonym {
				Price = supplier.Prices[0],
				Producer = producer,
				Name = "Тестовый синоним1"
			};
			Save(producerSynonym);
			var cr = new TestCore {
				Price = supplier.Prices[0],
				ProducerSynonym = producerSynonym,
				Quantity = "1",
				Code = "123",
				Period = "123"
			};
			Save(cr);
			Flush();
			Reopen();
			service.DeleteProducerSynonymWithRetrans(producerSynonym.Id);
			var savedSynonym = session.Query<ProducerSynonym>().Where(s => s.Id == producerSynonym.Id).ToList();
			Assert.That(savedSynonym.Count, Is.EqualTo(0));
			Assert.That(PriceRetrans.Retranses.Count, Is.EqualTo(1));
			Assert.That(PriceRetrans.Retranses.First(), Is.EqualTo(supplier.Prices[0].Costs[0].PriceItem.Id));
		}

		[Test]
		public void Log_updates()
		{
			var begin = DateTime.Now.AddSeconds(-2);
			var producer = localSession.Query<Producer>().First();
			var producerDto = new ProducerDto {
				Checked = !producer.Checked,
				Id = producer.Id,
				Name = producer.Name
			};
			service.UpdateProducer(producerDto);

			var users = localSession.CreateSQLQuery("select OperatorName from Logs.ProducerLogs where LogTime >= :begin")
				.SetParameter("begin", begin)
				.List<string>();
			Assert.That(users, Is.EquivalentTo(new[] { Environment.UserName }));
		}
	}
}