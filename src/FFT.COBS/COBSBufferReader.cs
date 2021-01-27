// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.IO.Pipelines;
  using System.Runtime.CompilerServices;
  using System.Threading;

  public static class COBSBufferReader
  {
    public static async IAsyncEnumerable<ReadOnlySequence<byte>> ReadMessages(this PipeReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var buffer = ArrayPool<byte>.Shared.Rent(1024);
      try
      {
        while (true)
        {
          var readResult = await reader.ReadAsync(cancellationToken);
          if (readResult.IsCanceled) yield break;
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
            yield return new ReadOnlySequence<byte>(buffer, 0, length);
            reader.AdvanceTo(endOfEncodedData);
          }
          else
          {
            if (readResult.IsCompleted) yield break;
            reader.AdvanceTo(readBuffer.Start, readBuffer.End);
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
  }
}
