using System;
using System.Configuration;
using System.IO;
using CassiniDev;
using NUnit.Framework;

namespace ProducerEditor.Tests
{
	[SetUpFixture]
	public class FixtureSetup
	{
		private Server webServer;

		[SetUp]
		public void Setup()
		{
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