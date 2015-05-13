using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using ProducerEditor.Contract;
using ProducerEditor.Presenters;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
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
			Assert.That(table.ViewPort.ToString(), Is.StringContaining("test"));
		}


		[Test]
		public void Sort_observable_collection()
		{
			var table = new VirtualTable(new TemplateManager<Tuple<string>>(
				() => Row.Headers(new Header("Тест").Sortable("Item1")),
				x => Row.Cells(x.Item1)));
			table.RegisterBehavior(new SortInList());

			var list = new ObservableCollection2<Tuple<string>>(Enumerable.Range(0, 10).Select(x => Tuple.Create(x.ToString())).ToList());
			table.TemplateManager.Source = list;
			table.Behavior<SortBehavior>().SortBy("Item1");
		}
	}
}