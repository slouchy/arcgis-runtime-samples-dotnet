using System;
using System.Collections.Generic;
using System.Text;

namespace ArcGISRuntime.Samples.Shared.Attributes
{
    public abstract class AdditionalFilesAttribute : Attribute
    {
        private string[] files;

        public AdditionalFilesAttribute(params string[] files)
        {
            this.files = files;
        }

        public IReadOnlyList<string> Files { get { return files; } }
    }

    public class XamlFilesAttribute : AdditionalFilesAttribute { }
    public class AndroidLayoutAttribute : AdditionalFilesAttribute { }
    public class ClassFileAttribute : AdditionalFilesAttribute { }
}
