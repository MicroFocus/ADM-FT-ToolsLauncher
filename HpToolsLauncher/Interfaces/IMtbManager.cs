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

using System.Collections.Generic;

namespace HpToolsLauncher
{
    public interface IMtbManager
    {
        
        string MtbName { get;}

        string MtbFileName { get;}

        bool IsDirty { get; set; }

        string DefaultFileExtension { get; }

        void New();

        List<string> Open();

        List<string> Parse(string fileName);

        /// <summary>
        /// Save the contents to a file in the file system
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        void Save(IEnumerable<string> paths);

        void InitializeContext(string mtbName="Untitled", string mtbFileName=null);

    }
}