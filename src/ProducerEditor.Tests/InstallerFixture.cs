using System.IO;
using Common.Tools;
using Installer;
using NUnit.Framework;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class InstallerFixture
	{
		private Installer.Installer installer;

		[SetUp]
		public void Setup()
		{
			var files = Directory.GetFiles(".", "*.lnk");
			files.Each(File.Delete);

			installer = new Installer.Installer();
			installer.KnownFolderDesktop = Path.GetFullPath(".");
			installer.KnownFolderPrograms = Path.GetFullPath(".");
			installer.BuildShortcuts();
		}

		[Test]
		public void Create_shortcut()
		{
			installer.CreateShortcuts();

			Assert.That(Directory.GetFiles(".", "*.lnk").Length, Is.GreaterThan(0));
		}

		[Test]
		public void Delete_shortcut()
		{
			installer.CreateShortcuts();
			installer.DeleteShortcuts();
			Assert.That(Directory.GetFiles(".", "*.lnk"), Is.Empty);
		}
	}
}