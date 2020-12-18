using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public IEnumerable<string> Lines { get { return _descLines; } }
    }
}
