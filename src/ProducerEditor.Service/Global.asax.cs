using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Web;
using Castle.Core;
using Castle.Facilities.WcfIntegration;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Common.MySql;
using Common.Service;
using Common.Service.Interceptors;
using log4net;
using log4net.Config;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
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
				container = Setup();
			}
			catch (Exception ex)
			{
				_log.Error("Не удалось инициализировать приложение", ex);
			}
		}

		public static IWindsorContainer Setup()
		{
#if DEBUG
			ServiceContext.GetUserName = () => System.Environment.UserName;
#endif
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

			var container = new WindsorContainer()
				.AddFacility<WcfFacility>()
				.Register(
					Component.For<ISessionFactory>().Instance(factory),
					Component.For<Mailer>(),

					Component.For<IServiceBehavior>().Instance(debug),
					Component.For<IServiceBehavior>().Instance(metadata),

					Component.For<ErrorLoggingInterceptor>(),

					AllTypes
						.FromAssembly(typeof (ProducerService).Assembly)
						.Pick()
						.If(t => t.Name.Contains("Service"))
						.WithService.AllInterfaces()
						.Configure(c => {
							var conf = c.Named(c.ServiceType.Name)
								.AsWcfService(new DefaultServiceModel().AddEndpoints(WcfEndpoint.BoundTo(binding)).Hosted())
								.Interceptors(InterceptorReference.ForType<ErrorLoggingInterceptor>()).Anywhere;
						})
				);

			return container;
		}

		public static ISessionFactory InitializeNHibernate()
		{
			ConnectionHelper.DefaultConnectionStringName = "Main";
			return new Configuration()
				.SetProperties(new Dictionary<string, string>
				{
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
					{Environment.ConnectionStringName, ConnectionHelper.GetConnectionName()},
					{Environment.ProxyFactoryFactoryClass,"NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
					{Environment.Hbm2ddlKeyWords, "none"}
				})
				.AddInputStream(HbmSerializer.Default.Serialize(typeof(Supplier).Assembly))
				.BuildSessionFactory();
		}

		protected void Application_End(object sender, EventArgs e)
		{
			if (container != null)
				container.Dispose();
		}
	}
}