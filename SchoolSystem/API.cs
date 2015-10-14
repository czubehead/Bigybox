using CsQuery;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;

namespace BakalariAPI
{
	/// <summary>
	/// Main Class for this API. Consider using it in using block
	/// </summary>
	public class API:IDisposable
	{
		private bool isDisposed = false;

		public string GradesUrl { get; set; }
		private string gradesUrlWthProtocol
		{
			get
			{
				if (GradesUrl.Contains("//"))
					return GradesUrl.Split(new string[] { "//" }, StringSplitOptions.None)[1];
				return GradesUrl;
			}
		}
		public AccountInfo Credentials { get; set; }
		public List<Subject> Subjects { get; set; }
		public bool IsBusy { get; set; }

		/// <summary>
		/// one day from every month in current semester in UNIX
		/// </summary>
		readonly List<DateTime> _absenceMonths = new List<DateTime>();

		readonly CookieAwareWebClient _client = new CookieAwareWebClient();

		/// <summary>
		/// Gets the ASP:NET_SessionId cookie for used connection
		/// </summary>
		public string SessionId
		{
			get
			{
				try
				{
					return _client.CookieContainer.GetCookies(new Uri(GradesUrl))["ASP.NET_SessionId"].Value;
				}
				catch
				{
					return null;
				}
			}
		}

		public API(AccountInfo credentials, string gradesUrl)
		{
			Credentials = credentials;
			if (gradesUrl.Contains("//"))//protocol is already set
			{
				GradesUrl = gradesUrl;
			}
			else// add http://
			{
				GradesUrl = "http://" + gradesUrl;
			}

			#region setting absence dates

			#region start + enddate setting
			DateTime startDate;
			DateTime endDate;
			DateTime today = DateTime.Now;

			if (today.Month == 1 ||
				(today.Month >= 9 && today.Month <= 12))//first semester
			{
				startDate = new DateTime(today.Year, 9, 1);
				endDate = new DateTime(today.AddYears(1).Year, 1, 31);
			}
			else//second semester or holiday
			{
				startDate = new DateTime(today.Year, 2, 1);
				endDate = new DateTime(today.Year, 6, 30);
			}
			#endregion

			//           ATTENTON: DO NOT TOUCH!

			//go through all months between start and end date
			for (DateTime helperDate = startDate;
				helperDate < endDate;
				helperDate = helperDate.AddMonths(1))
			{
				//handling what year to set
				int year;
				if (helperDate.Month == 1)//working on Jan
				{
					if (today.Month == 1)//this year

						year = today.Year;
					else//last year
						year = today.Year + 1;
				}
				else
				{
					if (today.Month == 1)
						year = today.Year - 1;
					else
						year = today.Year;
				}
				DateTime added = new DateTime(year, helperDate.Month, 1);
				_absenceMonths.Add(added);
			}

			#endregion
		}

		/// <summary>
		/// logs in with given credentials
		/// </summary>
		/// <returns>whether the user is valid</returns>
		public bool Login()
		{
			return Login(Credentials);
		}
		/// <summary>
		/// logs in with any credentials, current won't be overwritten
		/// </summary>
		/// <param name="credentials">credentials to use</param>
		/// <returns>whether the user is valid</returns>
		public bool Login(AccountInfo credentials)
		{
			if (isDisposed)
				throw new ObjectDisposedException("API");
			if (string.IsNullOrWhiteSpace(credentials?.Password) || string.IsNullOrWhiteSpace(credentials.Username))
				throw new ArgumentException("credentials cannot be empty");

			Cookie logid = new Cookie("baklogid", CombineUrl("login.aspx"), "/", gradesUrlWthProtocol);
			_client.CookieContainer.Add(logid);

			string docT = _client.GETRequest(CombineUrl("login.aspx"));

			string usernameName = ((CQ)docT)["#hlavni > div.loginmain > div:nth-child(4) > table > tbody > tr:nth-child(1) > td:nth-child(2) > table > tbody > tr > td > input"][0]["name"];
			string passwordName = ((CQ)docT)["#cphmain_TextBoxHeslo > tbody:nth-child(1) > tr:nth-child(1) > td:nth-child(1) > input:nth-child(1)"][0]["name"];

			var loginVals = Client.GetFieldsFromHtml(docT);

			loginVals.Set(usernameName, credentials.Username);
			loginVals.Set(passwordName, credentials.Password);

			_client.POSTRequest(CombineUrl("login.aspx"), loginVals);

			string html = _client.GETRequest(CombineUrl("uvod.aspx"));
			return !html.Contains("nepřihlášen");
		}

