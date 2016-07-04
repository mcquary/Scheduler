using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentScheduler
{
    public static class Scheduler
    {
        #region Internal Functions
        internal static bool IsValidDateRequest(Rule rule, DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                return false;
            if (startDate >= rule.ExpirationDate)
                return false;
            if (endDate <= rule.EffectiveDate)
                return false;
            return true;
        }
        internal static List<DateTime> TrimInvalidDates(DateTime startDate, DateTime endDate, List<DateTime> trimList)
        {
            return trimList.Where(x => x >= startDate && x <= endDate).ToList();
        }
        internal static DateTime GetStartOfWeek(DateTime date)
        {
            //MS day of week goes Sunday(0) - Saturday(6)
            int currentWeekday = (int)date.DayOfWeek;
            return date.AddDays(-1 * currentWeekday);
        }
        internal static List<DateTime> GetWeekMatches(WeekdayFlags flag, DateTime startDate)
        {
            var result = new List<DateTime>();
            var datePool = new List<DateTime>();

            for (int i = 0; i < 7; i++)
                datePool.Add(startDate.AddDays(i));

            // Startdate is Sunday
            foreach (WeekdayFlags item in Enum.GetValues(typeof(WeekdayFlags)))
            {
                if (item == WeekdayFlags.None)
                    continue;
                if (flag.HasFlag(item))
                {
                    var dow = GetDayOfWeekFlagEquiv(item);
                    result.Add(datePool.Single(x => x.DayOfWeek == dow));
                }
            }
            return result;
        }
        internal static DayOfWeek GetDayOfWeekFlagEquiv(WeekdayFlags dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case WeekdayFlags.Monday:
                    return DayOfWeek.Monday;
                case WeekdayFlags.Tuesday:
                    return DayOfWeek.Tuesday;
                case WeekdayFlags.Wednesday:
                    return DayOfWeek.Wednesday;
                case WeekdayFlags.Thursday:
                    return DayOfWeek.Thursday;
                case WeekdayFlags.Friday:
                    return DayOfWeek.Friday;
                case WeekdayFlags.Saturday:
                    return DayOfWeek.Saturday;
                case WeekdayFlags.Sunday:
                    return DayOfWeek.Sunday;
                default:
                    return DayOfWeek.Sunday;
            }
        }
        internal static WeekdayFlags GetWeekdayFlagEquiv(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    return WeekdayFlags.Monday;
                case DayOfWeek.Tuesday:
                    return WeekdayFlags.Tuesday;
                case DayOfWeek.Wednesday:
                    return WeekdayFlags.Wednesday;
                case DayOfWeek.Thursday:
                    return WeekdayFlags.Thursday;
                case DayOfWeek.Friday:
                    return WeekdayFlags.Friday;
                case DayOfWeek.Saturday:
                    return WeekdayFlags.Saturday;
                case DayOfWeek.Sunday:
                    return WeekdayFlags.Sunday;
                default:
                    return WeekdayFlags.None;
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules">
        ///     List of Rules to process
        /// </param>
        /// <param name="startDate">
        /// Start Date for extendend filtering
        /// </param>
        /// <param name="endDate">
        /// End Date for extended filtering
        /// </param>
        /// <returns>
        /// A list of all matching dates. Can contain duplicates if rules overlap.
        /// </returns>
        public static List<DateTime> GetRelevantDateSets(List<Rule> rules, DateTime startDate, DateTime endDate)
        {
            var results = new List<DateTime>();

            foreach (var rule in rules)
            {
                var items = GetRelevantDateSet(rule, startDate, endDate);
                if (items.Count > 1)
                    results.AddRange(items);
                else if (items.Count == 1)
                    results.Add(items[0]);
            }

            return results;
        }

        /// <summary>
        /// General Function for processing a single Rule, requires the rule have an EffectiveDate and ExpirationDate set
        /// </summary>
        /// <param name="rule">
        ///     The Rule for which this schedule should be generated from
        /// </param>
        /// <returns>A list of dates that match the supplied rule</returns>
        public static List<DateTime> GetRelevantDateSet(Rule rule)
        {
            return GetRelevantDateSet(rule, rule.EffectiveDate, rule.ExpirationDate.Value);
        }
        /// <summary>
        /// General Function for processing a single rule, with additional date restriction independent of the rule effective date and expiration date
        /// </summary>
        /// <param name="rule">
        ///     The Rule for which this schedule should be generated from
        /// </param>
        /// <param name="startDate">
        ///     Start date for extended filtering
        /// </param>
        /// <param name="endDate">
        ///     End date for extended filtering
        /// </param>
        /// <returns>A list of dates that match the supplied rule, with options to restrict results further than the effective and expiration dates</returns>
        public static List<DateTime> GetRelevantDateSet(Rule rule, DateTime startDate, DateTime endDate)
        {
            var results = new List<DateTime>();

            if (!IsValidDateRequest(rule, startDate, endDate)) // validate the request frame of time even includes the rule
                return results;

            switch (rule.RecurranceTypeEnum)
            {
                case AppointmentRecurranceType.NonRecurring:
                    results = new List<DateTime>() { rule.EffectiveDate };
                    break;
                case AppointmentRecurranceType.Daily:
                    var daily = ParseDailyRecurrances(rule, endDate);
                    if (daily.Count > 1)
                        results.AddRange(daily);
                    else if (daily.Count == 1)
                        results.Add(daily[0]);
                    break;
                case AppointmentRecurranceType.Monthly:
                    var additions = ParseMonthlyRecurrances(rule, endDate);
                    if (additions.Count > 1)
                        results.AddRange(additions);
                    else if (additions.Count == 1)
                        results.Add(additions[0]);
                    break;
                case AppointmentRecurranceType.Weekly:
                    var weeks = ParseWeeklyRecurrances(rule, endDate);
                    if (weeks.Count > 1)
                        results.AddRange(weeks);
                    else if (weeks.Count == 1)
                        results.Add(weeks[0]);
                    break;
                case AppointmentRecurranceType.Yearly:
                    var yearly = ParseYearlyRecurrances(rule, endDate);
                    if (yearly.Count > 1)
                        results.AddRange(yearly);
                    else if (yearly.Count == 1)
                        results.Add(yearly[0]);
                    break;
                default:
                    break;

            }
            return TrimInvalidDates(startDate, endDate, results);
        }

        #region Daily Recurrance Calcs
        internal static List<DateTime> ParseDailyRecurrances(Rule rule, DateTime endDate)
        {
            var results = new List<DateTime>();

            var interval = rule.Interval;
            if (!interval.HasValue) // if there is not an interval multiplier, set to 1
                interval = 1;

            //Determine specific recurrance type
            if (rule.DaysOfWeek != 0)
                ParseDailyWeekdayRecurrances(ref results, rule, endDate, interval); //recur every N weeks on specified weekdays
            else
                ParseDailyRecurrances(ref results, rule, endDate, interval); //recur every N weeks from EffectiveDate

            if (rule.Constant.HasValue) //rule has a fixed number of repititions, as long as it fits the date bounds supplied, good to go.
                results = results.Take(rule.Constant.Value).ToList();

            return results;
        }

        internal static void ParseDailyWeekdayRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {

            // Add every weekday from here until the end date
            if (rule.ExpirationDate.HasValue)
                endDate = rule.ExpirationDate.Value;

            var searchStartDate = rule.EffectiveDate;

            while (searchStartDate < endDate)
            {
                if (searchStartDate.DayOfWeek != DayOfWeek.Saturday && searchStartDate.DayOfWeek != DayOfWeek.Sunday)
                    results.Add(searchStartDate);
                searchStartDate = searchStartDate.AddDays(1);
            }
        }

        internal static void ParseDailyRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            if (rule.ExpirationDate.HasValue)
                endDate = rule.ExpirationDate.Value;

            var searchStartDate = rule.EffectiveDate;

            while (searchStartDate < endDate)
            {
                results.Add(searchStartDate);
                searchStartDate = searchStartDate.AddDays(interval.Value);
            }
        }

        #endregion

        #region Month Recurrance Calcs
        internal static void ParseForwardMonthlyOrdinalRecurrances(int count, ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            var startCuttoffdate = rule.EffectiveDate;
            if (rule.DaysOfWeek != 0)
            {
                var beginMonthDate = new DateTime(startCuttoffdate.Year, startCuttoffdate.Month, 1);
                var endMonthDate = (startCuttoffdate.Month != 12)
                    ? new DateTime(startCuttoffdate.Year, (startCuttoffdate.Month + 1) % 12, 1).AddDays(-1)
                    : new DateTime(startCuttoffdate.Year, 12, 31);
                var searchStartDate = GetStartOfWeek(beginMonthDate);
                var ordinalityCounter = 0;

                var searchEndDate = endDate;
                if (rule.ExpirationDate.HasValue)
                    searchEndDate = rule.ExpirationDate.Value;

                while (searchStartDate < searchEndDate)
                {
                    var daysToAdd = GetWeekMatches((WeekdayFlags)rule.DaysOfWeek, searchStartDate);
                    daysToAdd = daysToAdd.Where(x => x >= beginMonthDate && x <= endMonthDate).ToList();

                    if (daysToAdd.Count != 0)
                        ordinalityCounter++;

                    if (beginMonthDate < startCuttoffdate)
                        daysToAdd = daysToAdd.Where(x => x >= startCuttoffdate && x <= endMonthDate).ToList();

                    if (beginMonthDate.Month == searchEndDate.Month && beginMonthDate.Year == searchEndDate.Year)
                        daysToAdd = daysToAdd.Where(x => x <= searchEndDate).ToList();

                    if (ordinalityCounter == count)
                    {
                        if (daysToAdd.Count > 1)
                            results.AddRange(daysToAdd);
                        else if (daysToAdd.Count == 1)
                            results.Add(daysToAdd[0]);
                    }

                    searchStartDate = searchStartDate.AddDays(7); //let's go to the next week;
                    if (searchStartDate > endMonthDate)
                    {
                        try
                        {
                            beginMonthDate = new DateTime(searchStartDate.Year, searchStartDate.Month, 1);
                            beginMonthDate = beginMonthDate.AddMonths(interval.Value - 1);
                            if (beginMonthDate.Month == 12)
                                endMonthDate = new DateTime(beginMonthDate.Year, 12, 31);
                            else
                                endMonthDate = new DateTime(beginMonthDate.Year, beginMonthDate.AddMonths(1).Month, 1).AddDays(-1);
                            ordinalityCounter = 0;
                            searchStartDate = beginMonthDate;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        internal static void ParseReverseMonthlyOrdinalRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            var startCuttoffdate = rule.EffectiveDate;
            if (rule.DaysOfWeek != 0)
            {
                var endMonthDate = new DateTime(startCuttoffdate.Year, (startCuttoffdate.Month + 1) % 12, 1).AddDays(-1);
                var searchStartDate = endMonthDate.AddDays(-6);

                var searchEndDate = endDate;
                if (rule.ExpirationDate.HasValue)
                    searchEndDate = rule.ExpirationDate.Value;

                while (searchStartDate < searchEndDate)
                {
                    var daysToAdd = GetWeekMatches((WeekdayFlags)rule.DaysOfWeek, searchStartDate);
                    daysToAdd = daysToAdd.Where(x => x >= searchStartDate).ToList();

                    if (searchStartDate < startCuttoffdate)
                        daysToAdd = daysToAdd.Where(x => x >= startCuttoffdate).ToList();

                    if (daysToAdd.Count > 1)
                        results.AddRange(daysToAdd);
                    else if (daysToAdd.Count == 1)
                        results.Add(daysToAdd[0]);

                    endMonthDate = new DateTime(searchStartDate.AddMonths(2).Year, searchStartDate.AddMonths(2).Month, 1).AddDays(-1);
                    searchStartDate = endMonthDate.AddDays(-6);
                }
            }
        }
        internal static void ParseMonthlyOrdinalRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            var count = 0;
            switch (rule.OrdinalityEnum)
            {
                case CommonOrdinality.First:
                    count = 1;
                    break;
                case CommonOrdinality.Second:
                    count = 2;
                    break;
                case CommonOrdinality.Third:
                    count = 3;
                    break;
                case CommonOrdinality.Fourth:
                    count = 4;
                    break;
                case CommonOrdinality.Last:
                    count = -1;
                    break;
                default:
                    count = 0;
                    break;
            }
            if (count > 0)
                ParseForwardMonthlyOrdinalRecurrances(count, ref results, rule, endDate, interval);
            else
                ParseReverseMonthlyOrdinalRecurrances(ref results, rule, endDate, interval);
        }

        internal static void ParseMonthlyDayRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            //if (rule.EffectiveDate >= startDate)
            var startDate = rule.EffectiveDate;
            if (rule.ExpirationDate.HasValue)
                endDate = rule.ExpirationDate.Value;
            //endDate = rule.ExpirationDate;

            //what day am I, am I already past it?
            DateTime myDate;
            if (startDate.Day <= rule.DayOrdinal)
            {
                //begin with current month
                myDate = new DateTime(startDate.Year, startDate.Month, rule.DayOrdinal.Value);
            }
            else
            {
                var monthAdditive = (int)(1 * interval.Value);
                myDate = new DateTime(startDate.Year, startDate.AddMonths(monthAdditive).Month, rule.DayOrdinal.Value);
            }
            while (myDate < endDate)
            {
                results.Add(myDate);
                myDate = myDate.AddMonths(interval.Value);
            }
            //rule falls on a specific day of the month in this repitition
        }

        internal static void ParseMonthlyFirstFullWeekRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int interval)
        {
            var startCuttoffdate = rule.EffectiveDate;
            var beginMonthDate = new DateTime(startCuttoffdate.Year, startCuttoffdate.Month, 1);
            var searchStartDate = beginMonthDate;//GetStartOfWeek(beginMonthDate);

            var searchEndDate = endDate;
            if (rule.ExpirationDate.HasValue)
                searchEndDate = rule.ExpirationDate.Value;

            while (beginMonthDate <= endDate)
            {
                searchStartDate = GetStartOfWeek(beginMonthDate); // gives us the first sunday of the week in which the 1st of the month falls

                if (searchStartDate.AddDays(1).Month != beginMonthDate.Month) //if months do not match on Monday, lets go to next week, this will be the correct time
                    searchStartDate = searchStartDate.AddDays(7);
                else
                    searchStartDate = searchStartDate.AddDays(1); // monday matches, doesn't matter if we go forward the one day...

                if (rule.DaysOfWeek == (int)WeekdayFlags.None)
                {
                    results.Add(searchStartDate.AddDays(1)); // add as first monday
                }
                else
                {
                    var daysToAdd = GetWeekMatches((WeekdayFlags)rule.DaysOfWeek, searchStartDate);
                    if (daysToAdd.Count == 1)
                        results.Add(daysToAdd[0]);
                    if (daysToAdd.Count > 1)
                        results.AddRange(daysToAdd);
                }
                //advance beginMonthDate [interval] month(s)...
                beginMonthDate = new DateTime(searchStartDate.AddMonths(interval).Year, searchStartDate.AddMonths(interval).Month, 1);
            }
        }

        internal static List<DateTime> ParseMonthlyRecurrances(Rule rule, DateTime endDate)
        {
            var results = new List<DateTime>();

            var interval = rule.Interval;
            // if there is not an interval multiplier, set to 1
            if (interval == 0)
                interval = 1;
            if (!interval.HasValue)
                interval = 1;

            if (rule.UseFirstFullWorkWeek)
                ParseMonthlyFirstFullWeekRecurrances(ref results, rule, endDate, interval.Value);
            else if (rule.OrdinalityEnum != CommonOrdinality.None)
                ParseMonthlyOrdinalRecurrances(ref results, rule, endDate, interval);
            else if (rule.DayOrdinal.HasValue)
                ParseMonthlyDayRecurrances(ref results, rule, endDate, interval);

            if (rule.Constant.HasValue)
            {
                //rule has a fixed number of repititions, as long as it fits the date bounds supplied, good to go.
                results = results.Take(rule.Constant.Value).ToList();
            }

            return results;
        }
        #endregion

        #region Weekly Recurrance Calcs
        internal static void ParseWeeklyWeekdayRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            //startDate = rule.EffectiveDate;
            if (rule.ExpirationDate.HasValue)
                endDate = rule.ExpirationDate.Value;

            var searchStartDate = GetStartOfWeek(rule.EffectiveDate);

            while (searchStartDate < endDate)
            {
                var daysToAdd = GetWeekMatches((WeekdayFlags)rule.DaysOfWeek, searchStartDate);
                if (daysToAdd.Count > 1)
                    results.AddRange(daysToAdd);
                else if (daysToAdd.Count == 1)
                    results.Add(daysToAdd[0]);
                var dayMultiplier = (double)(7 * interval.Value);
                searchStartDate = searchStartDate.AddDays(dayMultiplier);
            }
        }
        internal static void ParseWeeklyRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            var startDate = rule.EffectiveDate;
            if (rule.ExpirationDate.HasValue)
                endDate = rule.ExpirationDate.Value;

            var searchStartDate = startDate;

            while (searchStartDate < endDate)
            {
                results.Add(searchStartDate);
                var dayMultiplier = (double)(7 * interval.Value);
                searchStartDate = searchStartDate.AddDays(dayMultiplier);
            }
        }
        internal static List<DateTime> ParseWeeklyRecurrances(Rule rule, DateTime endDate)
        {
            var results = new List<DateTime>();

            var interval = rule.Interval;
            if (!interval.HasValue) // if there is not an interval multiplier, set to 1
                interval = 1;

            //Determine specific recurrance type
            if (rule.DaysOfWeek != 0)
                ParseWeeklyWeekdayRecurrances(ref results, rule, endDate, interval); //recur every N weeks on specified weekdays
            else
                ParseWeeklyRecurrances(ref results, rule, endDate, interval); //recur every N weeks from EffectiveDate

            if (rule.Constant.HasValue) //rule has a fixed number of repititions, as long as it fits the date bounds supplied, good to go.
                results = results.Take(rule.Constant.Value).ToList();

            return results;
        }
        #endregion

        #region Yearly Recurrance Calcs
        internal static void ParseYearlyRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            //if (rule.EffectiveDate >= startDate)
            var startDate = rule.EffectiveDate;
            if (rule.ExpirationDate.HasValue)
                endDate = rule.ExpirationDate.Value;
            //endDate = rule.ExpirationDate;

            //what day am I, am I already past it?
            DateTime myDate;
            if (startDate.Day <= rule.DayOrdinal.Value && startDate.Month < rule.Month.Value)
                myDate = new DateTime(startDate.Year, rule.Month.Value, rule.DayOrdinal.Value);
            else
                myDate = new DateTime(startDate.Year + 1, rule.Month.Value, rule.DayOrdinal.Value);

            while (myDate < endDate)
            {
                results.Add(myDate);
                myDate = myDate.AddYears(interval.Value);
            }
            //rule falls on a specific day of the month in this repitition
        }
        internal static List<DateTime> ParseYearlyRecurrances(Rule rule, DateTime endDate)
        {
            var results = new List<DateTime>();


            var interval = rule.Interval;
            if (!interval.HasValue) // if there is not an interval multiplier, set to 1
                interval = 1;

            //Determine specific recurrance type
            if (rule.OrdinalityEnum != CommonOrdinality.None)
            {
                if (rule.Month.HasValue)
                {
                    if (rule.DaysOfWeek != 0)
                        ParseYearlyRecurranceWithOrdinality(ref results, rule, endDate, interval);
                }
            }
            else
            {
                // no ordinality, on a specific day every year
                ParseYearlyRecurrances(ref results, rule, endDate, interval);
            }

            if (rule.Constant.HasValue) //rule has a fixed number of repititions, as long as it fits the date bounds supplied, good to go.
                results = results.Take(rule.Constant.Value).ToList();

            return results;
        }
        internal static void ParseYearlyRecurranceWithOrdinality(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            var count = 0;
            switch (rule.OrdinalityEnum)
            {
                case CommonOrdinality.First:
                    count = 1;
                    break;
                case CommonOrdinality.Second:
                    count = 2;
                    break;
                case CommonOrdinality.Third:
                    count = 3;
                    break;
                case CommonOrdinality.Fourth:
                    count = 4;
                    break;
                case CommonOrdinality.Last:
                    count = -1;
                    break;
                default:
                    count = 0;
                    break;
            }
            if (count > 0)
                ParseForwardYearlyOrdinalRecurrances(count, ref results, rule, endDate, interval);
            else
                ParseReverseYearlyOrdinalRecurrances(ref results, rule, endDate, interval);

        }
        internal static void ParseReverseYearlyOrdinalRecurrances(ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            var startCuttoffdate = rule.EffectiveDate;
            if (rule.DaysOfWeek != 0)
            {
                DateTime endMonthDate;
                if (rule.Month.Value == 12)
                    endMonthDate = new DateTime(startCuttoffdate.Year, 12, 31);
                else
                    endMonthDate = new DateTime(startCuttoffdate.Year, rule.Month.Value + 1, 1).AddDays(-1);

                var searchStartDate = endMonthDate.AddDays(-6);

                var searchEndDate = endDate;
                if (rule.ExpirationDate.HasValue)
                    searchEndDate = rule.ExpirationDate.Value;

                while (searchStartDate < searchEndDate)
                {
                    var daysToAdd = GetWeekMatches((WeekdayFlags)rule.DaysOfWeek, searchStartDate);
                    daysToAdd = daysToAdd.Where(x => x >= searchStartDate).ToList();

                    if (searchStartDate < startCuttoffdate)
                        daysToAdd = daysToAdd.Where(x => x >= startCuttoffdate).ToList();

                    if (daysToAdd.Count > 1)
                        results.AddRange(daysToAdd);
                    else if (daysToAdd.Count == 1)
                        results.Add(daysToAdd[0]);

                    try
                    {
                        if (rule.Month.Value == 12)
                            endMonthDate = new DateTime(endMonthDate.Year + interval.Value, 12, 31);
                        else
                            endMonthDate = new DateTime(endMonthDate.Year + interval.Value, searchStartDate.AddMonths(1).Month, 1).AddDays(-1);
                        searchStartDate = endMonthDate.AddDays(-6);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
        }
        internal static void ParseForwardYearlyOrdinalRecurrances(int count, ref List<DateTime> results, Rule rule, DateTime endDate, int? interval)
        {
            var startCuttoffdate = rule.EffectiveDate;
            if (rule.DaysOfWeek != 0)
            {
                var beginMonthDate = new DateTime(startCuttoffdate.Year, startCuttoffdate.Month, 1);
                var endMonthDate = new DateTime(startCuttoffdate.Year, (startCuttoffdate.Month + 1) % 12, 1).AddDays(-1);
                var searchStartDate = GetStartOfWeek(beginMonthDate);
                var ordinalityCounter = 0;
                bool advanceToNext = false;

                var searchEndDate = endDate;
                if (rule.ExpirationDate.HasValue)
                    searchEndDate = rule.ExpirationDate.Value;

                while (searchStartDate < searchEndDate)
                {
                    var daysToAdd = GetWeekMatches((WeekdayFlags)rule.DaysOfWeek, searchStartDate);
                    daysToAdd = daysToAdd.Where(x => x >= beginMonthDate && x <= endMonthDate).ToList();

                    if (daysToAdd.Count != 0)
                        ordinalityCounter++;

                    if (beginMonthDate < startCuttoffdate)
                        daysToAdd = daysToAdd.Where(x => x >= startCuttoffdate && x <= endMonthDate).ToList();

                    if (beginMonthDate.Month == searchEndDate.Month && beginMonthDate.Year == searchEndDate.Year)
                        daysToAdd = daysToAdd.Where(x => x <= searchEndDate).ToList();

                    if (ordinalityCounter == count)
                    {
                        if (daysToAdd.Count > 1)
                            results.AddRange(daysToAdd);
                        else if (daysToAdd.Count == 1)
                            results.Add(daysToAdd[0]);
                        advanceToNext = true;
                    }

                    searchStartDate = searchStartDate.AddDays(7); //let's go to the next week;

                    if (searchStartDate > endMonthDate || advanceToNext)
                    {
                        try
                        {
                            searchStartDate = new DateTime(endMonthDate.Year, endMonthDate.Month, 1).AddYears(interval.Value);

                            beginMonthDate = searchStartDate;
                            if (beginMonthDate.Month == 12)
                                endMonthDate = new DateTime(beginMonthDate.Year, 12, 31);
                            else
                                endMonthDate = new DateTime(beginMonthDate.Year, beginMonthDate.AddMonths(1).Month, 1).AddDays(-1);
                            ordinalityCounter = 0;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }
        #endregion
    }
}
