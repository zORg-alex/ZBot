using System.Windows.Media;

namespace zLib.WPF {
	/// <summary>
	/// Use for color transformation
	/// </summary>
	public struct BetterColor {
		int r;
		int g;
		int b;
		public BetterColor(Color c) {
			r = c.R;
			g = c.G;
			b = c.B;
		}
		public BetterColor(int R, int G, int B) {
			r = R;
			g = G;
			b = B;
		}
		public Color GetColor() {
			return Color.FromRgb((byte)r, (byte)g, (byte)b);
		}
		public static implicit operator Color(BetterColor d)  // implicit digit to byte conversion operator
		{
			return d.GetColor();
		}
		public static BetterColor operator +(BetterColor a, BetterColor b) {
			return new BetterColor(a.r + b.r, a.g + b.g, a.b + b.b);
		}
		public static BetterColor operator -(BetterColor a, BetterColor b) {
			return new BetterColor(a.r - b.r, a.g - b.g, a.b - b.b);
		}
		public static BetterColor operator *(BetterColor a, int m) {
			return new BetterColor(a.r * m, a.g * m, a.b * m);
		}
		/// <summary>
		/// Linear interpolation between two colors
		/// </summary>
		/// <param name="a">First Color</param>
		/// <param name="b">Second Color</param>
		/// <param name="progress">Interpolation progress</param>
		/// <returns></returns>
		public static BetterColor Lerp(BetterColor a, BetterColor b, float progress) {
			return new BetterColor(
				(int)(a.r + (b.r - a.r) * progress),
				(int)(a.g + (b.g - a.g) * progress),
				(int)(a.b + (b.b - a.b) * progress)
				);
		}
		/// <summary>
		/// Linear interpolation between two colors
		/// </summary>
		/// <param name="b">Second Color</param>
		/// <param name="progress">Interpolation progress</param>
		/// <returns></returns>
		public BetterColor Lerp(BetterColor b, float progress) {
			return Lerp(this, b, progress);
		}
	}
}
