# QuickAnswer

A little REPL to help quickly answer the stuff I find myself repeatedly googling.

- Unit conversions (distance, mass, temperature, etc)
- Currency conversions
- Date and timezone math
- Basic math...

Each answer returned by the REPL can be referenced in subsequent queries.
In the below example, each prompt is numbered, and the answers returned
for each query can be referenced with `varN` notation (`var0`, `var1`, etc):

```
0> 13 days from now
4/08/2020 12:00:00 AM

1> Easter 2021
4/04/2021 12:00:00 AM

2> var1.DayOfWeek
Sunday

3> 500 THB to GBP
£ 12.47 GBP

4> var3 * 7
87.29

5> 25 C to F
77

6> 37 inches to cm
93.98

7> 10pm bangkok time in indiana time
22/07/2020 10:00:00 AM -05:00

8> 8pm sydney time in tokyo time
22/07/2020 7:00:00 PM +09:00

9> new[] { var7, var8 }.Min()
22/07/2020 7:00:00 PM +09:00
```

Each answer has a static data type that correponds to a .NET type, and full C# syntax is supported
in queries. In the above example, answer 0's data type is DateTime, answer 4's data type is double,
and answer 9's data type is DateTimeOffset.
