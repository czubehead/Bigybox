using System;

namespace BakalariAPI
{
	/// <summary>
	/// Represents a school grade.
	/// GradesCollection is highly recommented for storing. <seealso cref="SchoolSystem.GradesCollection"/>
	/// </summary>
	public class Grade:SubjectChild
	{
		public int Id { get; set; }

		/// <summary>
		/// How many times is the value counted in average grade logic.
		/// </summary>
		public int Weight { get; set; }
		/// <summary>
		/// Value of grade. If grade is "N", becomes 0.
		/// </summary>
		public double Value { get; set; }
		/// <summary>
		/// Main descreption. Teachers fill in shits mostly hovever.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Detailed descreption of grade. This is usually what user looks for.
		/// </summary>
		public string Detail { get; set; }

		/// <summary>
		/// when the user got this grade
		/// </summary>
		public DateTime Time { get; set; }
	}
}
