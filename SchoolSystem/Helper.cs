using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace BakalariAPI
{
	static class Helper
	{
		public static DateTime ToMonday(DateTime somedate)
		{
			int minusdays;
			switch (somedate.DayOfWeek)
			{
				case DayOfWeek.Monday: minusdays = 0;
					break;
				case DayOfWeek.Tuesday: minusdays = 1;
					break;
				case DayOfWeek.Wednesday: minusdays = 2;
					break;
				case DayOfWeek.Thursday: minusdays = 3;
					break;
				case DayOfWeek.Friday: minusdays = 4;
					break;
				case DayOfWeek.Saturday: minusdays = 5;
					break;
				case DayOfWeek.Sunday: minusdays = 6;
					break;
				default: minusdays = 0;
					break;
			}
			return somedate.AddDays((-1.0) * minusdays);
		}

		public static string RemoveStartingWhitespaces(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return "";
			if (input[0] != ' ')
				return input;

			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] != ' ')
				{
					return input.Remove(0, i);
				}
			}

			return input;
		}

		public static string RemoveEndingWhitespaces(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return "";
			if (input[input.Length - 1] != ' ')
				return input;

			for (int i = input.Length - 1; i >= 0; i--)
			{
				if (input[i] != ' ')
				{
					return input.Remove(i + 1, input.Length - i-1);
				}
			}
			return input;
		}

		public static string Decode(string input)
		{
			return HttpUtility.HtmlDecode(input);
		}

		public static string RemoveLineBreaks(string input)
		{
			return input.Replace("\r\n", "\n").Replace("\n", "");
		}

		public static long ToUnixtime(DateTime date)
		{
			TimeSpan input = date - (new DateTime(1970, 1, 1, 0, 0, 0, 0));
			return Int64.Parse(input.TotalSeconds.ToString(CultureInfo.InvariantCulture));
		}
		public static DateTime FromUnixTime(long unixtime)
		{
			return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(unixtime).ToLocalTime();
		}

		public static string VisualizeCollection(NameValueCollection input)
		{
			string ret = "";

			foreach (string k in input)
			{
				string v = input[k];
				ret += k + ":" + v+"\n";
			}

			return ret;	
		}
	}
}
