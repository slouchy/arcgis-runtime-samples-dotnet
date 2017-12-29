// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using ArcGISRuntime.Samples.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArcGISRuntime.Samples.Shared.Models
{
    public class SampleInfo
    {
        public SampleInfo(Type sampleType)
        {
            this.SampleType = sampleType;
            TypeInfo typeInfo = sampleType.GetTypeInfo();
            this.Category = ExtractCategoryFromNamespace(typeInfo);

            var sampleAttr = GetAttribute<SampleAttribute>(typeInfo);
            if (sampleAttr == null) { throw new ArgumentException("Type must be decorated with 'Sample' attribute"); }

            var offlineDataAttr = GetAttribute<OfflineDataAttribute>(typeInfo);
            var xamlAttr = GetAttribute<XamlFilesAttribute>(typeInfo);
            var androidAttr = GetAttribute<AndroidLayoutAttribute>(typeInfo);
            var classAttr = GetAttribute<ClassFileAttribute>(typeInfo);

            this.Description = sampleAttr.Description;
            this.Instructions = sampleAttr.Instructions;
            this.SampleName = sampleAttr.Name;
            this.Tags = sampleAttr.Tags;
            if (androidAttr != null) { this.AndroidLayouts = androidAttr.Files; }
            if (xamlAttr != null) { this.XamlLayouts = xamlAttr.Files; }
            if (classAttr != null) { this.ClassFiles = classAttr.Files; }
            if (offlineDataAttr != null) { this.OfflineDataItems = offlineDataAttr.Items; }
        }

        public string Path
        {
            get
            {
                return System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Samples", this.Category, SampleType.Name);
            }
        }

        private static string ExtractCategoryFromNamespace(TypeInfo typeInfo)
        {
            // Get the last part of the namespace name - this is the category
            string namespaceName = typeInfo.Namespace.Split('.').Last();

            // Replace _ with space and "Samples" with nothing
            namespaceName = namespaceName.Replace('_', ' ').Replace("Samples", "");

            return namespaceName;
        }

        private static T GetAttribute<T>(MemberInfo typeInfo) where T : Attribute
        {
            return typeInfo.GetCustomAttributes(typeof(T)).SingleOrDefault() as T;
        }

        public string SampleName { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public string Instructions { get; set; }

        public IEnumerable<string> OfflineDataItems { get; set; }

        public IEnumerable<string> Tags { get; set; }

        public IEnumerable<string> AndroidLayouts { get; set; }

        public IEnumerable<string> XamlLayouts { get; set; }

        public IEnumerable<string> ClassFiles { get; set; }

        public string Image { get { return String.Format("{0}.jpg", SampleType.Name); } }

        public Type SampleType { get; set; }

        public string SampleImageName { get { return System.IO.Path.Combine(this.Path, this.Image); } }
    }
}