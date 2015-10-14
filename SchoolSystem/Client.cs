using CsQuery;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BakalariAPI
{
	public class Client
	{
		public string hostProtocol = "http";
		public string host;
		public string CookieHeader
		{
			get
			{
				return Cookies.GetCookieHeader(new Uri(HostUrl()));
			}
		}
		public string lastPage = "";
		public CookieContainer Cookies = new CookieContainer();

		public string HostUrl()
		{
			return hostProtocol + "://" + host;
		}

		public string Request(NameValueCollection data, string method, string url)
		{
			HttpWebRequest req = HttpWebRequest.Create(url) as HttpWebRequest;
			req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
			req.KeepAlive = false;//lastPage != url;
			req.Referer = lastPage;
			req.Host = host;

			req.CookieContainer = Cookies;

			req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.65 Safari/537.36";
			req.AllowAutoRedirect = true;
			req.ContentType = "application/x-www-form-urlencoded";
			req.Headers["Origin"] = HostUrl();
			req.Headers[HttpRequestHeader.CacheControl] = "max-age=0";
			req.Method = method;

			req.Timeout = 10000;

			if (method == "POST")
			{
				byte[] bytes = Encoding.UTF8.GetBytes(ToAmpstands(data));
				req.ContentLength = bytes.Length;

				Stream stream = req.GetRequestStream();
				stream.Write(bytes, 0, bytes.Length);
				stream.Close();
			}

			HttpWebResponse resp = req.GetResponse() as HttpWebResponse;
			lastPage = url;
			
			using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
			{
				string output = HttpUtility.HtmlDecode(reader.ReadToEnd());
				return output;
			}
		}

		public string GETRequest(string url)
		{
			return Request(null, "GET", url);
		}

		public static string ToAmpstands(NameValueCollection input)
		{
			string ret = "";

			if (input == null)
				return "";
			if (input.Count == 0)
				return "";

			for (int i = 0; i < input.Count; i++)
			{
				ret += HttpUtility.UrlEncode(input.GetKey(i))+"=";
				ret += HttpUtility.UrlEncode(input.GetValues(i).FirstOrDefault());
				if (i != input.Count - 1)
					ret += "&";
			}
			return ret;
		}

		public static NameValueCollection GetFieldsFromHtml(string input)
		{
			NameValueCollection vals = new NameValueCollection();

			IDomObject formObj = ((CQ)input)["body form"][0];
			if (formObj == null)
				return vals;

			CQ form = formObj.InnerHTML;
			CQ inputs = form["input"];

			for (int i = 0; i < inputs.Length; i++)
			{
				IDomObject field = inputs[i];// <input>
				if (field.HasAttribute("name"))
				{
					if(field.HasAttribute("type"))
					{
						if (field.GetAttribute("type") == "image")
							continue;
					}

					string name = field.GetAttribute("name");
					string value = "";

					if (field.HasAttribute("value"))
					{
						value = field.GetAttribute("value");
					}

					vals.Add(name, value);
				}
			}

			return vals;
		}
	}
}
