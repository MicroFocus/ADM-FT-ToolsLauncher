using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace HpToolsLauncher
{
    public class JavaIniFile : Dictionary<string, JavaProperties>
    {
        Regex reg = new Regex(@"\[(?<section>.*?)\]", RegexOptions.Compiled);
        public void Load(string filename)
        {
            this.Clear();
            //Dictionary
            StringBuilder sb = new StringBuilder();
            //Dictionary<string, JavaProperties> retVal = new Dictionary<string, JavaProperties>();

            string[] fileLines = File.ReadAllLines(filename, Encoding.Default);
            int i = 0;
            string currentSectionName = null;
            foreach (string line in fileLines)
            {
                Match match = reg.Match(line);
                if (match.Success)
                {
                    //get the new section name
                    string sectName = match.Groups["section"].Value;
                    if (currentSectionName != null)
                    {
                        //load the previous section to a properties object
                        JavaProperties props = new JavaProperties();
                        using (MemoryStream s = new MemoryStream(Encoding.Default.GetBytes(sb.ToString())))
                        {
                            props.Load(s);
                        }

                        //add to the dictionary
                        this.Add(currentSectionName, props);
                    }
                    //the current section becomes the new section
                    currentSectionName = sectName;
                    //clear the old section from the builder
                    sb.Clear();
                }
                else
                {
                    sb.AppendLine(line);
                }
                ++i;
            }

            if (currentSectionName != null)
            {
                //load the last section to a properties object
                JavaProperties props1 = new JavaProperties();
                using (MemoryStream s1 = new MemoryStream(Encoding.Default.GetBytes(sb.ToString())))
                {
                    props1.Load(s1);
                }

                //add to the dictionary
                this.Add(currentSectionName, props1);
            }
        }

        public void Save(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string sect in this.Keys)
            {
                sb.AppendLine("[" + sect + "]");
                JavaProperties props = this[sect];
                using (MemoryStream s = new MemoryStream())
                {
                    props.Save(s, null);
                    sb.AppendLine(s.ToString());
                }
            }
            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
