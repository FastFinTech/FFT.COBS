// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS.Examples
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using Nerdbank.Streams;

  internal static class Utilities
  {
    private static readonly Random _rand = new Random();

    public static IEnumerable<ReadOnlySequence<byte>> GetRandomMessages(Range numMessages, Range numSegmentsPerMessage)
    {
      var actualNumMessages = _rand.Next(numMessages.Start.Value, numMessages.End.Value);
      for (var messageId = 0; messageId < actualNumMessages; messageId++)
      {
        using var sequence = new Sequence<byte>();
        var actualNumSegments = _rand.Next(numSegmentsPerMessage.Start.Value, numSegmentsPerMessage.End.Value);
        for (var j = 0; j < actualNumSegments; j++)
        {
          var segmentLength = _rand.Next(1, 1024);
          var buffer = new byte[segmentLength];
          _rand.NextBytes(buffer);
          sequence.Append(buffer);
        }

        yield return sequence.AsReadOnlySequence;
      }
    }
  }
}
