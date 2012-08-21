using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using ProducerEditor.Contract;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Tests.Subway
{
	[TestFixture]
	public class ObservableFixture
	{
		[Test]
		public void Rebuild_view_port_on_change()
		{
			var table = new VirtualTable(new TemplateManager<string>(
				() => Row.Headers("Тест"),
				value => Row.Cells(value)));

			var list = new ObservableCollection<string>();
			table.TemplateManager.Source = list;
			list.Add("test");
			Console.WriteLine(table.ViewPort.ToString());
			Assert.That(table.ViewPort.ToString(), Is.StringContaining("test"));
		}
	}
}