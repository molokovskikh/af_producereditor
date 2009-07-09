using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.ActiveRecord.Linq;
using Subway.Dom;
using Subway.Dom.Styles;
using Environment=NHibernate.Cfg.Environment;

namespace ProducerEditor
{
	public class Initialezer
	{
		public static void Initialize()
		{
			StylesHolder
				.Instance
				.RegisterClass("SelectedCell")
				.Set(StyleElementType.BackgroundColor, Color.FromArgb(215, 240, 255));

			StylesHolder
				.Instance
				.RegisterClass("UnFocusedSelectedCell")
				.Set(StyleElementType.BackgroundColor, Color.FromArgb(218, 218, 218));

			StylesHolder
				.Instance
				.RegisterStyleForDomElement<Row>()
				.Set(StyleElementType.BackgroundColor, Color.White);

			StylesHolder
				.Instance
				.RegisterStyleForDomElement<Cell>()
				.SetInherit(StyleElementType.BackgroundColor);

			StylesHolder
				.Instance
				.RegisterClass("SynonymsWithoutOffers")
				.Set(StyleElementType.BackgroundColor, Color.FromArgb(200, 200, 200));

			var config = new InPlaceConfigurationSource();
			config.Add(typeof (ActiveRecordBase),
			           new Dictionary<string, string>
			           	{
			           		{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
			           		{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
			           		{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
			           		{Environment.ConnectionStringName, "Slave"},
			           		{Environment.ProxyFactoryFactoryClass,"NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
			           		{Environment.Hbm2ddlKeyWords, "none"}
			           	});
			ActiveRecordStarter.Initialize(new[] {Assembly.Load("ProducerEditor")},
			                               config);
		}
	}

	[ActiveRecord(Table = "farm.CatalogFirmCr")]
	public class Producer : ActiveRecordLinqBase<Producer>
	{
		[PrimaryKey(Column = "CodeFirmCr")]
		public virtual uint Id { get; set; }

		[Property(Column = "FirmCr")]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Hidden { get; set; }
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
