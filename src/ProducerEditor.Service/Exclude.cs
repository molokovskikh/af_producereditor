using System.Collections.Generic;
using System.Runtime.Serialization;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Transform;

namespace ProducerEditor.Service
{
	[Class(Table = "Farm.Excludes")]
	public class Exclude
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[ManyToOne(ClassType = typeof (ProducerSynonym), Column = "ProducerSynonymId")]
		public virtual ProducerSynonym ProducerSynonym { get; set; }

		[ManyToOne(ClassType = typeof (CatalogProduct), Column = "CatalogId")]
		public virtual CatalogProduct CatalogProduct { get; set; }

		[Property]
		public virtual bool DoNotShow { get; set; }

		public static uint TotalPages(ISession session)
		{
			return (uint)(session.CreateSQLQuery(@"
select count(distinct e.id)
from farm.Excludes e
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = e.ProducerSynonymId
		join Catalogs.Producers p on p.Id = sfc.CodeFirmCr
	left join Catalogs.Assortment a on a.CatalogId = e.CatalogId
where e.DoNotShow = 0 and cd.FirmSegment = 0 and (a.Checked = 1 or p.Checked = 1)
").UniqueResult<long>());
		}

		public static IList<ExcludeDto> Load(uint page, ISession session)
		{
			return session.CreateSQLQuery(@"
select e.Id,
	c.Name as Catalog,
	p.Name as Producer,
	sfc.SynonymFirmCrCode as ProducerSynonymId,
	sfc.Synonym as ProducerSynonym,
	r.Region,
	cd.ShortName as Supplier,
	syn.Synonym as OriginalSynonym,
	e.OriginalSynonymId
from farm.Excludes e
	join Catalogs.Catalog c on c.Id = e.CatalogId
	left join farm.Synonym syn on syn.SynonymCode = e.OriginalSynonymId
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = e.ProducerSynonymId
		join Catalogs.Producers p on p.Id = sfc.CodeFirmCr
		left join Catalogs.Assortment a on a.CatalogId = c.Id
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
		join farm.Regions r on r.RegionCode = cd.RegionCode
where e.DoNotShow = 0 and cd.FirmSegment = 0 and (a.Checked = 1 or p.Checked = 1)
group by e.Id
order by e.CreatedOn
limit :begin, 100
")
				.SetResultTransformer(Transformers.AliasToBean<ExcludeDto>())
				.SetParameter("begin", page * 100)
				.List<ExcludeDto>();
		}

		public static Pager<ExcludeDto> Find(ISession session, string text, uint page)
		{
			var excludes = session.CreateSQLQuery(@"
select	e.Id,
	e.ProducerSynonymId,
	e.OriginalSynonymId,
	c.Name as Catalog,
	r.Region,
	p.Name as Producer,
	sfc.SynonymFirmCrCode as ProducerSynonymId,
	sfc.Synonym as ProducerSynonym,
	cd.ShortName as Supplier,
	syn.Synonym as OriginalSynonym,
	e.OriginalSynonymId
from Farm.Excludes e
	join Catalogs.Catalog c on c.Id = e.CatalogId
	left join farm.Synonym syn on syn.SynonymCode = e.OriginalSynonymId
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = e.ProducerSynonymId
		join Catalogs.Producers p on p.Id = sfc.CodeFirmCr
		left join Catalogs.Assortment a on a.CatalogId = c.Id
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
		join farm.Regions r on r.RegionCode = cd.RegionCode
where (e.DoNotShow = 0 and cd.FirmSegment = 0 and (a.Checked = 1 or p.Checked = 1)) and
		(p.Name like :text or c.Name like :text)
group by e.Id
order by e.CreatedOn
limit :begin, 100")
				.SetParameter("text", "%" + text + "%")
				.SetResultTransformer(new AliasToBeanResultTransformer(typeof(ExcludeDto)))
				.SetParameter("begin", page * 100)
				.List<ExcludeDto>();

			var excludesCount = session.CreateSQLQuery(@"
select	e.Id
from Farm.Excludes e
	join Catalogs.Catalog c on c.Id = e.CatalogId
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = e.ProducerSynonymId
		join Catalogs.Producers p on p.Id = sfc.CodeFirmCr
		left join Catalogs.Assortment a on a.CatalogId = c.Id
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
		join farm.Regions r on r.RegionCode = cd.RegionCode
where (e.DoNotShow = 0 and cd.FirmSegment = 0 and (a.Checked = 1 or p.Checked = 1)) and
		(p.Name like :text or c.Name like :text)
group by e.Id")
				.SetParameter("text", "%" + text + "%")
				.List<uint>().Count;

			return new Pager<ExcludeDto>(page, (uint)excludesCount, excludes);
		}
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service", Name = "Exclude")]
	public class ExcludeDto
	{
		[DataMember]
		public uint Id { get; set; }
		[DataMember]
		public string Supplier { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public string Catalog { get; set; }
		[DataMember]
		public string Producer { get; set; }
		[DataMember]
		public string ProducerSynonym { get; set; }
		[DataMember]
		public uint ProducerSynonymId { get; set; }
		[DataMember]
		public string OriginalSynonym { get; set; }
		[DataMember]
		public uint OriginalSynonymId { get; set; }
	}
}