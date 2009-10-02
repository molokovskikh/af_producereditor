using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;

namespace ProducerEditor.Service
{
	public class Offer
	{
		public string Product { get; set; }
		public string Producer { get; set; }
	}

	[ServiceContract]
	public class ProducerService
	{
		private readonly ISessionFactory _factory;
		private readonly Mailer _mailer;

		public ProducerService(ISessionFactory sessionFactory, Mailer mailer)
		{
			_factory = sessionFactory;
			_mailer = mailer;
		}

		[OperationContract]
		public IList<Offer> GetOffers(uint producerSynonymId)
		{
			using (var session = _factory.OpenSession())
			{
				return session.CreateSQLQuery(@"
select s.Synonym as Product, sfc.Synonym as Producer
from farm.core0 c
	join farm.Synonym s on s.SynonymCode = c.SynonymCode
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode
where c.SynonymFirmCrCode = :producerSynonymId")
					.SetResultTransformer(Transformers.AliasToBean<Offer>())
					.SetParameter("producerSynonymId", producerSynonymId)
					.List<Offer>();
			}
		}

		[OperationContract]
		public IList<SynonymReportItem> GetSynonymReport(DateTime begin, DateTime end)
		{
			if (begin.Date == end.Date)
				begin = begin.AddDays(-1);
			using(var session = _factory.OpenSession())
				return SynonymReportItem.Load(session, begin, end);
		}

		[OperationContract]
		public IList<SynonymReportItem> ShowSuspiciousSynonyms()
		{
			using(var session = _factory.OpenSession())
				return SynonymReportItem.Suspicious(session);
		}

		[OperationContract]
		public IList<string> GetEquivalents(uint producerId)
		{
			using (var session = _factory.OpenSession())
			{
				var producer = session.Get<Producer>(producerId);
				return producer.Equivalents.Select(e => e.Name).ToList();
			}
		}

		[OperationContract]
		public void DeleteProducerSynonym(uint producerSynonymId)
		{
			using (var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var synonym = session.Get<ProducerSynonym>(producerSynonymId);
				session.Delete(synonym);
				session.Save(new BlockedProducerSynonym(synonym));
				_mailer.SynonymWasDeleted(synonym);
				transaction.Commit();
			}
		}

		[OperationContract]
		public void Suspicious(uint producerSynonymId)
		{
			using(var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var synonym = session.Get<ProducerSynonym>(producerSynonymId);
				session.Save(new SuspiciousProducerSynonym(synonym));
				transaction.Commit();
			}
		}

		[OperationContract]
		public void DeleteSuspicious(uint producerSynonymId)
		{
			using (var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var suspicious = session.Linq<SuspiciousProducerSynonym>().Where(s => s.Synonym.Id == producerSynonymId).First();
				session.Delete(suspicious);
				transaction.Commit();
			}
		}
	}
}