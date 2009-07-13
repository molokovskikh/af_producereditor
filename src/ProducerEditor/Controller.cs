using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Scopes;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Transform;
using ProducerEditor.Models;

namespace ProducerEditor
{
	public class Controller
	{
		private List<Producer> producers;

		public IList<Producer> GetAllProducers()
		{
			producers = WithSession(s => s.CreateSQLQuery(@"
select cfc.CodeFirmCr as Id,
cfc.FirmCr as Name,
cfc.Hidden,
c.Id != 0 as HasOffers
from farm.CatalogFirmCr cfc
	left join farm.core0 c on c.CodeFirmCr = cfc.CodeFirmCr
where cfc.Hidden = 0
group by cfc.CodeFirmCr
order by cfc.FirmCr")
			                 	.SetResultTransformer(Transformers.AliasToBean(typeof(Producer)))
			                 	.List<Producer>()).ToList();
			return producers;
		}

		public void Update(Producer producer)
		{
			producer.Name = producer.Name.ToUpper();
			InMaster(producer.Update);
		}

		public void Join(Producer source, Producer target)
		{
			InMaster(() => WithSession(session => {
				var producerEquivalent = new ProducerEquivalent
											{
												Name = source.Name,
												Producer = target,
											};
				using (var transaction = session.BeginTransaction())
				{
					session.CreateSQLQuery(@"
update farm.SynonymFirmCr
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId")
						.SetParameter("SourceId", source.Id)
						.SetParameter("TargetId", target.Id)
						.ExecuteUpdate();
					session.Save(producerEquivalent);
					session.Delete(source);
					transaction.Commit();
				}
				producers.Remove(source);
			}));
		}

		public IList<SynonymView> Synonyms(Producer producer)
		{
			return WithSession(
				session => session.CreateSQLQuery(@"
select sfc.Synonym,
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
				           	.SetParameter("ProducerId", producer.Id)
				           	.SetResultTransformer(Transformers.AliasToBean(typeof (SynonymView)))
				           	.List<SynonymView>().ToList());
		}

		private static T WithSession<T>(Func<ISession, T> action)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try
			{
				return action(session);
			}
			finally
			{
				sessionHolder.ReleaseSession(session);
			}
		}

		private static void WithSession(Action<ISession> action)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try
			{
				action(session);
			}
			finally
			{
				sessionHolder.ReleaseSession(session);
			}
		}

		public List<Producer> SearchProducer(string text)
		{
			return producers.Where(p => p.Name.Contains((text ?? "").ToUpper())).ToList();
		}

		public List<ProductAndProducer> FindRelativeProductsAndProducers(Producer producer)
		{
			return WithSession(s => s.CreateSQLQuery(@"
drop temporary table if exists ProductsAndProducers;
create temporary table ProductsAndProducers engine 'memory'
select
ol.ProductId, ol.CodeFirmCr
from orders.orderslist ol
  join orders.orderslist sibling on ol.ProductId = sibling.ProductId
where sibling.CodeFirmCr = :ProducerId and ol.CodeFirmCr is not null
group by ol.ProductId, ol.CodeFirmCr
union
select
c.ProductId, c.CodeFirmCr
from farm.core0 c
  join farm.core0 sibling on c.ProductId = sibling.ProductId
where sibling.CodeFirmCr = :ProducerId and c.CodeFirmCr is not null
group by c.ProductId, c.CodeFirmCr;

select cast(concat(cn.Name, ' ', cf.Form, ' ', ifnull(group_concat(distinct pv.Value ORDER BY prop.PropertyName, pv.Value SEPARATOR ', '), '')) as CHAR) as Product,
       cfc.FirmCr as Producer
from ProductsAndProducers pap
  join catalogs.Products as p on p.id = pap.productid
	  join Catalogs.Catalog as c on p.catalogid = c.id
    	JOIN Catalogs.CatalogNames cn on cn.id = c.nameid
    	JOIN Catalogs.CatalogForms cf on cf.id = c.formid
  LEFT JOIN Catalogs.ProductProperties pp on pp.ProductId = p.Id
	  LEFT JOIN Catalogs.PropertyValues pv on pv.id = pp.PropertyValueId
    	LEFT JOIN Catalogs.Properties prop on prop.Id = pv.PropertyId
  join farm.CatalogFirmCr cfc on cfc.CodeFirmCr = pap.CodeFirmCr
group by pap.ProductId, pap.CodeFirmCr
order by cfc.FirmCr;")
			.SetParameter("ProducerId", producer.Id)
			.SetResultTransformer(Transformers.AliasToBean(typeof(ProductAndProducer)))
			.List<ProductAndProducer>()).ToList();
		}

		public List<OrderView> FindOrders(Producer producer)
		{
			return WithSession(s => s.CreateSQLQuery(@"
select oh.WriteTime,
drugstore.ShortName as Drugstore,
supplier.ShortName as Supplier,
sa.Synonym as ProductSynonym,
sfc.Synonym as ProducerSynonym
from orders.orderslist ol
  join orders.ordershead oh on ol.OrderId = oh.RowId
    join farm.SynonymArchive sa on sa.SynonymCode = ol.SynonymCode
    join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = ol.SynonymFirmCrCode
  join usersettings.clientsdata drugstore on drugstore.FirmCode = oh.ClientCode
  join usersettings.pricesdata pd on pd.PriceCode = oh.PriceCode
    join usersettings.clientsdata supplier on supplier.FirmCode = pd.FirmCode
where ol.CodeFirmCr = :ProducerId and oh.Deleted = 0
order by oh.WriteTime desc
limit 20")
					.SetParameter("ProducerId", producer.Id)
					.SetResultTransformer(Transformers.AliasToBean(typeof (OrderView)))
					.List<OrderView>()).ToList();
		}

		public List<OfferView> FindOffers(Producer producer)
		{
			return WithSession(s => s.CreateSQLQuery(@"
select cd.ShortName as Supplier, 
cd.FirmSegment as Segment,
s.Synonym as ProductSynonym, 
sfc.Synonym as ProducerSynonym
from farm.core0 c
  join farm.SynonymArchive s on s.SynonymCode = c.SynonymCode
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode
  join usersettings.PricesData pd on pd.PriceCode = c.PriceCode
    join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
where c.CodeFirmCr = :ProducerId
group by c.Id
order by cd.FirmCode
limit 50")
			                        	.SetParameter("ProducerId", producer.Id)
			                        	.SetResultTransformer(Transformers.AliasToBean(typeof (OfferView)))
			                        	.List<OfferView>()).ToList();
		}

		public void InMaster(Action action)
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Master"].ConnectionString))
			{
				connection.Open();
				using(new DifferentDatabaseScope(connection))
				{
					action();
				}
			}
		}
	}
}