		/// <summary>
		/// loads data from a specific existing session
		/// </summary>
		/// <param name="sessionId">ASP.NET_SessionId of a logged connection</param>
		/// <returns>user loaded from website</returns>
		public User Load(string sessionId)
		{
			if (string.IsNullOrWhiteSpace(sessionId))
				throw new ArgumentException("sessionId cannot be empty!");

			if (isDisposed)
				throw new ObjectDisposedException("API");

			User user = new User();
			_client.CookieContainer.GetCookies(new Uri(GradesUrl)).Add(new Cookie("ASP.NET_SessionId", sessionId));

			string welcome=_client.GETRequest(CombineUrl("uvod.aspx"));

			CQ welcomeHtml = welcome;//todo:reset back
			string nameLabel = Helper.Decode(
				Helper.RemoveStartingWhitespaces(
					Helper.RemoveEndingWhitespaces(
						Helper.RemoveLineBreaks(welcomeHtml[".logjmeno"][0].InnerText)
						)
					)
				);

			#region name formating

			string name;
			if (nameLabel.Contains(','))//remove the class name
			{
				name = nameLabel.Split(',')[0];
			}
			else
				name = nameLabel;

			if (name.Contains(' '))//switch name and surename to friendlier form (Smith John > John Smith)
			{
				user.Name = name.Split(' ')[1] + " " + name.Split(' ')[0];
			}
			else
				user.Name = name;

			#endregion

			//scrapping sequence
			string scheduleUrl = CombineUrl(welcomeHtml["#panelmenu > div > div:nth-child(4) > div > ul > li:nth-child(1) > a"][0]["href"]);
			string gradesUrl = CombineUrl(welcomeHtml["#panelmenu > div > div:nth-child(6) > div > ul > li:nth-child(1) > a"][0]["href"]);

			#region shedule scrapping

			NameValueCollection scheduleData = Client.GetFieldsFromHtml(_client.DownloadString(scheduleUrl));

			scheduleData.Set("hlavnimenuSI", "3i0");
			scheduleData.Set("ctl00$cphmain$radiorozvrh", "stálý rozvrh");
			scheduleData.Set("ctl00$cphmain$Flyrozvrh$checkucitel", "on");
			scheduleData.Set("ctl00$cphmain$Flyrozvrh$checkskupina", "on");
			scheduleData.Set("ctl00$cphmain$Flyrozvrh$Checkmistnost", "on");

			string scheduleHtml = _client.POSTRequest(scheduleUrl, scheduleData);//this is weekly schedule HTML

			user.Subjects = ParseSubjects(scheduleHtml);

			#endregion

			#region subjects' counts

			string countsHtml = _client.GETRequest(CombineUrl("prehled.aspx?s=9"));
			SetSubjectsCount(countsHtml, user.Subjects);

			#endregion

			#region grades
			NameValueCollection gradesData = Client.GetFieldsFromHtml(_client.GETRequest(gradesUrl));
			gradesData.Set("ctl00$cphmain$Checkdetail", "on");
			gradesData.Set("ctl00$cphmain$Flyout2$Checktypy", "on");
			gradesData.Set("ctl00$cphmain$Flyout2$Checkdatumy", "on");
			gradesData.Set("ctl00$cphmain$Flyout2$Checkprumery", "on");
			gradesData.Set("ctl00$cphmain$Flyout2$checkpoznamky", "on");
			gradesData.Set("hlavnimenuSI", "2i0");

			string gradesHtml = _client.POSTRequest(gradesUrl, gradesData);
			SetGrades(gradesHtml, user);

			#endregion

			#region absences

			//list for storing mondays of each week we know that contains absences
			List<DateTime> weeklyAbsences = new List<DateTime>();

			/*
			idea of monthly checks:
			it's not necessary to check every week in semester,
			when most of them should be empty. We'll save the weeks,
			which weren't empty and look up the absences
			note: this is slower for ppl with a lots of absences, but it's their problem, right?
			*/
			#region mothly checks
			//do not touch, optimisation is probably impossible! very fragile!
			#region getting the right values for monthly checks 

			NameValueCollection absenceData = Client.GetFieldsFromHtml(_client.GETRequest(CombineUrl("prehled.aspx?s=3")));
			absenceData.Set("ctl00$cphmain$listdobaoml", "zadané období");
			absenceData.Set("ctl00$cphmain$listjakoml", "seznam");

			absenceData = Client.GetFieldsFromHtml(
				_client.POSTRequest(CombineUrl("prehled.aspx?s=3"), absenceData));

			absenceData.Set("ctl00$cphmain$listabsencedo", "daný měsíc");
			absenceData.Set("ctl00$cphmain$listdobaoml", "zadané období");
			absenceData.Set("ctl00$cphmain$listjakoml", "seznam");
			#endregion


			//go through every month in semester and find weeks with absences
			foreach (DateTime monthDate in _absenceMonths)
			{
				string unixTime = Helper.ToUnixtime(monthDate).ToString() + "000";
				absenceData.Set("cphmain_listabsenceod_Raw", unixTime);

				CQ monthlyAbsenceHtml = _client.POSTRequest(CombineUrl("prehled.aspx?s=3"), absenceData);
				CQ rows = monthlyAbsenceHtml[".omlseztab > tbody> tr.omlsezbody"];

				string table = monthlyAbsenceHtml.RenderSelection();

				for (int r = 0; r < rows.Length; r++)//go through each row of table. table represents month's days
				{
					CQ cells = ((CQ)rows[r].InnerHTML)["td"];//row cells

					for (int i = 0; i < cells.Length; i++)
					{
						if (i == 0)//first cell with date, we know the date from "r" and "date"
							continue;
						else if (i == 1)//shortened day of week, e.g. "po", "út"...
							continue;

						string cell = cells[i].InnerText;

						if (string.IsNullOrWhiteSpace(cell))
							continue;//no absence for this cell

						//now we know there's an absence

						DateTime mondayOfCheckedWeek =
							Helper.ToMonday(new DateTime(monthDate.Year, monthDate.Month, r + 1));
						bool contains = weeklyAbsences.Contains(mondayOfCheckedWeek);

						if (!contains)
							weeklyAbsences.Add(mondayOfCheckedWeek);//we'll check this week later
						break;
					}
				}
			}
			#endregion

			#region weekly checks
			//look up every week we know that contains absences and save 'em

			NameValueCollection weeklyAbsenceData =
				Client.GetFieldsFromHtml(_client.GETRequest(CombineUrl("prehled.aspx?s=3")));
			weeklyAbsenceData.Set("ctl00$cphmain$Flyoutoml$Checkomlpredmety", "on");
			weeklyAbsenceData.Set("ctl00$cphmain$listdobaoml", "zadané období");
			weeklyAbsenceData.Set("ctl00$cphmain$listjakoml", "tabulka");

			weeklyAbsenceData = Client.GetFieldsFromHtml(
					_client.POSTRequest(CombineUrl("prehled.aspx?s=3"), weeklyAbsenceData));
			weeklyAbsenceData.Set("ctl00$cphmain$listabsencedo", "daný týden");
			weeklyAbsenceData.Set("ctl00$cphmain$Flyoutoml$Checkomlpredmety", "on");
			weeklyAbsenceData.Set("ctl00$cphmain$listdobaoml", "zadané období");
			weeklyAbsenceData.Set("ctl00$cphmain$listjakoml", "tabulka");

			foreach (DateTime weekDate in weeklyAbsences)
			{
				string unix = Helper.ToUnixtime(weekDate).ToString() + "000";
				weeklyAbsenceData.Set("cphmain_listabsenceod_Raw", unix);

				CQ weeklyAbsencesHtml = _client.POSTRequest(CombineUrl("prehled.aspx?s=3"), weeklyAbsenceData);

				//select even rows with subject's name inside
				CQ s_rows = weeklyAbsencesHtml["#trozvrh > table > tbody > tr:nth-child(even)"];
				//select odd rows with images to find out the absence's type
				CQ i_rows = weeklyAbsencesHtml["#trozvrh > table > tbody > tr:nth-child(odd)"];

				for (int row = 0; row < 5; row++)
				{
					for (int col = 0; col < 15; col++)
					{
						//the cell with possible name of subject
						IDomObject s_cell=Cq(s_rows[row + 1])["td"][col];
						
						//name of subject for abs, long
						string subjName = s_cell.HasAttribute("title") ? s_cell["title"] : null;

						if (!string.IsNullOrWhiteSpace(subjName))//there's an absence
						{
							//the source of image
							
							IDomObject i_cell = Cq(i_rows[row + 1])["td"][col+1];

							IDomObject img = Cq(i_cell)["img"][0];
							string imgSrc = img["src"];
							
							Absence.EType absType;//type of current absence
							if (imgSrc.Contains("wAbOk.gif"))
								absType = Absence.EType.Execused;
							else if (imgSrc.Contains("wAbsent.gif"))
								absType = Absence.EType.YetUnexecused;
							else if (imgSrc.Contains("wAbMiss.gif"))
								absType = Absence.EType.Unexecused;
							else if (imgSrc.Contains("wAbSoon.gif"))
								absType = Absence.EType.Early;
							else if (imgSrc.Contains("wAbLate.gif"))
								absType = Absence.EType.Late;
							else
								absType = Absence.EType.Uncountable;

							ShedulePosition pos = new ShedulePosition(row, col);

							Absence newAbsence = new Absence
							{
								Type = absType,
								Time = ShedulePosition.ToDateTime(pos, weekDate)
							};

							var query = from q in user.Subjects
										 where q.Info.LongName == subjName
										 select q.Absences;

							if (query.Any())
							{
								query.First().Add(newAbsence);
							}
							else
								Debug.WriteLine("no matching subject found for '" + subjName + "'");
						}
					}
				}
			}

			weeklyAbsenceData.Set("cphmain_listabsenceod_Raw", "");
			#endregion


			#endregion

			return user;
		}

