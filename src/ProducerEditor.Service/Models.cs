using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Transform;

namespace ProducerEditor.Service
{
	[Class(Table = "UserSettings.PricesData")]
	public class Price
	{
		[Id(0, Column = "PriceCode", Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[ManyToOne(ClassType = typeof (Supplier), Column = "FirmCode")]
		public virtual Supplier Supplier { get; set; }
	}

	[Class(Table = "UserSettings.ClientsData")]
	public class Supplier
	{
		[Id(0, Name = "Id", Column = "FirmCode")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ShortName { get; set; }

		[ManyToOne(ClassType = typeof (Region), Column = "RegionCode")]
		public virtual Region Region { get; set; }
	}

	[Class(Table = "Farm.Regions")]
	public class Region
	{
		[Id(0, Column = "RegionCode", Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual ulong Id { get; set; }

		[Property(Column = "Region")]
		public virtual string Name { get; set; }
	}

	[Class(Table = "Farm.BlockedProducerSynonyms")]
	public class BlockedProducerSynonym
	{
		protected BlockedProducerSynonym()
		{}

		public BlockedProducerSynonym(ProducerSynonym synonym)
		{
			Producer = synonym.Producer;
			Synonym = synonym.Name;
			BlockedOn = DateTime.Now;
		}

		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[ManyToOne(ClassType = typeof (Producer), Column = "ProducerId")]
		public virtual Producer Producer { get; set; }

		[Property]
		public virtual string Synonym { get; set; }

		[Property]
		public virtual DateTime BlockedOn { get; set; }
	}

	[Class(Table = "farm.SynonymFirmCr")]
	public class ProducerSynonym
	{
		[Id(0, Name = "Id", Column = "SynonymFirmCrCode")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property(Column = "Synonym")]
		public virtual string Name { get; set; }

		[ManyToOne(ClassType = typeof (Producer), Column = "CodeFirmCr")]
		public virtual Producer Producer { get; set; }

		[ManyToOne(ClassType = typeof (Price), Column = "PriceCode")]
		public virtual Price Price { get; set; }
	}

	[Class(Table = "Catalogs.Producers")]
	public class Producer
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Checked { get; set; }

		[Bag(0, Lazy = true, Inverse = true)]
		[Key(1, Column = "CodeFirmCr")]
		[OneToMany(2, ClassType = typeof (ProducerSynonym))]
		public virtual IList<ProducerSynonym> Synonyms { get; set; }

		[Bag(0, Lazy = true, Inverse = true)]
		[Key(1, Column = "ProducerId")]
		[OneToMany(2, ClassType = typeof (ProducerEquivalent))]
		public virtual IList<ProducerEquivalent> Equivalents { get; set; }
	}

	[Class(Table = "Catalogs.ProducerEquivalents")]
	public class ProducerEquivalent
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[ManyToOne(ClassType = typeof (Producer), Column = "ProducerId")]
		public virtual Producer Producer { get; set; }
	}

	[Class(Table = "Farm.SuspiciousSynonyms")]
	public class SuspiciousProducerSynonym
	{
		public SuspiciousProducerSynonym()
		{}

		public SuspiciousProducerSynonym(ProducerSynonym synonym)
		{
			Synonym = synonym;
		}

		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[ManyToOne(ClassType = typeof (ProducerSynonym), Column = "ProducerSynonymId")]
		public virtual ProducerSynonym Synonym { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class Assortment
	{
		[DataMember]
		public uint Id { get; set; }
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public string Producer { get; set; }

		public static IList<Assortment> Load(ISession session, uint page)
		{
			return session.CreateSQLQuery(@"
select	a.Id, 
		pr.Name as Producer,
		cast(concat(cn.Name, ' ', cf.Form, ' ', ifnull(group_concat(distinct pv.Value ORDER BY prop.PropertyName, pv.Value SEPARATOR ', '), '')) as CHAR) as Product
from catalogs.Assortment a
	join catalogs.Producers pr on pr.Id = a.ProducerId
	join catalogs.Products p on p.Id = a.ProductId
		join Catalogs.Catalog as c on p.catalogid = c.id
			join Catalogs.CatalogNames cn on cn.id = c.nameid
			join Catalogs.CatalogForms cf on cf.id = c.formid
	left join Catalogs.ProductProperties pp on pp.ProductId = p.Id
		left join Catalogs.PropertyValues pv on pv.id = pp.PropertyValueId
		left join Catalogs.Properties prop on prop.Id = pv.PropertyId
group by a.Id
limit :begin, 100")
					.SetParameter("begin", page * 100)
					.SetResultTransformer(Transformers.AliasToBean<Assortment>())
					.List<Assortment>();
		}

		public static uint TotalPages(ISession session)
		{
			return (uint) session.CreateSQLQuery(@"select count(*) from catalogs.Assortment").UniqueResult<long>() / 100;
		}

		public static uint GetPage(ISession session, uint assortimentId)
		{
			var count = session
				.CreateSQLQuery(@"select count(*) from catalogs.Assortment where id < :id")
				.SetParameter("id", assortimentId)
				.UniqueResult<long>();
			return (uint) (count/100);
		}

		public static uint Find(ISession session, string text)
		{
			var assortiment = session.CreateSQLQuery(@"
select	a.Id, 
		pr.Name as Producer,
		cast(concat(cn.Name, ' ', cf.Form, ' ', ifnull(group_concat(distinct pv.Value ORDER BY prop.PropertyName, pv.Value SEPARATOR ', '), '')) as CHAR) as Product
from catalogs.Assortment a
	join catalogs.Producers pr on pr.Id = a.ProducerId
	join catalogs.Products p on p.Id = a.ProductId
		join Catalogs.Catalog as c on p.catalogid = c.id
			join Catalogs.CatalogNames cn on cn.id = c.nameid
			join Catalogs.CatalogForms cf on cf.id = c.formid
	left join Catalogs.ProductProperties pp on pp.ProductId = p.Id
		left join Catalogs.PropertyValues pv on pv.id = pp.PropertyValueId
		left join Catalogs.Properties prop on prop.Id = pv.PropertyId
group by a.Id
having Product like :text
limit 1")
					.SetParameter("text", text + "%")
					.SetResultTransformer(Transformers.AliasToBean<Assortment>())
					.UniqueResult<Assortment>();
			return GetPage(session, assortiment.Id);
		}
	}

}
