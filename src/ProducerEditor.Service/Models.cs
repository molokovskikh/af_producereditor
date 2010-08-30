using System;
using System.Runtime.Serialization;
using NHibernate.Mapping.Attributes;

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

	[DataContract(Name = "ProducerSynonym", Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class ProducerSynonymDto
	{
		[DataMember]
		public virtual uint Id { get; set; }
		[DataMember]
		public virtual string Name { get; set; }
		[DataMember]
		public string Supplier { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public Int64 HaveOffers { get; set; }
	}


	[Class(Table = "Catalogs.Assortment")]
	public class ProductAssortment
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		public virtual uint Id { get; set; }
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
