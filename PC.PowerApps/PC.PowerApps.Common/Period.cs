using PC.PowerApps.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PC.PowerApps.Common
{
    public class Period
    {
        public DateTime From { get; }
        public DateTime Till { get; }

        public Period(DateTime? from, DateTime? till)
        {
            From = (from ?? DateTime.MinValue).Date;
            Till = (till ?? DateTime.MaxValue).Date;

            if (From > Till)
            {
                throw new ArgumentException("Period from must be less than or equal to till.");
            }
        }

        public int CalendarMonths => (Till.Year - From.Year) * 12 + Till.Month - From.Month + 1;

        public override string ToString()
        {
            return $"{From:yyyy-MM-dd} - {Till:yyyy-MM-dd}";
        }

        public Period ToCalendarMonths()
        {
            DateTime newFrom = From.GetFirstDayOfMonth();
            DateTime newTill = Till.GetLastDayOfMonth();
            return new(newFrom, newTill);
        }

        private List<Period> MergeWith(Period otherPeriod)
        {
            TimeSpan oneDay = new(1, 0, 0, 0);

            if (From - otherPeriod.Till > oneDay)
            {
                return new() { otherPeriod, this };
            }

            if (otherPeriod.From - Till > oneDay)
            {
                return new() { this, otherPeriod };
            }

            DateTime? newFrom = new[] { From, otherPeriod.From }.Min();
            DateTime? newTill = new[] { Till, otherPeriod.Till }.Max();
            return new() { new(newFrom, newTill) };
        }

        public static List<Period> Merge(IEnumerable<Period> sortedPeriods)
        {
            List<Period> results = new();
            Period currentPeriod = null;
            foreach (Period period in sortedPeriods)
            {
                if (currentPeriod == null)
                {
                    currentPeriod = period;
                }
                else
                {
                    List<Period> periodUnion = currentPeriod.MergeWith(period);
                    if (periodUnion.Count > 1)
                    {
                        results.Add(periodUnion[0]);
                        currentPeriod = periodUnion[1];
                    }
                    else
                    {
                        currentPeriod = periodUnion[0];
                    }
                }
            }
            if (currentPeriod != null)
            {
                results.Add(currentPeriod);
            }
            return results;
        }

        private List<Period> Subtract(Period otherPeriod)
        {
            List<Period> periods = new();

            if (otherPeriod.From > Till || otherPeriod.Till < From)
            {
                periods.Add(this);
                return periods;
            }

            if (otherPeriod.From > From)
            {
                Period newPeriod = new(From, otherPeriod.From.AddDays(-1));
                periods.Add(newPeriod);
            }

            if (otherPeriod.Till < Till)
            {
                Period newPeriod = new(otherPeriod.Till.AddDays(1), Till);
                periods.Add(newPeriod);
            }

            return periods;
        }

        public static List<Period> Subtract(List<Period> sourcePeriods, IEnumerable<Period> periodsToSubtract)
        {
            List<Period> subtractAllResults = sourcePeriods;
            foreach (Period periodToSubtract in periodsToSubtract)
            {
                List<Period> subtractOneResults = new();
                foreach (Period subtractAllResult in subtractAllResults)
                {
                    List<Period> subtractOneResult = subtractAllResult.Subtract(periodToSubtract);
                    subtractOneResults.AddRange(subtractOneResult);
                }
                subtractAllResults = subtractOneResults;
            }
            return subtractAllResults;
        }

        public Period Intersect(Period otherPeriod)
        {
            if (otherPeriod.Till < From || otherPeriod.From > Till)
            {
                return null;
            }

            DateTime newFrom = new[] { From, otherPeriod.From }.Max();
            DateTime newTill = new[] { Till, otherPeriod.Till }.Min();
            return new(newFrom, newTill);
        }

        public static List<Period> Intersect(IEnumerable<Period> periods, Period otherPeriod)
        {
            return periods
                .Select(p => p.Intersect(otherPeriod))
                .Where(p => p != null)
                .ToList();
        }

        public Period GetFirstCalendarMonth()
        {
            DateTime newFrom = From.GetFirstDayOfMonth();
            DateTime newTill = From.GetLastDayOfMonth();
            return new(newFrom, newTill);
        }

        public override bool Equals(object obj)
        {
            return obj is Period period &&
                   From == period.From &&
                   Till == period.Till;
        }

        public override int GetHashCode()
        {
            int hashCode = 1321290443;
            hashCode = hashCode * -1521134295 + From.GetHashCode();
            hashCode = hashCode * -1521134295 + Till.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Period left, Period right)
        {
            return EqualityComparer<Period>.Default.Equals(left, right);
        }

        public static bool operator !=(Period left, Period right)
        {
            return !(left == right);
        }
    }
}
