// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS
{
  using System;
  using System.Buffers;
  using System.Runtime.CompilerServices;
  using static System.Math;

  /// <summary>
  /// https://en.wikipedia.org/wiki/Consistent_Overhead_Byte_Stuffing
  /// </summary>
  public sealed class COBSBufferWriter : IBufferWriter<byte>, IDisposable
  {
    private readonly IBufferWriter<byte> _innerWriter;

    private byte[] _buffer;
    private int _bufferSize;
    private int _start; // start of cached data
    private int _end; // end of cached data + 1, so it's the beginning place to write new data.

    /// <summary>
    /// Initializes a new instance of the <see cref="COBSBufferWriter"/> class.
    /// </summary>
    /// <param name="innerWriter"></param>
    /// <param name="expectedMessageLength"></param>
    public COBSBufferWriter(IBufferWriter<byte> innerWriter)
    {
      _innerWriter = innerWriter;
      _buffer = ArrayPool<byte>.Shared.Rent(512);
      _bufferSize = _buffer.Length;
      _start = 0;
      _end = 0;
    }

    private int BytesCached
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _end - _start;
    }

    private int SpaceRemainingAtTail
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _bufferSize - _end;
    }

    private int TotalSpaceAvailable
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _bufferSize - _end + _start;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
      EnsureSpace(sizeHint);
      return new Memory<byte>(_buffer, _end, sizeHint);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
      EnsureSpace(sizeHint);
      return new Span<byte>(_buffer, _end, sizeHint);
    }

    /// <inheritdoc/>
    public void Advance(int count)
    {
      _end += count;
      Encode(false);
    }

    public void CommitMessage()
    {
      Encode(true);
      _start = 0;
      _end = 0;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
      ArrayPool<byte>.Shared.Return(_buffer);
    }

    private void EnsureSpace(int sizeHint)
    {
      if (sizeHint <= SpaceRemainingAtTail)
        return;

      if (sizeHint <= TotalSpaceAvailable)
      {
        new Span<byte>(_buffer, _start, BytesCached).CopyTo(_buffer);
        _end -= _start;
        _start = 0;
      }

      var spaceRequired = BytesCached + sizeHint;
      var newBuffer = ArrayPool<byte>.Shared.Rent(spaceRequired);
      new Span<byte>(_buffer, _start, BytesCached).CopyTo(newBuffer);
      ArrayPool<byte>.Shared.Return(_buffer);
      _buffer = newBuffer;
      _bufferSize = _buffer.Length;
      _end -= _start;
      _start = 0;
    }

    private void Encode(bool committing)
    {
      while (BytesCached > 0 && (committing || BytesCached >= 254))
      {
        var bytes = new Span<byte>(_buffer, _start, Min(254, BytesCached));
        var indexOfZero = bytes.IndexOf((byte)0);
        if (indexOfZero == -1)
        {
          _innerWriter.GetSpan(1)[0] = (byte)(bytes.Length + 1);
          _innerWriter.Advance(1);
          bytes.CopyTo(_innerWriter.GetSpan(bytes.Length));
          _innerWriter.Advance(bytes.Length);
          _start += bytes.Length;
        }
        else
        {
          _innerWriter.GetSpan(1)[0] = (byte)(indexOfZero + 1);
          _innerWriter.Advance(1);
          bytes.Slice(0, indexOfZero).CopyTo(_innerWriter.GetSpan(indexOfZero));
          _innerWriter.Advance(indexOfZero);
          _start += indexOfZero + 1;
          if (committing && BytesCached == 0)
          {
            _innerWriter.GetSpan(1)[0] = 1;
            _innerWriter.Advance(1);
          }
        }
      }

      if (committing)
      {
        _innerWriter.GetSpan(1)[0] = 0;
        _innerWriter.Advance(1);
      }
    }
  }
}
