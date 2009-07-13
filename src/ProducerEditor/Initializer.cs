using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Subway.Dom;
using Subway.Dom.Styles;
using Environment=NHibernate.Cfg.Environment;

namespace ProducerEditor
{
	public class Initializer
	{
		public static void Initialize()
		{
			StylesHolder
				.Instance
				.RegisterClass("WithoutOffers")
				.Set(StyleElementType.BackgroundColor, Color.FromArgb(231, 231, 200));

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
}
