using System.IO;
using System;

namespace Calyptus.Lob
{
	public class FileBlob : Blob
	{
		private string filename;

		public string Filename
		{
			get
			{
				return filename;
			}
		}

		public FileBlob(string filename)
		{
			if (filename == null) throw new ArgumentNullException("filename");
			this.filename = Path.GetFullPath(filename);
		}

		public FileBlob(FileInfo file)
		{
			if (file == null) throw new ArgumentNullException("file");
			this.filename = file.FullName;
		}

		public override Stream OpenReader()
		{
			return File.Open(this.filename, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public override bool Equals(Blob blob)
		{
			FileBlob fb = blob as FileBlob;
			if (fb != null) return fb.filename.Equals(this.filename);
			if (fb == this) return true;

			StreamBlob sb = blob as StreamBlob;
			if (sb == null) return false;
			FileStream fs = sb.UnderlyingStream as FileStream;
			if (fs == null) return false;
			try
			{
				return filename.Equals(fs.Name);
			}
			catch
			{
				return false;
			}
		}
	}
}