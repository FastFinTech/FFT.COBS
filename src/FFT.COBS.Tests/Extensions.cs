// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.COBS.Tests
{
  using System;
  using System.Buffers;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  internal static class Extensions
  {
    public static ReadOnlySequence<byte> AsReadOnlySequence(this byte[] buffer)
      => new ReadOnlySequence<byte>(buffer, 0, buffer.Length);

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

    public static void SequenceEqual(this Assert assert, Span<byte> s1, Span<byte> s2)
    {
      Assert.IsTrue(s1.Length == s2.Length && s1.IndexOf(s2) == 0);
    }
  }
}
