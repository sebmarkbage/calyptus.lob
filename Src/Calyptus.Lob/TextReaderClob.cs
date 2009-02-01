using System.IO;
using System.Text;
using System;

namespace Calyptus.Lob
{
	public class TextReaderClob : Clob
	{
		private TextReader reader;
		private bool needRestart;
		private long initialPosition;
		private bool alreadyOpen;



		public TextReaderClob(Stream stream, Encoding encoding)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (encoding == null) throw new ArgumentNullException("encoding");
			try
			{
				this.initialPosition = stream.CanSeek ? stream.Position : -1L;
			}
			catch
			{
				this.initialPosition = -1L;
			}
			reader = new StreamReader(stream, encoding);
		}

		public TextReaderClob(TextReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");
			StreamReader sr = reader as StreamReader;
			if (sr != null)
				try
				{
					this.initialPosition = sr.BaseStream.CanSeek ? sr.BaseStream.Position : -1L;
				}
				catch
				{
					this.initialPosition = -1L;
				}
			else
				this.initialPosition = -1L;
			this.reader = reader;
		}

		public override TextReader OpenReader()
		{
			lock (this)
			{
				if (needRestart && this.initialPosition < 0L)
					throw new Exception("The underlying TextReader cannot be reset. It has already been opened.");
				if (alreadyOpen)
					throw new Exception("There's already a reader open on this Clob. Close the first reader before requesting a new one.");
				if (needRestart)
					(reader as StreamReader).BaseStream.Seek(initialPosition, SeekOrigin.Begin);
				alreadyOpen = true;
			}
			return new ClobReader(this);
		}

		public override bool Equals(Clob clob)
		{
			TextReaderClob rc = clob as TextReaderClob;
			if (rc == null) return false;
			if (rc == this) return true;
			return this.reader == rc.reader;
		}

		#region Read-only stream wrapper class
		private class ClobReader : TextReader
		{
			private TextReaderClob clob;

			public ClobReader(TextReaderClob clob)
			{
				this.clob = clob;
			}

			private void ThrowClosed()
			{
				if (clob == null) throw new Exception("The TextReader is already closed.");
			}

			public override void Close()
			{
				Dispose(true);
			}

			protected override void Dispose(bool disposing)
			{
				if (clob != null)
					lock (clob)
					{
						clob.alreadyOpen = false;
						clob = null;
					}
			}

			public override int Peek()
			{
				ThrowClosed();
				return clob.reader.Peek();
			}

			public override int Read()
			{
				ThrowClosed();
				clob.needRestart = true;
				return clob.reader.Read();
			}

			public override int Read(char[] buffer, int index, int count)
			{
				ThrowClosed();
				if (index > 0 || count > 0) clob.needRestart = true;
				return clob.reader.Read(buffer, index, count);
			}

			public override int ReadBlock(char[] buffer, int index, int count)
			{
				ThrowClosed();
				if (index > 0 || count > 0) clob.needRestart = true;
				return clob.reader.ReadBlock(buffer, index, count);
			}

			public override string ReadLine()
			{
				ThrowClosed();
				clob.needRestart = true;
				return clob.reader.ReadLine();
			}

			public override string ReadToEnd()
			{
				ThrowClosed();
				clob.needRestart = true;
				return clob.reader.ReadToEnd();
			}
		}
		#endregion
	}
}