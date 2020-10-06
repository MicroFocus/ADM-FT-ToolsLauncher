/*
 *
 *  Certain versions of software and/or documents (“Material”) accessible here may contain branding from
 *  Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.  As of September 1, 2017,
 *  the Material is now offered by Micro Focus, a separately owned and operated company.  Any reference to the HP
 *  and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE
 *  marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * © Copyright 2012-2019 Micro Focus or one of its affiliates..
 *
 * The only warranties for products and services of Micro Focus and its affiliates
 * and licensors (“Micro Focus”) are set forth in the express warranty statements
 * accompanying such products and services. Nothing herein should be construed as
 * constituting an additional warranty. Micro Focus shall not be liable for technical
 * or editorial errors or omissions contained herein.
 * The information contained herein is subject to change without notice.
 * ___________________________________________________________________
 *
 */

using System;
using System.Collections.Generic;

namespace HpToolsLauncher
{
    public class TestSuiteRunResults
    {
        private List<TestRunResults> m_testRuns = new List<TestRunResults>();
        private int m_numErrors = 0;
        private int m_numFailures = 0;
        private int m_numTests = 0;
        private TimeSpan m_totalRunTime = TimeSpan.Zero;

        public string SuiteName { get; set; }

        public int NumFailures
        {
            get { return m_numFailures; }
            set { m_numFailures = value; }
        }

        public int NumTests
        {
            get { return m_numTests; }
            set { m_numTests = value; }
        }

        public TimeSpan TotalRunTime
        {
            get { return m_totalRunTime; }
            set { m_totalRunTime = value; }
        }

        public List<TestRunResults> TestRuns
        {
            get { return m_testRuns; }
            set { m_testRuns = value; }
        }

        public int NumErrors
        {
            get { return m_numErrors; }
            set { m_numErrors = value; }
        }


        internal void AppendResults(TestSuiteRunResults desc)
        {
            this.TestRuns.AddRange(desc.TestRuns);
            this.TotalRunTime += desc.TotalRunTime;
            this.NumErrors += desc.NumErrors;
            this.NumFailures += desc.NumFailures;
            this.NumTests += desc.NumTests;
        }
    }
}
