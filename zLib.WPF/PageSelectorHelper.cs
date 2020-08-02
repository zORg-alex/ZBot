using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zLib;

namespace zLib.WPF {
	public class PageSelectorHelper : Notifiable {
		private int pageNr;
		public int VisualPageNr { get { return pageNr + 1; } set { PageNr = value - 1; } }
		public int PageNr { get { return pageNr; } set { pageNr = Math.Min(Math.Max(value, 0), totalPages - 1); PageSet(); RaisePropertyChanged("VisualPageNr"); } }

		public UVMCommand NextPage { get; set; }
		public UVMCommand PrevPage { get; set; }
		public UVMCommand LastPage { get; set; }
		public UVMCommand FirstPage { get; set; }
		public Action PageSet { get; set; }

		private int totalPages;
		public int TotalPages { get { return totalPages; } set { totalPages = value; RaisePropertyChanged("TotalPages"); } }

		public void SetPageOutside(int Page) {
			pageNr = Page;
			RaisePropertyChanged("VisualPageNr");
		}

		public void TotalPagesChanged(int Total, int Current) {
			TotalPages = Total;
			SetPageOutside(Current);
		}

		public Action<int> OnPageChanged = i => { };
	}
}
