//// Copyright (c) True Goodwill. All rights reserved.
//// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//namespace FFT.COBS
//{
//  using System;
//  using System.IO;
//  using System.IO.Pipelines;
//  using System.Threading;
//  using System.Threading.Tasks;

//  public sealed class COBSStreamWriter : Stream
//  {
//    private readonly Stream _innerStream;
//    private readonly COBSBufferWriter _writer;

//    public override bool CanRead => false;
//    public override bool CanSeek => false;
//    public override bool CanWrite => true;
//    public override long Length => throw new NotSupportedException();
//    public override long Position => throw new NotSupportedException();
//    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
//    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
//    public override void SetLength(long value) => throw new NotSupportedException();
//    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
//    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
//    public override bool CanTimeout => _innerStream.CanTimeout;
//    public override void CopyTo(Stream destination, int bufferSize) => throw new NotSupportedException();
//    public override void Close() => throw new NotSupportedException();
//    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();
//    public override int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();
//    public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
//    public override int Read(Span<byte> buffer) => throw new NotSupportedException();
//    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
//    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
//    public override int ReadByte() => throw new NotSupportedException();
//    public override int ReadTimeout { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
//    public override int WriteTimeout { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

//    public COBSStreamWriter(Stream innerStream)
//    {
//      _innerStream = innerStream;
//      _writer = new COBSBufferWriter(PipeWriter.Create(_innerStream));
//    }

//    public override void Write(ReadOnlySpan<byte> buffer)
//    {
//      buffer.CopyTo(_writer.GetSpan(buffer.Length));
//      _writer.Advance(buffer.Length);
//    }

//    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
//    {
//      Write(new ReadOnlySpan<byte>(buffer, offset, count));
//      return Task.CompletedTask;
//    }

//    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
//    {
//      Write(buffer.Span);
//      return default;
//    }

//    public override void WriteByte(byte value)
//    {
//      _writer.GetSpan(1)[0] = value;
//      _writer.Advance(1);
//    }

//    public override void Write(byte[] buffer, int offset, int count)
//    {
//      Write(new ReadOnlySpan<byte>(buffer, offset, count));
//    }

//    public void CommitMessage()
//    {
//      _writer.CommitMessage();
//    }

//    public override void Flush()
//    {
//      PipeWriter x;
//      x.fu
//    }
//  }
//}
