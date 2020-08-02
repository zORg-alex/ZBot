using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace zLib.WPF.Converters {
    public class PathToDrawingBrushConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (null == value) {
				return value;
			}
			Path p = (Path)value;
			string par = (string)parameter;
			var d = new DrawingBrush(new GeometryDrawing());
			var gd = new GeometryDrawing();
			d.Drawing = gd;
			//System.Windows.Data.BindingOperations.SetBinding(d.Drawing, GeometryDrawing.GeometryProperty, new Binding() { Path = new PropertyPath("Data"), Source = p });
			gd.Geometry = p.Data;
			//d.Viewport = new Rect(0, 0, p.Width, p.Height);
			gd.Brush = p.Fill;
			gd.Pen = new Pen() {
				Brush = p.Stroke,
				DashCap = p.StrokeDashCap,
				DashStyle = new DashStyle() {
					Dashes = p.StrokeDashArray,
					Offset = p.StrokeDashOffset
				},
				EndLineCap = p.StrokeEndLineCap,
				LineJoin = p.StrokeLineJoin,
				MiterLimit = p.StrokeMiterLimit,
				StartLineCap = p.StrokeStartLineCap,
				Thickness = p.StrokeThickness
			};
			if (par != null)
				if (par.ToLower().Contains("nofill"))
					gd.Brush = Brushes.Transparent;
			d.Stretch = Stretch.Uniform;
			return d;
		}

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			return null;
        }
    }
}
