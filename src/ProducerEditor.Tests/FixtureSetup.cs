using System;
using System.Configuration;
using System.IO;
using CassiniDev;
using NHibernate;
using NUnit.Framework;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	[SetUpFixture]
	public class FixtureSetup
	{
		private Server webServer;
		public static ISessionFactory sessionFactory;

		[SetUp]
		public void Setup()
		{
			sessionFactory = Global.InitializeNHibernate();
			Test.Support.Setup.Initialize();
			StartServer();
		}

		[TearDown]
		public void TearDown()
		{
			webServer.Dispose();
		}

		public void StartServer()
		{
			var port = Int32.Parse(ConfigurationManager.AppSettings["webPort"]);
			var webDir = ConfigurationManager.AppSettings["webDirectory"];

			webServer = new Server(port, "/", Path.GetFullPath(webDir));
			webServer.Start();
		}
	}
}