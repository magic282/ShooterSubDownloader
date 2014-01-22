using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ShooterSubDownloader
{
    public class Logger
    {
        #region Log
        private static object LogLock = new object();
        private static string LogFilePath = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\ShooterSubDownloaderLog.txt";
        public static void Log(string message, EventLogEntryType type)
        {
            try
            {
                lock (LogLock)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.AppendAllText(LogFilePath, DateTime.Now.ToString() + "\t" +
                            "[Shooter]" + ": " + message + "\r\n");
                    }
                    else
                    {
                        File.WriteAllText(LogFilePath, DateTime.Now.ToString() + "\t" +
                             "[Shooter]" + ": " + message + "\r\n");
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        public static void Log(string message)
        {
            Log(message, EventLogEntryType.Information);
        }
        public static void Log(Exception e)
        {
            Log(e.Message + "\r\n" + e.StackTrace + "\r\n", EventLogEntryType.Error);
        }
        #endregion

        public static void clear()
        {
            FileInfo f = new FileInfo(LogFilePath);
            try
            {
                f.Delete();
            }
            catch
            {

            }

        }
    }
}
