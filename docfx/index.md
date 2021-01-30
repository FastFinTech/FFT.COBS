[!include[ReadMe](~/../README.md)]

# [Pipes](#tab/pipes)

The code sample below demonstrates writing to and reading from a `System.IO.Pipes.Pipe` using `FFT.COBS`.

**Writing**

The `GetPipeLoadedWithData()` method demonstrates writing COBS-encoded messages to a pipe (or any `IBufferWriter<byte>`). In particular, it shows that messages can be written just a part at a time, and the `CommitMessage()` method is used to finish the encoding for the current message, and get ready for the next message.

>[!WARNING]
> The `COBSBufferWriter` must be disposed when you're finished writing in order to have it return a buffer to the `ArrayPool`. Failing to dispose it will result in memory leak.

**Reading**

The remaining methods demonstrate how to consume COBS-encoded message from a `PipeReader`, including the various ways of pausing and resuming the message reading. Supported ways of pausing the reading are: 

- Using the `PipeReader.CancelPendingRead()` method.
- Using a `CancellationToken`.
- Using the `break` keyword.
- Throwing an exception.
  
>[!TIP]
>Using a `CancellationToken` is the only way to pause the reading whilst waiting for messages to arrive in the pipe. The other ways can be used to stop the reading only immediately after a message has been received.

>[!WARNING]
>The `Memory<byte>` variable returned by the iterator is ONLY valid inside the iterator block. You must copy or process the message data while still within the iterator block, or you will get unexpected data that you need to explain to your colleagues!

[!code-csharp[Main](~/../src/FFT.COBS.Examples/PipesExample.cs)]

# [Streams](#tab/streams)

Streams are not implemented yet.

***