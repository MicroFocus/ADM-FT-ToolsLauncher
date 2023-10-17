/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2023 Open Text
 *
 * The only warranties for products and services of Open Text and
 * its affiliates and licensors ("Open Text") are as may be set forth
 * in the express warranty statements accompanying such products and services.
 * Nothing herein should be construed as constituting an additional warranty.
 * Open Text shall not be liable for technical or editorial errors or
 * omissions contained herein. The information contained herein is subject
 * to change without notice.
 *
 * Except as specifically indicated otherwise, this document contains
 * confidential information and a valid license is required for possession,
 * use or copying. If this work is provided to the U.S. Government,
 * consistent with FAR 12.211 and 12.212, Commercial Computer Software,
 * Computer Software Documentation, and Technical Data for Commercial Items are
 * licensed to the U.S. Government under vendor's standard commercial license.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ___________________________________________________________________
 */

using System;

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
