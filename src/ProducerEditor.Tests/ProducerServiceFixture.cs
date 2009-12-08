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

				session.CreateSQLQuery(queryInsertFirm)
					.SetParameter("FirmCode", firmCode)
					.SetParameter("FirmSegment", 0)
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
			Assert.IsTrue(synonyms.Count == 1);
		}
	}
}
