using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zLib {
	public class ExceptionContainer {
        //Test data for Designer
		public ExceptionContainer() {
//#if TEST
            PropertyList = new Dictionary<string, object>();
			PropertyList.Add("Property1", "Value");
			PropertyList.Add("Property2", "Value");
			PropertyList.Add("Property3", "Value");
			PropertyList.Add("Property4", "Value");
			InnerException = new ExceptionContainer(null);
			InnerException.PropertyList.Add("InnerException Property1", "Value");
			InnerException.PropertyList.Add("InnerException Property2", "Value");
			InnerException.PropertyList.Add("InnerException Property3", "Value");
			InnerException.PropertyList.Add("InnerException Property4", "Value");
//#endif
		}
		public ExceptionContainer(Exception e) {
			if (e == null) return;
			Exception = e;
			PropertyList = Exception.GetType().GetProperties()
				.ToDictionary(
					p=>p.Name,
					p=>Exception.GetType().GetProperty(p.Name)?.GetValue(Exception, null)
				);
			InnerException = new ExceptionContainer(Exception.InnerException);
		}

		public Exception Exception { get; private set; }
		public ExceptionContainer InnerException { get; private set; }

		public Dictionary<string, object> PropertyList { get; private set; }
	}
}
