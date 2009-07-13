﻿using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace ProducerEditor.Models
{
	[ActiveRecord(Table = "farm.CatalogFirmCr")]
	public class Producer : ActiveRecordLinqBase<Producer>
	{
		[PrimaryKey(Column = "CodeFirmCr")]
		public virtual uint Id { get; set; }

		[Property(Column = "FirmCr")]
		public virtual string Name { get; set; }

		[Property]
		public virtual byte Hidden { get; set; }

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

	public class SynonymView
	{
		public string Synonym { get; set; }
		public string Supplier { get; set; }
		public string Region { get; set; }
		public byte Segment { get; set; }
		public Int64 HaveOffers { get; set; }

		public string SegmentAsString()
		{
			return Segment == 0 ? "Опт" : "Розница";
		}
	}

	public class ProductAndProducer
	{
		public string Producer { get; set; }
		public string Product { get; set; }
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
