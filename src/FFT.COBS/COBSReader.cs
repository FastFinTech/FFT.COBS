// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.IO;
  using System.IO.Pipelines;
  using System.Runtime.CompilerServices;
  using System.Threading;
  using System.Threading.Tasks;

  /// <summary>
  /// Provides methods for reading COBS-encoded messages from a <see cref="PipeReader"/>.
  /// https://en.wikipedia.org/wiki/Consistent_Overhead_Byte_Stuffing.
  /// </summary>
  public static class COBSReader
  {
    /// <summary>
    /// Reads COBS-encoded messages from <paramref name="reader"/> until all data has been consumed and the pipe reader is completed,
    /// or until the enumeration is canceled by any of the methods demonstrated in the FFT.COBS.Examples project.
    /// https://en.wikipedia.org/wiki/Consistent_Overhead_Byte_Stuffing.
    /// </summary>
    /// <param name="reader">The <see cref="PipeReader"/> that COBS-encoded messages will be read from.</param>
    /// <param name="cancellationToken">
    /// When canceled, the message reading stops. No exception is thrown to the using code.
    /// </param>
    public static async IAsyncEnumerable<Memory<byte>> ReadCOBSMessages(this PipeReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      var buffer = ArrayPool<byte>.Shared.Rent(1024);
      try
      {
        while (true)
        {
          ReadResult readResult;
          try
          {
            readResult = await reader.ReadAsync(cancellationToken);
          }
          catch (OperationCanceledException)
          {
            // Cancellation token was canceled.
            // Don't rethrow the exception to the calling code, just stop reading and end the enumeration.
            yield break;
          }

          // User code called reader.CancelPendingRead()
          if (readResult.IsCanceled)
            yield break;

          var readBuffer = readResult.Buffer;
          var zeroBytePosition = readBuffer.PositionOf<byte>(0);
          if (zeroBytePosition.HasValue)
          {
            var endOfEncodedData = readBuffer.GetPosition(1, zeroBytePosition.Value);
            var encodedData = readBuffer.Slice(0, endOfEncodedData);
            if (encodedData.Length > buffer.Length)
            {
              ArrayPool<byte>.Shared.Return(buffer);
              buffer = ArrayPool<byte>.Shared.Rent((int)encodedData.Length);
            }

            var length = Decode(buffer, encodedData);
            reader.AdvanceTo(endOfEncodedData);
            yield return new Memory<byte>(buffer, 0, length);
          }
          else
          {
            reader.AdvanceTo(readBuffer.Start, readBuffer.End);
            if (readResult.IsCompleted) yield break;
          }
        }
      }
      finally
      {
        ArrayPool<byte>.Shared.Return(buffer);
      }

      static int Decode(byte[] buffer, ReadOnlySequence<byte> encodedData)
      {
        var position = 0;
        var insertZero = false;
        var reader = new SequenceReader<byte>(encodedData);
        while (true)
        {
          if (!reader.TryRead(out var header))
          {
            throw new Exception("Issue reading COBS buffer.");
          }

          if (header == 0)
          {
            if (!reader.End)
            {
              throw new Exception("Issue reading COBS buffer.");
            }

            return position;
          }

          if (insertZero)
          {
            buffer[position++] = 0;
          }

          if (header > 1)
          {
            if (!reader.TryCopyTo(buffer.AsSpan().Slice(position, header - 1)))
            {
              throw new Exception("Issue reading COBS buffer.");
            }

            // because the TryCopy method above does not advance the reader.
            reader.Advance(header - 1);

            position += header - 1;
          }

          insertZero = header != 255;
        }
      }
    }

    /// <summary>
    /// Reads COBS-encoded messages from <paramref name="stream"/> until all data has been consumed,
    /// or until the enumeration is canceled by any of the methods demonstrated in the FFT.COBS.Examples project.
    /// https://en.wikipedia.org/wiki/Consistent_Overhead_Byte_Stuffing.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> that COBS-encoded messages will be read from.</param>
    /// <param name="cancellationToken">
    /// When canceled, the message reading stops. No exception is thrown to the using code.
    /// </param>
    public static async IAsyncEnumerable<Memory<byte>> ReadCOBSMessages(this Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var reader = PipeReader.Create(stream);
      await foreach (var message in reader.ReadCOBSMessages(cancellationToken))
        yield return message;
    }
  }
}
