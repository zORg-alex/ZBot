using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace zLib {
	public class Notifiable : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		[DebuggerStepThrough]
		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
			// take a copy to prevent thread issues
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) {
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression) {
			// take a copy to prevent thread issues
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null && propertyExpression != null) {
				if (propertyExpression.Body is MemberExpression mex) {
					handler(this, new PropertyChangedEventArgs(mex.Member.Name));
				} else if (propertyExpression.Body is UnaryExpression uex) {
					throw new NotImplementedException();
				}
			}
			throw new NotImplementedException();
		}
	}
}
