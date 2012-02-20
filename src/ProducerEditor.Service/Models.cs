using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using ProducerEditor.Contract;
using ProducerEditor.Service.Models;

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
			Price = synonym.Price;
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

		[ManyToOne(ClassType = typeof (Price), Column = "PriceCode")]
		public virtual Price Price { get; set; }
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

		public static List<ProducerSynonymDto> Load(ISession session, Query query)
		{
			var filter = "";
			if (query.Field == "ProducerId")
				filter = "sfc.CodeFirmCr = :value";
			if (query.Field == "Name")
				filter = "sfc.Synonym = :value";
			if (filter == "")
				throw new Exception(String.Format("Не знаю как фильтровать по {0}", query.Field));
			return session.CreateSQLQuery(String.Format(@"
select sfc.Synonym as Name,
p.Name as Producer,
sfc.SynonymFirmCrCode as Id,
cd.ShortName as Supplier,
r.Region,
c.Id is not null as HaveOffers
from farm.SynonymFirmCr sfc
  join Catalogs.Producers p on p.Id = sfc.CodeFirmCr
  join usersettings.PricesData pd on sfc.PriceCode = pd.PriceCode
	join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
	  join farm.Regions r on cd.RegionCode = r.RegionCode
  left join farm.Core0 c on c.SynonymFirmCrCode = sfc.SynonymFirmCrCode
where {0} and cd.BillingCode <> 921 and cd.FirmSegment = 0
group by sfc.SynonymFirmCrCode", filter))
				.SetParameter("value", query.Value)
				.SetResultTransformer(new AliasToPropertyTransformer(typeof (ProducerSynonymDto)))
				.List<ProducerSynonymDto>().ToList();
		}

		public virtual bool Exist(ISession session)
		{
			return session
				.Query<ProducerSynonym>()
				.FirstOrDefault(s => s.Price == Price && s.Producer == Producer && s.Name == Name) != null;
		}
	}

	[Class(Table = "farm.Synonym")]
	public class Synonym
	{
		[Id(0, Name = "Id", Column = "SynonymCode")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }

		[Property(Column = "Synonym")]
		public virtual string Name { get; set; }

		[Property(Column = "Junk")]
		public virtual bool Junk { get; set; }

		[Property(Column = "ProductId")]
		public virtual uint ProductId { get; set; }

		[ManyToOne(ClassType = typeof(Price), Column = "PriceCode")]
		public virtual Price Price { get; set; }
	}

	[Class(Table = "Catalogs.ProducerEquivalents")]
	public class ProducerEquivalent
	{
		protected ProducerEquivalent()
		{}

		public ProducerEquivalent(Producer producer, string name)
		{
			Producer = producer;
			Name = name.ToUpper();
		}

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

	[Class(Table = "Catalogs.Catalog")]
	public class CatalogProduct
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }
	}
}
