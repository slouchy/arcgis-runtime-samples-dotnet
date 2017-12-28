// Copyright 2016 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using ArcGISRuntime.Samples.Models;
using ArcGISRuntime.Samples.Shared.Attributes;
using ArcGISRuntime.Samples.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel;
#endif
#if __WPF__
using System.Windows.Controls;
#endif

namespace ArcGISRuntime.Samples.Managers
{
    /// <summary>
    /// Single instance class to manage samples.
    /// </summary>
    public class SampleManager 
    {
        private Assembly _samplesAssembly;

        // Private constructor
        private SampleManager() { }

        // Static initialization of the unique instance 
        private static readonly SampleManager SingleInstance = new SampleManager();

        public static SampleManager Current
        {
            get { return SingleInstance; }
        }

        public IList<SampleInfo> AllSamples { get; set; }
        public SearchableTreeNode FullTree { get; set; }
        public SampleInfo SelectedSample { get; set; }

        public async Task InitializeAsync()
        {
            _samplesAssembly = this.GetType().GetTypeInfo().Assembly;
            AllSamples = CreateSampleInfos(_samplesAssembly).OrderBy(info => info.Category)
                .ThenBy(info => info.SampleName.ToLowerInvariant())
                .ToList();

            FullTree = BuildFullTree(AllSamples);
        }

        private static IList<SampleInfo> CreateSampleInfos(Assembly assembly)
        {
            var sampleTypes = assembly.GetTypes()
                .Where(type => type.GetTypeInfo().GetCustomAttributes().OfType<SampleAttribute>().Any());

            var samples = new List<SampleInfo>();
            foreach(Type type in sampleTypes)
            {
                try
                {
                    samples.Add(MakeSampleInfo(type));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Could not create sample from " + type + ": " + ex);
                }
            }
            return samples;
        }

        private static SampleInfo MakeSampleInfo(Type sampleType)
        {
            TypeInfo typeInfo = sampleType.GetTypeInfo();
            string category = ExtractCategoryFromNamespace(typeInfo);

            // TODO - make sample attr optional once all samples have been converted
            //var sampleAttr = GetRequiredAttribute<SampleAttribute>(typeInfo);
            var sampleAttr = GetOptionalAttribute<SampleAttribute>(typeInfo);
            if (sampleAttr == null) { return null; }

            var offlineDataAttr = GetOptionalAttribute<OfflineDataAttribute>(typeInfo);
            var xamlAttr = GetOptionalAttribute<XamlFilesAttribute>(typeInfo);
            var androidAttr = GetOptionalAttribute<AndroidLayoutAttribute>(typeInfo);
            var classAttr = GetOptionalAttribute<ClassFileAttribute>(typeInfo);

            var sample = new SampleInfo()
            {
                SampleName = sampleAttr.Name,
                Category = category,
                Description = sampleAttr.Description,
                Instructions = sampleAttr.Instructions,
                Tags = sampleAttr.Tags.ToArray(),
                SampleType = sampleType
            };

            if (offlineDataAttr != null) { sample.OfflineDataItems = offlineDataAttr.Items.ToArray(); }
            if (xamlAttr != null) { sample.XamlLayouts = xamlAttr.Files.ToArray(); }
            if (androidAttr != null) { sample.AndroidLayouts = androidAttr.Files.ToArray(); }
            if (classAttr != null) { sample.ClassFiles = classAttr.Files.ToArray(); }

            return sample;
        }

        private static T GetRequiredAttribute<T>(MemberInfo typeInfo) where T : Attribute
        {
            return (T)typeInfo.GetCustomAttributes(typeof(T)).Single();
        }

        private static T GetOptionalAttribute<T>(MemberInfo typeInfo) where T : Attribute
        {
            return typeInfo.GetCustomAttributes(typeof(T)).SingleOrDefault() as T;
        }

        private static SearchableTreeNode BuildFullTree(IEnumerable<SampleInfo> allSamples)
        {
            return new SearchableTreeNode(
                "All Samples",
                allSamples.ToLookup(s => s.Category) // put samples into lookup by category
                .OrderBy(s => s.Key)
                .Select(BuildTreeForCategory) // create a tree for each category
                .ToList());
        }

        private static SearchableTreeNode BuildTreeForCategory(IGrouping<string, SampleInfo> byCategory)
        {
            // only supporting one-level hierarchies for now, no subcategories
            return new SearchableTreeNode(
                byCategory.Key.ToString(),
                byCategory.OrderBy(si => si.SampleName)
                .ToList());
        }

        private static string ExtractCategoryFromNamespace(TypeInfo typeInfo)
        {
            string namespaceName = typeInfo.Namespace.Split('.').Last();

            // Replace _ with space
            namespaceName = namespaceName.Replace('_', ' ');

            return namespaceName;
        }

        /// <summary>
        /// Creates a new control from sample.
        /// </summary>
        /// <param name="sampleModel">Sample that is transformed into a control</param>
        /// <returns>Sample as a control.</returns>
        public object SampleToControl(SampleInfo sampleModel)
        {
            var item = Activator.CreateInstance(sampleModel.SampleType);
            return item;
        }

    }
}
