using System;


namespace BakalariAPI
{
	/// <summary>
	/// Represents one cell on shedule, it isn't bound to any kind of certain time.
	/// </summary>
	public class SheduleCell
	{
		public int Id { get; set; }

		public string Room { get; set; }
		public SubjectInfo Info { get; set; }
		
		public ShedulePosition ShedulePosition { get; set; }
	}
	
	/// <summary>
	/// represents a certain cell on shedule board, is always part of existing subject
	/// Bound to week type and a subject
	/// </summary>
	public class SheduleTime:SubjectChild
	{
		public int Id { get; set; }

		public string Room { get; set; }
		public SubjectInfo Info { get; set; }

		public ShedulePosition ShedulePosition { get; set; }

		public enum EEvenOdd { Even, Odd, Standard };
		public EEvenOdd Week { get; set; }
	}

	/// <summary>
	/// Represents a certain change in shedule.
	/// If AllDay is true, aditional info contains the label.
	/// </summary>
	public class SheduleException:SheduleCell
	{
		public string AditionalInfo { get; set; }

		public bool AllDay { get; set; }
		public DateTime Time { get; set; }
	}

	/// <summary>
	/// coordinates on shedule board
	/// </summary>
	public class ShedulePosition
	{

		
		public int Id { get; set; }

		public int Hour { get; set; }
		public int Day { get; set; }

		public ShedulePosition(int day, int hour)
		{
			Hour = hour;
			Day = day;
		}

		public ShedulePosition() { }

		public static ShedulePosition FromDatetime(DateTime input)
		{
			ShedulePosition res = new ShedulePosition();

			int days;
			switch (input.DayOfWeek)
			{
				case DayOfWeek.Monday: days = 0;
					break;
				case DayOfWeek.Tuesday: days = 1;
					break;
				case DayOfWeek.Wednesday: days = 2;
					break;
				case DayOfWeek.Thursday: days = 3;
					break;
				case DayOfWeek.Friday: days = 4;
					break;
				case DayOfWeek.Saturday: days = 5;
					break;
				case DayOfWeek.Sunday: days = 6;
					break;
				default: days = 0;
					break;
			}

			int hour = 0;
			DateTime dayZeroHour = new DateTime(input.Year, input.Month, input.Day, 7, 5, 0);

			for (int i = 0; i < 15; i++)
			{
				int plusMinutes = i * 55;
				if (i >= 2)
					plusMinutes += 10;

				if (input >= dayZeroHour.AddMinutes(plusMinutes) && input < dayZeroHour.AddMinutes(plusMinutes + 45))
				{
					hour = i;
				}
				if (input.Hour == 8 && input.Minute == 55)
				{
					hour = 2;
					break;
				}
			}

			res.Hour = hour;
			res.Day = days;
			return res;
		}

		/// <summary>
		/// Converts given ShedulePosition to datetime
		/// </summary>
		/// <param name="someDayOfWeek">Any day of desired week.</param>
		/// <returns></returns>
		public static DateTime ToDateTime(ShedulePosition input,DateTime someDayOfWeek)
		{
			DateTime monday = Helper.ToMonday(someDayOfWeek).AddDays(input.Day);

			monday = monday.AddHours(-1.0 * monday.Hour);
			monday = monday.AddMinutes(-1.0 * monday.Minute);

			if (input.Hour != 0)
			{
				int plusminutes = 0;
				monday = monday.AddMinutes((60 * 8));
				for (int i = 0; i < input.Hour; i++)
				{
					if (i > 0)
						plusminutes += 55;
					if (i == 2)
						plusminutes += 10;
				}
				monday = monday.AddMinutes(plusminutes);
			}
			else
			{
				monday = monday.AddMinutes(7 * 60.0 + 5);
			}

			return monday;
		}

		public override string ToString()
		{
			return "day:" + Day + "\n" + Hour;
		}
	}
}
