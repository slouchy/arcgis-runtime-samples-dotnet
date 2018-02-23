// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace ArcGISRuntime.UWP.Samples.GenerateOfflineMap
{
    public partial class GenerateOfflineMap
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

                // Configure authentication.
                UpdateAuthenticationManager();

                // Enable the 'take offline' button now that the sample is ready.
                TakeMapOfflineButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // Display the message to the user
                await new MessageDialog($"An error occurred. Message: {ex.Message}", "Error").ShowAsync();
            }
        }

        private async void OnTakeMapOfflineClicked(object sender, RoutedEventArgs e)
        {
            // Get the offline map path.
            var packagePath = Path.Combine(GetDataFolder(), "SampleData", "GenerateOfflineMap", $"NaperilleWaterNetwork_sample1{Guid.NewGuid().ToString()}");

            // Create the directory
            Directory.CreateDirectory(packagePath);

            try
            {
                // Show the loading indicator.
                BusyIndicator.Visibility = Visibility.Visible;

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
                    await new MessageDialog("Creating offline map package failed.", "Error").ShowAsync();

                    // Hide the loading indicator.
                    BusyIndicator.Visibility = Visibility.Collapsed;
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
                    await new MessageDialog(errorText, "Errors on taking layers offline").ShowAsync();
                }

                // Show the generated offline map.
                MyMapView.Map = results.OfflineMap;

                // Keep MapView in a same position where it was when taking a map offline.
                MyMapView.SetViewpoint(new Viewpoint(areaOfInterest));

                // Update the UI.
                AreaOfInterestFrame.Visibility = Visibility.Collapsed;
                BusyIndicator.Visibility = Visibility.Collapsed;
                TakeMapOfflineButton.Visibility = Visibility.Collapsed;
                OfflineArea.Visibility = Visibility.Visible;
            }
            catch (TaskCanceledException)
            {
                await new MessageDialog($"Taking map offline was canceled.").ShowAsync();
                BusyIndicator.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                await new MessageDialog($"An error occurred. Message: {ex.Message}", "Taking map offline failed").ShowAsync();
                BusyIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async void _job_ProgressChanged(object sender, EventArgs e)
        {
            // Get the job.
            var job = (GenerateOfflineMapJob)sender;

            // Update the progress in the UI; this must be done on the UI thread.
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Update the UI.
                Percentage.Text = job.Progress > 0 ? $"{job.Progress} %" : string.Empty;
                ProgressBar.Value = job.Progress;
            });
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            // Cancel the job.
            _job.Cancel();
        }

        // Returns the area of interest for the sample as an envelope.
        private Envelope AreaOfInterestAsEnvelope()
        {
            // The top left and bottom right points of the area selection rectangle are used to define the map bounds.
            // - Get the top left point.
            Point topLeftPoint = AreaOfInterestFrame.TransformToVisual(MyMapView)
                              .TransformPoint(new Point(0, 0));
            // - Get the bottom right point.
            Point bottomRightPoint = AreaOfInterestFrame.TransformToVisual(MyMapView)
                  .TransformPoint(new Point(AreaOfInterestFrame.ActualWidth, AreaOfInterestFrame.ActualHeight));
            // - Construct the envelope.
            var areaOfInterest = new Envelope(
                MyMapView.ScreenToLocation(topLeftPoint),
                MyMapView.ScreenToLocation(bottomRightPoint));

            return areaOfInterest;
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
            catch (Exception)
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
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
    }
}