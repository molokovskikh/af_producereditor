using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using Common.Models.Helpers;
using Common.MySql;
using NHibernate;
using NHibernate.Linq;
using ProducerEditor.Contract;
using ProducerEditor.Service.Models;
using ConnectionManager = Common.MySql.ConnectionManager;
using ISession=NHibernate.ISession;
using ProducerEditor.Service.Helpers;

namespace ProducerEditor.Service
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class ProductAndProducer
	{
		[DataMember]
		public bool Selected { get; set; }
		[DataMember]
		public long ExistsInRls { get; set; }
		[DataMember]
		public uint ProducerId { get; set; }
		[DataMember]
		public string Producer { get; set; }
		[DataMember]
		public uint CatalogId { get; set; }
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public long OrdersCount { get; set; }
		[DataMember]
		public long OffersCount { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class OfferView
	{
		[DataMember]
		public string ProductSynonym { get; set; }
		[DataMember]
		public string ProducerSynonym { get; set; }
		[DataMember]
		public string Supplier { get; set; }
		[DataMember]
		public byte Segment { get; set; }
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
		public void UpdateProducer(ProducerDto item)
		{
			if (!String.IsNullOrEmpty(item.Name))
				item.Name = item.Name.ToUpper();

			Update(item);
		}

		[OperationContract]
		public void UpdateAssortment(AssortmentDto item)
		{
			Update(item);
		}

		[OperationContract]
		public void Update(object item)
		{
			if (item is ProducerDto || item is AssortmentDto)
			{
				Transaction(s => {
					var id = item.GetType().GetProperty("Id").GetValue(item, null);
					var entity = s.Load("ProducerEditor.Service." + item.GetType().Name.Replace("Dto", ""), id);

					var value = item.GetType().GetProperty("Checked").GetValue(item, null);
					entity.GetType().GetProperty("Checked").SetValue(entity, value, null);

					if (item is ProducerDto)
						((Producer) entity).Name = ((ProducerDto) item).Name;

					s.Update(entity);
				});
			}
			else
				throw new Exception(String.Format("Не знаю как применять изменения для объекта {0}", item));
		}

		[OperationContract]
		public virtual IList<ProducerDto> GetProducers()
		{
			return Slave(s => s.CreateSQLQuery(@"
select p.Id,
p.Name,
p.Checked,
c.Id != 0 as HasOffers
from Catalogs.Producers p
	left join farm.core0 c on c.CodeFirmCr = p.Id
group by p.Id
order by p.Name")
				.SetResultTransformer(new AliasToPropertyTransformer(typeof (ProducerDto)))
				.List<ProducerDto>());
		}

		[OperationContract]
		public virtual List<OfferView> ShowOffers(OffersQuery query)
		{
			return Slave(s => query.Apply(s).List<OfferView>()).ToList();
		}

		[OperationContract]
		public virtual List<ProductAndProducer> ShowProductsAndProducers(uint producerId)
		{
			return Slave(s => s.CreateSQLQuery(@"
drop temporary table if exists ProductFromOrders;
create temporary table ProductFromOrders engine 'memory'
select productid
from orders.orderslist
where CodeFirmCr = :ProducerId
group by ProductId;

drop temporary table if exists ProductsAndProducers;
create temporary table ProductsAndProducers engine 'memory'
select
ol.ProductId, ol.CodeFirmCr, 0 as OffersCount, 0 as OrdersCount, 0 as ExistsInRls
from orders.orderslist ol
  join ProductFromOrders p on ol.ProductId = p.ProductId
where ol.CodeFirmCr is not null
group by ol.ProductId, ol.CodeFirmCr
union
select
c.ProductId, c.CodeFirmCr, 0 as OffersCount, 0 as OrdersCount, 0 as ExistsInRls
from farm.core0 c
	join catalogs.Products products on products.Id = c.ProductId
		join catalogs.Products p on p.CatalogId = products.CatalogId
			join farm.core0 sibling on sibling.ProductId = p.Id
where sibling.CodeFirmCr = :ProducerId and c.CodeFirmCr is not null
group by c.ProductId, c.CodeFirmCr;

update ProductsAndProducers pap
set pap.OffersCount = (select count(*) from farm.core0 c where c.CodeFirmCr = pap.CodeFirmCr and c.ProductId = pap.ProductId),
    pap.OrdersCount = (select count(*) from orders.orderslist ol where ol.CodeFirmCr = pap.CodeFirmCr and ol.ProductId = pap.ProductId);

update ProductsAndProducers pap
set ExistsInRls = exists(select * from farm.core0 c where c.CodeFirmCr = pap.CodeFirmCr and c.ProductId = pap.ProductId and c.PriceCode = 1864);

select p.CatalogId,
	   concat(cn.Name, ' ', cf.Form) as Product,
	   pr.Id as ProducerId,
       pr.Name as Producer,
	   pap.OrdersCount,
	   pap.OffersCount,
	   pap.ExistsInRls
from ProductsAndProducers pap
  join catalogs.Products as p on p.id = pap.productid
	  join Catalogs.Catalog as c on p.catalogid = c.id
    	JOIN Catalogs.CatalogNames cn on cn.id = c.nameid
    	JOIN Catalogs.CatalogForms cf on cf.id = c.formid
  join Catalogs.Producers pr on pr.Id = pap.CodeFirmCr
group by pap.ProductId, pap.CodeFirmCr
order by p.Id;")
				.SetParameter("ProducerId", producerId)
				.SetResultTransformer(new AliasToPropertyTransformer(typeof (ProductAndProducer)))
				.List<ProductAndProducer>()).ToList();
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
					target.MergeToEquivalent(source, session);

					session.Delete(source);
				}
			});
		}

		[OperationContract]
		public virtual IList<ProducerSynonymDto> GetSynonyms(uint producerId)
		{
			return Slave(session => ProducerSynonym.Load(session, new Query("ProducerId", producerId)));
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

				session.CreateSQLQuery(@"
DELETE FROM Farm.Excludes
WHERE OriginalSynonymId = :SynonymId")
					.SetParameter("SynonymId", synonymId)
					.ExecuteUpdate();

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
		public virtual Pager<AssortmentDto> ShowAssortment(uint assortimentId)
		{
			return Slave(session => {
				uint page = 0;
				if (assortimentId != 0)
					page = Assortment.GetPage(session, assortimentId);
				return Assortment.Search(session, page, null);
			});
		}

		[OperationContract]
		public virtual Pager<AssortmentDto> GetAssortmentPage(uint page)
		{
			return Slave(session => Assortment.Search(session, page, null));
		}

		[OperationContract]
		public virtual Pager<AssortmentDto> SearchAssortment(string text, uint page)
		{
			return Slave(session => Assortment.Search(session, page, new Query("CatalogName", "%" + text + "%")));
		}

		[OperationContract]
		public virtual Pager<AssortmentDto> ShowAssortmentForProducer(uint producerId, uint page)
		{
			return Slave(session => Assortment.Search(session, page, new Query("ProducerId", producerId)));
		}

		[OperationContract]
		public virtual Pager<ExcludeDto> ShowExcludes(uint page, bool isRefresh)
		{
			Pager<ExcludeDto> pager = null;

			if (isRefresh)
			{
				Transaction(session => {
					var total = Exclude.TotalPages(session);
					pager = new Pager<ExcludeDto>(page, total, Exclude.Load(page, session));
				});
				return pager;
			}
			return Slave(session => {
				var total = Exclude.TotalPages(session);
				return new Pager<ExcludeDto>(page, total, Exclude.Load(page, session));
			});
		}

		[OperationContract]
		public virtual Pager<ExcludeDto> SearchExcludes(string text, uint page, bool isRefresh)
		{
			if (isRefresh)
			{
				Pager<ExcludeDto> pager = null;
				Transaction(session => {
					var total = Exclude.TotalPages(session);
					pager = new Pager<ExcludeDto>(page, total, Exclude.Load(page, session));
				});
				return pager;
			}
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
		public void AddToAssotrment(uint excludeId, uint producerId, string equivalent)
		{
			Transaction(s => {
				var exclude = s.Load<Exclude>(excludeId);
				var producer = s.Load<Producer>(producerId);
				var assortment = new Assortment(exclude.CatalogProduct, producer) {
					Checked = true
				};

				if (assortment.Exist(s))
					throw new Exception("Запись в ассортименте уже существует");

				if (!String.IsNullOrEmpty(equivalent))
				{
					equivalent = equivalent.Trim();
					if (!producer.Equivalents.Any(e => e.Name.Equals(equivalent, StringComparison.CurrentCultureIgnoreCase)))
						s.Save(new ProducerEquivalent(producer, equivalent));
				}

				s.Delete(exclude);
				s.Save(assortment);
			});
		}

		[OperationContract]
		public ExcludeData GetExcludeData(uint excludeId)
		{
			return Slave(s => {
				var exclude = s.Load<Exclude>(excludeId);
				return new ExcludeData {
					Assortments = Assortment.Search(s, 0, new Query("CatalogId", exclude.CatalogProduct.Id)).Content.ToList(),
					Synonyms = ProducerSynonym.Load(s, new Query("Name", exclude.ProducerSynonym)),
				};
			});
		}

/*
		[OperationContract]
		public virtual IList<uint> AddToAssotrment(uint excludeId)
		{
			var deletedExcludesIds = new List<uint>();

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

				excludes.Each(e => deletedExcludesIds.Add(e.Id));
				excludes.Each(session.Delete);

				session.Delete(exclude);
				assortment.Checked = true;
				session.Save(assortment);
			});
			return deletedExcludesIds;
		}
*/

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

		public T Slave<T>(Func<ISession, T> func)
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
