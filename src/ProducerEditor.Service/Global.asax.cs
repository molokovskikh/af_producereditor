using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Web;
using Castle.Facilities.WcfIntegration;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using log4net;
using log4net.Config;
using NHibernate;
using NHibernate.Cfg;
using Environment=NHibernate.Cfg.Environment;

namespace ProducerEditor.Service
{
	public class Global : HttpApplication
	{
		private readonly ILog _log = LogManager.GetLogger(typeof (Global));
		private IWindsorContainer container;

		protected void Application_Start(object sender, EventArgs e)
		{
			try
			{
				XmlConfigurator.Configure();
				Setup();
			}
			catch (Exception ex)
			{
				_log.Error("Не удалось инициализировать приложение", ex);
			}
		}

		private void Setup()
		{
			var debug = new ServiceDebugBehavior
			{
#if DEBUG
				IncludeExceptionDetailInFaults = true,
#else
				IncludeExceptionDetailInFaults = false,
#endif
				HttpHelpPageEnabled = false,
				HttpsHelpPageEnabled = false,
			};
			var metadata = new ServiceMetadataBehavior
			{
				HttpGetEnabled = false,
				HttpsGetEnabled = false,
			};
			var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly)
			{
				MaxBufferSize = int.MaxValue,
				MaxReceivedMessageSize = int.MaxValue,
				ReaderQuotas = {MaxArrayLength = int.MaxValue}
			};

			var factory = InitializeNHibernate();

			container = new WindsorContainer()
				.AddFacility<WcfFacility>()
				.Register(
					Component.For<ISessionFactory>().Instance(factory),

					Component.For<IServiceBehavior>().Instance(debug),
					Component.For<IServiceBehavior>().Instance(metadata),
					AllTypes.Pick()
						.FromAssembly(typeof (ProducerService).Assembly)
						.If(t => t.Name.Contains("Service"))
						.Configure(c => {
							c.Named(c.ServiceType.Name).ActAs(new DefaultServiceModel().AddEndpoints(WcfEndpoint.BoundTo(binding)).Hosted());
						})
				);

		}

		public static ISessionFactory InitializeNHibernate()
		{
			return new Configuration()
				.SetProperties(new Dictionary<string, string>
				{
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
					{Environment.ConnectionStringName, "Main"},
					{Environment.ProxyFactoryFactoryClass,"NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
					{Environment.Hbm2ddlKeyWords, "none"}
				})
				.BuildSessionFactory();
		}

		protected void Application_End(object sender, EventArgs e)
		{
			if (container != null)
				container.Dispose();
		}
	}
}