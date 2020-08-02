using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Markup;

namespace zLib.WPF {
	public class SelfBinding : MarkupExtension {

        string Path { get; }

        public SelfBinding() {

        }

        public SelfBinding(string Path) {
            this.Path = Path;
        }
        public override object ProvideValue(IServiceProvider serviceProvider) {
            try {

                if (string.IsNullOrEmpty(Path))
                    throw new ArgumentNullException("Path", "The Path can not be null");

                //Получим провайдер, с информацией об объектре и првязках
                var providerValuetarget = (IProvideValueTarget)serviceProvider
                  .GetService(typeof(IProvideValueTarget));

                //Получим объект, вызвавший привязку
                DependencyObject _targetObject = (DependencyObject)providerValuetarget.TargetObject;

                //Получим свойство для возвращения значения привязки
                PropertyInfo _sourceProperty = _targetObject.GetType().GetProperty(Path);

                //Вернем значение свойства
                return _sourceProperty.GetValue(_targetObject, null);

            } catch (Exception ex) {
                Debug.WriteLine(ex);
                return null;
            }
        }
    }
}
