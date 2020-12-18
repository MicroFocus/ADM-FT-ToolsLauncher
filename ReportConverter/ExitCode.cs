using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter
{
    class ExitCode
    {
        public static readonly ExitCodeData Success = new ExitCodeData(0, "");
        public static readonly ExitCodeData GeneralError = new ExitCodeData(1, Properties.Resources.Exit_GeneralError);
        public static readonly ExitCodeData MissingArgument = new ExitCodeData(2, Properties.Resources.Exit_MissingArgument);
        public static readonly ExitCodeData InvalidArgument = new ExitCodeData(3, Properties.Resources.Exit_InvalidArgument);
        public static readonly ExitCodeData CannotReadFile = new ExitCodeData(4, Properties.Resources.Exit_CannotReadFile);
        public static readonly ExitCodeData CannotWriteFile = new ExitCodeData(5, Properties.Resources.Exit_CannotWriteFile);
        public static readonly ExitCodeData FileNotFound = new ExitCodeData(6, Properties.Resources.Exit_FileNotFound);
        public static readonly ExitCodeData InvalidInput = new ExitCodeData(7, Properties.Resources.Exit_InvalidInput);
        public static readonly ExitCodeData UnknownOutputFormat = new ExitCodeData(20, Properties.Resources.Exit_UnknownOutputFormat);

        public class ExitCodeData
        {
            public ExitCodeData(int code, string message)
            {
                Code = code;
                Message = message;
            }

            public int Code { get; private set; }
            public string Message { get; private set; }
        }
    }

    static class ProgramExit
    {
        public static void Exit(ExitCode.ExitCodeData ecd, bool writeCommandUsage = false)
        {
            if (!string.IsNullOrWhiteSpace(ecd.Message))
            {
                OutputWriter.WriteLines(ecd.Message);
            }

            if (writeCommandUsage)
            {
                OutputWriter.WriteLine();
                OutputWriter.WriteCommandUsage();
            }

            Environment.Exit(ecd.Code);
        }
    }
}
