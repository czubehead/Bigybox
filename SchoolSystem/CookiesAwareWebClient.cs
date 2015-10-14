using CsQuery;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BakalariAPI
{
	public class CookieAwareWebClient : WebClient
	{
		public CookieContainer CookieContainer { get; private set; }

		public CookieAwareWebClient()
		{
			CookieContainer = new CookieContainer();
		}
		protected override WebRequest GetWebRequest(Uri address)
		{
			//Grabs the base request being made 
			var request = (HttpWebRequest)base.GetWebRequest(address);
			//Adds the existing cookie container to the Request
			request.CookieContainer = CookieContainer;
			request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
			return request;
		}

		public string GETRequest(string url)
		{
			try
			{
				return Encoding.UTF8.GetString(DownloadData(url));
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		public string POSTRequest(string url,NameValueCollection data)
		{
			try
			{
				return Encoding.UTF8.GetString(UploadValues(url, data));
			}
			catch
			{
				throw;
			}
		}

		public static NameValueCollection GetFieldsFromHtml(string input)
		{
			return Client.GetFieldsFromHtml(input);
		}
	}
}
