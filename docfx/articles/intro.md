# TimeZoneCalculator

### Api specification links

 The [TimeZoneCalculator](xref:FFT.TimeStamps.TimeZoneCalculator) class helps you to: 
 
 - Access fast, utilities for converting values between timezones
 - Convert `DateTime` and `DateTimeOffset` values to `TimeStamp` and vice-versa.

### Basic usage

The static [`TimeZoneCalculator.Convert`](xref:FFT.TimeStamps.TimeZoneCalculator.Convert(System.TimeZoneInfo,System.TimeZoneInfo,System.Int64)) method allows you to convert times directly from one timezone to another. This is the slowest utility method available in the library (but much faster than you can find anywhere else), and you would use it in situations where its either not used in a critical hot path, or where you don't know in advance which timezones will be converted.

### TimeZoneCalculator.GetSegment

The [`TimeZoneCalculator.TimeZoneSegment`](xref:FFT.TimeStamps.TimeZoneCalculator.TimeZoneSegment) class is returned by the [`TimeZoneCalculator.GetSegment(TimeStamp time)`](xref:FFT.TimeStamps.TimeZoneCalculator.GetSegment(FFT.TimeStamps.TimeStamp)) method and the [`TimeZoneCalculator.GetSegment(long ticks, TimeKind ticksKind)`](xref:FFT.TimeStamps.TimeZoneCalculator.GetSegment(System.Int64,FFT.TimeStamps.TimeKind)) method.

It provides you with a lot of helpful information about a specific period of time in which a timezone's offset remains constant. By obtaining and caching a `TimeZoneCalculator.TimeZoneSegment` you can speed up your code and create more sophisticated logic.

- The `IsInvalid` property tells you if the current period of time is actually skipped over by the timezone clock as it flies forward. Typically this happens as Daylight Savings starts.
- The `IsAmbiguous` property tells you if the current period of time happens twice, as the clock flies backward and then repeats the time period again. Typically this happens when Daylights Savings comes to an end.
- The `OffsetTicks` property tells you the timezone's offset from Utc timezone during the current period.
- The `Next` and `Previous` properties allow you to access the next and previous periods of time without accessing the `TimeZoneCalculator.GetSegment` method again - this can make your code a lot faster!
- The `StartTicks` and `EndTicks` property tells you the start and end of the time period, expressed in ticks in either Utc timezone, or the specific timezone.
- The `SegmentKind` property tells you which timezone is being used by the `StartTicks` and `EndTicks` properties.

>[!TIP]
> The period of time represented by the `TimeZoneSegment` is EXCLUSIVE of the `TimeZoneSegment.EndTicks`. In fact, `TimeZoneSegment.EndTicks` is actually the same as the `TimeZoneSegment.StartTicks` of the next segment!

### Basic examples

[!code-csharp[TimeZoneOffsetCalculator example code](../../src/FFT.TimeStamps.Examples/TimeZoneCalculatorExamples.cs)]

### Advanced examples

Checkout the code for the [`ToUtcIterator`](https://github.com/FastFinTech/FFT.TimeStamps/blob/main/src/FFT.TimeStamps/ConversionIterators.ToUtcIterator.cs) class for an example of advanced `TimeZoneCalculator.TimeZoneSegment` usage.

### Benchmarking

The slowest possible conversion methods in [`TimeZoneCalculator`](xref:FFT.TimeStamps.TimeZoneCalculator) allows you to perform timezone conversion calculations twice as fast as the in-built methods supplied by the .net framework.

However, you can also use the same techniques as the Converstion iterators to achieve results more than [32 times faster](conversionIterators.md#benchmarking).

|                 Method |     Mean |   Error |  StdDev |
|----------------------- |---------:|--------:|--------:|
|       WithTimeZoneInfo | 379.9 ns | 3.10 ns | 2.75 ns |
| WithTimeZoneCalculator | 125.5 ns | 1.46 ns | 1.36 ns |

[!code-csharp[TimeZoneCalculator benchmark code](../../src/FFT.TimeStamps.Benchmarks/SimpleConversions.cs)]
