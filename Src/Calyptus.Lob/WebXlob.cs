using System.IO;
using System;
using System.Net;
using System.Xml;
using System.Collections.Specialized;

namespace Calyptus.Lob
{
	public class WebXlob : Xlob
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

		public WebXlob(string uri) : this(uri, null, null) { }

		public WebXlob(Uri uri) : this(uri, null, null) { }

		public WebXlob(string uri, ICredentials credentials) : this(uri, null, credentials) { }

		public WebXlob(Uri uri, ICredentials credentials) : this(uri, null, credentials) { }

		public WebXlob(string uri, NameValueCollection customHeaders) : this(uri, customHeaders, null) { }

		public WebXlob(Uri uri, NameValueCollection customHeaders) : this(uri, customHeaders, null) { }

		public WebXlob(string uri, NameValueCollection customHeaders, ICredentials credentials)
		{
			if (uri == null) throw new ArgumentNullException("uri");
			this.uri = new Uri(uri);
			this.headers = customHeaders;
			this.credentials = credentials;
		}

		public WebXlob(Uri uri, NameValueCollection customHeaders, ICredentials credentials)
		{
			if (uri == null) throw new ArgumentNullException("uri");
			this.uri = uri;
			this.headers = customHeaders;
			this.credentials = credentials;
		}

		public override XmlReader OpenReader()
		{
			WebRequest request = WebRequest.Create(uri);
			if (credentials != null)
				request.Credentials = credentials;
			if (headers != null)
				request.Headers.Add(headers);
			WebResponse response = request.GetResponse();
			return new WebResponseXmlReader(response);
		}

		public override bool Equals(Xlob xlob)
		{
			if (xlob == null) return false;
			if (xlob == this) return true;
			WebXlob xb = xlob as WebXlob;
			if (xb != null) return this.uri.Equals(xb.uri) && this.credentials == xb.credentials && this.headers == xb.headers;
			if (!this.uri.IsFile) return false;
			FileXlob fb = xlob as FileXlob;
			if (fb == null) return false;
			return this.uri.LocalPath.Equals(fb.Filename);
		}

		private class WebResponseXmlReader : XmlTextReader
		{
			private WebResponse response;

			public WebResponseXmlReader(WebResponse response)
				: base(response.GetResponseStream(), XmlNodeType.Element, GetParser(response))
			{
				this.response = response;
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
					this.response.Close();
				base.Dispose(disposing);
			}

			private static XmlParserContext GetParser(WebResponse response)
			{
				XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.Preserve);
				HttpWebResponse httpResponse = response as HttpWebResponse;
				if (httpResponse != null && httpResponse.CharacterSet != null)
					try
					{
						context.Encoding = System.Text.Encoding.GetEncoding(httpResponse.CharacterSet);
					}
					catch
					{
					}
				context.BaseURI = response.ResponseUri.ToString();
				return context;
			}
		}
	}
}