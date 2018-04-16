using System;

namespace TreeRouter.WebSocket
{

	public interface IClock
	{
		DateTime Now { get; }
		DateTime UtcNow { get; }
		DateTime Today { get; }
		DateTime UtcToday { get;  }
	}
	
	public class RealClock : IClock
	{
		public DateTime Now => DateTime.Now;
		public DateTime UtcNow => DateTime.UtcNow;
		public DateTime Today => DateTime.Today;
		public DateTime UtcToday => UtcNow.Date;
	}

	public class FakeClock : IClock
	{
		private TimeSpan _timeSpan;
		private DateTime? _freeze;

		public FakeClock(DateTime? baseTime = null, bool freeze = false)
		{
			if (freeze)
				Freeze();
			SetTime(baseTime);	
		}

		public void SetTime(DateTime? baseTime = null)
		{
			if (_freeze != null)
				_freeze = baseTime ?? DateTime.Now;
			else
				_timeSpan = baseTime == null ? TimeSpan.Zero : DateTime.Now.Subtract(baseTime.Value);
		}

		public void Freeze()
		{
			if (_freeze != null) return;
			_timeSpan = TimeSpan.Zero;
			_freeze = Now;
		}

		public void Unfreeze()
		{
			if (_freeze == null) return;
			_timeSpan = DateTime.Now.Subtract(_freeze.Value);
			_freeze = null;
		}

		public void AddTicks(int ticks) => AddTime(new TimeSpan(ticks));
		public void AddSeconds(int seconds) => AddTime(new TimeSpan(0, 0, seconds));
		public void AddMinutes(int minutes) => AddTime(new TimeSpan(0, minutes, 0));
		public void AddHours(int hours) => AddTime(new TimeSpan(hours, 0, 0));
		public void AddDays(int days) => AddTime(new TimeSpan(days, 0, 0, 0));
		public void AddTime(TimeSpan ts) => _timeSpan = _timeSpan.Add(ts);

		public DateTime Now => _freeze?.Add(_timeSpan) ?? DateTime.Now.Add(_timeSpan);
		public DateTime UtcNow => _freeze?.Add(_timeSpan).ToUniversalTime() ?? DateTime.UtcNow.Add(_timeSpan);
		public DateTime Today => Now.Date;
		public DateTime UtcToday => UtcNow.Date;
	}
}