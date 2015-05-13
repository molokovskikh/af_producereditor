using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;
using log4net;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Views;

namespace ProducerEditor.Presenters
{
	public class ShowExcludesPresenter : Presenter
	{
		private Pager<ExcludeDto> _excludes;
		private string _searchText;
		private bool _showHidden;
		private bool _showPharmacie;
		private List<ProducerSynonymDto> _synonyms;
		private List<ProducerOrEquivalentDto> _producers;
		private ExcludeDto _currentExclude;

		public Pager<ExcludeDto> Excludes
		{
			get { return _excludes; }
			set
			{
				_excludes = value;
				OnUpdate("Excludes", value);
			}
		}

		public List<ProducerSynonymDto> ProducerSynonyms
		{
			get { return _synonyms; }
			set
			{
				value = SortAndMark(value);
				_synonyms = value;
				OnUpdate("ProducerSynonyms", value);
			}
		}

		public List<ProducerOrEquivalentDto> Producers
		{
			get { return _producers; }
			set
			{
				_producers = value;
				OnUpdate("ProducerOrEquivalents", value);
			}
		}

		public Pager<ExcludeDto> page
		{
			get { return Excludes; }
			set { Excludes = value; }
		}

		public void ShowHidden(bool flag)
		{
			_showHidden = flag;
			Refresh();
		}

		public void ShowPharmacie(bool flag)
		{
			_showPharmacie = flag;
			Refresh();
		}

		private List<ProducerSynonymDto> SortAndMark(List<ProducerSynonymDto> synonyms)
		{
			foreach (var synonym in synonyms) {
				if (synonym.Name.Equals(_currentExclude.ProducerSynonym, StringComparison.CurrentCultureIgnoreCase)
					&& synonym.Supplier == _currentExclude.Supplier
					&& synonym.Region == _currentExclude.Region) {
					synonym.SameAsCurrent = true;
				}
			}

			return synonyms.Where(s => s.SameAsCurrent).Concat(
				synonyms.Where(s => !s.SameAsCurrent).OrderBy(s => s.Supplier).ThenBy(s => s.Region)).ToList();
		}

		public void CurrentChanged(ExcludeDto exclude)
		{
			_currentExclude = exclude;
			Action(s => {
				var data = s.GetExcludeData(exclude.Id);
				ProducerSynonyms = data.Synonyms;
				Producers = data.Producers;
			});
		}

		public void Search(string text)
		{
			if (String.IsNullOrEmpty(text) && String.IsNullOrEmpty(_searchText))
				return;

			_searchText = text;

			Action(s => {
				var pager = s.SearchExcludes(text, _showPharmacie, _showHidden, 0, false);
				if (pager == null) {
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
			this.page = Request(s => s.SearchExcludes(_searchText, _showPharmacie, _showHidden, page, isRefresh));
			return this.page;
		}

		public void Refresh()
		{
			page = RequestExcludes(page.Page, true);
		}

		public Pager<ExcludeDto> Page(uint page)
		{
			return RequestExcludes(page, false);
		}

		public void DeleteSynonym(ExcludeDto current)
		{
			if (String.IsNullOrEmpty(current.OriginalSynonym) && current.OriginalSynonymId == 0) {
				MessageBox.Show("Для выбранного исключения не задано оригинальное наименование", "Предупреждение",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			Action(s => s.DeleteSynonym(current.OriginalSynonymId));
			Refresh();
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

		public void MistakenProducerSynonym(ProducerSynonymDto current)
		{
			Action(s => {
				s.DeleteProducerSynonym(current.Id);
				ProducerSynonyms = s.GetExcludeData(_currentExclude.Id).Synonyms;
			});
		}

		public void MistakenExclude(ExcludeDto current)
		{
			Action(s => { s.DeleteExclude(current.Id); });
			Refresh();
		}

		public void AddEquivalent(ProducerOrEquivalentDto current)
		{
			var result = MessageBox.Show(
				String.Format("Создать эквивалент '{0}' для производителя '{1}'",
					_currentExclude.ProducerSynonym.ToUpper(),
					current.Name),
				"Запрос",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Question);

			if (result != DialogResult.OK)
				return;

			Action(s => { s.CreateEquivalent(_currentExclude.Id, current.Id); });
			Refresh();
		}
	}
}