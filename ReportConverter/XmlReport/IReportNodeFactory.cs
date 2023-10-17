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

namespace ReportConverter.XmlReport
{
    /// <summary>
    /// Represents a factory that creates the new report node instance.
    /// </summary>
    public interface IReportNodeFactory
    {
        /// <summary>
        /// Creates a new instance of type <typeparamref name="T"/> from the specified
        /// <see cref="ReportNodeType"/> instance read from the XML report.
        /// </summary>
        /// <typeparam name="T">The type of the instance that implements the <see cref="IReportNode"/> interface.</typeparam>
        /// <param name="node">The <see cref="ReportNodeType"/> instance read from the XML report.</param>
        /// <param name="parentNode">The parent <see cref="ReportNodeType"/> instance read from the XML report.</param>
        /// <param name="owner">The owner of the newly created report node instance.</param>
        /// <returns>
        /// The new report node instance of type <typeparamref name="T"/> which implements the <see cref="IReportNode"/> interface
        /// or <see cref="null"/> if failed to create the instance from the <see cref="ReportNodeType"/> instance.
        /// </returns>
        T Create<T>(ReportNodeType node, ReportNodeType parentNode, IReportNodeOwner owner) where T : class, IReportNode;
    }
}
