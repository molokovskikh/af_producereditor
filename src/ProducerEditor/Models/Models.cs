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

/*		[Property]
		public virtual byte Hidden { get; set; }*/

		[HasMany(Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.Delete, ColumnKey = "CodeFirmCr")]
		public virtual IList<ProducerSynonym> Synonyms { get; set; }

		[HasMany(Inverse = true, Lazy = true, Cascade = ManyRelationCascadeEnum.Delete, ColumnKey = "ProducerId")]
		public virtual IList<ProducerEquivalent> Equivalents { get; set; }

		public virtual long HasOffers { get; set;}
	}

	[ActiveRecord(Table = "farm.SynonymFirmCr")]
	public class ProducerSynonym : ActiveRecordLinqBase<ProducerSynonym>
	{
		[PrimaryKey(Column = "SynonymFirmCrCode")]
		public virtual uint Id { get; set; }

		[Property(Column = "Synonym")]
		public virtual string Name { get; set; }

		[BelongsTo(Column = "CodeFirmCr")]
		public virtual Producer Producer { get; set; }
	}

	public class SynonymView : ProducerSynonym
	{
		public string Supplier { get; set; }
		public string Region { get; set; }
		public byte Segment { get; set; }
		public Int64 HaveOffers { get; set; }

		public string SegmentAsString()
		{
			return Segment == 0 ? "Опт" : "Розница";
		}
	}

	public class SynonymReportItem
	{
		public string User { get; set; }
		public string Price { get; set; }
		public string Region { get; set; }
		public string Synonym { get; set; }
		public string Producer { get; set; }
		public string Products { get; set; }

		public static IList<SynonymReportItem> Load(DateTime begin, DateTime end)
		{
			return With.Session(s => s.CreateSQLQuery(@"
SELECT sfcl.OperatorName as User,
       cd.ShortName as Price,
       r.Region,
       sfc.Synonym,
       pr.Name as Producer,
       (select group_concat(distinct concat(cn.Name, ' ', cf.Form) separator ', ')
        from farm.core0 c
          left join catalogs.products p on p.id = c.productid
            left join catalogs.catalog cc on cc.Id = p.CatalogId
              left join catalogs.catalognames cn on cn.id = cc.NameId
              left join catalogs.catalogforms cf on cf.id = cc.formid
        where c.codefirmcr = sfc.CodeFirmCr) as Products
FROM logs.SynonymFirmCrLogs sfcl
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = sfcl.SynonymFirmCrCode
    join usersettings.pricesdata pd on pd.pricecode = sfc.pricecode
      join usersettings.clientsdata cd on pd.FirmCode = cd.FirmCode
        join farm.Regions r on r.RegionCode = cd.RegionCode
    join Catalogs.Producers pr on pr.Id = sfc.CodeFirmCr
where sfcl.Operation = 0 and sfcl.LogTime between :begin and :end
group by sfc.SynonymFirmCrCode;")
					.SetParameter("begin", begin)
					.SetParameter("end", end)
					.SetResultTransformer(Transformers.AliasToBean(typeof (SynonymReportItem)))
					.List<SynonymReportItem>()).ToList();;
		}
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
