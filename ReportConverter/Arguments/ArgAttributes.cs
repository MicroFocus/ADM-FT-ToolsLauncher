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
using System.Collections.Generic;
using System.Linq;

namespace ReportConverter
{
    [AttributeUsage(AttributeTargets.Property)]
    class OptionalArgAttribute : Attribute
    {
        public OptionalArgAttribute(string name) : this(new string[] { name })
        {
        }

        public OptionalArgAttribute(string name, string shortForm) : this(new string[] { name, shortForm })
        {
        }

        public OptionalArgAttribute(string[] names)
        {
            this.Names = names;
            this.Required = false;
        }

        public IEnumerable<string> Names { get; private set; }

        public bool Required { get; set; }

        public string FirstName
        {
            get
            {
                if (Names != null)
                {
                    return Names.FirstOrDefault();
                }
                return string.Empty;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class OptionalArgValueAttribute : Attribute
    {
        public OptionalArgValueAttribute(string placeholdtext)
        {
            this.PlaceholderText = placeholdtext;
        }

        public string PlaceholderText { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class PositionalArgAttribute : Attribute
    {
        public PositionalArgAttribute(int position, string placeholderText)
        {
            this.Position = position;
            this.PlaceholderText = placeholderText;
        }

        public int Position { get; private set; }

        public string PlaceholderText { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class ArgDescriptionAttribute : Attribute
    {
        private List<string> _descLines;

        private ArgDescriptionAttribute()
        {
            _descLines = new List<string>();
        }

        public ArgDescriptionAttribute(string description) : this()
        {
            _descLines.Add(description);
        }

        public ArgDescriptionAttribute(params string[] descriptionLines) : this()
        {
            _descLines.AddRange(descriptionLines);
        }

        public IEnumerable<string> Lines
        {
            get
            {
                if (_descLines.Count == 0 && !string.IsNullOrWhiteSpace(ResourceName))
                {
                    _descLines.Add(Properties.Resources.ResourceManager.GetString(ResourceName));
                }
                return _descLines;
            }
        }

        public string ResourceName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    class ArgSampleAttribute : Attribute
    {
        private List<string> _sampleLines;

        private ArgSampleAttribute()
        {
            _sampleLines = new List<string>();
        }

        public ArgSampleAttribute(string description) : this()
        {
            _sampleLines.Add(description);
        }

        public ArgSampleAttribute(params string[] descriptionLines) : this()
        {
            _sampleLines.AddRange(descriptionLines);
        }

        public IEnumerable<string> Lines
        {
            get
            {
                if (_sampleLines.Count == 0 && !string.IsNullOrWhiteSpace(ResourceName))
                {
                    _sampleLines.Add(Properties.Resources.ResourceManager.GetString(ResourceName));
                }
                return _sampleLines;
            }
        }

        public string ResourceName { get; set; }
    }
}
