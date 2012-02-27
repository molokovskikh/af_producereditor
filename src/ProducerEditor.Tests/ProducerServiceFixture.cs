using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using NUnit.Framework;
using NHibernate;
using NHibernate.Linq;
using ProducerEditor.Service;
using ProducerEditor.Service.Models;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ProducerServiceFixture
	{
		ISessionFactory sessionFactory;
		ProducerService service;
		Mailer mailer;

		[SetUp]
		public void Setup()
		{
			mailer = new Mailer();
			sessionFactory = Global.InitializeNHibernate();
			service = new ProducerService(sessionFactory, mailer);
		}

		[Test]
		public void Check_service()
		{
			foreach (var method in typeof(ProducerService).GetMethods())
			{
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
			var exclude = excludes.Content[0];
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
			Assert.That(equivalents.First(e => e == exclude.ProducerSynonym.ToUpper()), Is.Not.Null, "не создали эквивалент");

			var synonym = service
				.GetSynonyms(producer.Id)
				.FirstOrDefault(s => s.Name == exclude.ProducerSynonym && s.Supplier == exclude.Supplier);
			Assert.That(synonym, Is.Not.Null, "не создали синоним");
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
			Assert.That(equivalents.First(e => e == exclude.ProducerSynonym.ToUpper()), Is.Not.Null, "не создали эквивалент");

			var synonym = service
				.GetSynonyms(producer.Id)
				.FirstOrDefault(s => s.Name == exclude.ProducerSynonym && s.Supplier == exclude.Supplier);
			Assert.That(synonym, Is.Not.Null, "не создали синоним");
		}

		[Test]
		public void TestCreateEquivalentForProducer()
		{
			var equivalents = new[] { "test", "новый", "Эквивалент", "1test23", "тест" };
			uint producerId;

			using (var session = sessionFactory.OpenSession())
			{
				const string ProducerName = "Test producer for creating equivalent";
				var testProducer = session.Query<Producer>().FirstOrDefault(p => p.Name == ProducerName);
				if (testProducer != null)
					session.Delete(testProducer);
				session.Flush();

				var producer = new Producer();
				producer.Name = ProducerName;
				session.Save(producer);
				producerId = producer.Id;
				session.Flush();

				var queryDeleteFirmCr = @"
delete from farm.catalogfirmcr
where CodeFirmCr = :CodeFirmCr or FirmCr = :FirmCr
";
				var queryInsertFirmCr = @"
insert into farm.catalogfirmcr
values(:CodeFirmCr, :FirmCr, 0)
";
				// Пытаемся удалить запись из таблицы farm.CatalogFirmCr
				session.CreateSQLQuery(queryDeleteFirmCr)
					.SetParameter("CodeFirmCr", producerId)
					.SetParameter("FirmCr", ProducerName).ExecuteUpdate();
				// Вставляем запись для созданного производителя в таблицу farm.CatalogFirmCr
				session.CreateSQLQuery(queryInsertFirmCr)
					.SetParameter("CodeFirmCr", producerId)
					.SetParameter("FirmCr", ProducerName).ExecuteUpdate();

				// Создаем эквиваленты
				foreach (var equivalent in equivalents)
					service.CreateEquivalentForProducer(producer.Id, equivalent);

				session.Flush();
			}

			using (var session = sessionFactory.OpenSession())
			{
				var querySelectCountEquivalent = @"
select 
	count(Id) 
from
	Catalogs.ProducerEquivalents 
where 
	ProducerId = :ProducerId and Name = :EqName
";
				foreach (var equivalent in equivalents)
				{
					var count = Convert.ToInt32(session.CreateSQLQuery(querySelectCountEquivalent)
						.SetParameter("ProducerId", producerId)
						.SetParameter("EqName", equivalent)
						.UniqueResult());
					Assert.IsTrue(count == 1);
				}
			}
		}
	}
}
