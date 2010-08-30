using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Common.Models.Helpers;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace ProducerEditor.Service
{
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
		public string ProducerSynonym { get; set; }
		[DataMember]
		public string OriginalSynonym { get; set; }
		[DataMember]
		public uint OriginalSynonymId { get; set; }
	}

	public class UniuqAttribute : Attribute
	{}

	[Class(Table = "Farm.Excludes")]
	public class Exclude
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ProducerSynonym { get; set; }

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
where e.DoNotShow = 0 and cd.FirmSegment = 0
").UniqueResult<long>());
		}

		public static IList<ExcludeDto> Load(uint page, ISession session)
		{
			return session.CreateSQLQuery(@"
select e.Id,
	c.Name as Catalog,
	e.ProducerSynonym,
	r.Region,
	cd.ShortName as Supplier,
	ifnull(syn.Synonym, synarch.Synonym) as OriginalSynonym,
	e.OriginalSynonymId
from farm.Excludes e
	join Catalogs.Catalog c on c.Id = e.CatalogId
	left join farm.Synonym syn on syn.SynonymCode = e.OriginalSynonymId
	left join farm.SynonymArchive synarch on synarch.SynonymCode = e.OriginalSynonymId
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
		join farm.Regions r on r.RegionCode = cd.RegionCode
where e.DoNotShow = 0 and cd.FirmSegment = 0
group by e.Id
order by e.CreatedOn
limit :begin, 100
")
				.SetResultTransformer(new AliasToPropertyTransformer(typeof(ExcludeDto)))
				.SetParameter("begin", page * 100)
				.List<ExcludeDto>();
		}

		public static Pager<ExcludeDto> Find(ISession session, string text, uint page)
		{
			var excludes = session.CreateSQLQuery(@"
select	e.Id,
	e.OriginalSynonymId,
	c.Name as Catalog,
	r.Region,
	e.ProducerSynonym,
	cd.ShortName as Supplier,
	ifnull(syn.Synonym, synarch.Synonym) as OriginalSynonym,
	e.OriginalSynonymId
from Farm.Excludes e
	join Catalogs.Catalog c on c.Id = e.CatalogId
	left join farm.Synonym syn on syn.SynonymCode = e.OriginalSynonymId
	left join farm.SynonymArchive synarch on synarch.SynonymCode = e.OriginalSynonymId
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
		join farm.Regions r on r.RegionCode = cd.RegionCode
where e.DoNotShow = 0 and cd.FirmSegment = 0 and (e.ProducerSynonym like :text or c.Name like :text)
group by e.Id
order by e.CreatedOn
limit :begin, 100")
				.SetParameter("text", "%" + text + "%")
				.SetResultTransformer(new AliasToPropertyTransformer(typeof(ExcludeDto)))
				.SetParameter("begin", page * 100)
				.List<ExcludeDto>();

			var excludesCount = session.CreateSQLQuery(@"
select	e.Id
from Farm.Excludes e
	join Catalogs.Catalog c on c.Id = e.CatalogId
	join usersettings.PricesData pd on pd.PriceCode = e.PriceCode
		join usersettings.ClientsData cd on cd.FirmCode = pd.FirmCode
		join farm.Regions r on r.RegionCode = cd.RegionCode
where e.DoNotShow = 0 and cd.FirmSegment = 0 and (e.ProducerSynonym like :text or c.Name like :text)
group by e.Id")
				.SetParameter("text", "%" + text + "%")
				.List<uint>().Count;

			return new Pager<ExcludeDto>(page, (uint)excludesCount, excludes);
		}
	}
}