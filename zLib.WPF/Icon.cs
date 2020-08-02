using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace zLib.WPF {

	public class Icon : Control {

		public Icon() {
			UseLayoutRounding = true;
		}

		[Category("Appearance")]
		public Brush IconOpacityMask {
			get { return (Brush)GetValue(IconOpacityMaskProperty); }
			set { SetValue(IconOpacityMaskProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IconOpacityMask.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IconOpacityMaskProperty =
			DependencyProperty.Register("IconOpacityMask", typeof(Brush), typeof(Icon), new PropertyMetadata(null, IconOpacityMaskChanged));

		private static void IconOpacityMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Icon i = (Icon)d;
			if (i.Template != null)
			{
				((Border)i.Template.FindName("b", i)).SetValue(OpacityMaskProperty, e.NewValue as Brush);
			}
		}

		[Category("Appearance")]
		public Path IconPath {
			get { return (Path)GetValue(IconPathProperty); }
			set { SetValue(IconPathProperty, value); }
		}
		public static readonly DependencyProperty IconPathProperty =
			DependencyProperty.Register("IconPath", typeof(Path), typeof(Icon), new PropertyMetadata(null));

		[Category("Appearance")]
		public Path _AltIconPath {
			get { return (Path)GetValue(_AltIconPathProperty); }
			set { SetValue(_AltIconPathProperty, value); }
		}
		public static readonly DependencyProperty _AltIconPathProperty =
			DependencyProperty.Register("_AltIconPath", typeof(Path), typeof(Icon), new PropertyMetadata(null));

		[Category("Brush")]
		public Brush IconBrush {
			get { return (Brush)GetValue(IconBrushProperty); }
			set { SetValue(IconBrushProperty, value); }
		}
		public static readonly DependencyProperty IconBrushProperty =
			DependencyProperty.Register("IconBrush", typeof(Brush), typeof(Icon), new PropertyMetadata(null));
		public Brush _AltIconBrush {
			get { return (Brush)GetValue(_AltIconBrushProperty); }
			set { SetValue(_AltIconBrushProperty, value); }
		}
		public static readonly DependencyProperty _AltIconBrushProperty =
			DependencyProperty.Register("_AltIconBrush", typeof(Brush), typeof(Icon), new PropertyMetadata(null));

		[Category("Appearance")]
		public bool SetAltIcon {
			get { return (bool)GetValue(SetAltIconProperty); }
			set { SetValue(SetAltIconProperty, value); }
		}
		public static readonly DependencyProperty SetAltIconProperty =
			DependencyProperty.Register("SetAltIcon", typeof(bool), typeof(Icon), new PropertyMetadata(false));



		[Category("Appearance")]
		public Stretch Stretch {
			get { return (Stretch)GetValue(StretchProperty); }
			set { SetValue(StretchProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Stretch.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StretchProperty =
			DependencyProperty.Register("Stretch", typeof(Stretch), typeof(Icon), new PropertyMetadata(Stretch.None, StretchPropertyChanged));

		private static void StretchPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			Icon icon = (Icon)d;
			Stretch s = (Stretch)e.NewValue;
			switch (s) {
				case Stretch.None:
					icon.Width = icon.IconWidth;
					icon.Height = icon.IconHeight;
					break;
				case Stretch.Fill:
					icon.Width = double.NaN;
					icon.Height = double.NaN;
					break;
				case Stretch.Uniform:
					break;
				case Stretch.UniformToFill:
					break;
				default:
					break;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public double IconWidth {
			get { return (double)GetValue(IconWidthProperty); }
			set { SetValue(IconWidthProperty, value); }
		}
		public static readonly DependencyProperty IconWidthProperty =
			DependencyProperty.Register("IconWidth", typeof(double), typeof(Icon), new PropertyMetadata(double.NaN));



		[EditorBrowsable(EditorBrowsableState.Never)]
		public double IconHeight {
			get { return (double)GetValue(IconHeightProperty); }
			set { SetValue(IconHeightProperty, value); }
		}
		public static readonly DependencyProperty IconHeightProperty =
			DependencyProperty.Register("IconHeight", typeof(double), typeof(Icon), new PropertyMetadata(double.NaN));




		public bool Spin {
			get { return (bool)GetValue(SpinProperty); }
			set { SetValue(SpinProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Spin.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SpinProperty =
			DependencyProperty.Register("Spin", typeof(bool), typeof(Icon), new PropertyMetadata(false));


	}
}
