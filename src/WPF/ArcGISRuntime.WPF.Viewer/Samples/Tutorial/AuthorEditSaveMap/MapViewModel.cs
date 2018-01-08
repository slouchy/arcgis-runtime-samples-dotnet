// Copyright 2016 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ArcGISRuntime.WPF.Samples.TutorialSamples
{
    // ViewModel class that is bound to the View (AuthorEditSaveMap)
    public class MapViewModel : INotifyPropertyChanged
    {
        // Store the map view used by the app
        private MapView _mapView;

        public MapView AppMapView
        {
            set { _mapView = value; }
        }

        private Map _map = new Map(Basemap.CreateStreetsVector());

        // Gets or sets the map
        public Map Map
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(); }
        }

        // String array to store basemap constructor types
        private string[] _basemapTypes = new string[]
        {
            "Topographic",
            "Topographic Vector",
            "Streets",
            "Streets Vector",
            "Imagery",
            "Oceans"
        };

        // Read-only property to return the available basemap names
        public string[] BasemapChoices
        {
            get { return _basemapTypes; }
        }

        public void ChangeBasemap(string basemap)
        {
            // Apply the selected basemap to the map
            switch (basemap)
            {
                case "Topographic":
                    // Set the basemap to Topographic
                    _map.Basemap = Basemap.CreateTopographic();
                    break;

                case "Topographic Vector":
                    // Set the basemap to Topographic (vector)
                    _map.Basemap = Basemap.CreateTopographicVector();
                    break;

                case "Streets":
                    // Set the basemap to Streets
                    _map.Basemap = Basemap.CreateStreets();
                    break;

                case "Streets Vector":
                    // Set the basemap to Streets (vector)
                    _map.Basemap = Basemap.CreateStreetsVector();
                    break;

                case "Imagery":
                    // Set the basemap to Imagery
                    _map.Basemap = Basemap.CreateImagery();
                    break;

                case "Oceans":
                    // Set the basemap to Oceans
                    _map.Basemap = Basemap.CreateOceans();
                    break;
            }
        }

        // Save the current map to ArcGIS Online. The initial extent, title, description, and tags are passed in.
        public async Task SaveNewMapAsync(Viewpoint initialViewpoint, string title, string description, string[] tags, RuntimeImage img)
        {
            // Get the ArcGIS Online portal
            ArcGISPortal agsOnline = await ArcGISPortal.CreateAsync(new Uri("https://www.arcgis.com/sharing/rest"));

            // Set the map's initial viewpoint using the extent (viewpoint) passed in
            _map.InitialViewpoint = initialViewpoint;

            // Save the current state of the map as a portal item in the user's default folder
            await _map.SaveAsAsync(agsOnline, null, title, description, tags, img);
        }

        public async void UpdateMapItem()
        {
            // Save the map
            await _map.SaveAsync();

            // Export the current map view for the thumbnail
            RuntimeImage thumbnailImg = await _mapView.ExportImageAsync();

            // Get the file stream from the new thumbnail image
            Stream imageStream = await thumbnailImg.GetEncodedBufferAsync();

            // Update the item thumbnail
            (_map.Item as PortalItem).SetThumbnailWithImage(imageStream);
            await _map.SaveAsync();
        }

        public void ResetMap()
        {
            // Set the current map to null
            _map = null;

            // Create a new map with light gray canvas basemap
            Map newMap = new Map(Basemap.CreateLightGrayCanvasVector());

            // Store the new map
            this.Map = newMap;
        }

        public bool MapIsSaved
        {
            // Return True if the current map has a value for the Item property
            get { return (_map != null && _map.Item != null); }
        }

        // Raises the "MapViewModel.PropertyChanged" event
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var propertyChangedHandler = PropertyChanged;
            if (propertyChangedHandler != null)
                propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}