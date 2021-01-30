// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS.Examples
{
  using System;
  using System.Buffers;
  using System.IO.Pipelines;
  using System.Threading;
  using System.Threading.Tasks;

  internal class PipesExample : IExample
  {
    public string Name => "Simple Pipe Reading Writing Example";

    public async ValueTask RunAsync()
    {
      await DemonstrateSimpleReadAsync();
      await DemonstrateReadStoppingAndResumingAsync();
    }

    private static async Task DemonstrateSimpleReadAsync()
    {
      var pipe = GetPipeLoadedWithData();
      await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages())
      {
        Console.WriteLine($"Received a message of length {message.Length}");
      }

      // If you need to "complete" the reader, you need to do that yourself because the
      // "ReadCOBSMessage" enumerator does not.
      await pipe.Reader.CompleteAsync();
    }

    private static async Task DemonstrateReadStoppingAndResumingAsync()
    {
      var messageCount = 0;
      var pipe = GetPipeLoadedWithData();

      // Use the "CancelPendingRead()" method to stop reading after two messages.
      await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages())
      {
        messageCount++;
        if (messageCount == 2)
          pipe.Reader.CancelPendingRead();
      }

      // Resume reading, and use a cancellation token to stop reading after the next two messages.
      using var cts = new CancellationTokenSource();
      await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages().WithCancellation(cts.Token))
      {
        messageCount++;
        if (messageCount == 4)
          cts.Cancel();
      }

      // Resume reading, and stop reading with a "break" statement.
      await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages())
      {
        messageCount++;
        if (messageCount == 6)
          break;
      }

      // Resume reading, and stop reading with an exception which we swallow.
      try
      {
        await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages())
        {
          messageCount++;
          if (messageCount == 8)
            throw new Exception("boom");
        }
      }
      catch
      {
      }

      // Resume reading, all the way to the end.
      await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages())
      {
        messageCount++;
      }

      // If you need to "complete" the reader, you need to do that yourself because the
      // "ReadCOBSMessage" enumerator does not.
      await pipe.Reader.CompleteAsync();
    }

    /// <summary>
    /// Writes random COBS-endoded messages to a pipe,
    /// marks the pipe writer complete, and returns the pipe.
    /// </summary>
    private static Pipe GetPipeLoadedWithData()
    {
      // Create a COBSBufferWriter that wraps another IBufferWriter<byte>
      // Note that it is IDisposable, so we have handled that with a using expression.
      var pipe = new Pipe();
      using var cobsWriter = new COBSBufferWriter(pipe.Writer);

      // Get a bunch of messages that need to be sent with COBS encoding.
      foreach (ReadOnlySequence<byte> message in Utilities.GetRandomMessages(numMessages: 10..20, numSegmentsPerMessage: 1..100))
      {
        // Demonstrates the fact that messages can be written a segment at a time,
        // just as you would with any IBufferWriter<byte>.
        // No need to wait until the entire message is available before writing what parts you have.
        foreach (ReadOnlyMemory<byte> segment in message)
        {
          var span = cobsWriter.GetSpan(segment.Length);
          segment.Span.CopyTo(span);
          cobsWriter.Advance(segment.Length);
        }

        // Commit each message once it has been written.
        // The allows the cobsWriter to perform end-of-message
        // encoding and gets it ready for the next message.
        cobsWriter.CommitMessage();
      }

      // Optionally mark the pipe writer as complete, or you can go ahead and continue
      // using it to send more data any way you like.
      pipe.Writer.Complete();

      return pipe;
    }
  }
}
