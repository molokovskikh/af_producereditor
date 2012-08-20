using System.Linq;
using NUnit.Framework;
using ProducerEditor.Infrastructure;
using ProducerEditor.Presenters;
using ProducerEditor.Views;
using Subway.VirtualTable;

namespace ProducerEditor.Tests.View
{
	[TestFixture]
	public class ShowProducersFixture
	{
		private ShowProducers view;
		private ShowProducersPresenter presenter;

		[SetUp]
		public void Setup()
		{
			view = new ShowProducers();
			presenter = (ShowProducersPresenter)view.Presenter;
		}

		[Test]
		public void Update_producer_on_search()
		{
			presenter.Search("фарм");
			Assert.That(presenter.Producers.Count, Is.Not.EqualTo(0));
			Assert.That(presenter.Producers.Count, Is.LessThan(ShowProducers.producers.Count));
			var host = view.Children().OfType<TableHost>().First(h => h.Name == "Producers");
			Assert.That(host.Table.TemplateManager.Source, Is.EqualTo(presenter.Producers));
		}
	}
}