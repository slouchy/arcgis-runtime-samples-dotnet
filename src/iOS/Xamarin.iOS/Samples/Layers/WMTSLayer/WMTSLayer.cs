// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using System;
using System.Collections.Generic;
using UIKit;

namespace ArcGISRuntime.Samples.WMTSLayer
{
    [Register("WMTSLayer")]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "WMTS layer",
        "Layers",
        "This sample demonstrates how to display a WMTS layer on a map via a Uri and WmtsLayerInfo.",
        "")]
    public class WMTSLayer : UIViewController
    {
        // Create and hold reference to the used MapView
        private readonly MapView _myMapView = new MapView();

        // Create button
        private UIButton _button1;

        // Create button
        private UIButton _button2;

        // Create toolbar
        private readonly UIToolbar _toolbar = new UIToolbar();

        // Create help label
        private UILabel _label;

        public WMTSLayer()
        {
            Title = "WMTS layer";
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Create the UI, setup the control references
            CreateLayout();
        }

        public override void ViewDidLayoutSubviews()
        {
            int controlHeight = 30;
            int margin = 5;

            // Setup the visual frame for the MapView
            _myMapView.Frame = new CoreGraphics.CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);

            // Setup the visual frame for the toolbar
            _toolbar.Frame = new CoreGraphics.CGRect(0, View.Bounds.Height - (2 * controlHeight) - (3 * margin), View.Bounds.Width, 2 * controlHeight + 3 * margin);

            // Setup the visual frame for the help label
            _label.Frame = new CoreGraphics.CGRect(margin, View.Bounds.Height - (2 * controlHeight) - (2 * margin), View.Bounds.Width - (2 * margin), controlHeight);

            // Setup the visual frame for button1
            _button1.Frame = new CoreGraphics.CGRect(margin, View.Bounds.Height - controlHeight - margin, View.Bounds.Width / 2 - (2 * margin), controlHeight);

            // Setup the visual frame for button2
            _button2.Frame = new CoreGraphics.CGRect(View.Bounds.Width / 2 + margin, View.Bounds.Height - controlHeight - (margin), View.Bounds.Width / 2 - (2 * margin), controlHeight);

            base.ViewDidLayoutSubviews();
        }

        private void OnButton1Clicked(object sender, EventArgs e)
        {
            try
            {
                // Define the Uri to the WMTS service (NOTE: iOS applications require the use of Uri's to be https:// and not http://)
                var myUri = new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/WorldTimeZones/MapServer/WMTS");

                // Create a new instance of a WMTS layer using a Uri and provide an Id value
                WmtsLayer myWmtsLayer = new WmtsLayer(myUri, "WorldTimeZones");

                // Create a new map
                Map myMap = new Map();

                // Get the basemap from the map
                Basemap myBasemap = myMap.Basemap;

                // Get the layer collection for the base layers
                LayerCollection myLayerCollection = myBasemap.BaseLayers;

                // Add the WMTS layer to the layer collection of the map
                myLayerCollection.Add(myWmtsLayer);

                // Assign the map to the MapView
                _myMapView.Map = myMap;
            }
            catch (Exception ex)
            {
                // Report error
                UIAlertController alert = UIAlertController.Create("Error", ex.Message, UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                PresentViewController(alert, true, null);
            }
        }

        private async void OnButton2Clicked(object sender, EventArgs e)
        {
            try
            {
                // Define the Uri to the WMTS service (NOTE: iOS applications require the use of Uri's to be https:// and not http://)
                var myUri = new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/WorldTimeZones/MapServer/WMTS");

                // Define a new instance of the WMTS service
                WmtsService myWmtsService = new WmtsService(myUri);

                // Load the WMTS service 
                await myWmtsService.LoadAsync();

                // Get the service information (i.e. metadata) about the WMTS service
                WmtsServiceInfo myWMTSServiceInfo = myWmtsService.ServiceInfo;

                // Obtain the read only list of WMTS layer info objects
                IReadOnlyList<WmtsLayerInfo> myWmtsLayerInfos = myWMTSServiceInfo.LayerInfos;

                // Create a new instance of a WMTS layer using the first item in the read only list of WMTS layer info objects
                WmtsLayer myWmtsLayer = new WmtsLayer(myWmtsLayerInfos[0]);

                // Create a new map
                Map myMap = new Map();

                // Get the basemap from the map
                Basemap myBasemap = myMap.Basemap;

                // Get the layer collection for the base layers
                LayerCollection myLayerCollection = myBasemap.BaseLayers;

                // Add the WMTS layer to the layer collection of the map
                myLayerCollection.Add(myWmtsLayer);

                // Assign the map to the MapView
                _myMapView.Map = myMap;
            }
            catch (Exception ex)
            {
                // Report error
                UIAlertController alert = UIAlertController.Create("Error", ex.Message, UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                PresentViewController(alert, true, null);
            }
        }

        private void CreateLayout()
        {

            // Create button1
            _button1 = new UIButton();
            _button1.SetTitle("Service URL", UIControlState.Normal);
            _button1.SetTitleColor(View.TintColor, UIControlState.Normal);
            _button1.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;

            // Hook to touch event to do button1
            _button1.TouchUpInside += OnButton1Clicked;

            // Create button2
            _button2 = new UIButton();
            _button2.SetTitle("WmtsLayerInfo", UIControlState.Normal);
            _button2.SetTitleColor(View.TintColor, UIControlState.Normal);
            _button2.HorizontalAlignment = UIControlContentHorizontalAlignment.Right;

            // Hook to touch event to do button2
            _button2.TouchUpInside += OnButton2Clicked;

            // Create the help label
            _label = new UILabel
            {
                Text = "Construct layer with:",
                TextAlignment = UITextAlignment.Center
            };

            // Add controls to the page
            View.AddSubviews(_myMapView, _toolbar, _label, _button1, _button2);
        }
    }
}