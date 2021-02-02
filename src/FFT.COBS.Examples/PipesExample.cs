// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS.Examples
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.IO.Pipelines;
  using System.Threading;
  using System.Threading.Tasks;

  internal class PipesExample : IExample
  {
    public string Name => "Simple Pipe Reading Writing Example";

    public async ValueTask RunAsync()
    {
      await SimpleReadAsync();
      await ReadStoppingAndResumingAsync();
      await ManualIterationAsync();
    }

    /// <summary>
    /// Demonstrates simple reading of all messages in the pipe
    /// followed by completion of the pipe reader.
    /// </summary>
    private static async Task SimpleReadAsync()
    {
      var pipe = GetPipeLoadedWithData();
      await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages())
      {
        // Message data is invalid outside this iterator block.
        // The same slice of memory will be overwritten for the next message,
        // so you need to copy or process it right here.
        Console.WriteLine($"Received a message of length {message.Length}");
      }

      // If you need to "complete" the reader, you need to do that yourself because the
      // "ReadCOBSMessages" enumerator does not.
      await pipe.Reader.CompleteAsync();
    }

    /// <summary>
    /// Demonstrates how you can read some but not all messages from a pipe,
    /// stop, and then resume reading more messages.
    /// Each of the techniques for exiting a foreach loop are shown.
    /// </summary>
    private static async Task ReadStoppingAndResumingAsync()
    {
      var messageCount = 0;
      var pipe = GetPipeLoadedWithData();

      // In each of the examples below, the "foreach" language feature ensures the
      // reader's enumerator is properly disposed.

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
      catch { }

      // Resume reading, all the way to the end.
      await foreach (Memory<byte> message in pipe.Reader.ReadCOBSMessages())
      {
        messageCount++;
      }

      // If you need to "complete" the pipe reader, you need to do that yourself because the
      // "ReadCOBSMessages" enumerator does not.
      await pipe.Reader.CompleteAsync();
    }

    /// <summary>
    /// Demonstrates manual use of the COBS reader enumerator.
    /// Most important to note is that it MUST BE DISPOSED to avoid
    /// creating memory leaks.
    /// </summary>
    private static async Task ManualIterationAsync()
    {
      var pipe = GetPipeLoadedWithData();

      // In this example, we have a using statement to ensure the enumerator
      // is correctly disposed when we are finished with it.
      await using IAsyncEnumerator<Memory<byte>> enumerator = pipe.Reader.ReadCOBSMessages().GetAsyncEnumerator();
      while (await enumerator.MoveNextAsync())
      {
        Memory<byte> message = enumerator.Current;
      }
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
      using var cobsWriter = new COBSWriterBuffer(pipe.Writer);

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
