﻿using System;
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
		public void TestGetSynonyms()
		{
			uint producerId;
			uint synonymCode = 3000;
			uint priceCode = 3000;
			uint firmCode = 3000;
			uint billingCode = 3000;
			using(var session = sessionFactory.OpenSession())
			{
				const string ProducerName = "test-producer";

				var testProducer = session.Linq<Producer>().FirstOrDefault(p => p.Name == ProducerName);
				if (testProducer != null)
					session.Delete(testProducer);
				session.Flush();

				var producer = new Producer();
				producer.Name = ProducerName;				
				session.Save(producer);

				producerId = producer.Id;

				// Создаем синоним для этого производителя
				var queryDeleteSynonym = @"
delete from farm.SynonymFirmCr
where SynonymFirmCrCode = :SynonymCode
";
				var queryInsertSynonym = @"
insert into farm.SynonymFirmCr
values(:SynonymCode, :PriceCode, :ProducerId, ""Test synonym"")
";
				var queryDeletePrice = @"
delete from usersettings.PricesData
where PriceCode = :PriceCode
";
				var queryInsertPrice = @"
insert into usersettings.PricesData
values(:PriceCode, :FirmCode, 1, 1, 1, 0, 0, ""Test price"", 0, 0, NULL, 0, NULL)
";
				var queryDeleteFirm = @"
delete from usersettings.ClientsData
where FirmCode = :FirmCode
";
				var queryInsertFirm = @"
insert into usersettings.ClientsData
values(:FirmCode, 1, 1, :FirmSegment, 0, :BillingCode, 1, 1, 1, ""Test"", ""Test"", NULL, NULL, NULL, NULL, NULL)
";
				var queryDeletePayer = @"
delete from billing.payers
where PayerId = :PayerId
";
				var queryInsertPayer = @"
insert into billing.payers
values (:PayerId, ""Test"", NULL, NULL, NULL, NULL, NULL, 
NULL, 0, 0, 0, 0, 0, 0, 0, NULL, NULL,
NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, 0, 0, 
NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL,
NULL, 0, 0, NULL, 0, 0, 0, 0, 0, NULL, NULL, 0)
";

				session.CreateSQLQuery(queryDeleteSynonym)
					.SetParameter("SynonymCode", synonymCode)
					.ExecuteUpdate();

				session.CreateSQLQuery(queryInsertSynonym)
					.SetParameter("SynonymCode", synonymCode)
					.SetParameter("PriceCode", priceCode)
					.SetParameter("ProducerId", producerId)
					.ExecuteUpdate();

				session.CreateSQLQuery(queryDeletePayer)
					.SetParameter("PayerId", billingCode)
					.ExecuteUpdate();

				session.CreateSQLQuery(queryInsertPayer)
					.SetParameter("PayerId", billingCode)
					.ExecuteUpdate();

				session.CreateSQLQuery(queryDeleteFirm)
					.SetParameter("FirmCode", firmCode)
					.ExecuteUpdate();
				session.CreateSQLQuery(queryDeleteFirm)
					.SetParameter("FirmCode", firmCode + 1)
					.ExecuteUpdate();

				// Создаем две фирмы с разным значением сегмента
				session.CreateSQLQuery(queryInsertFirm)
					.SetParameter("FirmCode", firmCode)
					.SetParameter("FirmSegment", 0)
					.SetParameter("BillingCode", billingCode)
					.ExecuteUpdate();
				session.CreateSQLQuery(queryInsertFirm)
					.SetParameter("FirmCode", firmCode + 1)
					.SetParameter("FirmSegment", 1)
					.SetParameter("BillingCode", billingCode)
					.ExecuteUpdate();


				session.CreateSQLQuery(queryDeletePrice)
					.SetParameter("PriceCode", priceCode)
					.ExecuteUpdate();

				session.CreateSQLQuery(queryInsertPrice)
					.SetParameter("PriceCode", priceCode)
					.SetParameter("FirmCode", firmCode)
					.ExecuteUpdate();

				session.Flush();
			}

			var synonyms = service.GetSynonyms(producerId);

			// Вернуться должен только 1 синоним (той фирмы, у которой сегмент "Опт")
			Assert.IsTrue(synonyms.Count == 1);
		}

		[Test]
		public void TestCreateEquivalentForProducer()
		{
			const int CountEquivalents = 5;
			var equivalents = new string[CountEquivalents] { "test", "новый", "Эквивалент", "1test23", "тест" };
			uint producerId = 0;

			using (var session = sessionFactory.OpenSession())
			{
				const string ProducerName = "Test producer for creating equivalent";
				var testProducer = session.Linq<Producer>().FirstOrDefault(p => p.Name == ProducerName);
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
