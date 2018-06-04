﻿// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;
using Foundation;
using Esri.ArcGISRuntime.UI.Controls;

namespace ArcGISRuntime.Samples.FindServiceArea
{
    [Register("FindServiceArea")]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Find Service Area",
        "Network Analysis",
        "Demonstrates how to find services areas around a point using the ServiceAreaTask. A service area shows locations that can be reached from a facility based off a certain impedance [such as travel time]. Barriers can also be added which can effect the impedance by not letting traffic through or adding the time is takes to pass that barrier.",
        "")]
    public partial class FindServiceArea : UIViewController
    {
        private MapView _myMapView = new MapView();
        private UIToolbar _toolbar = new UIToolbar();

        private const int _yPageOffset = 60;

        // All location where service areas will be found
        private List<ServiceAreaFacility> _serviceAreaFacilities;
        // Task to find service area around a facility
        private ServiceAreaTask _serviceAreaTask;
        // Used for solving task above
        private ServiceAreaParameters _serviceAreaParameters;
        // For displaying service area facilities to the mapview
        private GraphicsOverlay _facilityOverlay;
        // For displaying service areas to the mapview
        private GraphicsOverlay _serviceAreasOverlay;
        // For displaying barriers to mapview
        private GraphicsOverlay _barrierOverlay;
        // Used to make barriers
        private PolylineBuilder _barrierBuilder;
        // Fills service areas with a color when displayed to mapview
        private List<SimpleFillSymbol> _fillSymbols;
        // Used for placing geometry on mapview
        private SpatialReference _spatialReference = SpatialReferences.WebMercator;

        // Symbol for facilities
        private PictureMarkerSymbol _facilitySymbol;

        // Used for service area outlines
        private SimpleLineSymbol _outline;

        // Used to see whether the user is placing barriers or facilities
        private Boolean _barrierMode = false;
        private Boolean _facilityMode = false;

        // Uri for service area
        private Uri _sanDiegoServiceAreaUri = new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/NetworkAnalysis/SanDiego/NAServer/ServiceArea");

        // Picture for facility symbol
        private Uri _pictureMarkerUri = new Uri("http://static.arcgis.com/images/Symbols/SafetyHealth/Hospital.png");

        UIButton addFacilitiesButton = new UIButton(UIButtonType.Plain);
        UIButton addBarrierButton = new UIButton(UIButtonType.Plain);
        UIButton showServiceAreasButton = new UIButton(UIButtonType.Plain);
        UIButton resetButton = new UIButton(UIButtonType.Plain);

        public FindServiceArea()
        {
            Title = "Find service area";
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Create a new MapView control and provide its location coordinates on the frame
            _myMapView = new MapView();

            addFacilitiesButton.SetTitle("Facilities", UIControlState.Normal);
            addFacilitiesButton.SetTitleColor(UIColor.Blue, UIControlState.Normal);
            addFacilitiesButton.TouchUpInside += AddFacilities_Click;

            addBarrierButton.SetTitle("Barrier", UIControlState.Normal);
            addBarrierButton.SetTitleColor(UIColor.Blue, UIControlState.Normal);
            addBarrierButton.TouchUpInside += AddBarrier_Click;

            showServiceAreasButton.SetTitle("Service areas", UIControlState.Normal);
            showServiceAreasButton.SetTitleColor(UIColor.Blue, UIControlState.Normal);
            showServiceAreasButton.TouchUpInside += ShowServiceAreas_Click;

            resetButton.SetTitle("Reset", UIControlState.Normal);
            resetButton.SetTitleColor(UIColor.Blue, UIControlState.Normal);
            resetButton.TouchUpInside += Reset_Click;

            // Add the bar button items to an array of UIBarButtonItems
            View.AddSubviews(_myMapView, _toolbar, addFacilitiesButton, addBarrierButton, showServiceAreasButton, resetButton);

            Initialize();
        }
        public override void ViewDidLayoutSubviews()
        {
            // Setup the visual frame for the MapView
            _myMapView.Frame = new CoreGraphics.CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);

            // Setup the visual frame for the Toolbar
            _toolbar.Frame = new CoreGraphics.CGRect(0, _yPageOffset, View.Bounds.Width, 80);

            // Update toolbar item layouts
            //_runAnalysisButton.Frame = new CoreGraphics.CGRect(0, _yPageOffset + 80, View.Bounds.Width, 40);

            addFacilitiesButton.Frame = new CoreGraphics.CGRect(0, _yPageOffset, View.Bounds.Width/2, 40);
            addBarrierButton.Frame = new CoreGraphics.CGRect(View.Bounds.Width / 2, _yPageOffset, View.Bounds.Width/2, 40);
            showServiceAreasButton.Frame = new CoreGraphics.CGRect(0, _yPageOffset+40, View.Bounds.Width/2, 40);
            resetButton.Frame = new CoreGraphics.CGRect(View.Bounds.Width / 2, _yPageOffset+40, View.Bounds.Width/2, 40);


            base.ViewDidLayoutSubviews();
        }

        private void Initialize()
        {
            // Create a new service area
            CreateServiceArea();

            // Center the map on San Diego
            Map map = new Map(Basemap.CreateStreets());
            _myMapView.Map = map;
            _myMapView.SetViewpointCenterAsync(32.73, -117.14, 30000);

            // Create graphics overlays for all of the elements of the map
            _serviceAreasOverlay = new GraphicsOverlay();
            _facilityOverlay = new GraphicsOverlay();
            _barrierOverlay = new GraphicsOverlay();
            _myMapView.GraphicsOverlays.Add(_serviceAreasOverlay);
            _myMapView.GraphicsOverlays.Add(_facilityOverlay);
            _myMapView.GraphicsOverlays.Add(_barrierOverlay);

            // Link the action of tapping on the map with the MyMapView_GeoViewTapped method
            _myMapView.GeoViewTapped += MyMapView_GeoViewTapped;

            // Create a polyline builder for user constructed barriers
            _barrierBuilder = new PolylineBuilder(_spatialReference);

            // Create a list of serviceAreaFacilities
            _serviceAreaFacilities = new List<ServiceAreaFacility>();

            // Symbology for a service area
            _outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 3.0f);

            // Create a list of Fill Symbols for the polygons.
            _fillSymbols = new List<SimpleFillSymbol>();
            _fillSymbols.Add(new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(70, 255, 0, 0), _outline));
            _fillSymbols.Add(new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(70, 255, 165, 0), _outline));

            // Symbology for a facility
            _facilitySymbol = new PictureMarkerSymbol(_pictureMarkerUri);
            _facilitySymbol.Height = 30;
            _facilitySymbol.Width = 30;

        }
        
        private async void CreateServiceArea()
        {
            // Create the service area task and paramaters based on the Uri.
            _serviceAreaTask = await ServiceAreaTask.CreateAsync(_sanDiegoServiceAreaUri);
            _serviceAreaParameters = await _serviceAreaTask.CreateDefaultParametersAsync();

            // Set the parameters to return polygons.
            _serviceAreaParameters.ReturnPolygons = true;
            _serviceAreaParameters.ReturnPolylines = false;

            // Add impedance cutoffs for facilities.
            _serviceAreaParameters.DefaultImpedanceCutoffs.Add(2.0);
            _serviceAreaParameters.DefaultImpedanceCutoffs.Add(5.0);

            // Set the level of detail for the polygons.
            _serviceAreaParameters.PolygonDetail = ServiceAreaPolygonDetail.High;
        }

        private void MyMapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            // Create a MapPoint where the user clicked.
            MapPoint userTappedMapPoint = _myMapView.ScreenToLocation(e.Position);

            // Check to see if the user is in facility mode.
            if (_facilityMode)
            {
                // Add the new facility location.
                _serviceAreaFacilities.Add(new ServiceAreaFacility(userTappedMapPoint));

                // Update the graphic.
                _facilityOverlay.Graphics.Add(new Graphic(userTappedMapPoint, _facilitySymbol));
            }
            else if (_barrierMode)
            {
                // Add the point to the polyline builder.
                _barrierBuilder.AddPoint(userTappedMapPoint);
                // Update the barrier graphics.
                _barrierOverlay.Graphics.Add(new Graphic(_barrierBuilder.ToGeometry(), _outline));
            }
        }
        private void AddFacilities_Click(object sender, EventArgs e)
        {
            // Disable the facilities button.
            addFacilitiesButton.Enabled = false;
            // Enable the barrier button.
            addBarrierButton.Enabled = true;

            // Turn on facility mode.
            _facilityMode = true;
            _barrierMode = false;

        }
        private void AddBarrier_Click(object sender, EventArgs e)
        {
            // Disable the barrier button.
            addBarrierButton.Enabled = false;

            // Enable the facilities button.
            addFacilitiesButton.Enabled = true;

            // Turn on barrier mode.
            _facilityMode = false;
            _barrierMode = true;

        }
        private async void ShowServiceAreas_Click(object sender, EventArgs e)
        {
            // Enable both facility and barrier buttons.
            addBarrierButton.Enabled = true;
            addFacilitiesButton.Enabled = true;

            // Turn off any drawing mode.
            _facilityMode = false;
            _barrierMode = false;

            // Check that there is at least 1 facility to find a service area for.
            if (_serviceAreaFacilities.Count > 0)
            {
                List<PolylineBarrier> polylineBarriers = new List<PolylineBarrier>();
                foreach (Graphic barrier in _barrierOverlay.Graphics.ToList())
                {
                    polylineBarriers.Add(new PolylineBarrier((Polyline)barrier.Geometry));
                }

                // Add the barriers to the service area parameters.
                _serviceAreaParameters.SetPolylineBarriers(polylineBarriers);

                // Clear existing graphics for service areas.
                _serviceAreasOverlay.Graphics.Clear();

                // Update the parameters to include all of the placed facilities.
                _serviceAreaParameters.SetFacilities(_serviceAreaFacilities);


                try
                {

                    ServiceAreaResult result = await _serviceAreaTask.SolveServiceAreaAsync(_serviceAreaParameters);

                    // Loop over each facility.
                    for (int i = 0; i < _serviceAreaFacilities.Count; i++)
                    {
                        // Create list of polygons from a service facility.
                        List<ServiceAreaPolygon> polygons = result.GetResultPolygons(i).ToList();

                        // Loop over every polygon in every facilities result.
                        for (int j = 0; j < polygons.Count; j++)
                        {
                            // Add graphic for service area. Alternate the color of each polygon.
                            _serviceAreasOverlay.Graphics.Add(new Graphic(polygons[j].Geometry, _fillSymbols[j % 2]));
                        }
                    }
                }
                catch (Esri.ArcGISRuntime.Http.ArcGISWebException exception)
                {
                    if (exception.Message.ToString().Equals("Unable to complete operation."))
                    {
                        //System.Windows.MessageBox.Show("Facility not within San Diego area!", "Sample error");
                        CreateErrorDialog("Facility not within San Diego area!");
                    }
                    else
                    {
                        //System.Windows.MessageBox.Show("An ArcGIS web exception occurred. \n" + exception.Message.ToString(), "Sample error");
                        CreateErrorDialog("An ArcGIS web exception occurred. \n" + exception.Message.ToString());
                    }
                }
            }
            else
            {
                //System.Windows.MessageBox.Show("Must have at least 1 Facility!", "Sample error");
                CreateErrorDialog("Must have at least 1 Facility!");
            }
        }
        private void Reset_Click(object sender, EventArgs e)
        {
            // Re-enable both buttons for adding features
            addBarrierButton.Enabled = true;
            addFacilitiesButton.Enabled = true;

            // Disable the drawing mode for both features
            _facilityMode = false;
            _barrierMode = false;

            // Clear all of the graphics
            _facilityOverlay.Graphics.Clear();
            _serviceAreasOverlay.Graphics.Clear();
            _barrierOverlay.Graphics.Clear();

            // Create a new service area
            CreateServiceArea();

            // Clear the current list of facilities
            _serviceAreaFacilities.Clear();

            // Clear the existing barriers
            _barrierBuilder = new PolylineBuilder(_spatialReference);

        }
        private void CreateErrorDialog(String message)
        {
            //Create Alert
            var okAlertController = UIAlertController.Create("Error", message, UIAlertControllerStyle.Alert);

            //Add Action
            okAlertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

            // Present Alert
            PresentViewController(okAlertController, true, null);
        }
    }
}