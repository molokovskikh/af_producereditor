using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Common.Models.Helpers;
using Common.MySql;
using NHibernate;
using NHibernate.Linq;
using ProducerEditor.Contract;
using ProducerEditor.Service.Models;
using ISession = NHibernate.ISession;
using ProducerEditor.Service.Helpers;

namespace ProducerEditor.Service
{
	public class ProducerService : IProducerService
	{
		private readonly ISessionFactory _factory;
		private readonly Mailer _mailer;
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

		public virtual void UpdateProducer(ProducerDto item)
		{
			if (!String.IsNullOrEmpty(item.Name))
				item.Name = item.Name.ToUpper();

			Update(item);
		}

		public virtual void UpdateAssortment(AssortmentDto item)
		{
			Update(item);
		}

		public void Update(ProducerEquivalentDto equivalent)
		{
			Transaction(s => {
				var producerEquivalent = s.Load<ProducerEquivalent>(equivalent.Id);
				producerEquivalent.Name = equivalent.Name;
				s.SaveOrUpdate(producerEquivalent);
			});
		}

		public virtual void Update(object item)
		{
			if (item is ProducerDto || item is AssortmentDto) {
				Transaction(s => {
					var doCleanup = false;

					var id = item.GetType().GetProperty("Id").GetValue(item, null);
					var entity = s.Load("ProducerEditor.Service.Models." + item.GetType().Name.Replace("Dto", ""), id);

					if (entity is Assortment
						&& !((Assortment)entity).Checked
						&& ((AssortmentDto)item).Checked)
						doCleanup = true;

					var value = item.GetType().GetProperty("Checked").GetValue(item, null);
					entity.GetType().GetProperty("Checked").SetValue(entity, value, null);

					if (item is ProducerDto)
						((Producer)entity).Name = ((ProducerDto)item).Name;

					s.Update(entity);

					if (doCleanup)
						((Assortment)entity).CleanupExcludes(s);
				});
			}
			else
				throw new Exception(String.Format("Не знаю как применять изменения для объекта {0}", item));
		}

		public virtual IList<ProducerDto> GetProducers(string text = "")
		{
			text = "%" + (text ?? "") + "%";
			return Slave(s => s.CreateSQLQuery(@"
select p.Id,
p.Name,
p.Checked,
exists(select * from farm.core0 c where c.CodeFirmCr = p.Id) as HasOffers
from Catalogs.Producers p
where p.Name like :text
or exists(select * from Catalogs.ProducerEquivalents e where e.Name like :text and e.ProducerId = p.Id)
order by p.Name")
				.SetResultTransformer(new AliasToPropertyTransformer(typeof(ProducerDto)))
				.SetParameter("text", text)
				.List<ProducerDto>());
		}

		public virtual List<OfferView> ShowOffers(OffersQueryParams query)
		{
			return Slave(s => new OffersQuery(query).Apply(s).List<OfferView>()).ToList();
		}

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
				.SetResultTransformer(new AliasToPropertyTransformer(typeof(ProductAndProducer)))
				.List<ProductAndProducer>()).ToList();
		}

