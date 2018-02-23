// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Esri.ArcGISRuntime.Data;

namespace ArcGISRuntime.WPF.Samples.GenerateOfflineMap
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
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(CreateKnownCredentials);

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
                MessageBox.Show("An error occurred. " + ex, "Error");
            }
        }

        private async void OnTakeMapOfflineClicked(object sender, RoutedEventArgs e)
        {
            // Get the offline map path.
            var packagePath = Path.Combine(GetDataFolder(), "SampleData", "GenerateOfflineMap", "NaperilleWaterNetwork_sample1");

            // If we already have a package with this name, replace it with a new one.
            if (Directory.Exists(packagePath))
            {
                Directory.Delete(packagePath, true);
            }
            else
            {
                Directory.CreateDirectory(packagePath);
            }

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
                    MessageBox.Show("Creating offline map package failed.", "Error");

                    // Hide the loading indicator.
                    BusyIndicator.Visibility = Visibility.Collapsed;
                }

                // If downloading one or more layers fails, show the errors to the user.
                if (results.LayerErrors.Any() || results.TableErrors.Any())
                {
                    var errorBuilder = new StringBuilder();
                    foreach (KeyValuePair<Layer, Exception> layerError in results.LayerErrors)
                    {
                        errorBuilder.AppendLine(string.Format("{0} : {1}", layerError.Key.Id, layerError.Value.Message));
                    }
                    foreach (KeyValuePair<FeatureTable, Exception> tableError in results.TableErrors)
                    {
                        errorBuilder.AppendLine(string.Format("{0} : {1}", tableError.Key.TableName, tableError.Value.Message));
                    }
                    var errorText = errorBuilder.ToString();
                    MessageBox.Show(errorText, "Errors on taking layers offline");
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
                MessageBox.Show("Taking map offline was canceled");
                BusyIndicator.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Taking map offline failed.");
                BusyIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void _job_ProgressChanged(object sender, EventArgs e)
        {
            // Get the job.
            var job = (GenerateOfflineMapJob)sender;

            // Update the progress in the UI; this must be done on the UI thread.
            Dispatcher.Invoke(() =>
            {
                // Update the UI.
                Percentage.Text = job.Progress > 0 ? String.Format("{0} %", job.Progress) : string.Empty;
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
                              .Transform(new Point(0, 0));
            // - Get the bottom right point.
            Point bottomRightPoint = AreaOfInterestFrame.TransformToVisual(MyMapView)
                  .Transform(new Point(AreaOfInterestFrame.ActualWidth, AreaOfInterestFrame.ActualHeight));
            // - Construct the envelope.
            var areaOfInterest = new Envelope(
                MyMapView.ScreenToLocation(topLeftPoint),
                MyMapView.ScreenToLocation(bottomRightPoint));

            return areaOfInterest;
        }

        // ChallengeHandler function that will be called whenever access to a secured resource is attempted.
        public async Task<Credential> CreateKnownCredentials(CredentialRequestInfo info)
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

        private void UpdateAuthenticationManager()
        {
            // Register the server information with the AuthenticationManager.
            ServerInfo portalServerInfo = new ServerInfo
            {
                ServerUri = new Uri(ArcGISOnlineUrl),
                OAuthClientInfo = new OAuthClientInfo
                {
                    ClientId = AppClientId,
                    RedirectUri = new Uri(OAuthRedirectUrl)
                },
                // Specify OAuthAuthorizationCode if you need a refresh token (and have specified a valid client secret).
                // Otherwise, use OAuthImplicit.
                TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit
            };

            // Get a reference to the (singleton) AuthenticationManager for the app.
            AuthenticationManager thisAuthenticationManager = AuthenticationManager.Current;

            // Register the server information.
            thisAuthenticationManager.RegisterServer(portalServerInfo);

            // Use the OAuthAuthorize class in this project to create a new web view that contains the OAuth challenge handler..
            thisAuthenticationManager.OAuthAuthorizeHandler = new OAuthAuthorize();

            // Create a new ChallengeHandler that uses a method in this class to challenge for credentials.
            thisAuthenticationManager.ChallengeHandler = new ChallengeHandler(CreateKnownCredentials);
        }

        internal static string GetDataFolder()
        {
            return Directory.GetCurrentDirectory();
        }
    }

    #region OAuth handler

    public class OAuthAuthorize : IOAuthAuthorizeHandler
    {
        // Window to contain the OAuth UI.
        private Window _window;

        // Use a TaskCompletionSource to track the completion of the authorization.
        private TaskCompletionSource<IDictionary<string, string>> _tcs;

        // URL for the authorization callback result (the redirect URI configured for your application).
        private string _callbackUrl;

        // URL that handles the OAuth request.
        private string _authorizeUrl;

        // Function to handle authorization requests, takes the URIs for the secured service, the authorization endpoint, and the redirect URI.
        public Task<IDictionary<string, string>> AuthorizeAsync(Uri serviceUri, Uri authorizeUri, Uri callbackUri)
        {
            // If the TaskCompletionSource or Window are not null, authorization is in progress.
            if (_tcs != null || _window != null)
            {
                // Allow only one authorization process at a time.
                throw new Exception();
            }

            // Store the authorization and redirect URLs.
            _authorizeUrl = authorizeUri.AbsoluteUri;
            _callbackUrl = callbackUri.AbsoluteUri;

            // Create a task completion source.
            _tcs = new TaskCompletionSource<IDictionary<string, string>>();

            // Call a function to show the login controls, make sure it runs on the UI thread for this app.
            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher == null)
                AuthorizeOnUiThread(_authorizeUrl);
            else
            {
                var authorizeOnUiAction = new Action(() => AuthorizeOnUiThread(_authorizeUrl));
                dispatcher.BeginInvoke(authorizeOnUiAction);
            }

            // Return the task associated with the TaskCompletionSource.
            return _tcs != null ? _tcs.Task : null;
        }

        // Challenge for OAuth credentials on the UI thread.
        private void AuthorizeOnUiThread(string authorizeUri)
        {
            // Create a WebBrowser control to display the authorize page.
            var webBrowser = new WebBrowser();

            // Handle the navigation event for the browser to check for a response to the redirect URL.
            webBrowser.Navigating += WebBrowserOnNavigating;

            // Display the web browser in a new window.
            _window = new Window
            {
                Content = webBrowser,
                Height = 430,
                Width = 395,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // Set the app's window as the owner of the browser window (if main window closes, so will the browser).
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                _window.Owner = Application.Current.MainWindow;
            }

            // Handle the window closed event then navigate to the authorize URL.
            _window.Closed += OnWindowClosed;
            webBrowser.Navigate(authorizeUri);

            // Display the Window.
            _window.ShowDialog();
        }

        // Handle the browser window closing.
        private void OnWindowClosed(object sender, EventArgs e)
        {
            // If the browser window closes, return the focus to the main window.
            if (_window != null && _window.Owner != null)
            {
                _window.Owner.Focus();
            }

            // If the task wasn't completed, the user must have closed the window without logging in.
            if (_tcs != null && !_tcs.Task.IsCompleted)
            {
                // Set the task completion source exception to indicate a canceled operation.
                _tcs.SetException(new OperationCanceledException());
            }

            // Set the task completion source and window to null to indicate the authorization process is complete.
            _tcs = null;
            _window = null;
        }

        // Handle browser navigation (content changing).
        private void WebBrowserOnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            // Check for a response to the callback URL.
            const string portalApprovalMarker = "/oauth2/approval";
            var webBrowser = sender as WebBrowser;
            Uri uri = e.Uri;

            // If no browser, URI, task completion source, or an empty URL, return.
            if (webBrowser == null || uri == null || _tcs == null || string.IsNullOrEmpty(uri.AbsoluteUri))
                return;

            // Check for redirect.
            bool isRedirected = uri.AbsoluteUri.StartsWith(_callbackUrl) ||
                _callbackUrl.Contains(portalApprovalMarker) && uri.AbsoluteUri.Contains(portalApprovalMarker);

            if (isRedirected)
            {
                // Browser was redirected to the callbackUrl (success!).
                //    - close the window.
                //    - decode the parameters (returned as fragments or query).
                //    - return these parameters as result of the Task.
                e.Cancel = true;
                TaskCompletionSource<IDictionary<string, string>> tcs = _tcs;
                _tcs = null;
                if (_window != null)
                {
                    _window.Close();
                }

                // Call a helper function to decode the response parameters.
                IDictionary<string, string> authResponse = DecodeParameters(uri);

                // Set the result for the task completion source.
                tcs.SetResult(authResponse);
            }
        }

        private static IDictionary<string, string> DecodeParameters(Uri uri)
        {
            // Create a dictionary of key value pairs returned in an OAuth authorization response URI query string.
            var answer = string.Empty;

            // Get the values from the URI fragment or query string.
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                answer = uri.Fragment.Substring(1);
            }
            else
            {
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    answer = uri.Query.Substring(1);
                }
            }

            // Parse parameters into key / value pairs.
            Dictionary<string, string> keyValueDictionary = new Dictionary<string, string>();
            string[] keysAndValues = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var kvString in keysAndValues)
            {
                string[] pair = kvString.Split('=');
                string key = pair[0];
                string value = string.Empty;
                if (key.Length > 1)
                {
                    value = Uri.UnescapeDataString(pair[1]);
                }

                keyValueDictionary.Add(key, value);
            }

            // Return the dictionary of string keys/values.
            return keyValueDictionary;
        }
    }

    #endregion OAuth handler
}