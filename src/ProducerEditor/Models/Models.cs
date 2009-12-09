using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using NHibernate.Transform;
using ProducerEditor.Views;

namespace ProducerEditor.Models
{
	[ActiveRecord(Table = "Catalogs.Producers")]
	public class Producer : ActiveRecordLinqBase<Producer>
	{
		[PrimaryKey(Column = "Id")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Checked { get; set; }

		[HasMany(Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.Delete, ColumnKey = "ProducerId")]
		public virtual IList<ProducerEquivalent> Equivalents { get; set; }

		public virtual long HasOffers { get; set;}
	}

	public class ProductAndProducer
	{
		public bool Selected { get; set; }
		public long ExistsInRls { get; set; }
		public uint ProducerId { get; set; }
		public string Producer { get; set; }
		public uint CatalogId { get; set; }
		public string Product { get; set; }
		public long OrdersCount { get; set; }
		public long OffersCount { get; set; }

		public static List<ProductAndProducer> FindRelativeProductsAndProducers(Producer producer)
		{
			var result = With.Session(s => s.CreateSQLQuery(@"
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
				.SetParameter("ProducerId", producer.Id)
				.SetResultTransformer(Transformers.AliasToBean(typeof (ProductAndProducer)))
				.List<ProductAndProducer>()).ToList();
			return result;
		}
	}

	public class OrderView
	{
		public string Supplier { get; set; }
		public string Drugstore { get; set; }
		public DateTime WriteTime { get; set; }
		public string ProductSynonym { get; set; }
		public string ProducerSynonym { get; set; }
	}

	public class OfferView
	{
		public string ProductSynonym { get; set; }
		public string ProducerSynonym { get; set; }
		public string Supplier { get; set; }
		public byte Segment { get; set; }

		public string SegmentAsString()
		{
			return Segment == 0 ? "Опт" : "Розница";
		}
	}

	[ActiveRecord(Table = "ProducerEquivalents", Schema = "catalogs")]
	public class ProducerEquivalent
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo(Column = "ProducerId")]
		public virtual Producer Producer { get; set; }
	}
}
