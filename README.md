# FFT.COBS

[![Source code](https://img.shields.io/static/v1?style=flat&label=&message=Source%20Code&logo=read-the-docs&color=informational)](https://github.com/FastFinTech/FFT.COBS)
[![NuGet package](https://img.shields.io/nuget/v/FFT.COBS.svg)](https://nuget.org/packages/FFT.COBS)
[![Full documentation](https://img.shields.io/static/v1?style=flat&label=&message=Documentation&logo=read-the-docs&color=green)](https://fastfintech.github.io/FFT.COBS/)

**Consistent Overhead Byte Stuffing (COBS)** is an algorithm you can use to frame messages without ambiguity and without the overhead required by adding a length prefix.

[Read the full COBS spec on wikipedia](https://en.wikipedia.org/wiki/Consistent_Overhead_Byte_Stuffing)

`FFT.COBS` provides a fast, allocation-free implementation of COBS encoding and decoding for pipes and streams.

### Pipes

The `COBSBufferWriter` wraps an `IBufferWriter<byte>`. Write messages to it just as you would to the `IBufferWriter<byte>` normally, and then call the `COBSBufferWriter.Commit()` method to complete framing and sending the message.

The `COBSBufferReader` is a static class that provides the `IAsyncEnumerable<ReadOnlySequence<byte>> ReadMessages(this PipeReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)` extension method. You can use it to wrap a `System.IO.Pipes.PipeReader`.

// TODO: Add a link to example code.

### Streams

Not implemented yet.