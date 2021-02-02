// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS
{
  using System;
  using System.Buffers;
  using System.Diagnostics.CodeAnalysis;
  using System.IO;
  using System.IO.Pipelines;
  using System.Threading;
  using System.Threading.Tasks;

  /// <summary>
  /// Use this class to write COBS-encoded messages to an underlying stream or buffer writer.
  /// Mark the end of messages using the <see cref="CommitMessage"/> method.
  /// </summary>
  public sealed class COBSWriterStream : Stream
  {
    private readonly COBSWriterBuffer _writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="COBSWriterStream"/> class.
    /// </summary>
    /// <param name="innerStream">The <see cref="Stream"/> that COBS-encoded messages will be written to.</param>
    public COBSWriterStream(Stream innerStream)
    {
      _writer = new COBSWriterBuffer(innerStream);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="COBSWriterStream"/> class.
    /// </summary>
    /// <param name="innerWriter">The <see cref="IBufferWriter{T}"/> that COBS-encoded messages will be written to.</param>
    public COBSWriterStream(IBufferWriter<byte> innerWriter)
    {
      _writer = new COBSWriterBuffer(innerWriter);
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
      buffer.CopyTo(_writer.GetSpan(buffer.Length));
      _writer.Advance(buffer.Length);
    }

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
      Write(new ReadOnlySpan<byte>(buffer, offset, count));
      return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
      Write(buffer.Span);
      return default;
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
      _writer.GetSpan(1)[0] = value;
      _writer.Advance(1);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
      Write(new ReadOnlySpan<byte>(buffer, offset, count));
    }

    /// <summary>
    /// Marks the completion of a message so that the boundary of the current
    /// message and the next message can be correctly encoded according to the COBS specification.
    /// </summary>
    public void CommitMessage()
    {
      _writer.CommitMessage();
    }

    /// <inheritdoc/>
    [DoesNotReturn]
    public override void Flush() => throw new NotSupportedException($"Flushing this stream is not supported. Flush the underlying Stream or IBufferWriter after calling CommitMessage on this stream.");

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
      Flush();
      return Task.CompletedTask; // never executes - compiler happiness only.
    }

#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
    public override void CopyTo(Stream destination, int bufferSize) => throw new NotSupportedException();
    public override void Close() => throw new NotSupportedException();
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    public override int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();
    public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
    public override int Read(Span<byte> buffer) => throw new NotSupportedException();
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override int ReadByte() => throw new NotSupportedException();
    public override bool CanTimeout => throw new NotSupportedException();
    public override int ReadTimeout { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int WriteTimeout { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
  }
}
