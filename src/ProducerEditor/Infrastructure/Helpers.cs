using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Scopes;
using Common.Tools;
using MySql.Data.MySqlClient;
using NHibernate;
using ProducerEditor.Models;
using Subway.Helpers;
using Subway.VirtualTable;

namespace ProducerEditor.Views
{
	public class Dialog : Form
	{
		protected TableLayoutPanel table;

		public Dialog()
		{
			AcceptButton = new Button
			{
				DialogResult = DialogResult.OK,
				Text = "Сохранить",
				AutoSize = true,
			};
			CancelButton = new Button
			{
				DialogResult = DialogResult.Cancel,
				Text = "Отмена",
				AutoSize = true,
			};
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			MinimizeBox = false;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent;
			var flow = new FlowLayoutPanel
			{
				AutoSize = true,
				Dock = DockStyle.Bottom,
				FlowDirection = FlowDirection.RightToLeft
			};
			flow.Controls.Add((Control)AcceptButton);
			flow.Controls.Add((Control)CancelButton);
			table = new TableLayoutPanel
			{
				//AutoSize = true,
				RowCount = 1,
				ColumnCount = 1,
				Dock = DockStyle.Fill
			};
			table.RowStyles.Add(new RowStyle());
			table.ColumnStyles.Add(new ColumnStyle());

			Controls.Add(table);
			Controls.Add(flow);
			AutoSize = true;
			Height = 80;
			//AutoSizeMode = AutoSizeMode.GrowAndShrink;
		}
	}

	public class InputLanguageHelper
	{
		public static void SetToRussian()
		{
			TryToSetKeyboardLayout(CultureInfo.GetCultureInfo("ru-RU"));
		}

		public static void SetToEnglish()
		{
			TryToSetKeyboardLayout(CultureInfo.GetCultureInfo("en-US"));
		}

		private static void TryToSetKeyboardLayout(CultureInfo culture)
		{
			if (Application.CurrentInputLanguage.Culture.Equals(culture))
				return;

			InputLanguage russianInputLanguage = null;
			foreach (InputLanguage inputLanguage in InputLanguage.InstalledInputLanguages)
			{
				if (inputLanguage.Culture.Equals(culture))
				{
					russianInputLanguage = inputLanguage;
					break;
				}
			}

			if (russianInputLanguage != null)
				Application.CurrentInputLanguage = russianInputLanguage;
		}
	}

	public static class Extentions
	{
		public static ToolStrip Edit(this ToolStrip toolStrip, string name)
		{
			var edit = new ToolStripTextBox
			{
				Name = name
			};
			toolStrip.Items.Add(edit);
			return toolStrip;
		}

		public static ToolStrip Button(this ToolStrip toolStrip, string label, Action onclick)
		{
			var button = new ToolStripButton
			{
				Text = label
			};
			button.Click += (sender, args) => onclick();
			toolStrip.Items.Add(button);
			return toolStrip;
		}

		public static ToolStrip Button(this ToolStrip toolStrip, string name, string label, Action onclick)
		{
			var button = new ToolStripButton
			{
				Text = label,
				Name = name
			};
			button.Click += (sender, args) => onclick();
			toolStrip.Items.Add(button);
			return toolStrip;
		}

		public static ToolStrip Button(this ToolStrip toolStrip, string name, string label)
		{
			var button = new ToolStripButton
			{
				Text = label,
				Name = name
			};
			toolStrip.Items.Add(button);
			return toolStrip;
		}

		public static ToolStrip Host(this ToolStrip toolStrip, Control control)
		{
			var host = new ToolStripControlHost(control);
			toolStrip.Items.Add(host);
			return toolStrip;
		}

		public static ToolStrip Label(this ToolStrip toolStrip, string label)
		{
			toolStrip.Items.Add(new ToolStripLabel
			{
				Text = label
			});
			return toolStrip;
		}

		public static ToolStrip Label(this ToolStrip toolStrip, string name, string label)
		{
			toolStrip.Items.Add(new ToolStripLabel
			{
				Text = label,
				Name = name,
			});
			return toolStrip;
		}

		public static ToolStrip Separator(this ToolStrip toolStrip)
		{
			toolStrip.Items.Add(new ToolStripSeparator());
			return toolStrip;
		}
	}

