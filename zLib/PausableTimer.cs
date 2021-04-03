using System;
using System.Timers;

namespace zLib {
	/// <summary>
	/// System.Timers.Timer modified to allow pausing.
	/// It saves start and pause time to release for reminder and correct back to original time
	/// </summary>
	public class PausableTimer : Timer {
		public PausableTimer(double interval) : base(interval) {
			Interval = interval;
			base.Elapsed += PausableTimer_Elapsed;
		}

		public PausableTimer() : base() {
			base.Elapsed += PausableTimer_Elapsed;
		}

		private void PausableTimer_Elapsed(object sender, ElapsedEventArgs e) {
			ElapsedEventHandler temp = Elapsed;
			if (temp != null) {
				temp(sender, e);
			}
		}

		private double interval;
		public new double Interval {
			get { return interval; } 
			set { interval = value; if (!wasPaused) base.Interval = interval; }
		}

		public new event ElapsedEventHandler Elapsed;

		private bool wasPaused;
		private DateTime startedAt;
		private DateTime pausedAt;

		public new void Start() {
			startedAt = DateTime.Now;
			base.Start();
		}

		public void Pause() {
			wasPaused = true;
			pausedAt = DateTime.Now;
			base.Stop();
		}

		public void Release() {
			//Set temporal interval
			base.Interval = Interval - ((pausedAt - startedAt).TotalMilliseconds % Interval);
			base.Elapsed += PausableTimer_ElapsedAfterPause;
			base.Start();
		}

		private void PausableTimer_ElapsedAfterPause(object sender, ElapsedEventArgs e) {
			base.Interval = Interval;
			base.Elapsed -= PausableTimer_ElapsedAfterPause;
			wasPaused = false;
		}
	}
}
