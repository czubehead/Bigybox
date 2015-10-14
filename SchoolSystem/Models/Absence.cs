using System;

namespace BakalariAPI
{
	public class Absence:SubjectChild
	{
		public int Id { get; set; }

		public DateTime Time { get; set; }
		
		public enum EType { 
			Execused, 
			YetUnexecused, 
			Unexecused, 
			Uncountable, 
			Late, 
			Early };

		public EType Type { get; set; }
	}
}