	public class With
	{
		public static void Master(Action action)
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Master"].ConnectionString))
			{
				connection.Open();
				using(new DifferentDatabaseScope(connection))
				{
					SetupParametersForTriggerLogging(Environment.UserName, Environment.MachineName);
					action();
				}
			}
		}

		public static T Session<T>(Func<ISession, T> action)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try
			{
				return action(session);
			}
			finally
			{
				sessionHolder.ReleaseSession(session);
			}
		}

		public static void Session(Action<ISession> action)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try
			{
				action(session);
			}
			finally
			{
				sessionHolder.ReleaseSession(session);
			}
		}

		private static void SetupParametersForTriggerLogging(object parameters, ISession session)
		{
			using (var command = session.Connection.CreateCommand())
			{
				foreach (var property in parameters.GetType().GetProperties(BindingFlags.GetProperty
																					 | BindingFlags.Public
																					 | BindingFlags.Instance))
				{
					var value = property.GetValue(parameters, null);
					command.CommandText += String.Format(" SET @{0} = ?{0}; ", property.Name);
					var parameter = command.CreateParameter();
					parameter.Value = value;
					parameter.ParameterName = "?" + property.Name;
					command.Parameters.Add(parameter);
				}
				if (command.Parameters.Count == 0)
					return;

				command.ExecuteNonQuery();
			}
		}

		public static void SetupParametersForTriggerLogging(string user, string host)
		{
			Session(session => SetupParametersForTriggerLogging(new { InUser = user, InHost = host }, session));
		}

		public static void SetupParametersForTriggerLogging(object parameters)
		{
			Session(session => SetupParametersForTriggerLogging(parameters, session));
		}
	}

	public static class NavigatorExtention
	{
		public static ToolStrip ActAsNavigator(this ToolStrip toolStrip)
		{
			toolStrip.Items
				.OfType<ToolStripButton>()
				.Each(b => b.Click += MaintainNavigation);
			return toolStrip;
		}

		private static void MaintainNavigation(object sender, EventArgs e)
		{
			var button = (ToolStripButton) sender;
			if (!button.Checked)
				button.Checked = true;

			button.Owner.Items.OfType<ToolStripButton>()
				.Where(b => b != button && b.Checked)
				.Each(b => b.Checked = false);
		}
	}

	public static class PaginatorExtention
	{
		public static ToolStrip ActAsPaginator<T>(this ToolStrip toolStrip, Pager<T> pager, Func<uint, Pager<T>> page)
		{
			Action<ToolStripButton> move = b => {
				var pageIndex = Convert.ToInt32(b.Tag);
				if (pageIndex < 0)
					return;

				pager = page((uint) pageIndex);
				toolStrip.UpdatePaginator(pager);
			};
			toolStrip.Items["Prev"].Click += (s, a) => move((ToolStripButton)s);
			toolStrip.Items["Next"].Click += (s, a) => move((ToolStripButton)s);


			toolStrip.UpdatePaginator(pager);

			var form = toolStrip.Parent;
			if (form == null)
				throw new Exception("У paginatora нет родителя, всего скорее ты нужно добавлять поведение позже");
			var table = form.Controls.OfType<TableHost>().First();

			table.InputMap()
				.KeyDown(Keys.Left, () => move((ToolStripButton) toolStrip.Items["Prev"]))
				.KeyDown(Keys.Right, () => move((ToolStripButton) toolStrip.Items["Next"]));
			return toolStrip;
		}

		public static void UpdatePaginator<T>(this ToolStrip toolStrip, Pager<T> pager)
		{
			toolStrip.Items["PageLabel"].Text = String.Format("Страница {0} из {1}", pager.Page, pager.TotalPages);

			var next = toolStrip.Items["Next"];
			next.Enabled = pager.Page < pager.TotalPages;
			if (next.Enabled)
				next.Tag = pager.Page + 1;
			else
				next.Tag = -1;

			var prev = toolStrip.Items["Prev"];
			prev.Enabled = pager.Page > 0;
			if (prev.Enabled)
				prev.Tag = pager.Page - 1;
			else
				prev.Tag = -1;
		}
	}
}
