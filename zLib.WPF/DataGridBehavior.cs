using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace zLib.WPF {
	/// <summary>
	/// Using this behavior on a dataGRid will ensure to display only columns with "Browsable Attributes"
	/// </summary>
	public static class DataGridBehavior {
		public static readonly DependencyProperty UseBrowsableAttributeOnColumnProperty =
			DependencyProperty.RegisterAttached("UseBrowsableAttributeOnColumn",
			typeof(bool),
			typeof(DataGridBehavior),
			new UIPropertyMetadata(false, UseBrowsableAttributeOnColumnChanged));

		public static bool GetUseBrowsableAttributeOnColumn(DependencyObject obj) {
			return (bool)obj.GetValue(UseBrowsableAttributeOnColumnProperty);
		}

		public static void SetUseBrowsableAttributeOnColumn(DependencyObject obj, bool val) {
			obj.SetValue(UseBrowsableAttributeOnColumnProperty, val);
		}

		private static void UseBrowsableAttributeOnColumnChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
			var dataGrid = obj as DataGrid;
			if (dataGrid != null) {
				if ((bool)e.NewValue) {
					dataGrid.AutoGeneratingColumn += DataGridOnAutoGeneratingColumn;
				} else {
					dataGrid.AutoGeneratingColumn -= DataGridOnAutoGeneratingColumn;
				}
			}
		}

		private static void DataGridOnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) {
			var propDesc = e.PropertyDescriptor as PropertyDescriptor;

			if (propDesc != null) {
				foreach (Attribute att in propDesc.Attributes) {
					var browsableAttribute = att as BrowsableAttribute;
					if (browsableAttribute != null) {
						if (!browsableAttribute.Browsable) {
							e.Cancel = true;
						}
					}

					// As proposed by "dba" stackoverflow user on webpage: 
					// https://stackoverflow.com/questions/4000132/is-there-a-way-to-hide-a-specific-column-in-a-datagrid-when-autogeneratecolumns
					// I added few next lines:
					var displayName = att as DisplayNameAttribute;
					if (displayName != null) {
						e.Column.Header = displayName.DisplayName;
					}

					var displayFormat = att as DisplayFormatAttribute;
					if (displayFormat != null) {
						((DataGridTextColumn)e.Column).Binding = new Binding(e.PropertyName) { StringFormat = displayFormat.DataFormatString };
						//var dt = Application.Current.Resources.FindName("NormalDate") as DataTemplate;
						//Style s = new Style(typeof(DataGridCell),((DataGrid)sender).CellStyle);
						//s.Setters.Add(new Setter(DataGridCell.ContentStringFormatProperty, displayFormat.DataFormatString));
						//var t = new ControlTemplate();
						//var fef = new FrameworkElementFactory(typeof(TextBlock));
						//var b = new Binding(propDesc.Name);
						//b.StringFormat = displayFormat.DataFormatString;
						//fef.SetBinding(TextBlock.TextProperty, b);
						//t.VisualTree = fef;
						//s.Setters.Add(new Setter(DataGridCell.TemplateProperty, t));
						//e.Column.CellStyle = s;
					}
				}
			}
		}
	}

	public class BindableMultiSelectDataGrid : DataGrid {
		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.Register("SelectedItems", typeof(IList), typeof(BindableMultiSelectDataGrid), new PropertyMetadata(default(IList)));

		public new IList SelectedItems {
			get { return (IList)GetValue(SelectedItemsProperty); }
			set { throw new Exception("This property is read-only. To bind to it you must use 'Mode=OneWayToSource'."); }
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
			base.OnSelectionChanged(e);
			SetValue(SelectedItemsProperty, base.SelectedItems);
		}
	}
	public class BindableMultiSelectListBox : ListBox {
		public static new readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.Register("SelectedItems", typeof(IList), typeof(BindableMultiSelectListBox), new PropertyMetadata(default(IList)));

		public new IList SelectedItems {
			get { return (IList)GetValue(SelectedItemsProperty); }
			set { throw new Exception("This property is read-only. To bind to it you must use 'Mode=OneWayToSource'."); }
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
			base.OnSelectionChanged(e);
			SetValue(SelectedItemsProperty, base.SelectedItems);
		}
	}
}