		#region parsing functions
		#region shedule
		private List<Subject> ParseSubjects(string html)
		{
			CQ DOM = html;
			List<Subject> innerSubs = new List<Subject>();

			IDomObject[] mainLines = DOM[".r_roztable > tbody > tr"].ToArray();
			for (int y = 1; y < 6; y++)
			{
				IDomObject[] dayLines = ((CQ)mainLines[y].OuterHTML)["tr > td"].ToArray();

				for (int x = 1; x < 16; x++)
				{
					IDomObject hourCell = dayLines[x];
					if (hourCell.HasClass("r_rr"))//no subject there
					{
						//Debug.WriteLine("empty cell [d,h]: [" + (y - 1) + "," + (x - 1) + "]");
					}
					else if (hourCell.HasClass("r_rrw"))//something there
					{
						if ((((CQ)hourCell.InnerHTML)[".r_bunka_2"][0]) == null)
						{
							IDomObject subName = ((CQ)hourCell.InnerHTML)[".r_predm"][0];

							string fullName = WebUtility.HtmlDecode(subName["title"]);
							var checkQuery = from q in innerSubs
											  where q.Info.LongName == fullName
											  select q;

							string shortName = WebUtility.HtmlDecode(subName.InnerText);
							IDomObject teacher = (((CQ)hourCell.InnerHTML)[".r_ucit"][0]);
							string longTeacher = WebUtility.HtmlDecode(teacher["title"]);
							string shortTeacher = WebUtility.HtmlDecode(teacher.InnerText);

							SubjectInfo cellInfo = new SubjectInfo
							{
								LongName = fullName,
								LongTeacher = longTeacher,
								ShortTeacher = shortTeacher,
								ShortName = shortName
							};

							SheduleTime thisTime = ParseScheduleTime(hourCell, (y - 1), (x - 1));//this cell
							thisTime.Info = cellInfo;

							if (!checkQuery.Any())//new subj
							{
								if (shortName.Contains("S: ") || shortName.Contains("L: "))//detect even/odd
								{
									thisTime.Week = shortName.Contains("S: ") ? SheduleTime.EEvenOdd.Even : SheduleTime.EEvenOdd.Odd;
								}

								Subject newSubject = new Subject(thisTime)
								{
									Info = cellInfo,
									Id = innerSubs.Count
								};

								newSubject.Info.ShortName = newSubject.Info.ShortName.Replace("S: ", "").Replace("L: ", "");

								innerSubs.Add(newSubject);
							}
							else//we already know this subject, add new sheduleTime
							{
								Subject oldSubject = checkQuery.First();
								oldSubject.When.Add(thisTime);
							}
						}
						else
						{
							CQ divCell = ((CQ)hourCell.InnerHTML)[".r_bunka_2"][0].InnerHTML;

							ProceedDividedCell(divCell[".r_bunka_in2"][0].InnerHTML, innerSubs, y - 1, x - 1);
							ProceedDividedCell(divCell[".r_bunka_in2last"][0].InnerHTML, innerSubs, y - 1, x - 1);
						}
					}
				}
			}

			return innerSubs;
		}

