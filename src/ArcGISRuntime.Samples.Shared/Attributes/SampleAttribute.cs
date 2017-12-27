using System;
using System.Collections.Generic;
using System.Text;

namespace ArcGISRuntime.Samples.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class SampleAttribute : Attribute
    {
        private string name;
        private string description;
        private string instructions;
        private string[] tags;

        public SampleAttribute(string name, string description, string instructions, params string[] tags)
        {
            this.name = name;
            this.description = description;
            this.instructions = instructions;
            this.tags = tags;
        }

        public string Name { get { return name; } }
        public string Description { get { return description; } }
        public string Instructions { get { return instructions; } }
        public IReadOnlyList<string> Tags { get { return tags; } }
    }
}
