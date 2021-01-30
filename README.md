# FFT.COBS

[![Source code](https://img.shields.io/static/v1?style=flat&label=&message=Source%20Code&logo=read-the-docs&color=informational)](https://github.com/FastFinTech/FFT.COBS)
[![NuGet package](https://img.shields.io/nuget/v/FFT.COBS.svg)](https://nuget.org/packages/FFT.COBS)
[![Full documentation](https://img.shields.io/static/v1?style=flat&label=&message=Documentation&logo=read-the-docs&color=green)](https://fastfintech.github.io/FFT.COBS/)

`FFT.COBS` provides a fast, allocation-free implementation of **Consistent Overhead Byte Stuffing (COBS)** encoding and decoding for pipes and streams.

COBS is an algorithm you can use to frame messages without ambiguity and without the performance overhead required by the common method of buffering a message and prepending a length prefix.

[Read the full COBS spec on wikipedia](https://en.wikipedia.org/wiki/Consistent_Overhead_Byte_Stuffing)