		private void ProceedDividedCell(CQ Cell, List<Subject> targetList, int day, int hour)
		{
			string fullName = WebUtility.HtmlDecode(Cell[".r_predm_in2"][0]["title"]);
			string shortName = WebUtility.HtmlDecode(Cell[".r_predm_in2"][0].InnerText);

			string longTeacher = WebUtility.HtmlDecode(Cell[".r_ucit_in2"][0]["title"]);
			string shortTeacher = WebUtility.HtmlDecode(Cell[".r_ucit_in2"][0].InnerText);

			string shortRoom = WebUtility.HtmlDecode(Cell[".r_mist_in2 > div.r_dole"][0]["title"]);

			var selectAciveQ = from q in targetList
							   where q.Info.LongName == fullName
							   select q;

			SubjectInfo cellInfo = new SubjectInfo
			{
				LongName = fullName,
				ShortName = shortName,
				LongTeacher = longTeacher,
				ShortTeacher = shortTeacher
			};

			SheduleTime thisTime = new SheduleTime
			{
				Info = cellInfo,
				Week = ((shortName.Contains("L: ")) ? SheduleTime.EEvenOdd.Odd : SheduleTime.EEvenOdd.Even),
				ShedulePosition = new ShedulePosition
				{
					Day = day,
					Hour = hour
				}
			};
			thisTime.Info.ShortName = thisTime.Info.ShortName.Replace("L: ", "").Replace("S: ", "");

			if (!selectAciveQ.Any())//add new subject
			{
				Subject newSubject = new Subject(thisTime)
				{
					Info = cellInfo,
					Id = targetList.Count
				};

				targetList.Add(newSubject);
			}
			else//assign to an existing subject
			{
				selectAciveQ.ElementAt(0).When.Add(thisTime);
			}
		}
		private static SheduleTime ParseScheduleTime(IDomObject hourCell, int day, int hour)
		{
			IDomObject room = ((CQ)hourCell.InnerHTML)[".r_mist > .r_dole"][0];
			string shortRoom = WebUtility.HtmlDecode(room.InnerText);
			string shortName = WebUtility.HtmlDecode(((CQ)hourCell.InnerHTML)[".r_predm"][0].InnerText);

			SheduleTime.EEvenOdd evenOdd;
			if (shortName.Contains("L: "))
				evenOdd = SheduleTime.EEvenOdd.Odd;
			else if (shortName.Contains("S: "))
				evenOdd = SheduleTime.EEvenOdd.Even;
			else
				evenOdd = SheduleTime.EEvenOdd.Standard;

			return new SheduleTime
			{
				Info = new SubjectInfo
				{
					ShortName = shortName
				},
				Room = shortRoom,
				Week = evenOdd,
				ShedulePosition = new ShedulePosition
				{
					Hour = hour,
					Day = day
				}
			};
		}

