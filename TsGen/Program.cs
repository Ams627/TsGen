using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsGen
{
    using System.Globalization;

    internal class DateHours
    {
        public DateTime Date { get; set; }
        public int Hours { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new Exception("You must supply at least one timesheet file to process");
                }
                foreach (var filename in args)
                {
                    var datehoursList = new List<DateHours>();
                    var outputname = Path.GetFileNameWithoutExtension(filename) + ".html";
                    var linenumber = 0;
                    foreach (var line in File.ReadLines(filename))
                    {
                        var trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            var split = trimmedLine.Split(':');
                            if (split.Length != 2)
                            {
                                throw new Exception(
                                    $"Malformed line at line {linenumber}. Line must contain one and only one colon");
                            }
                            if (!DateTime.TryParseExact(split[0], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var date))
                            {
                                throw new Exception($"Invalid date {split[0]} in file {filename} at line {linenumber}");
                            }
                            if (!int.TryParse(split[1], out var hours))
                            {
                                throw new Exception(
                                    $"Invalid number of hours '{split[1]}' in file {filename} at line {linenumber}");
                            }
                            if (hours < 0 || hours > 23)
                            {
                                throw new Exception(
                                    $"Invalid number of hours '{split[1]}' in file {filename} at line {linenumber}");
                            }
                            datehoursList.Add(new DateHours {Date = date, Hours = hours});
                        }
                        linenumber++;
                    }
                    var years = datehoursList.GroupBy(x => x.Date.Year).Count();
                    if (years > 1)
                    {
                        throw new Exception(
                            $"All years in one timesheet file must be the same but the file {filename} contains {years} different years.");
                    }
                    var months = datehoursList.GroupBy(x => x.Date.Month).Count();
                    if (months > 1)
                    {
                        throw new Exception(
                            $"All months in one timesheet file must be the same but the file {filename} contains {months} different months.");
                    }

                    var saturdays = datehoursList.Where(x => x.Date.DayOfWeek == DayOfWeek.Saturday).ToList();
                    saturdays.ForEach(x => Console.WriteLine($"Warning - Saturday included: {x.Date:yyyy-MM-dd}"));
                    var sundays = datehoursList.Where(x => x.Date.DayOfWeek == DayOfWeek.Sunday).ToList();
                    sundays.ForEach(x => Console.WriteLine($"Warning - Sunday included: {x.Date:yyyy-MM-dd}"));
                    var datesIncluded = new HashSet<DateTime>();
                    datesIncluded.UnionWith(datehoursList.Select(x => x.Date));
                    var startDate = new DateTime(datehoursList[0].Date.Year, datehoursList[0].Date.Month, 1);
                    for (var d = startDate; d.Month == startDate.Month; d = d.AddDays(1))
                    {
                        if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                        {
                            if (!datesIncluded.Contains(d))
                            {
                                Console.WriteLine($"Warning: working day {d:yyyy-MM-dd} missing from timesheet file.");
                            }
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                var codeBase = System.Reflection.Assembly.GetEntryAssembly().CodeBase;
                var progname = Path.GetFileNameWithoutExtension(codeBase);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
