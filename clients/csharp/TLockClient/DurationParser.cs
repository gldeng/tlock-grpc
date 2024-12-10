namespace TLockClient;

using System;
using System.Text.RegularExpressions;

public class DurationParseError : Exception
{
    public DurationParseError(string? message) : base(message)
    {
    }
}

public class DurationParser
{
    private static readonly string TimeUnits = "smhMdwy";

    public static TimeSpan ParseDurationsAsSeconds(DateTime start, string input)
    {
        // Check for valid input format
        var pattern = $"^([0-9]+[{TimeUnits}])+$";
        if (!Regex.IsMatch(input, pattern) || Regex.Matches(input, pattern).Count != 1)
        {
            throw new DurationParseError("Invalid duration format");
        }

        // Calculate total duration
        var totalDuration = TimeSpan.Zero;
        foreach (var timeUnit in TimeUnits)
        {
            var unitPattern = $@"[0-9]+{timeUnit}";
            var matches = Regex.Matches(input, unitPattern);
            if (matches.Count > 1)
            {
                throw new DurationParseError("Duplicate duration");
            }

            if (matches.Count == 0)
            {
                continue;
            }

            var match = matches[0].Value;
            if (int.TryParse(match.Substring(0, match.Length - 1), out int durationLength))
            {
                totalDuration += DurationFrom(start, durationLength, timeUnit);
            }
            else
            {
                throw new DurationParseError("Invalid number format");
            }
        }

        return totalDuration;
    }

    private static TimeSpan DurationFrom(DateTime start, int value, char duration)
    {
        switch (duration)
        {
            case 's':
                return TimeSpan.FromSeconds(value);
            case 'm':
                return TimeSpan.FromMinutes(value);
            case 'h':
                return TimeSpan.FromHours(value);
            case 'd':
                return start.AddDays(value) - start;
            case 'w':
                return start.AddDays(value * 7) - start;
            case 'M':
                return start.AddMonths(value) - start;
            case 'y':
                return start.AddYears(value) - start;
            default:
                return TimeSpan.Zero;
        }
    }
}