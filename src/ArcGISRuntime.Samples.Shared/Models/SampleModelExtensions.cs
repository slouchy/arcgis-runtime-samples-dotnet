// Copyright 2016 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using System;

namespace ArcGISRuntime.Samples.Models
{
    /// <summary>
    /// Extension methods for SampleModel
    /// </summary>
    public static class SampleModelExtensions
    {
        public static string GetSampleName(this SampleModel model)
        {
            return model.SampleName;
        }

        /// <summary>
        /// Gets the name of C# the xaml file.
        /// </summary>
        public static string GetSamplesXamlFileName(this SampleModel model)
        {
            return string.Format("{0}.xaml", model.GetSampleName());
        }

        /// <summary>
        /// Gets the name of the C# code behind file.
        /// </summary>
        public static string GetSamplesCodeBehindFileName(this SampleModel model)
        {
            return string.Format("{0}.xaml.cs", model.GetSampleName());
        }

        /// <summary>
        /// Gets the relative path to the solution folder where sample is located.
        /// </summary>
        /// <remarks>
        /// This assumes that output folder is 3 levels from the repository root folder ie. repositoryRoot\output\desktop\debug
        /// </remarks>
        public static string GetSampleFolderInRelativeSolution(this SampleModel model)
        {
                    return string.Format(
                        "..\\..\\..\\src\\WPF\\ArcGISRuntime.WPF.Samples\\{0}\\{1}\\{2}",
                            model.SampleFolder.Parent.Parent.Name,
                            model.SampleFolder.Parent.Name,
                            model.SampleFolder.Name);
        }
    }
}
