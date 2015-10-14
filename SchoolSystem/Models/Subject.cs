namespace BakalariAPI
{
	/// <summary>
	/// Represents school subject.
	/// </summary>
	public class Subject
	{
		public int Id { get; set; }

		public SubjectChildren<Grade> Grades { get; set; }
		public SubjectChildren<SheduleTime> When { get; set; }
		public SubjectChildren<Absence> Absences { get; set; }

		public SubjectInfo Info { get; set; }

		public int HoursPerSemester { get; set; }

		/// <summary>
		/// Initializes new Subject. Typicaly used by serializer/ADO.NET
		/// </summary>
		public Subject()
		{
			Grades = new SubjectChildren<Grade>(this);
			Absences = new SubjectChildren<Absence>(this);
			When = new SubjectChildren<SheduleTime>(this);
		}

		/// <summary>
		/// Initializes usually completely new subject with initial sheduleTimes
		/// </summary>
		/// <param name="initialSheduleTime">SheduleTimes to add</param>
		public Subject(params SheduleTime[] sheduleTimes):this()
		{
			When.Add(sheduleTimes);
		}
	}

	/// <summary>
	/// information about name and teacher.
	/// </summary>
	public class SubjectInfo
	{
		public string LongName { get; set; }
		public string ShortName { get; set; }
		public string LongTeacher { get; set; }
		public string ShortTeacher { get; set; }
	}
}
