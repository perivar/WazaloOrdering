using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace WazaloOrdering.DataStore
{
    public static class Utils
    {
        public static bool CaseInsensitiveContains(this string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }

        public static FileDate FindLastCacheFile(string cacheDir, string cacheFileNamePrefix)
        {
            string dateFromToRegexPattern = @"(\d{4}\-\d{2}\-\d{2})\-(\d{4}\-\d{2}\-\d{2})\.csv$";
            return FindLastCacheFile(cacheDir, cacheFileNamePrefix, dateFromToRegexPattern, "yyyy-MM-dd", "\\-");
        }

        public static FileDate FindLastCacheFile(string cacheDir, string cacheFileNamePrefix, string dateFromToRegexPattern, string dateParsePattern, string separator)
        {
            var dateDictonary = new SortedDictionary<DateTime, FileDate>();

            string regexp = string.Format("{0}{1}{2}", cacheFileNamePrefix, separator, dateFromToRegexPattern);
            Regex reg = new Regex(regexp);

            string directorySearchPattern = string.Format("{0}*", cacheFileNamePrefix);
            IEnumerable<string> filePaths = Directory.EnumerateFiles(cacheDir, directorySearchPattern);
            foreach (var filePath in filePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var match = reg.Match(fileName);
                if (match.Success)
                {
                    var from = match.Groups[1].Value;
                    var to = match.Groups[2].Value;

                    var dateFrom = DateTime.ParseExact(from, dateParsePattern, CultureInfo.InvariantCulture);
                    var dateTo = DateTime.ParseExact(to, dateParsePattern, CultureInfo.InvariantCulture);
                    var fileDate = new FileDate
                    {
                        From = dateFrom,
                        To = dateTo,
                        FilePath = filePath
                    };
                    dateDictonary.Add(dateTo, fileDate);
                }
            }

            if (dateDictonary.Count() > 0)
            {
                // the first element is the newest date
                return dateDictonary.Last().Value;
            }

            // return a default file date
            return default(FileDate);
        }

        public static List<T> ReadCacheFile<T>(string filePath)
        {
            if (File.Exists(filePath))
            {
                using (TextReader fileReader = File.OpenText(filePath))
                {
                    using (var csvReader = new CsvReader(fileReader))
                    {
                        csvReader.Configuration.Delimiter = ",";
                        csvReader.Configuration.HasHeaderRecord = true;
                        csvReader.Configuration.CultureInfo = CultureInfo.InvariantCulture;

                        return csvReader.GetRecords<T>().ToList();
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public static void WriteCacheFile<T>(string filePath, List<T> values)
        {
            using (var sw = new StreamWriter(filePath))
            {
                var csvWriter = new CsvWriter(sw);
                csvWriter.Configuration.Delimiter = ",";
                csvWriter.Configuration.HasHeaderRecord = true;
                csvWriter.Configuration.CultureInfo = CultureInfo.InvariantCulture;

                csvWriter.WriteRecords(values);
            }
        }

        public static byte[] WriteCsvToMemory<T>(IEnumerable<T> records, Type mapType)
        {
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var csvWriter = new CsvWriter(streamWriter))
            {
                csvWriter.Configuration.RegisterClassMap(mapType);
                csvWriter.WriteRecords(records);
                streamWriter.Flush();
                return memoryStream.ToArray();
            }
        }

        public static void SendMailWithAttachment(string subject, string body, string to, string cc, string fileDownloadName, byte[] bytes)
        {
            var config = new MyConfiguration();
            var emailSMTPServer = config.GetString("EmailSMTPServer");
            var emailSMTPPort = config.GetInt("EmailSMTPPort");
            var emailUserName = config.GetString("EmailUserName");
            var emailPassword = config.GetString("EmailPassword");
            var emailDisplayName = config.GetString("EmailDisplayName");

            /* 
            Note:
            Google may block sign in attempts from some apps or devices that do not use modern security standards. 
            Since these apps and devices are easier to break into, blocking them helps keep your account safer.
            Please make sure, in Gmail settings of your account, enable access for less secure apps to avoid below error:
            The SMTP server requires a secure connection or the client was not 
            authenticated. The server response was: 5.5.1 Authentication Required?
            */
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(emailUserName, emailDisplayName);
                mail.To.Add(to);
                if (cc != null) mail.CC.Add(cc);

                mail.Subject = subject;
                mail.Body = body;
                mail.Priority = MailPriority.High;

                mail.Attachments.Add(new Attachment(new MemoryStream(bytes), fileDownloadName));
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient(emailSMTPServer, emailSMTPPort))
                {
                    smtp.Credentials = new NetworkCredential(emailUserName, emailPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
        }

        /// <summary>
        /// Gets the 12:00:00 instance of a DateTime
        /// </summary>
        public static DateTime AbsoluteStart(this DateTime dateTime)
        {
            return dateTime.Date;
        }

        /// <summary>
        /// Gets the 11:59:59 instance of a DateTime
        /// </summary>
        public static DateTime AbsoluteEnd(this DateTime dateTime)
        {
            return AbsoluteStart(dateTime).AddDays(1).AddTicks(-1);
        }
    }

    public class FileDate
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string FilePath { get; set; }
    }

    public class Date
    {
        DateTime currentDate;
        DateTime yesterday;
        DateTime firstDayOfTheYear;
        DateTime lastDayOfTheYear;

        public DateTime CurrentDate
        {
            get { return Utils.AbsoluteEnd(currentDate); }
        }

        public DateTime Yesterday
        {
            get { return yesterday; }
        }

        public DateTime FirstDayOfTheYear
        {
            get { return firstDayOfTheYear; }
        }

        public DateTime LastDayOfTheYear
        {
            get { return lastDayOfTheYear; }
        }

        public Date()
        {
            currentDate = DateTime.Now.Date;
            yesterday = currentDate.AddDays(-1);
            firstDayOfTheYear = new DateTime(currentDate.Year, 1, 1);
            lastDayOfTheYear = new DateTime(currentDate.Year, 12, 31);
        }
    }
}
