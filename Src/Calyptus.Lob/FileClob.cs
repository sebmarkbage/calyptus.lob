using System.IO;
using System;
using System.Text;

namespace Calyptus.Lob
{
	public class FileClob : Clob
	{
		private string filename;
		private Encoding encoding;

		public string Filename
		{
			get
			{
				return filename;
			}
		}

		public Encoding Encoding
		{
			get
			{
				return encoding;
			}
		}

		public FileClob(string filename)
		{
			if (filename == null) throw new ArgumentNullException("filename");
			this.filename = Path.GetFullPath(filename);
		}

		public FileClob(string filename, Encoding encoding) : this(filename)
		{
			this.encoding = encoding;
		}

		public FileClob(FileInfo file)
		{
			if (file == null) throw new ArgumentNullException("file");
			this.filename = file.FullName;
		}

		public FileClob(FileInfo file, Encoding encoding) : this(file)
		{
			this.encoding = encoding;
		}

		public override TextReader OpenReader()
		{
			return encoding == null ? new StreamReader(filename, true) : new StreamReader(filename, encoding);
		}

		public override bool Equals(Clob clob)
		{
			FileClob fc = clob as FileClob;
			if (fc == null) return false;
			if (fc == this) return true;
			return fc.filename.Equals(this.filename) && (fc.encoding == null || this.encoding == null || fc.encoding == this.encoding);
		}
	}
}