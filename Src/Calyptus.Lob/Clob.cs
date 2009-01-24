using System.IO;
using System.Text;

namespace NHibernate.Lob
{
	public abstract class Clob
	{
		public static Clob Create(Stream input, Encoding encoding)
		{

		}

		public static Clob Create(TextReader reader)
		{

		}

		public static Clob Create(char[] characters)
		{

		}

		public static Clob Create(string text)
		{

		}

		public static implicit operator Clob(TextReader reader)
		{
			return Create(reader);
		}

		public static implicit operator Clob(char[] characters)
		{
			return Create(characters);
		}

		public static implicit operator Clob(string text)
		{
			return Create(text);
		}

		public abstract TextReader Open();

		public virtual void WriteTo(Stream output, Encoding encoding)
		{

		}

		public virtual void WriteTo(TextWriter writer)
		{

		}
	}
}