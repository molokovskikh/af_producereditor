using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Common.MySql;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using ConnectionManager = Common.MySql.ConnectionManager;
using ISession=NHibernate.ISession;
using ProducerEditor.Service.Helpers;

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

		public Pager(uint page, uint total, IList<T> content)
		{
			Page = page;
			TotalPages = total / 100;
			if (TotalPages == 0)
				TotalPages = 1;
			else if (total % 100 != 0)
				TotalPages++;
			Content = content;
		}
	}

	[ServiceContract]
	public class ProducerService
	{
		private readonly ISessionFactory _factory;
		private readonly Mailer _mailer;
		private readonly ConnectionManager _connectionManager = new ConnectionManager();
		private readonly Executor _execute;

		public ProducerService(ISessionFactory sessionFactory, Mailer mailer)
		{
			_factory = sessionFactory;
			_mailer = mailer;
			_execute = new Executor(_factory);
		}

		public ProducerService(ISessionFactory sessionFactory, Mailer mailer, Executor executor)
		{
			_factory = sessionFactory;
			_mailer = mailer;
			_execute = executor;
		}

		[OperationContract]
		public virtual IList<Offer> ShowOffersBySynonym(uint producerSynonymId)
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
		public virtual void DoJoin(uint[] sourceProduceIds, uint targetProducerId)
		{
			Transaction(session => {
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
;

drop temporary table if exists catalogs.TempForUpdateAssortment;
create temporary table catalogs.TempForUpdateAssortment engine 'memory'
  select * from catalogs.Assortment A where A.ProducerId = :SourceId and A.Checked = 1;
update catalogs.Assortment as ADest set Checked = 1 where ADest.ProducerId = :TargetId and
 exists (
		select * from catalogs.TempForUpdateAssortment as ASrc
		where ADest.CatalogId = ASrc.CatalogId and ASrc.ProducerId = :SourceId and ASrc.Checked = 1
);
drop temporary table if exists catalogs.TempForUpdateAssortment;
")
						.SetParameter("SourceId", sourceId)
						.SetParameter("TargetId", targetProducerId)
						.ExecuteUpdate();
					target.MergeToEquivalent(source);

					session.Delete(source);
				}
			});
		}

		[OperationContract]
		public virtual IList<ProducerSynonymDto> GetSynonyms(uint producerId)
		{
			return Slave(session => session.CreateSQLQuery(@"
select sfc.Synonym as Name,
sfc.SynonymFirmCrCode as Id,
cd.ShortName as Supplier,
r.Region,
c.Id is not null as HaveOffers
from farm.SynonymFirmCr sfc
  join usersettings.PricesData pd on sfc.PriceCode = pd.PriceCode
    join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
      join farm.Regions r on cd.RegionCode = r.RegionCode
  left join farm.Core0 c on c.SynonymFirmCrCode = sfc.SynonymFirmCrCode
where sfc.CodeFirmCr = :ProducerId and cd.BillingCode <> 921 and cd.FirmSegment = 0
group by sfc.SynonymFirmCrCode")
				.SetParameter("ProducerId", producerId)
				.SetResultTransformer(Transformers.AliasToBean(typeof (ProducerSynonymDto)))
				.List<ProducerSynonymDto>().ToList());
		}

		[OperationContract]
		public virtual IList<SynonymReportItem> ShowSynonymReport(DateTime begin, DateTime end)
		{
			if (begin.Date == end.Date)
				begin = begin.AddDays(-1);

			return Slave(s => SynonymReportItem.Load(s, begin, end));
		}

		[OperationContract]
		public virtual IList<SynonymReportItem> ShowSuspiciousSynonyms()
		{
			return Slave(s => SynonymReportItem.Suspicious(s));
		}

		[OperationContract]
		public virtual IList<string> GetEquivalents(uint producerId)
		{
			return Slave(s => {
				var producer = s.Get<Producer>(producerId);
				return producer.Equivalents.Select(e => e.Name).ToList();
			});
		}

		[OperationContract]
		public virtual void CreateEquivalentForProducer(uint producerId, string equivalentName)
		{
			Transaction(session => {
                var producer = session.Get<Producer>(producerId);
                producer.Equivalents.Add(new ProducerEquivalent(producer, equivalentName));
            });
		}

		[OperationContract]
		public virtual void DeleteProducerSynonym(uint producerSynonymId)
		{
			Transaction(session => {
				var synonym = session.Load<ProducerSynonym>(producerSynonymId);
				session.Delete(synonym);
				session.Save(new BlockedProducerSynonym(synonym));
				_mailer.SynonymWasDeleted(synonym);
			});
		}

		[OperationContract]
		public virtual void DeleteSynonym(uint synonymId)
		{
			Transaction(session => {
    			var synonym = session.Get<Synonym>(synonymId);
				if (synonym == null)
					return;
    			session.Delete(synonym);
				_mailer.SynonymWasDeleted(synonym);
    		});
		}

		[OperationContract]
		public virtual void Suspicious(uint producerSynonymId)
		{
			Transaction(session => {
				var synonym = session.Get<ProducerSynonym>(producerSynonymId);
				session.Save(new SuspiciousProducerSynonym(synonym));
			});
		}

		[OperationContract]
		public virtual void DeleteSuspicious(uint producerSynonymId)
		{
			Transaction(session => {
				var suspicious = session.Linq<SuspiciousProducerSynonym>().Where(s => s.Synonym.Id == producerSynonymId).First();
				session.Delete(suspicious);
			});
		}

		[OperationContract]
		public virtual void DeleteProducer(uint producerId)
		{
			Transaction(session => {
				var producer = session.Get<Producer>(producerId);
				session.Delete(producer);
			});
		}

		[OperationContract]
		public virtual void DeleteAssortment(uint assortmentId)
		{
			With.DeadlockWraper(() =>
				Transaction(session => {
				var productAssortment = session.Load<ProductAssortment>(assortmentId);
				session.Delete(productAssortment);

				var assortment = session.Load<Assortment>(assortmentId);
				session.CreateSQLQuery(@"
delete from farm.Core0
where CodeFirmCr = :ProducerId and ProductId in (
	select id from catalogs.Products where CatalogId = :CatalogId)")
				.SetParameter("CatalogId", assortment.CatalogProduct.Id)
				.SetParameter("ProducerId", assortment.Producer.Id)
				.ExecuteUpdate();
			}));
		}

		[OperationContract]
		public virtual void SetAssortmentChecked(uint assortmentId, bool @checked)
		{
			Transaction(session => {
				var assortment = session.Load<Assortment>(assortmentId);
				assortment.Checked = @checked;
				session.Update(assortment);
			});
		}

		[OperationContract]
		public virtual Pager<AssortmentDto> ShowAssortment(uint assortimentId)
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
		public virtual Pager<AssortmentDto> GetAssortmentPage(uint page)
		{
			return Slave(session => {
				var total = Assortment.TotalPages(session);
				return new Pager<AssortmentDto>(page, total, Assortment.Load(session, page));
			});
		}

		[OperationContract]
		public virtual Pager<AssortmentDto> SearchAssortment(string text, uint page)
		{
			return Slave(session => Assortment.Find(session, text, page));
		}

		[OperationContract]
		public virtual Pager<AssortmentDto> ShowAssortmentForProducer(uint producerId, uint page)
		{
			return Slave(s => Assortment.ByProducer(s, producerId, page));
		}

		[OperationContract]
		public virtual Pager<ExcludeDto> ShowExcludes(uint page)
		{
			return Slave(session => {
				var total = Exclude.TotalPages(session);
				return new Pager<ExcludeDto>(page, total, Exclude.Load(page, session));
			});
		}

		[OperationContract]
		public virtual Pager<ExcludeDto> SearchExcludes(string text, uint page)
		{
			return Slave(session => Exclude.Find(session, text, page));
		}

		[OperationContract]
		public virtual void DoNotShow(uint excludeId)
		{
			Transaction(session => {
				var exclude = session.Load<Exclude>(excludeId);
				exclude.DoNotShow = true;
				session.Update(exclude);
			});
		}

		[OperationContract]
		public virtual void AddToAssotrment(uint excludeId)
		{
			Transaction(session => {
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
				assortment.Checked = true;
				session.Save(assortment);
			});
		}

		[OperationContract]
		public string GetSupplierEmails(uint supplierId)
		{
			var sql = @"
select distinct c.contactText
from usersettings.clientsdata cd
  join contacts.contact_groups cg on cd.ContactGroupOwnerId = cg.ContactGroupOwnerId
    join contacts.contacts c on cg.Id = c.ContactOwnerId
where
    firmcode = :FirmCode
and cg.Type = 2
and c.Type = 0

union

select distinct c.contactText
from usersettings.clientsdata cd
  join contacts.contact_groups cg on cd.ContactGroupOwnerId = cg.ContactGroupOwnerId
    join contacts.persons p on cg.id = p.ContactGroupId
      join contacts.contacts c on p.Id = c.ContactOwnerId
where
    firmcode = :FirmCode
and cg.Type = 2
and c.Type = 0";
			IList<string> emails = null;
			Transaction(s => emails = s.CreateSQLQuery(sql).SetParameter("FirmCode", supplierId).List<string>());
			var emailList = emails.Aggregate("", (s, a) => s + a + "; ");
			return emailList;
		}
		
		private void Transaction(Action<ISession> action)
		{
			_execute.WithTransaction(action);
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