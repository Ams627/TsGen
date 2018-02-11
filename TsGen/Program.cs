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
        public static void WriteCSS(StreamWriter s)
        {
           s.WriteLine("table,td {");
           s.WriteLine("border: solid 1px #dddddd;");
           s.WriteLine("border-collapse: collapse;");
           s.WriteLine("}");
           s.WriteLine("td {");
           s.WriteLine("width:5em;");
           s.WriteLine("height:5em;");
           s.WriteLine("}");
           s.WriteLine(".daycell {");
           s.WriteLine("position:relative;");
           s.WriteLine("}");
           s.WriteLine(".date {");
           s.WriteLine("    position: absolute;");
           s.WriteLine("    top: 0;");
           s.WriteLine("    left: 0;");
           s.WriteLine("    z-index: 10;");
           s.WriteLine("    text-align:left;");
           s.WriteLine("    vertical-align:top;");
           s.WriteLine("    color:#cccccc;");
           s.WriteLine("}");
           s.WriteLine(".hours {");
           s.WriteLine("    position: absolute;");
           s.WriteLine("    top: 40%;");
           s.WriteLine("    left: 40%;");
           s.WriteLine("    z-index: 10;");
           s.WriteLine("    text-align:center;");
           s.WriteLine("    vertical-align:middle;");
           s.WriteLine("    color:blue;");
           s.WriteLine("}");
        }

        public static DateTime RoundToMonday(DateTime start)
        {
            var daysToSubtract = ((int)start.DayOfWeek + 6) % 7;
            return start.AddDays(-daysToSubtract);
        }
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
                        if (!string.IsNullOrEmpty(trimmedLine) && line.Substring(0, 2) != "--")
                        {
                            var split = trimmedLine.Split(' ');
                            if (split.Length != 2)
                            {
                                throw new Exception(
                                    $"Malformed line at line {linenumber}. Line must contain one and only two fields: date and hours separated by whitespace");
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

                    // warn about Saturdays and Sundays as they are not normally present in a timesheet:
                    var saturdays = datehoursList.Where(x => x.Date.DayOfWeek == DayOfWeek.Saturday).ToList();
                    saturdays.ForEach(x => Console.WriteLine($"Warning - Saturday included: {x.Date:yyyy-MM-dd}"));
                    var sundays = datehoursList.Where(x => x.Date.DayOfWeek == DayOfWeek.Sunday).ToList();
                    sundays.ForEach(x => Console.WriteLine($"Warning - Sunday included: {x.Date:yyyy-MM-dd}"));
                    
                    // check all week days present in file:
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

                    var dayGroups = datehoursList.GroupBy(x => x.Date).Where(x => x.Count() > 1).ToList();
                    dayGroups.ForEach(x =>
                        Console.WriteLine($"Error: The date {x.Key.Date:yyyy-MM-dd} occurs more than once in file {filename}"));
                    if (dayGroups.Any())
                    {
                        Environment.Exit(1);
                    }

                    var dayDict = datehoursList.ToDictionary(x => x.Date, x => x.Hours);

                    var monthName = $"{datehoursList[0].Date:MMMM}";
                    var year = datehoursList[0].Date.Year;
                    using (var streamWriter = new StreamWriter(outputname))
                    {
                        streamWriter.WriteLine($"<!doctype html>\r\n<html>\r\n<head>\r\n<title>Timesheet {monthName} {year}</title>\r\n<style type=\"text/css\">\r\n");
                        WriteCSS(streamWriter);
                        streamWriter.WriteLine("</style>\r\n</head>\r\n<body>\r\n");
                        streamWriter.WriteLine($"<h1>Timesheet {monthName} {year}</h1>\r\n");
                        streamWriter.WriteLine($"<table class=\"sheet1\"><tr>");
                        streamWriter.WriteLine($"<th>Mon</th>");
                        streamWriter.WriteLine($"<th>Tue</th>");
                        streamWriter.WriteLine($"<th>Wed</th>");
                        streamWriter.WriteLine($"<th>Thu</th>");
                        streamWriter.WriteLine($"<th>Fri</th>");
                        streamWriter.WriteLine($"<th>Sat</th>");
                        streamWriter.WriteLine($"<th>Sun</th></tr><tr>");

                        var startMonday = RoundToMonday(startDate);
                        var cell = 0;
                        for (var d = startMonday; d.Month <= startDate.Month; d = d.AddDays(1))
                        {
                            var hours = dayDict.TryGetValue(d, out var h) ? h : 0;
                            streamWriter.WriteLine($"<td class=\"daycell\"><span class=\"date\">{d.Day}</span><span class=\"hours\">{h}</span> </td>");
                            if (++cell % 7 == 0)
                            {
                                streamWriter.WriteLine("</tr>\r\n<tr>");
                            }
                        }
                        streamWriter.WriteLine("</tr></table>\r\n");
                        streamWriter.WriteLine($"<h2>total hours for {startDate:MMMM}:{datehoursList.Select(x=>x.Hours).Sum()}</h2>");
                        streamWriter.WriteLine("</body>\r\n");
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
