// Copyright 2016 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArcGISRuntime.WPF.Samples.TutorialSamples
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Author, edit, and save a map",
        "This sample demonstrates how to author and save a map as an ArcGIS portal item (web map). It is also the solution to the [Author, edit, and save maps to your portal tutorial](https://developers.arcgis.com/net/latest/wpf/guide/author-edit-and-save-maps-to-your-portal.htm). Saving a map to arcgis.com requires an ArcGIS Online login.",
        "1. Pan and zoom to the extent you would like for your map.\n2. Choose a basemap from the list of available basemaps.\n3. Click 'Save ...' and provide info for the new portal item (Title, Description, and Tags).\n4. Click 'Save Map to Portal'.\n5. After successfully logging in to your ArcGIS Online account, the map will be saved to your default folder.\n6. You can make additional changes, update the map, and then re-save to store changes in the portal item.")]
    [ArcGISRuntime.Samples.Shared.Attributes.ClassFile("MapViewModel.cs", "OAuthorize.cs")]
    public partial class AuthorEditSaveMap
    {
        private MapViewModel _mapViewModel;

        // Constants for OAuth-related values ...
        // URL of the server to authenticate with (ArcGIS Online)
        private const string ArcGISOnlineUrl = "https://www.arcgis.com/sharing/rest";

        // Client ID for the app registered with the server (Portal Maps)
        private const string AppClientId = "2Gh53JRzkPtOENQq";

        // Redirect URL after a successful authorization (configured for the Portal Maps application)
        private const string OAuthRedirectUrl = "https://developers.arcgis.com";

        public AuthorEditSaveMap()
        {
            InitializeComponent();

            // Get the view model (defined as a resource in the XAML)
            _mapViewModel = this.FindResource("MapViewModel") as MapViewModel;

            // Pass the current map view to the view model
            _mapViewModel.AppMapView = MyMapView;

            // Define a selection handler on the basemap list
            BasemapListBox.SelectionChanged += OnBasemapsClicked;

            // Define a handler for the Save Map click
            SaveMapButton.Click += OnSaveMapClick;

            // Define a handler for the New Map click
            NewMapButton.Click += OnNewMapClicked;

            UpdateAuthenticationManager();
        }

        private void OnBasemapsClicked(object sender, SelectionChangedEventArgs e)
        {
            // Get the text (basemap name) selected in the list box
            var basemapName = e.AddedItems[0].ToString();

            // Pass the basemap name to the view model method to change the basemap
            _mapViewModel.ChangeBasemap(basemapName);
        }

        private async void OnSaveMapClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a challenge request for portal credentials (OAuth credential request for arcgis.com)
                CredentialRequestInfo challengeRequest = new CredentialRequestInfo();

                // Use the OAuth implicit grant flow
                challengeRequest.GenerateTokenOptions = new GenerateTokenOptions
                {
                    TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit
                };

                // Indicate the url (portal) to authenticate with (ArcGIS Online)
                challengeRequest.ServiceUri = new Uri("https://www.arcgis.com/sharing/rest");

                // Call GetCredentialAsync on the AuthenticationManager to invoke the challenge handler
                await AuthenticationManager.Current.GetCredentialAsync(challengeRequest, false);

                // Get information for the new portal item
                var title = TitleTextBox.Text;
                var description = DescriptionTextBox.Text;
                var tags = TagsTextBox.Text.Split(',');

                // Throw an exception if the text is null or empty for title or description
                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description))
                {
                    throw new Exception("Title and description are required");
                }

                // Get current map extent (viewpoint) for the map initial extent
                var currentViewpoint = MyMapView.GetCurrentViewpoint(Esri.ArcGISRuntime.Mapping.ViewpointType.BoundingGeometry);

                // Export the current map view to use as the item's thumbnail
                RuntimeImage thumbnailImg = await MyMapView.ExportImageAsync();

                // See if the map has already been saved
                if (!_mapViewModel.MapIsSaved)
                {
                    // Call the SaveNewMapAsync method on the view model, pass in the required info
                    await _mapViewModel.SaveNewMapAsync(currentViewpoint, title, description, tags, thumbnailImg);

                    // Report success
                    MessageBox.Show("Map '" + title + "' was saved to your portal");
                }
                else
                {
                    // Map has previously been saved as a portal item, update it (title, description, and tags will remain the same)
                    _mapViewModel.UpdateMapItem();

                    // Report success
                    MessageBox.Show("Changes to '" + title + "' were updated to the portal.");
                }
            }
            catch (Exception ex)
            {
                // Report error
                MessageBox.Show("Error while saving: " + ex.Message);
            }
        }

        // Reset (create a new) map
        private void OnNewMapClicked(object sender, EventArgs e)
        {
            _mapViewModel.ResetMap();
        }

        // ChallengeHandler function that will be called whenever access to a secured resource is attempted
        public async Task<Credential> CreateCredentialAsync(CredentialRequestInfo info)
        {
            Credential credential = null;

            try
            {
                // IOAuthAuthorizeHandler will challenge the user for OAuth credentials
                credential = await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri);
            }
            catch (Exception ex)
            {
                // Exception will be reported in calling function
                throw (ex);
            }

            return credential;
        }

        private void UpdateAuthenticationManager()
        {
            // Register the server information with the AuthenticationManager
            ServerInfo portalServerInfo = new ServerInfo
            {
                ServerUri = new Uri(ArcGISOnlineUrl),
                OAuthClientInfo = new OAuthClientInfo
                {
                    ClientId = AppClientId,
                    RedirectUri = new Uri(OAuthRedirectUrl)
                },
                // Specify OAuthAuthorizationCode if you need a refresh token (and have specified a valid client secret)
                // Otherwise, use OAuthImplicit
                TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit
            };

            // Get a reference to the (singleton) AuthenticationManager for the app
            AuthenticationManager thisAuthenticationManager = AuthenticationManager.Current;

            // Register the server information
            thisAuthenticationManager.RegisterServer(portalServerInfo);

            // Use the OAuthAuthorize class in this project to create a new web view that contains the OAuth challenge handler.
            thisAuthenticationManager.OAuthAuthorizeHandler = new OAuthAuthorize();

            // Create a new ChallengeHandler that uses a method in this class to challenge for credentials
            thisAuthenticationManager.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);
        }
    }
}