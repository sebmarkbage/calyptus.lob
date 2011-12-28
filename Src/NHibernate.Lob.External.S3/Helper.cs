// This software code is made available "AS IS" without warranties of any        
// kind.  You may copy, display, modify and redistribute the software            
// code either by itself or as incorporated into your code; provided that        
// you do not remove any proprietary notices.  Your use of this software         
// code is at your own risk and you waive any claim against Amazon               
// Digital Services, Inc. or its affiliates with respect to your use of          
// this software code. (c) 2006 Amazon Digital Services, Inc. or its             
// affiliates.          


using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace NHibernate.Lob.External
{
    class Helper
    {
        public static readonly string METADATA_PREFIX = "x-amz-meta-";
        public static readonly string AMAZON_HEADER_PREFIX = "x-amz-";
        public static readonly string ALTERNATIVE_DATE_HEADER = "x-amz-date";
        public static readonly string DEFAULT_SERVER = "s3.amazonaws.com";

        public static string Host(string bucket)
        {
            if (bucket == "")
                return DEFAULT_SERVER;
            else
                return bucket + "." + DEFAULT_SERVER;
        }

        private static int securePort = 443;
        public static int SecurePort
        {
            get
            {
                return securePort;
            }
            set
            {
                securePort = value;
            }
        }

        private static int insecurePort = 80;
        public static int InsecurePort
        {
            get
            {
                return insecurePort;
            }
            set
            {
                insecurePort = value;
            }
        }

		/// <summary>
		/// Add the given headers to the WebRequest
		/// </summary>
		/// <param name="req">Web request to add the headers to.</param>
		/// <param name="headers">A map of string to string representing the HTTP headers to pass (can be null)</param>
		public static void addHeaders(WebRequest req, SortedList headers)
		{
			addHeaders(req, headers, "");
		}

		/// <summary>
		/// Add the given metadata fields to the WebRequest.
		/// </summary>
		/// <param name="req">Web request to add the headers to.</param>
		/// <param name="metadata">A map of string to string representing the S3 metadata for this resource.</param>
		public static void addMetadataHeaders(WebRequest req, SortedList metadata)
		{
			addHeaders(req, metadata, METADATA_PREFIX);
		}

		/// <summary>
		/// Add the given headers to the WebRequest with a prefix before the keys.
		/// </summary>
		/// <param name="req">WebRequest to add the headers to.</param>
		/// <param name="headers">Headers to add.</param>
		/// <param name="prefix">String to prepend to each before ebfore adding it to the WebRequest</param>
		public static void addHeaders(WebRequest req, SortedList headers, string prefix)
		{
			if (headers != null)
			{
				foreach (string key in headers.Keys)
				{
					if (prefix.Length == 0 && key.Equals("Content-Type"))
					{
						req.ContentType = headers[key] as string;
					}
					else
					{
						req.Headers.Add(prefix + key, headers[key] as string);
					}
				}
			}
		}

		/// <summary>
		/// Add the appropriate Authorization header to the WebRequest
		/// </summary>
		/// <param name="request">Request to add the header to</param>
		/// <param name="resource">The resource name (bucketName + "/" + key)</param>
		public static void addAuthHeader(WebRequest request, string resource, string awsAccessKeyId, string awsSecretAccessKey)
		{
			if (request.Headers[ALTERNATIVE_DATE_HEADER] == null)
			{
				request.Headers.Add(ALTERNATIVE_DATE_HEADER, getHttpDate());
			}

			string canonicalString = makeCanonicalString(resource, request);
			string encodedCanonical = encode(awsSecretAccessKey, canonicalString, false);
			request.Headers.Add("Authorization", "AWS " + awsAccessKeyId + ":" + encodedCanonical);
		}

		/// <summary>
		/// Create a new URL object for the given resource.
		/// </summary>
		/// <param name="resource">The resource name (bucketName + "/" + key)</param>
		public static string makeURL(string server, string resource, bool isSecure)
		{
			if (server == null)
				server = DEFAULT_SERVER;

			StringBuilder url = new StringBuilder();
			url.Append(isSecure ? "https" : "http").Append("://");
			url.Append(server).Append(":").Append(isSecure ? securePort : insecurePort).Append("/");
			url.Append(resource);
			return url.ToString();
		}

        public static string makeCanonicalString(string resource, WebRequest request)
        {
            SortedList headers = new SortedList();
            foreach (string key in request.Headers)
            {
                headers.Add(key, request.Headers[key]);
            }
            if (headers["Content-Type"] == null)
            {
                headers.Add("Content-Type", request.ContentType);
            }
            return makeCanonicalString(request.Method, resource, headers, null);
        }

        public static string makeCanonicalString(string verb, string resource,
                                                  SortedList headers, string expires)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append(verb);
            buf.Append("\n");

            SortedList interestingHeaders = new SortedList();
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    string lk = key.ToLower();
                    if (lk.Equals("content-type") ||
                         lk.Equals("content-md5") ||
                         lk.Equals("date") ||
                         lk.StartsWith(AMAZON_HEADER_PREFIX))
                    {
                        interestingHeaders.Add(lk, headers[key]);
                    }
                }
            }
            if (interestingHeaders[ALTERNATIVE_DATE_HEADER] != null)
            {
                interestingHeaders.Add("date", "");
            }

            // if the expires is non-null, use that for the date field.  this
            // trumps the x-amz-date behavior.
            if (expires != null)
            {
                interestingHeaders.Add("date", expires);
            }

            // these headers require that we still put a new line after them,
            // even if they don't exist.
            {
                string[] newlineHeaders = { "content-type", "content-md5" };
                foreach (string header in newlineHeaders)
                {
                    if (interestingHeaders.IndexOfKey(header) == -1)
                    {
                        interestingHeaders.Add(header, "");
                    }
                }
            }

            // Finally, add all the interesting headers (i.e.: all that startwith x-amz- ;-))
            foreach (string key in interestingHeaders.Keys)
            {
                if (key.StartsWith(AMAZON_HEADER_PREFIX))
                {
                    buf.Append(key).Append(":").Append((interestingHeaders[key] as string).Trim());
                }
                else
                {
                    buf.Append(interestingHeaders[key]);
                }
                buf.Append("\n");
            }

            // Do not include the query string parameters
            int queryIndex = resource.IndexOf('?');
            if (queryIndex == -1)
            {
                buf.Append("/" + resource);
            }
            else
            {
                buf.Append("/" + resource.Substring(0, queryIndex));
            }

            return buf.ToString();
        }

        public static string encode(string awsSecretAccessKey, string canonicalString, bool urlEncode)
        {
            Encoding ae = new UTF8Encoding();
            HMACSHA1 signature = new HMACSHA1(ae.GetBytes(awsSecretAccessKey));
            string b64 = Convert.ToBase64String(
                                        signature.ComputeHash(ae.GetBytes(
                                                        canonicalString.ToCharArray()))
                                               );
                return b64;
        }

        public static string slurpInputStream(Stream stream)
        {
            StringBuilder data = new StringBuilder();

            try
            {
                System.Text.Encoding encode =
                    System.Text.Encoding.GetEncoding("utf-8");
                StreamReader readStream = new StreamReader(stream, encode);
                const int stride = 4096;
                Char[] read = new Char[stride];

                int count = readStream.Read(read, 0, stride);
                while (count > 0)
                {
                    string str = new string(read, 0, count);
                    data.Append(str);
                    count = readStream.Read(read, 0, stride);
                }
            }
            finally
            {
                stream.Close();
            }

            return data.ToString();
        }

        public static DateTime parseDate(string dateStr)
        {
            return DateTime.Parse(dateStr);
        }

        public static string getHttpDate()
        {
            // Setting the Culture will ensure we get a proper HTTP Date.
            string date = System.DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss ", System.Globalization.CultureInfo.InvariantCulture) + "GMT";
            return date;
        }

        public static long currentTimeMillis()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}
