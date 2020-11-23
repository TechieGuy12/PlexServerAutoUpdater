using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE
{
    /// <summary>
    /// Contains the properties and methods to write a log file.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// The name of the updater log file.
        /// </summary>
        private const string LogFileName = "plex-updater.txt";

        private static string _defaultFolder;

        /// <summary>
        /// Gets the the full path to the log file.
        /// </summary>
        public static string FilePath { get; private set; }

        /// <summary>
        /// Gets the folder to the log file.
        /// </summary>
        public static string Folder { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Log"/> class.
        /// </summary>
        static Log()
        {
            _defaultFolder = Path.GetTempPath();
            Folder = _defaultFolder;

            // Set the full path to the log file
            FilePath = Path.Combine(Folder, LogFileName);
        }

        /// <summary>
        /// Gets the formatted timestamp value.
        /// </summary>
        /// <returns>
        /// A string representation of the timestamp.
        /// </returns>
        private static string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ");
        }

        /// <summary>
        /// Deletes the log file.
        /// </summary>
        public static void Delete()
        {
            File.Delete(FilePath);
        }

        /// <summary>
        /// Sets the full path to the log file.
        /// </summary>
        /// <param name="path">
        /// The full path to the log file.
        /// </param>
        public static void SetFolder(string path)
        {
            try
            {
                // Call this to validate the path
                Path.GetFullPath(path);

                Folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(Folder))
                {
                    Directory.CreateDirectory(Folder);
                }

                FilePath = Path.Combine(Folder, LogFileName);
            }
            catch
            {
                Folder = _defaultFolder;
                FilePath = Path.Combine(Folder, LogFileName);
            }
        }

        /// <summary>
        /// Writes a string value to the log file.
        /// </summary>
        public static void Write(string text, bool appendDate = true)
        {
            string timeStamp = string.Empty;
            if (appendDate)
            {
                timeStamp = GetTimeStamp();
            }

            File.AppendAllText(FilePath, $"{timeStamp}{text}{NewLine}");
        }

        /// <summary>
        /// Writes information about an exception to the log file.
        /// </summary>
        /// <param name="ex">
        /// The <see cref="Exception"/> object that contains information to write to
        /// the log file.
        /// </param>
        public static void Write(Exception ex, bool appendDate = true)
        {
            if (ex == null)
            {
                return;
            }

            string timeStamp = string.Empty;
            if (appendDate)
            {
                timeStamp = GetTimeStamp();
            }

            File.AppendAllText(
                FilePath,
                $"{timeStamp}Message:{NewLine}{ex.Message}{NewLine}{NewLine}Inner Exception:{NewLine}{ex.InnerException}{NewLine}{NewLine}Stack Trace:{NewLine}{ex.StackTrace}{NewLine}");
        }
    }
}
