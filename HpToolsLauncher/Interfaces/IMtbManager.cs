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