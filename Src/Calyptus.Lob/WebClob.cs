using System.IO;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace Calyptus.Lob
{
	public class WebClob : Clob
	{
		private Uri uri;
		private NameValueCollection headers;
		private ICredentials credentials;

		public Uri Uri
		{
			get
			{
				return uri;
			}
		}

		public NameValueCollection CustomHeaders
		{
			get
			{
				return headers;
			}
		}

		public ICredentials Credentials
		{
			get
			{
				return credentials;
			}
		}

		public WebClob(string uri) : this(uri, null, null) { }

		public WebClob(Uri uri) : this(uri, null, null) { }

		public WebClob(string uri, ICredentials credentials) : this(uri, null, credentials) { }

		public WebClob(Uri uri, ICredentials credentials) : this(uri, null, credentials) { }

		public WebClob(string uri, NameValueCollection customHeaders) : this(uri, customHeaders, null) { }

		public WebClob(Uri uri, NameValueCollection customHeaders) : this(uri, customHeaders, null) { }

		public WebClob(string uri, NameValueCollection customHeaders, ICredentials credentials)
		{
			if (uri == null) throw new ArgumentNullException("uri");
			this.uri = new Uri(uri);
			this.headers = customHeaders;
			this.credentials = credentials;
		}

		public WebClob(Uri uri, NameValueCollection customHeaders, ICredentials credentials)
		{
			if (uri == null) throw new ArgumentNullException("uri");
			this.uri = uri;
			this.headers = customHeaders;
			this.credentials = credentials;
		}

		public override TextReader OpenReader()
		{
			WebRequest request = WebRequest.Create(uri);
			if (credentials != null)
				request.Credentials = credentials;
			if (headers != null)
				request.Headers.Add(headers);
			WebResponse response = request.GetResponse();
			HttpWebResponse httpResponse = response as HttpWebResponse;
			if (httpResponse != null)
				return new WebResponseReader(httpResponse);
			else
				return new WebResponseReader(response);
		}

		public override bool Equals(Clob clob)
		{
			if (clob == null) return false;
			if (clob == this) return true;
			WebClob wb = clob as WebClob;
			if (wb != null) return this.uri.Equals(wb.uri) && this.credentials == wb.credentials && this.headers == wb.headers;
			if (!this.uri.IsFile) return false;
			FileClob fb = clob as FileClob;
			if (fb == null) return false;
			return this.uri.LocalPath.Equals(fb.Filename);
		}

		private class WebResponseReader : StreamReader
		{
			private WebResponse response;

			public WebResponseReader(HttpWebResponse response) : base(response.GetResponseStream(), GetEncoding(response.CharacterSet))
			{
				this.response = response;
			}

			public WebResponseReader(WebResponse response) : base(response.GetResponseStream(), true)
			{
				this.response = response;
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					this.BaseStream.Close();
					this.response.Close();
				}
				base.Dispose(disposing);
			}

			private static Encoding GetEncoding(string charset)
			{
				if (charset != null)
				try
				{
					return Encoding.GetEncoding(charset);
				}
				catch
				{
				}
				return Encoding.GetEncoding("iso8859-1");
			}
			
		}
	}
}