using System;
using System.Collections.Generic;

namespace HpToolsLauncher.TestRunners
{
    public class IterationInfo
    {
        public const string RANGE_ITERATION_MODE = "rngIterations";
        public const string ONE_ITERATION_MODE = "oneIteration";
        public const string ALL_ITERATION_MODE = "rngAll";
        public static ISet<String> AvailableTypes = new HashSet<String>() { RANGE_ITERATION_MODE, ONE_ITERATION_MODE, ALL_ITERATION_MODE };

        public string IterationMode { get; set; }

        public string StartIteration { get; set; }

        public string EndIteration { get; set; }
    }
}