		#endregion
		#region subjects' counts
		private void SetSubjectsCount(string HTMLdom, List<Subject> subjects)
		{
			IDomObject[] IdomObjlines = ((CQ)(HTMLdom))[".zamtable > tbody:nth-child(1) > .rozseznampred,.zamtable > tbody:nth-child(1) > .rozseznampredhi"].ToArray();

			foreach (IDomObject idomobjline in IdomObjlines)
			{
				CQ line = idomobjline.InnerHTML;

				string subject = WebUtility.HtmlDecode(line["td"][0].InnerText);
				IDomObject hours = line["td"][1];

				var query = from item in subjects
							 where (item.Info.LongName == subject)
							 select item;

				foreach (Subject s in query)
				{
					s.HoursPerSemester = int.Parse(hours.InnerText.Replace(" ", ""), CultureInfo.InvariantCulture);
				}
			}
		}
		#endregion
		#region grades
		private static void SetGrades(string HTMLdom, User user)
		{
			if (user.Subjects.Count > 0)
			{
				CQ dom = HTMLdom;
				CQ HTMLtable = dom[".dettable tbody"].RenderSelection();

				List<string> lines = new List<string>();

				int i = 0;
				while (true)
				{
					try
					{
						lines.Add(HTMLtable.Select("tr")[i].InnerHTML);
						i++;
					}
					catch { break; }
				}

				string currentSubj = "";

				foreach (string line in lines)
				{
					CQ CQline = line;
					string strmark = CQline["td:nth-child(2)"].Text().Replace(" ", "");
					double dblmark;

					string type = WebUtility.HtmlDecode(CQline[".dettyp"].Text().Replace(" ", ""));
					if (type == "C")
						type = "10";

					int weight = int.Parse(type, CultureInfo.InvariantCulture);
					string[] strdate = (CQline[".detdatum"].Text().Replace(" ", "")).Split('.');

					DateTime date = new DateTime((int.Parse(strdate[2], CultureInfo.InvariantCulture) + 2000), int.Parse(strdate[1], CultureInfo.InvariantCulture), int.Parse(strdate[0], CultureInfo.InvariantCulture));
					string label = WebUtility.HtmlDecode(CQline[".detcaption"].Text());
					string detail = WebUtility.HtmlDecode(CQline[".detpozn2"].Text().Replace("(", "").Replace(")", ""));

					if (strmark.ToUpper(CultureInfo.InvariantCulture).Contains("N"))
					{
						dblmark = 0.0;
					}
					else if (strmark.Contains("-"))
					{
						dblmark = double.Parse((strmark.Replace("-", ".5")), CultureInfo.InvariantCulture);
					}
					else
					{
						dblmark = double.Parse(strmark.Replace(" ", ""), CultureInfo.InvariantCulture);
					}

					if (!string.IsNullOrEmpty((CQline[".detpredm"].Text())))
					{
						currentSubj = CQline[".detpredm"].Text();
					}

					var query = from q in user.Subjects
								 where (q.Info.LongName == currentSubj)
								 select q;

					Grade grade = new Grade
					{
						Time = date,
						Detail = detail,
						Name = label,
						Value = dblmark,
						Weight = weight
					};

					if (query.Any())
					{
						Subject parent = query.First();
						parent.Grades.Add(grade);
					}
				}
			}
		}
		#endregion
		#endregion

		#region helpers
		private CQ Cq(IDomObject obj)
		{
			return obj.InnerHTML;
		}

		private string CombineUrl(string addition)
		{
			return GradesUrl + "/" + addition;
		}
		#endregion

		public void Dispose()
		{
			_client.Dispose();
			isDisposed = true;
		}
	}

	/// <summary>
	/// Contains just basic credentials.
	/// </summary>
	public class AccountInfo
	{
		public int Id { get; set; }

		public AccountInfo(string username, string password)
		{
			Username = username;
			Password = password;
		}
		public AccountInfo() { }

		public string Username { get; set; }
		public string Password { get; set; }
	}
}
