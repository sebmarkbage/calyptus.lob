using System.IO;
using System;
using System.Text;
using System.Xml;

namespace Calyptus.Lob
{
	public class FileXlob : Xlob
	{
		private string filename;
		private XmlReaderSettings settings;

		public string Filename
		{
			get
			{
				return filename;
			}
		}

		public XmlReaderSettings ReaderSettings
		{
			get
			{
				return settings;
			}
		}

		public FileXlob(string filename)
		{
			if (filename == null) throw new ArgumentNullException("filename");
			this.filename = Path.GetFullPath(filename);
		}

		public FileXlob(FileInfo file)
		{
			if (file == null) throw new ArgumentNullException("file");
			this.filename = file.FullName;
		}

		public FileXlob(string filename, XmlReaderSettings readerSettings) : this(filename)
		{
			this.settings = readerSettings;
		}

		public FileXlob(FileInfo file, XmlReaderSettings readerSettings) : this(file)
		{
			this.settings = readerSettings;
		}

		public override XmlReader OpenReader()
		{
			return XmlReader.Create(filename, ReaderSettings);
		}

		public override bool Equals(Xlob xlob)
		{
			FileXlob fx = xlob as FileXlob;
			if (fx == null) return false;
			if (fx == this) return true;
			return fx.filename.Equals(this.filename) && (fx.settings == null || this.settings == null || fx.settings.Equals(this.settings));
		}
	}
}