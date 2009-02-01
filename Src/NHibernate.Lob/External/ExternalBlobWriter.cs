using System.IO;
using System;

public abstract class ExternalBlobWriter : Stream
{
	public abstract byte[] Commit();

	public override bool CanRead
	{
		get { return false; }
	}

	public override bool CanWrite
	{
		get { return true; }
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		throw new NotSupportedException();
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		throw new NotSupportedException();
	}

	public override int ReadByte()
	{
		throw new NotSupportedException();
	}
}