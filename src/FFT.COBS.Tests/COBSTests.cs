// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS.Tests
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.IO.Pipelines;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  [TestClass]
  public class COBSTests
  {
    private static readonly List<(byte[] Data, byte[] Encoded)> _tests;

    static COBSTests()
    {
      var testDefinitions = new (string Data, string Encoded)[]
      {
        ("00",                    "01 01 00"),
        ("00 00",                 "01 01 01 00"),
        ("11 22 00 33",           "03 11 22 02 33 00"),
        ("11 22 33 44",           "05 11 22 33 44 00"),
        ("11 00 00 00",           "02 11 01 01 01 00"),
        ("01 02 03 ... FD FE",    "FF 01 02 03 ... FD FE 00"),
        ("00 01 02 ... FC FD FE", "01 FF 01 02 ... FC FD FE 00"),
        ("01 02 03 ... FD FE FF", "FF 01 02 03 ... FD FE 02 FF 00"),
        ("02 03 04 ... FE FF 00", "FF 02 03 04 ... FE FF 01 01 00"),
        ("03 04 05 ... FF 00 01", "FE 03 04 05 ... FF 02 01 00"),
      };
      _tests = testDefinitions.Select(x => (Create(x.Data), Create(x.Encoded))).ToList();

      static byte[] Create(string format)
      {
        var result = new List<byte>();
        byte value = 0;
        var items = format.Split(' ').Cast<string>().GetEnumerator();
        while (items.MoveNext())
        {
          if (items.Current == "...")
          {
            items.MoveNext();
            var nextValue = Convert.ToByte(items.Current, 16);
            for (value++; value <= nextValue; value++)
            {
              result.Add(value);
              if (value == 255)
                break;
            }
          }
          else
          {
            value = Convert.ToByte(items.Current, 16);
            result.Add((byte)value);
          }
        }

        return result.ToArray();
      }
    }

    [TestMethod]
    public async Task PipeEncoding_OneMessageAtATime()
    {
      foreach (var (data, encoded) in _tests)
      {
        var pipe = new Pipe();
        using var writer = new COBSWriterBuffer(pipe.Writer);
        data.AsSpan().CopyTo(writer.GetSpan(data.Length));
        writer.Advance(data.Length);
        writer.CommitMessage();
        pipe.Writer.Complete();
        var readResult = await pipe.Reader.ReadAsync();
        Assert.IsTrue(readResult.IsCompleted);
        var readBuffer = readResult.Buffer;
        Assert.That.SequenceEqual(encoded.AsReadOnlySequence(), readBuffer.Slice(0, encoded.Length));
        pipe.Reader.Complete();
      }
    }

    [TestMethod]
    public async Task PipeEncoding_AllMessagesTogether()
    {
      var pipeReader = GetLoadedPipeReader();

      var readResult = await pipeReader.ReadAsync();
      Assert.IsTrue(readResult.IsCompleted);
      var readBuffer = readResult.Buffer;
      foreach (var (_, encoded) in _tests)
      {
        Assert.That.SequenceEqual(encoded.AsReadOnlySequence(), readBuffer.Slice(0, encoded.Length));
        readBuffer = readBuffer.Slice(encoded.Length);
      }

      Assert.AreEqual(0, readBuffer.Length);
    }

    [TestMethod]
    public async Task PipeEncoding_AndDecoding()
    {
      var pipeReader = GetLoadedPipeReader();
      var enumerator = pipeReader.ReadCOBSMessages(default).GetAsyncEnumerator();
      foreach (var (data, _) in _tests)
      {
        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.That.SequenceEqual(data, enumerator.Current.Span);
      }

      Assert.IsFalse(await enumerator.MoveNextAsync());
    }

    [TestMethod]
    public async Task PipeEncoding_CancellationTokenAndResumeReading()
    {
      var pipeReader = GetLoadedPipeReader();

      var count = 0;
      var messages = new List<byte[]>();
      using var cts = new CancellationTokenSource();

      // make sure that cancellation works.
      await foreach (var message in pipeReader.ReadCOBSMessages().WithCancellation(cts.Token))
      {
        messages.Add(message.ToArray());
        if (count++ == 2)
          cts.Cancel();
      }

      Assert.AreEqual(3, messages.Count);

      // and the other kind of cancellation
      await foreach (var message in pipeReader.ReadCOBSMessages())
      {
        messages.Add(message.ToArray());
        if (count++ == 4)
          pipeReader.CancelPendingRead();
      }

      Assert.AreEqual(5, messages.Count);

      // now resume reading the rest of the messages (without a cancellation token)
      await foreach (var message in pipeReader.ReadCOBSMessages())
      {
        messages.Add(message.ToArray());
      }

      // make sure that all messages were read correctly.
      for (var i = 0; i < _tests.Count; i++)
      {
        Assert.IsTrue(_tests[i].Data.SequenceEqual(messages[i]));
      }
    }

    private static PipeReader GetLoadedPipeReader()
    {
      var pipe = new Pipe();
      using var writer = new COBSWriterBuffer(pipe.Writer);
      foreach (var (data, _) in _tests)
      {
        data.AsSpan().CopyTo(writer.GetSpan(data.Length));
        writer.Advance(data.Length);
        writer.CommitMessage();
      }

      pipe.Writer.Complete();
      return pipe.Reader;
    }
  }
}
