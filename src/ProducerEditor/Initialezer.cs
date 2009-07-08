using System.Collections.Generic;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.ActiveRecord.Linq;
using NHibernate.ByteCode.Castle;

namespace ProducerEditor
{
	public class Initialezer
	{
		public static void Initialize()
		{
			var config = new InPlaceConfigurationSource();
			config.Add(typeof(ActiveRecordBase), new Dictionary<string, string>
			                                     	{
			                                     		{"dialect", "NHibernate.Dialect.MySQLDialect"},
														{"connection.driver_class", "NHibernate.Driver.MySqlDataDriver"},
														{"connection.provider", "NHibernate.Connection.DriverConnectionProvider"},
														{"connection.connection_string_name", "Main"},
														{"proxyfactory.factory_class", typeof(ProxyFactoryFactory).Name},
			                                     	});
			ActiveRecordStarter.Initialize(new[]
			                               	{
			                               		Assembly.Load("ProducerEditor"),
			                               	},
			                               config);
		}
	}

	[ActiveRecord(Table = "CatalogFirmCr", Schema = "farm")]
	public class Producer : ActiveRecordLinqBase<Producer>
	{
		[PrimaryKey(Column = "CodeFirmCr")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[HasMany(Lazy = false, OrderBy = "Name")]
		public virtual IList<ProducerSynonym> Synonyms { get; set; }
	}

	[ActiveRecord(Table = "SynonymFirmCr", Schema = "farm")]
	public class ProducerSynonym : ActiveRecordLinqBase<ProducerSynonym>
	{
		[PrimaryKey(Column = "SynonymFirmCrCode")]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo]
		public virtual Producer Producer { get; set; }
	}

	[ActiveRecord(Table = "ProducerEquivalent", Schema = "catalogs")]
	public class ProducerEquivalent
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo]
		public virtual Producer Producer { get; set; }
	}
}