		public virtual void DoJoin(uint[] sourceProduceIds, uint targetProducerId)
		{
			Transaction(session => {
				var target = session.Load<Producer>(targetProducerId);
				foreach (var sourceId in sourceProduceIds) {
					var source = session.Load<Producer>(sourceId);
					session.CreateSQLQuery(@"
drop temporary table if exists for_delete;
create temporary table for_delete engine=memory
select d.SynonymFirmCrCode as Id
from farm.SynonymFirmCr d
	join farm.SynonymFirmCr s on s.PriceCode = d.PriceCode and s.Synonym = d.Synonym and s.CodeFirmCr = :TargetId
where d.CodeFirmCr = :SourceId
;

delete s
from farm.SynonymFirmCr s
join for_delete d on d.Id = s.SynonymFirmCrCode
;

drop temporary table if exists for_delete;

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
					target.MergeToEquivalent(source, session);

					session.Delete(source);
				}
			});
		}

		public virtual IList<ProducerSynonymDto> GetSynonyms(uint producerId)
		{
			return Slave(session => ProducerSynonym.Load(session, new Query("ProducerId", producerId)));
		}

		public virtual IList<SynonymReportItem> ShowSynonymReport(DateTime begin, DateTime end)
		{
			if (begin.Date == end.Date)
				begin = begin.AddDays(-1);

			return Slave(s => SynonymReportQuery.Load(s, begin, end));
		}

		public virtual IList<SynonymReportItem> ShowSuspiciousSynonyms()
		{
			return Slave(s => SynonymReportQuery.Suspicious(s));
		}

		public virtual IList<ProducerEquivalentDto> GetEquivalents(uint producerId)
		{
			return Slave(s => {
				var producer = s.Get<Producer>(producerId);
				return producer.Equivalents.Select(e => new ProducerEquivalentDto(e.Id, e.Name)).ToList();
			});
		}

		public virtual void CreateEquivalentForProducer(uint producerId, string equivalentName)
		{
			Transaction(session => {
				var producer = session.Get<Producer>(producerId);
				producer.AddEquivalent(equivalentName);
			});
		}

		public virtual void DeleteProducerSynonym(uint producerSynonymId)
		{
			Transaction(session => {
				var synonym = session.Load<ProducerSynonym>(producerSynonymId);
				session.Delete(synonym);

				session.Save(new BlockedProducerSynonym(synonym));
				_mailer.SynonymWasDeleted(synonym);
			});
		}

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
				var productName = session.CreateSQLQuery(@"
select c.Name
from Catalogs.Products p
join Catalogs.Catalog c on c.Id = p.CatalogId
where p.Id = :productId")
					.SetParameter("productId", synonym.ProductId)
					.UniqueResult<string>();
				_mailer.SynonymWasDeleted(synonym, productName);
			});
		}

		public virtual void Suspicious(uint producerSynonymId)
		{
			Transaction(session => {
				var synonym = session.Get<ProducerSynonym>(producerSynonymId);
				session.Save(new SuspiciousProducerSynonym(synonym));
			});
		}

		public virtual void DeleteSuspicious(uint producerSynonymId)
		{
			Transaction(session => {
				var suspicious = session.Query<SuspiciousProducerSynonym>().First(s => s.Synonym.Id == producerSynonymId);
				session.Delete(suspicious);
			});
		}

		public void DeleteEquivalent(uint id)
		{
			Delete<ProducerEquivalent>(id);
		}

		public virtual void DeleteProducer(uint producerId)
		{
			Delete<Producer>(producerId);
		}

		public virtual void DeleteExclude(uint excludeId)
		{
			Delete<Exclude>(excludeId);
		}

		public void Delete<T>(uint id)
		{
			Transaction(session => {
				var producer = session.Get<T>(id);
				session.Delete(producer);
			});
		}

		public virtual void DeleteAssortment(uint assortmentId)
		{
			With.DeadlockWraper(() =>
				Transaction(session => {
					var productAssortment = session.Load<Assortment>(assortmentId);
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

		public virtual Pager<AssortmentDto> ShowAssortment(uint assortimentId)
		{
			return Slave(session => {
				uint page = 0;
				if (assortimentId != 0)
					page = Assortment.GetPage(session, assortimentId);
				return Assortment.Search(session, page, null);
			});
		}

		public virtual Pager<AssortmentDto> GetAssortmentPage(uint page)
		{
			return Slave(session => Assortment.Search(session, page, null));
		}

		public virtual Pager<AssortmentDto> SearchAssortment(string text, uint page)
		{
			return Slave(session => Assortment.Search(session, page, new Query("CatalogName", "%" + text + "%")));
		}

		public virtual Pager<AssortmentDto> ShowAssortmentForProducer(uint producerId, uint page)
		{
			return Slave(session => Assortment.Search(session, page, new Query("ProducerId", producerId)));
		}

		public virtual Pager<ExcludeDto> ShowExcludes()
		{
			return SearchExcludes("", false, false, 0, false);
		}

		public virtual Pager<ExcludeDto> SearchExcludes(string text, bool showPharmacie, bool showHidden, uint page, bool isRefresh)
		{
			text = "%" + text + "%";
			return Slave(
				session => session.SqlQuery<ExcludeDto>()
					.Filter("(e.ProducerSynonym like :text or c.Name like :text)", new { text })
					.Filter("e.DoNotShow = :showHidden", new { showHidden })
					.Filter("(:showPharmacie = 0 or c.Pharmacie = :showPharmacie)", new { showPharmacie })
					.Page(page));
		}

		public virtual void DoNotShow(uint excludeId)
		{
			Transaction(session => {
				var exclude = session.Load<Exclude>(excludeId);
				exclude.DoNotShow = true;
				session.Update(exclude);
			});
		}

		public virtual void AddToAssotrment(uint excludeId, uint producerId, string equivalent)
		{
			Transaction(s => {
				var exclude = s.Load<Exclude>(excludeId);
				var producer = s.Load<Producer>(producerId);
				var assortment = new Assortment(exclude.CatalogProduct, producer) {
					Checked = true
				};

				if (assortment.Exist(s)) {
					assortment = s.Query<Assortment>()
						.First(a => a.Producer == assortment.Producer && a.CatalogProduct == assortment.CatalogProduct);
					assortment.Checked = true;
				}

				if (!String.IsNullOrEmpty(equivalent)) {
					equivalent = equivalent.Trim();
					if (!producer.Equivalents.Any(e => e.Name.Equals(equivalent, StringComparison.CurrentCultureIgnoreCase)))
						s.Save(new ProducerEquivalent(producer, equivalent));
				}

				var synonym = new ProducerSynonym {
					Price = exclude.Price,
					Name = exclude.ProducerSynonym,
					Producer = producer
				};

				if (!synonym.Exist(s))
					s.Save(synonym);

				s.SaveOrUpdate(assortment);
				exclude.Remove(s);
				s.Flush();

				assortment.CleanupExcludes(s);
			});
		}

		public virtual void CreateEquivalent(uint excludeId, uint producerId)
		{
			Transaction(s => {
				var exclude = s.Load<Exclude>(excludeId);
				var producer = s.Load<Producer>(producerId);

				var equivalent = exclude.ProducerSynonym.Trim();

				if (!producer.Equivalents.Any(e => e.Name.Equals(equivalent, StringComparison.CurrentCultureIgnoreCase)))
					s.Save(new ProducerEquivalent(producer, equivalent));

				var synonym = new ProducerSynonym {
					Price = exclude.Price,
					Name = exclude.ProducerSynonym,
					Producer = producer
				};

				if (!synonym.Exist(s))
					s.Save(synonym);

				exclude.Remove(s);
			});
		}

		public virtual ExcludeData GetExcludeData(uint excludeId)
		{
			return Slave(s => {
				var exclude = s.Load<Exclude>(excludeId);
				var equivalients = s.CreateSQLQuery(@"
select e.ProducerId as Id, concat(e.Name, ' [', p.Name, ' ]') as Name
from Catalogs.ProducerEquivalents e
join Catalogs.Assortment a on a.ProducerId = e.ProducerId
join Catalogs.Producers p on p.Id = a.ProducerId
where a.Checked = 1 and a.CatalogId = :catalogId")
					.SetParameter("catalogId", exclude.CatalogProduct.Id)
					.SetResultTransformer(new AliasToPropertyTransformer(typeof(ProducerOrEquivalentDto)))
					.List<ProducerOrEquivalentDto>();
				var assortment = Assortment.Search(s, 0, new Query("CatalogId", exclude.CatalogProduct.Id)).Content.Where(a => a.Checked).ToList();
				var producers = equivalients
					.Concat(assortment.Select(a => new ProducerOrEquivalentDto { Id = a.ProducerId, Name = a.Producer }))
					.OrderBy(p => p.Name).ToList();
				return new ExcludeData {
					Producers = producers,
					Synonyms = ProducerSynonym.Load(s, new Query("Name", exclude.ProducerSynonym)),
				};
			});
		}

		public virtual string GetSupplierEmails(uint supplierId)
		{
			var sql = @"
select distinct c.contactText
from Customers.Suppliers s
	join contacts.contact_groups cg on s.ContactGroupOwnerId = cg.ContactGroupOwnerId
		join contacts.contacts c on cg.Id = c.ContactOwnerId
where
	s.Id = :FirmCode
and cg.Type = 2
and c.Type = 0

union

select distinct c.contactText
from Customers.Suppliers s
	join contacts.contact_groups cg on s.ContactGroupOwnerId = cg.ContactGroupOwnerId
		join contacts.persons p on cg.id = p.ContactGroupId
			join contacts.contacts c on p.Id = c.ContactOwnerId
where
	s.Id = :FirmCode
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
			using (var session = _factory.OpenSession())
				return func(session);
		}

		public bool CheckProductIsMonobrend(uint excludeId, uint producerId)
		{
			bool result = false;
			Transaction(s => {
				var exclude = s.Load<Exclude>(excludeId);
				if (exclude.CatalogProduct.Monobrend) {
					var assortiment = s.Query<Assortment>().Where(a => a.CatalogProduct.Id == exclude.CatalogProduct.Id && a.Producer.Id == producerId);
					if (assortiment.Count() == 0) {
						result = true;
					}
					else
						result = false;
				}
			});
			return result;
		}
	}
}