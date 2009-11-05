﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using ConnectionManager = Common.MySql.ConnectionManager;

namespace ProducerEditor.Service
{
	public class Offer
	{
		public string Product { get; set; }
		public string Producer { get; set; }
	}

	[DataContract]
	public class Pager<T>
	{
		[DataMember]
		public uint Page { get; set; }
		[DataMember]
		public uint TotalPages { get; set; }
		[DataMember]
		public IList<T> Content { get; set; }

		public Pager(uint page, uint totalPages, IList<T> content)
		{
			Page = page;
			TotalPages = totalPages;
			Content = content;
		}
	}

	[ServiceContract]
	public class ProducerService
	{
		private readonly ISessionFactory _factory;
		private readonly Mailer _mailer;
		private readonly ConnectionManager _connectionManager = new ConnectionManager();

		public ProducerService(ISessionFactory sessionFactory, Mailer mailer)
		{
			_factory = sessionFactory;
			_mailer = mailer;
		}

		[OperationContract]
		public IList<Offer> ShowOffersBySynonym(uint producerSynonymId)
		{
			return Slave(s => s.CreateSQLQuery(@"
select s.Synonym as Product, sfc.Synonym as Producer
from farm.core0 c
	join farm.Synonym s on s.SynonymCode = c.SynonymCode
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode
where c.SynonymFirmCrCode = :producerSynonymId
order by Product, Producer")
				.SetResultTransformer(Transformers.AliasToBean<Offer>())
				.SetParameter("producerSynonymId", producerSynonymId)
				.List<Offer>());
		}

		[OperationContract]
		public void DoJoin(uint[] sourceProduceIds, uint targetProducerId)
		{
			using(var session = _factory.OpenSession())
			using(var transaction = session.BeginTransaction())
			{
				var target = session.Load<Producer>(targetProducerId);
				foreach (var sourceId in sourceProduceIds)
				{
					var source = session.Load<Producer>(sourceId);
					session.CreateSQLQuery(@"
update farm.SynonymFirmCr
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId
;

update farm.core0
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId
;

update orders.orderslist
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId
;")
						.SetParameter("SourceId", sourceId)
						.SetParameter("TargetId", targetProducerId)
						.ExecuteUpdate();
					target.MergeToEquivalent(source);

					session.Delete(source);
				}

				transaction.Commit();
			}
		}

		[OperationContract]
		public IList<ProducerSynonymDto> GetSynonyms(uint producerId)
		{
			return Slave(session => session.CreateSQLQuery(@"
select sfc.Synonym as Name,
sfc.SynonymFirmCrCode as Id,
cd.ShortName as Supplier,
cd.FirmSegment as Segment,
r.Region,
c.Id is not null as HaveOffers
from farm.SynonymFirmCr sfc
  join usersettings.PricesData pd on sfc.PriceCode = pd.PriceCode
    join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
      join farm.Regions r on cd.RegionCode = r.RegionCode
  left join farm.Core0 c on c.SynonymFirmCrCode = sfc.SynonymFirmCrCode
where sfc.CodeFirmCr = :ProducerId and cd.BillingCode <> 921
group by sfc.SynonymFirmCrCode")
				.SetParameter("ProducerId", producerId)
				.SetResultTransformer(Transformers.AliasToBean(typeof (ProducerSynonymDto)))
				.List<ProducerSynonymDto>().ToList());
		}

		[OperationContract]
		public IList<SynonymReportItem> ShowSynonymReport(DateTime begin, DateTime end)
		{
			if (begin.Date == end.Date)
				begin = begin.AddDays(-1);

			return Slave(s => SynonymReportItem.Load(s, begin, end));
		}

		[OperationContract]
		public IList<SynonymReportItem> ShowSuspiciousSynonyms()
		{
			return Slave(s => SynonymReportItem.Suspicious(s));
		}

		[OperationContract]
		public IList<string> GetEquivalents(uint producerId)
		{
			return Slave(s => {
				var producer = s.Get<Producer>(producerId);
				return producer.Equivalents.Select(e => e.Name).ToList();
			});
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

		[OperationContract]
		public void DeleteProducer(uint producerId)
		{
			using (var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var producer = session.Get<Producer>(producerId);
				session.Delete(producer);
				transaction.Commit();
			}
		}

		[OperationContract]
		public void DeleteAssortment(uint assortmentId)
		{
			using (var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var assortment = session.Get<ProductAssortment>(assortmentId);
				session.Delete(assortment);
				transaction.Commit();
			}
		}

		[OperationContract]
		public Pager<AssortmentDto> ShowAssortment(uint assortimentId)
		{
			return Slave(session => {
				var total = Assortment.TotalPages(session);
				uint page = 1;
				if (assortimentId != 0)
					page = Assortment.GetPage(session, assortimentId);
				var assortments = Assortment.Load(session, page);
				return new Pager<AssortmentDto>(page, total, assortments);
			});
		}

		[OperationContract]
		public Pager<AssortmentDto> GetAssortmentPage(uint page)
		{
			return Slave(session => {
				var total = Assortment.TotalPages(session);
				return new Pager<AssortmentDto>(page, total, Assortment.Load(session, page));
			});
		}

		[OperationContract]
		public Pager<AssortmentDto> SearchAssortment(string text)
		{
			return Slave(session => {
				var total = Assortment.TotalPages(session);
				var page = Assortment.Find(session, text);
				if (page == -1)
					return null;
				return new Pager<AssortmentDto>((uint) page, total, Assortment.Load(session, (uint) page));
			});
		}

		[OperationContract]
		public Pager<ExcludeDto> ShowExcludes(uint page)
		{
			return Slave(session => {
				var total = Exclude.TotalPages(session);
				return new Pager<ExcludeDto>(page, total, Exclude.Load(page, session));
			});
		}

		[OperationContract]
		public void DoNotShow(uint excludeId)
		{
			using (var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var exclude = session.Load<Exclude>(excludeId);
				exclude.DoNotShow = false;
				session.Update(exclude);
				transaction.Commit();
			}
		}

		[OperationContract]
		public void AddToAssotrment(uint excludeId)
		{
			using (var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var exclude = session.Load<Exclude>(excludeId);
				var assortment = new Assortment(exclude.CatalogProduct, exclude.ProducerSynonym.Producer);

				if (assortment.Exist(session))
					throw new Exception("Запись в ассортименте уже существует");

				var excludes = (
					from ex in session.Linq<Exclude>()
					where ex.CatalogProduct == assortment.CatalogProduct
						&& ex.ProducerSynonym.Producer == assortment.Producer
					select ex).ToList();

				excludes.Each(session.Delete);

				session.Delete(exclude);
				session.Save(assortment);
				transaction.Commit();
			}
		}

		private T Slave<T>(Func<ISession, T> func)
		{
			using (var connection = _connectionManager.GetConnection())
			using (var session = _factory.OpenSession(connection))
			{
				connection.Open();
				return func(session);
			}
		}
	}
}