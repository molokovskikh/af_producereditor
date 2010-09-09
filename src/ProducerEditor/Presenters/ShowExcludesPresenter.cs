using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Forms;
using log4net;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using ProducerEditor.Views;

namespace ProducerEditor.Presenters
{
	public class ShowExcludesPresenter
	{
		private ILog _log = LogManager.GetLogger(typeof (ShowExcludesPresenter));
		private Pager<ExcludeDto> _excludes;
		private string _searchText;
		private List<ProducerSynonymDto> _synonyms;
		private List<ProducerOrEquivalentDto> _producers;

		public event Action<string, object> Update;

		public Pager<ExcludeDto> Excludes
		{
			get { return _excludes; }
			set
			{
				_excludes = value;
				if (Update != null)
					Update("Excludes", value);
			}
		}

		public List<ProducerSynonymDto> Synonyms
		{
			get { return _synonyms; }
			set
			{
				_synonyms = value;
				if (Update != null)
					Update("Synonyms", value);
			}
		}

		public List<ProducerOrEquivalentDto> Producers
		{
			get { return _producers; }
			set
			{
				_producers = value;
				if (Update != null)
					Update("Producers", value);
			}
		}

		public Pager<ExcludeDto> page
		{
			get { return Excludes; }
			set { Excludes = value; }
		}

		public void CurrentChanged(ExcludeDto exclude)
		{
			Action(s => {
				var data = s.GetExcludeData(exclude.Id);
				Synonyms = data.Synonyms;
				Producers = data.Producers;
			});
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
		private Pager<ExcludeDto> RequestExcludes(uint page, bool isRefresh)
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

		public Pager<ExcludeDto> Page(uint page)
		{
			return RequestExcludes(page, false);
		}

		public void DeleteSynonym(ExcludeDto current)
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

		public void AddToAssortment(ExcludeDto current)
		{
			var view = new AddToAssortmentView(current, ShowProducers.producers);
			if (view.ShowDialog() == DialogResult.OK)
				Refresh();
		}

		public void DoNotShow(ExcludeDto current)
		{
			Action(s => s.DoNotShow(current.Id));
			Refresh();
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