namespace HpToolsLauncher
{
    enum ExportTypeSuffixEnum
    {
        ExportTypeSuffix_SD, //step details
        ExportTypeSuffix_DT, //data table
        ExportTypeSuffix_SM, //system monitor
        ExportTypeSuffix_SR, //screen recorder
        ExportTypeSuffix_LT  //log tracking
    }

    public class ExportOptions
    {
        public const string ExportDataTable = "ExportDataTable";
        public const string ExportForFailed = "ExportForFailed";
        public const string ExportLogTracking = "ExportLogTracking";
        public const string ExportScreenRecorder = "ExportScreenRecorder";
        public const string ExportStepDetails = "ExportStepDetails";
        public const string ExportSystemMonitor = "ExportSystemMonitor";
        public const string ExportLocation = "ExportLocation";
        public const string XslPath = "XSLPath";
        public const string ExportFormat = "ExportFormat";
        public const string ExportType = "ExportType";

    }
}