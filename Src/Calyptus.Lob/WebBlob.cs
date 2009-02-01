using System.IO;
using System;
using System.Net;
using System.Collections.Specialized;

namespace Calyptus.Lob
{
	public class WebBlob : Blob
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

		public WebBlob(string uri) : this(uri, null, null) { }

		public WebBlob(Uri uri) : this(uri, null, null) { }

		public WebBlob(string uri, ICredentials credentials) : this(uri, null, credentials) { }

		public WebBlob(Uri uri, ICredentials credentials) : this(uri, null, credentials) { }

		public WebBlob(string uri, NameValueCollection customHeaders) : this(uri, customHeaders, null) { }

		public WebBlob(Uri uri, NameValueCollection customHeaders) : this(uri, customHeaders, null) { }

		public WebBlob(string uri, NameValueCollection customHeaders, ICredentials credentials)
		{
			if (uri == null) throw new ArgumentNullException("uri");
			this.uri = new Uri(uri);
			this.headers = customHeaders;
			this.credentials = credentials;
		}

		public WebBlob(Uri uri, NameValueCollection customHeaders, ICredentials credentials)
		{
			if (uri == null) throw new ArgumentNullException("uri");
			this.uri = uri;
			this.headers = customHeaders;
			this.credentials = credentials;
		}

		public override Stream OpenReader()
		{
			WebClient client = new WebClient();
			if (credentials != null)
				client.Credentials = credentials;
			if (headers != null)
				client.Headers.Add(headers);
			return client.OpenRead(uri);
		}

		public override bool Equals(Blob blob)
		{
			if (blob == null) return false;
			if (blob == this) return true;
			WebBlob wb = blob as WebBlob;
			if (wb != null) return this.uri.Equals(wb.uri) && this.credentials == wb.credentials && this.headers == wb.headers;
			if (!this.uri.IsFile) return false;
			FileBlob fb = blob as FileBlob;
			if (fb == null) return false;
			return this.uri.LocalPath.Equals(fb.Filename);
		}
	}
}