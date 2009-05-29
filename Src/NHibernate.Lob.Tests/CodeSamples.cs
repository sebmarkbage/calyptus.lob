using System;
using Calyptus.Lob;
using System.IO;
using System.Xml;
using System.Text;
using System.Xml.Serialization;

namespace NHibernate.Lob.Samples
{
	public class Product
	{
		public int ID { get; set; }
		public string Title { get; set; }
		public Blob Image { get; set; }
		public Clob Description { get; set; }
		public Xlob Specifications { get; set; }
	}
	/*
	public class Product
	{
		public int ID { get; set; }
		
		private Blob image;

		public void ChangeImage(Stream input)
		{
			this.image = input;
		}

		public void CopyImageFrom(Product product)
		{
			this.image = product.image;
		}

		public void WriteImageTo(Stream output)
		{
			this.image.WriteTo(output);
		}
	}*/

	public class CodeSamples
	{
		public static void Sample()
		{
			ISessionFactory sessionFactory;

			using (ISession session = sessionFactory.OpenSession())
			{
				Product product = session.Get<Product>(100);
				
				Response.Clear();
				Response.ContentType = "image/jpeg";
				Response.BufferOutput = false;
				product.Image.WriteTo(Response.OutputStream);

				using (XmlWriter writer = XmlWriter.Create(@"C:\MyFiles\SomeData.xml"))
				{
					product.Specifications.WriteTo(writer);
				}

				using (TextReader reader = product.Description.OpenReader())
				{
					string firstLine = reader.ReadLine();
				}
			}

			using (ISession session = sessionFactory.OpenSession())
			using (ITransaction transaction = session.BeginTransaction())
			{
				Product product = session.Get<Product>(100);
				product.Image = Blob.Create(@"C:\MyFolder\MyImage.jpg");
				product.Description = Clob.Create("My short description.");
				product.Specifications = Xlob.Create(new Uri("http://domain/document.xml"));
				transaction.Commit();
			}

			using (ISession session = sessionFactory.OpenSession())
			using (ITransaction transaction = session.BeginTransaction())
			{
				Product product = session.Get<Product>(100);
				product.Image = Request.Files["uploadedImage"].InputStream;
				product.Description = Request.Form["description"];
				product.Specifications = Xlob.Empty;
				transaction.Commit();
			}
		}
	}

	public abstract class Blob
	{
		public static Blob Empty { get; }
		public static Blob Create(byte[] data);
		public static Blob Create(Stream input);
		public static Blob Create(string filename);

		public static implicit operator Blob(byte[] data);
		public static implicit operator Blob(FileInfo file);
		public static implicit operator Blob(Stream input);
		public static implicit operator Blob(Uri uri);
	}

	public abstract class Clob
	{
		public static Clob Empty { get; }
		public static Clob Create(char[] characters);
		public static Clob Create(string text);
		public static Clob Create(TextReader reader);
		public static Clob Create(Stream input, Encoding encoding);
		public static Clob Create(string filename, Encoding encoding);

		public static implicit operator Clob(char[] characters);
		public static implicit operator Clob(string text);
		public static implicit operator Clob(TextReader reader);
	}

	public abstract class Xlob
	{
		public static Xlob Empty { get; }
		public static Xlob Create(IXmlSerializable obj);
		public static Xlob Create(Stream stream);
		public static Xlob Create(string xmlFragment);
		public static Xlob Create(TextReader reader);
		public static Xlob Create(Uri uri);
		public static Xlob Create(XmlDocument document);
		public static Xlob Create(XmlReader reader);
		public static Xlob Create(Stream stream, XmlParserContext inputContext);
		public static Xlob Create(Stream stream, XmlReaderSettings settings);
		public static Xlob Create(TextReader reader, XmlParserContext inputContext);
		public static Xlob Create(TextReader reader, XmlReaderSettings settings);
		public static Xlob Create(Stream stream, XmlReaderSettings settings, XmlParserContext inputContext);
		public static Xlob Create(TextReader reader, XmlReaderSettings settings, XmlParserContext inputContext);

		public static implicit operator Xlob(string xml);
		public static implicit operator Xlob(Uri uri);
		public static implicit operator Xlob(XmlDocument document);
		public static implicit operator Xlob(XmlReader reader);
	}
}