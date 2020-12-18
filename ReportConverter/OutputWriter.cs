using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter
{
    static class OutputWriter
    {
        private static readonly Assembly _progAssembly = Assembly.GetEntryAssembly();

        private static TextWriter Writer { get; set; }

        static OutputWriter()
        {
            Writer = Console.Out;
        }

        public static void SetWriter(TextWriter w)
        {
            Writer = w == null ? TextWriter.Null : w;
        }

        public static void WriteTitle()
        {
            Writer.Write(Properties.Resources.Prog_Title);
            Writer.Write(" ");
            WriteVersion();
            Writer.WriteLine();
        }

        public static void WriteVersion()
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(_progAssembly.Location);
            Writer.WriteLine(fvi.FileVersion);
        }

        public static void WriteCommandUsage(params string[] messages)
        {
            if (messages.Length > 0)
            {
                WriteLines(messages);
                Writer.WriteLine();
            }

            ArgHelper.Instance.WriteUsage(Writer, _progAssembly.GetName().Name);
        }

        public static void WriteLines(params string[] lines)
        {
            if (lines.Length > 0)
            {
                foreach (string line in lines)
                {
                    Writer.WriteLine(line);
                }
            }
        }

        public static void WriteLine()
        {
            Writer.WriteLine();
        }

        public static void WriteLine(string value)
        {
            Writer.WriteLine(value);
        }

        public static void WriteLine(string format, params string[] arg)
        {
            Writer.WriteLine(format, arg);
        }
    }
}
