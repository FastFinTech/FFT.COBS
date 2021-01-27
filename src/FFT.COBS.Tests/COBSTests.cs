using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFT.COBS.Tests
{
  [TestClass]
  public class COBSTests
  {
    private static readonly List<(byte[] Data, byte[] Encoded)> _tests;

    static COBSTests()
    {
      _tests = new();
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
        using var writer = new COBSBufferWriter(pipe.Writer);
        data.AsSpan().CopyTo(writer.GetSpan(data.Length));
        writer.Advance(data.Length);
        writer.CommitMessage();
        pipe.Writer.Complete();
        var readResult = await pipe.Reader.ReadAsync();
        Assert.IsTrue(readResult.IsCompleted); // or our assumption about how the pipe works is wrong.
        var readBuffer = readResult.Buffer;
        Assert.That.SequenceEqual(encoded.AsReadOnlySequence(), readBuffer.Slice(0, encoded.Length));
        pipe.Reader.Complete();
      }
    }

    [TestMethod]
    public async Task PipeEncoding_AllMessagesTogether()
    {
      var pipe = new Pipe();
      using var writer = new COBSBufferWriter(pipe.Writer);
      foreach (var (data, _) in _tests)
      {
        data.AsSpan().CopyTo(writer.GetSpan(data.Length));
        writer.Advance(data.Length);
        writer.CommitMessage();
      }

      pipe.Writer.Complete();
      var readResult = await pipe.Reader.ReadAsync();
      Assert.IsTrue(readResult.IsCompleted); // or our assumption about how the pipe works is wrong.
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
      var pipe = new Pipe();
      using var writer = new COBSBufferWriter(pipe.Writer);
      foreach (var (data, _) in _tests)
      {
        data.AsSpan().CopyTo(writer.GetSpan(data.Length));
        writer.Advance(data.Length);
        writer.CommitMessage();
      }

      pipe.Writer.Complete();
      var enumerator = pipe.Reader.ReadMessages(default).GetAsyncEnumerator();
      foreach (var (data, _) in _tests)
      {
        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.That.SequenceEqual(new ReadOnlySequence<byte>(data), enumerator.Current);
      }

      Assert.IsFalse(await enumerator.MoveNextAsync());
    }
  }

  public static class Extensions
  {
    public static void SequenceEqual(this Assert assert, ReadOnlySequence<byte> s1, ReadOnlySequence<byte> s2)
    {
      var reader1 = new SequenceReader<byte>(s1);
      var reader2 = new SequenceReader<byte>(s2);
      while (reader1.TryRead(out var b1))
      {
        Assert.IsTrue(reader2.TryRead(out var b2));
        Assert.AreEqual(b1, b2);
      }

      Assert.IsFalse(reader2.TryRead(out _));
    }

    public static ReadOnlySequence<byte> AsReadOnlySequence(this byte[] buffer)
      => new ReadOnlySequence<byte>(buffer, 0, buffer.Length);
  }
}
