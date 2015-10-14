using Newtonsoft.Json;
using System.Collections.Generic;

namespace BakalariAPI
{
	/// <summary>
	/// Main data structure in Bakalari. Contains almost all importatnt data.
	/// </summary>
	public class User
	{
		/// <summary>
		/// Credentials for Bakalari account
		/// </summary>
		public AccountInfo Credetnials { get; set; }

		/// <summary>
		/// Real name, got from grades webiste
		/// </summary>
		public string Name { get; set; }
		public List<Subject> Subjects { get; set; }
		public List<SheduleException> SheduleExceptions { get; set; }

		public User()
		{
			Subjects = new List<Subject>();
			SheduleExceptions = new List<SheduleException>();
		}

		public string ToJson()
		{
				return
				JsonConvert.SerializeObject(this, Formatting.Indented,
					new JsonSerializerSettings
					{
						ReferenceLoopHandling = ReferenceLoopHandling.Ignore
					});
		}
	}
}
