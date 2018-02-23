// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;
using Xamarin.Forms;

#if WINDOWS_UWP
using Colors = Windows.UI.Colors;
#else
using Colors = System.Drawing.Color;
#endif

namespace ArcGISRuntimeXamarin.Samples.GenerateOfflineMap
{
    public partial class GenerateOfflineMap : ContentPage
    {
        // Job for generating the offline map.
        private GenerateOfflineMapJob _job;

        // Constants for OAuth-related values ...
        // URL of the server to authenticate with (ArcGIS Online)
        private const string ArcGISOnlineUrl = "https://www.arcgis.com/sharing/rest";

        // Client ID for the app registered with the server (Portal Maps)
        private const string AppClientId = "2Gh53JRzkPtOENQq";

        // Redirect URL after a successful authorization (configured for the Portal Maps application)
        private const string OAuthRedirectUrl = "https://developers.arcgis.com";

        public GenerateOfflineMap()
        {
            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            // Update the authentication manager challenge handler; generating the offline map requires authentication.
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);

            try
            {
                // Load the portal.
                ArcGISPortal portal = await ArcGISPortal.CreateAsync();

                // Load the portal item by item ID.
                PortalItem webmapItem = await PortalItem.CreateAsync(portal, "acc027394bc84c2fb04d1ed317aac674");

                // Create the Map from the webmap item.
                Map myMap = new Map(webmapItem);

                // Show the Map in the MapView.
                MyMapView.Map = myMap;

                // Show a graphic representation of the map area selection.
                // - Create a new symbol for the extent graphic.
                SimpleLineSymbol lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Colors.Red, 2);
                // - Create graphics overlay for the extent graphic and apply a renderer.
                GraphicsOverlay extentOverlay = new GraphicsOverlay();
                extentOverlay.Renderer = new SimpleRenderer(lineSymbol);
                // - Add graphics overlay to the map view.
                MyMapView.GraphicsOverlays.Add(extentOverlay);
                // - Set up an event handler for when the viewpoint (extent) changes.
                MyMapView.ViewpointChanged += MapViewExtentChanged;

                // Configure authentication.
                UpdateAuthenticationManager();

