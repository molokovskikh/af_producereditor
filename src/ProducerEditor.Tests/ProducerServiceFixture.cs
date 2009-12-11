using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NHibernate;
using NHibernate.Linq;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ProducerServiceFixture
	{
		private ISessionFactory _sessionFactory;

		[SetUp]
		public void Setup()
		{
			_sessionFactory = Global.InitializeNHibernate();
		}

		[Test]
		public void TestGetSynonyms()
		{
			uint producerId;
			uint synonymCode = 3000;
			uint priceCode = 3000;
			uint firmCode = 3000;
			uint billingCode = 3000;
			using(var session = _sessionFactory.OpenSession())
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

			var service = new ProducerService(_sessionFactory, new Mailer());
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

            using (var session = _sessionFactory.OpenSession())
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

				var service = new ProducerService(_sessionFactory, new Mailer());
				// Создаем эквиваленты
				foreach (var equivalent in equivalents)
					service.CreateEquivalentForProducer(producer.Id, equivalent);

				session.Flush();
            }

			using (var session = _sessionFactory.OpenSession())
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
