// Copyright 2016 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using System;
using System.Runtime.Serialization;

namespace ArcGISRuntime.Samples.Shared.Models
{

    public class SampleInfo
    {
        public string Path { get; set; }
        public string SampleName { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public string Instructions { get; set; }

        public string[] OfflineDataItems { get; set; }

        public string[] Tags { get; set; }

        public string[] AndroidLayouts { get; set; }

        public string[] XamlLayouts { get; set; }

        public string[] ClassFiles { get; set; }

        public string Image { get; set; }

        public Type SampleType { get; set; }
    }
}
