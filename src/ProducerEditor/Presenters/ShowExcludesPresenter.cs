using System;
using System.ServiceModel;
using System.Windows.Forms;
using log4net;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using ProducerEditor.Views;

namespace ProducerEditor.Presenters
{
	public class ShowExcludesPresenter
	{
		private ILog _log = LogManager.GetLogger(typeof (ShowExcludesPresenter));
		private Pager<Exclude> _excludes;
		private string _searchText;

		public event Action<string, object> Update;

		public Pager<Exclude> Excludes
		{
			get { return _excludes; }
			set
			{
				_excludes = value;
				if (Update != null)
					Update("Excludes", value);
			}
		}

		public Pager<Exclude> page
		{
			get { return Excludes; }
			set { Excludes = value; }
		}

		public void Search(string text)
		{
			if (String.IsNullOrEmpty(text) && String.IsNullOrEmpty(_searchText))
				return;

			_searchText = text;

			Action(s => {
				var pager = s.SearchExcludes(text, 0, false);
				if (pager == null)
				{
					MessageBox.Show("По вашему запросу ничего не найдено");
					return;
				}
				page = pager;
			});
		}

		// Если флажок isRefresh равен true, тогда данные выбираются из мастера.
		// Это нужно, потому что возникали ситуации, когда из мастера запись удалили, а она снова выбралась из слейва
		// (репликация не успела)
		private Pager<Exclude> RequestExcludes(uint page, bool isRefresh)
		{
			if (String.IsNullOrEmpty(_searchText))
				this.page = Request(s => s.ShowExcludes(page, isRefresh));
			this.page = Request(s => s.SearchExcludes(_searchText, page, isRefresh));
			return this.page;
		}

		private void Refresh()
		{
			page = RequestExcludes(page.Page, true);
		}

		public Pager<Exclude> Page(uint page)
		{
			return RequestExcludes(page, false);
		}

		public void DeleteSynonym(Exclude current)
		{
			if (String.IsNullOrEmpty(current.OriginalSynonym) && current.OriginalSynonymId == 0)
			{
				MessageBox.Show("Для выбранного исключения не задано оригинальное наименование", "Предупреждение",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			Refresh();
			Action(s => s.DeleteSynonym(current.OriginalSynonymId));
		}

		public void AddToAssortment(Exclude current)
		{
			var view = new AddToAssortmentView(current, ShowProducers.producers);
			if (view.ShowDialog() == DialogResult.OK)
				Refresh();
		}

		public void DoNotShow(Exclude current)
		{
			Action(s => s.DoNotShow(current.Id));
			page.Content.Remove(current);
		}

		protected T Request<T>(Func<ProducerService, T> func)
		{
			var result = default(T);
			WithService(s => {
				result = func(s);
			});
			return result;
		}

		protected void Action(Action<ProducerService> action)
		{
			WithService(action);
		}

		protected void WithService(Action<ProducerService> action)
		{
			ICommunicationObject communicationObject = null;
			try
			{
				var chanel = FactoryHolder.Factory.CreateChannel();
				communicationObject = chanel as ICommunicationObject;
				action(chanel);
				communicationObject.Close();
			}
			catch (Exception e)
			{
				if (communicationObject != null 
					&& communicationObject.State != CommunicationState.Closed)
					communicationObject.Abort();

				_log.Error("Ошибка при обращении к серверу", e);
				throw;
			}
		}
	}
}