                // Enable the 'take offline' button now that the sample is ready.
                TakeMapOfflineButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // Display the message to the user
                await DisplayAlert("Error", $"An error occurred. Message: {ex.Message}", "OK");
            }
        }

        private void MapViewExtentChanged(object sender, EventArgs e)
        {
            // Get the new viewpoint.
            Viewpoint myViewPoint = MyMapView?.GetCurrentViewpoint(ViewpointType.BoundingGeometry);

            // Get the updated extent for the new viewpoint.
            Envelope extent = myViewPoint?.TargetGeometry as Envelope;
            
            // Return if extent is null.
            if (extent == null)
            {
                return;
            }

            // Create an envelope that is a bit smaller than the extent.
            EnvelopeBuilder envelopeBldr = new EnvelopeBuilder(extent);
            envelopeBldr.Expand(0.80);

            // Get the (only) graphics overlay in the map view.
            var extentOverlay = MyMapView.GraphicsOverlays.First();

            // Get the extent graphic.
            Graphic extentGraphic = extentOverlay.Graphics.FirstOrDefault();

            // Create the extent graphic and add it to the overlay if it doesn't exist.
            if (extentGraphic == null)
            {
                extentGraphic = new Graphic(envelopeBldr.ToGeometry());
                extentOverlay.Graphics.Add(extentGraphic);
            }
            else
            {
                // Otherwise, update the graphic's geometry.
                extentGraphic.Geometry = envelopeBldr.ToGeometry();
            }
        }

        private async void OnTakeMapOfflineClicked(object sender, EventArgs e)
        {
            // Get the offline map path.
            var packagePath = Path.Combine(GetDataFolder(), "SampleData", "GenerateOfflineMap", $"NaperilleWaterNetwork_sample1{Guid.NewGuid().ToString()}");

            // Create the directory.
            Directory.CreateDirectory(packagePath);

            try
            {
                // Show the loading indicator.
                BusyIndicator.IsVisible = true;

                // Convert area of interest frame to an geographical envelope.
                var areaOfInterest = AreaOfInterestAsEnvelope();

                // Create task and set parameters.
                OfflineMapTask task = await OfflineMapTask.CreateAsync(MyMapView.Map);
                // - Create default parameters.
                GenerateOfflineMapParameters parameters =
                    await task.CreateDefaultGenerateOfflineMapParametersAsync(areaOfInterest);

                // Override offline package metadata.
                // - Create image from the current MapView.
                var thumbnail = await MyMapView.ExportImageAsync();
                // - Set current image to package thumbnail.
                parameters.ItemInfo.Thumbnail = thumbnail;
                // - Override title.
                parameters.ItemInfo.Title = parameters.ItemInfo + " Central";

                // Create the job.
                _job = task.GenerateOfflineMap(parameters, packagePath);

                // Subscribe to progress change events (enables showing the progress).
                _job.ProgressChanged += _job_ProgressChanged;

                // Generate all geodatabases, export all tile and vector tile packages and create a mobile map package.
                GenerateOfflineMapResult results = await _job.GetResultAsync();

                // If a job fails, something went wrong when creating the offline package.
                if (_job.Status != JobStatus.Succeeded)
                {
                    await DisplayAlert("Error", "Creating offline map package failed.", "OK");

                    // Hide the loading indicator.
                    BusyIndicator.IsVisible = false;
                }

                // If downloading one or more layers fails, show the errors to the user.
                if (results.LayerErrors.Any() || results.TableErrors.Any())
                {
                    var errorBuilder = new StringBuilder();
                    foreach (KeyValuePair<Layer, Exception> layerError in results.LayerErrors)
                    {
                        errorBuilder.AppendLine($"{layerError.Key.Id} : {layerError.Value.Message}");
                    }
                    foreach (KeyValuePair<FeatureTable, Exception> tableError in results.TableErrors)
                    {
                        errorBuilder.AppendLine($"{tableError.Key.TableName} : {tableError.Value.Message}");
                    }
                    var errorText = errorBuilder.ToString();
                    await DisplayAlert("Errors on taking layers offline", errorText, "OK");
                }

                // Show the generated offline map.
                MyMapView.Map = results.OfflineMap;

                // Keep MapView in a same position where it was when taking a map offline.
                MyMapView.SetViewpoint(new Viewpoint(areaOfInterest));

                // Update the UI.
                BusyIndicator.IsVisible = false;
                TakeMapOfflineButton.IsVisible = false;
                OfflineArea.IsVisible = true;
                MyMapView.ViewpointChanged -= MapViewExtentChanged;
                MyMapView.GraphicsOverlays.Clear();
            }
            catch (TaskCanceledException)
            {
                await DisplayAlert("Warning", "Taking map offline was canceled.", "OK");
                BusyIndicator.IsVisible = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Taking map offline failed", $"An error occurred. Message: {ex.Message}", "OK");
                BusyIndicator.IsVisible = false;
            }
        }

        private void _job_ProgressChanged(object sender, EventArgs e)
        {
            // Get the job.
            var job = (GenerateOfflineMapJob)sender;

            // Update the progress in the UI; this must be done on the UI thread.
            Device.BeginInvokeOnMainThread(() =>
            {
                // Update the UI.
                BusyText.Text = job.Progress > 0 ? $"Generating: {job.Progress} %" : string.Empty;
                ProgressBar.Progress = job.Progress / 100.0;
            });
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            // Cancel the job.
            _job.Cancel();
        }

        // Returns the area of interest for the sample as an envelope.
        private Envelope AreaOfInterestAsEnvelope()
        {
            // Get the envelope from the overlay graphic.
            return (Envelope)MyMapView.GraphicsOverlays.First().Graphics.First().Geometry;
        }

        private void UpdateAuthenticationManager()
        {
            // Define the server information for ArcGIS Online.
            ServerInfo portalServerInfo = new ServerInfo
            {
                ServerUri = new Uri(ArcGISOnlineUrl),
                TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit
            };

            // Define the OAuth information.
            OAuthClientInfo oAuthInfo = new OAuthClientInfo
            {
                ClientId = AppClientId,
                RedirectUri = new Uri(OAuthRedirectUrl)
            };
            portalServerInfo.OAuthClientInfo = oAuthInfo;

            // Get a reference to the (singleton) AuthenticationManager for the app.
            AuthenticationManager thisAuthenticationManager = AuthenticationManager.Current;

            // Register the ArcGIS Online server information with the AuthenticationManager.
            thisAuthenticationManager.RegisterServer(portalServerInfo);

            // Create a new ChallengeHandler that uses a method in this class to challenge for credentials.
            thisAuthenticationManager.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);
        }

        // ChallengeHandler function that will be called whenever access to a secured resource is attempted.
        public async Task<Credential> CreateCredentialAsync(CredentialRequestInfo info)
        {
            Credential credential;

            try
            {
                // IOAuthAuthorizeHandler will challenge the user for OAuth credentials.
                credential = await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri);
            }
            catch (Exception ex)
            {
                // First, cancel the job (the job can only succeed if the user is authenticated).
                // Note: this will happen in the event that the user cancels authentication or closes the authentication window.
                OnCancelClicked(null, null);

                // Exception will be reported in calling function.
                throw;
            }

            return credential;
        }

        internal static string GetDataFolder()
        {
#if NETFX_CORE
                return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#elif __ANDROID__ || __IOS__
                return Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
#endif
        }
    }
}
