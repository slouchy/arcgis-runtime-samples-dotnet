// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace ArcGISRuntime.WPF.Samples.MapSamples
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Search a portal for maps",
        "This sample demonstrates searching a portal for web maps and loading them in the map view. You can search ArcGIS Online public web maps using tag values or browse the web maps in your account. OAuth is used to authenticate with ArcGIS Online to access items in your account.",
        "1. When the sample starts, you will be presented with a dialog for entering OAuth settings. If you need to create your own settings, sign in with your developer account and use the [ArcGIS for Developers dashboard](https://developers.arcgis.com/dashboard) to create an Application to store these settings.\n2. Enter values for the following OAuth settings.\n\t1. **Client ID**: a unique alphanumeric string identifier for your application\n\t2. **Redirect URL**: a URL to which a successful OAuth login response will be sent\n3. If you do not enter OAuth settings, you will be able to search public web maps on ArcGIS Online. Browsing the web map items in your ArcGIS Online account will be disabled, however.")]
    [ArcGISRuntime.Samples.Shared.Attributes.ClassFile("OAuthAuthorize_SPM")]
    public partial class SearchPortalMaps
    {
        // Variables for OAuth with default values ...
        // URL of the server to authenticate with (ArcGIS Online)
        private const string ArcGISOnlineUrl = "https://www.arcgis.com/sharing/rest";

        // Client ID for the app registered with the server (Portal Maps)
        private string _appClientId = "2Gh53JRzkPtOENQq";

        // Redirect URL after a successful authorization (configured for the Portal Maps application)
        private string _oAuthRedirectUrl = "https://developers.arcgis.com";

        // Constructor for sample class
        public SearchPortalMaps()
        {
            InitializeComponent();

            // Show the OAuth settings in the page
            ClientIdTextBox.Text = _appClientId;
            RedirectUrlTextBox.Text = _oAuthRedirectUrl;

            // Display a default map
            DisplayDefaultMap();
        }

        private void DisplayDefaultMap()
        {
            // Create a new Map instance
            Map myMap = new Map(Basemap.CreateLightGrayCanvas());

            // Provide Map to the MapView
            MyMapView.Map = myMap;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Get web map portal items in the current user's folder or from a keyword search
            IEnumerable<PortalItem> mapItems = null;
            ArcGISPortal portal;

            // See if the user wants to search public web map items
            if (SearchPublicMaps.IsChecked == true)
            {
                // Connect to the portal (anonymously)
                portal = await ArcGISPortal.CreateAsync();

                // Create a query expression that will get public items of type 'web map' with the keyword(s) in the items tags
                var queryExpression = string.Format("tags:\"{0}\" access:public type: (\"web map\" NOT \"web mapping application\")", SearchText.Text);
                
                // Create a query parameters object with the expression and a limit of 10 results
                PortalQueryParameters queryParams = new PortalQueryParameters(queryExpression, 10);

                // Search the portal using the query parameters and await the results
                PortalQueryResultSet<PortalItem> findResult = await portal.FindItemsAsync(queryParams);
                
                // Get the items from the query results
                mapItems = findResult.Results;
            }
            else
            {
                // Call a sub that will force the user to log in to ArcGIS Online (if they haven't already)
                var loggedIn = await EnsureLoggedInAsync();
                if (!loggedIn) { return; }

                // Connect to the portal (will connect using the provided credentials)
                portal = await ArcGISPortal.CreateAsync(new Uri(ArcGISOnlineUrl));

                // Get the user's content (items in the root folder and a collection of sub-folders)
                PortalUserContent myContent = await portal.User.GetContentAsync();

                // Get the web map items in the root folder
                mapItems = from item in myContent.Items where item.Type == PortalItemType.WebMap select item;

                // Loop through all sub-folders and get web map items, add them to the mapItems collection
                foreach (PortalFolder folder in myContent.Folders)
                {
                    IEnumerable<PortalItem> folderItems = await portal.User.GetContentAsync(folder.FolderId);
                    mapItems.Concat(from item in folderItems where item.Type == PortalItemType.WebMap select item);
                }
            }

            // Show the web map portal items in the list box
            MapListBox.ItemsSource = mapItems;
        }

        private void LoadMapButtonClick(object sender, RoutedEventArgs e)
        {
            // Get the selected web map item in the list box
            PortalItem selectedMap = MapListBox.SelectedItem as PortalItem;
            if (selectedMap == null) { return; }

            // Create a new map, pass the web map portal item to the constructor
            Map webMap = new Map(selectedMap);

            // Handle change in the load status (to report load errors)
            webMap.LoadStatusChanged += WebMapLoadStatusChanged;
            
            // Show the web map in the map view
            MyMapView.Map = webMap;
        }

        private void WebMapLoadStatusChanged(object sender, Esri.ArcGISRuntime.LoadStatusEventArgs e)
        {
            // Get the current status
            var status = e.Status;

            // Report errors if map failed to load
            if(status == Esri.ArcGISRuntime.LoadStatus.FailedToLoad)
            {
                var map = sender as Map;
                var err = map.LoadError;
                if(err != null)
                {
                    MessageBox.Show(err.Message, "Map Load Error");
                }
            }
        }

        private void RadioButtonUnchecked(object sender, RoutedEventArgs e)
        {
            // When the search/user radio buttons are unchecked, clear the list box
            MapListBox.ItemsSource = null;

            // Set the map to the default (if necessary)
            if (MyMapView.Map.Item != null)
            {
                DisplayDefaultMap();
            }
        }

        private async Task<bool> EnsureLoggedInAsync()
        {
            bool loggedIn = false;

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
                challengeRequest.ServiceUri = new Uri(ArcGISOnlineUrl);

                // Call GetCredentialAsync on the AuthenticationManager to invoke the challenge handler
                var cred = await AuthenticationManager.Current.GetCredentialAsync(challengeRequest, false);
                loggedIn = cred != null;
            }
            catch (OperationCanceledException ex)
            {
                // OAuth login was canceled
                // .. ignore this, the user can still search public maps
            }
            catch (Exception ex)
            {
                // Login failure
                MessageBox.Show("Login failed: " + ex.Message);
            }

            return loggedIn;
        }

        private void UpdateAuthenticationManager()
        {
            // Define the server information for ArcGIS Online
            ServerInfo portalServerInfo = new ServerInfo();

            // ArcGIS Online URI
            portalServerInfo.ServerUri = new Uri(ArcGISOnlineUrl);

            // Type of token authentication to use
            portalServerInfo.TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit;

            // Define the OAuth information
            OAuthClientInfo oAuthInfo = new OAuthClientInfo
            {
                ClientId = _appClientId,
                RedirectUri = new Uri(_oAuthRedirectUrl)
            };
            portalServerInfo.OAuthClientInfo = oAuthInfo;

            // Get a reference to the (singleton) AuthenticationManager for the app
            AuthenticationManager thisAuthenticationManager = AuthenticationManager.Current;

            // Register the ArcGIS Online server information with the AuthenticationManager
            thisAuthenticationManager.RegisterServer(portalServerInfo);

            // Use the OAuthAuthorize_SPM class in this project to create a new web view to show the login UI
            thisAuthenticationManager.OAuthAuthorizeHandler = new OAuthAuthorize_SPM();

            // Create a new ChallengeHandler that uses a method in this class to challenge for credentials
            thisAuthenticationManager.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);
        }

        // ChallengeHandler function that will be called whenever access to a secured resource is attempted
        public async Task<Credential> CreateCredentialAsync(CredentialRequestInfo info)
        {
            Credential credential = null;

            try
            {
                // IOAuthAuthorize_SPMHandler will challenge the user for OAuth credentials
                credential = await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri);
            }
            catch (Exception ex)
            {
                // Exception will be reported in calling function
                throw (ex);
            }

            return credential;
        }

        private void SaveOAuthSettingsClicked(object sender, RoutedEventArgs e)
        {
            // Settings were provided, update the configuration settings for OAuth authorization
            _appClientId = ClientIdTextBox.Text.Trim();
            _oAuthRedirectUrl = RedirectUrlTextBox.Text.Trim();

            // Update authentication manager with the OAuth settings
            UpdateAuthenticationManager();

            // Hide the OAuth input, show the search UI
            OAuthSettingsGrid.Visibility = Visibility.Collapsed;
            SearchUI.Visibility = Visibility.Visible;
        }

        private void CancelOAuthSettingsClicked(object sender, RoutedEventArgs e)
        {
            // Warn that browsing user's ArcGIS Online maps won't be available without OAuth settings
            var warning = "Without OAuth settings, you will not be able to browse maps from your ArcGIS Online account.";
            var noAuth = MessageBox.Show(warning, "No OAuth Settings", MessageBoxButton.OKCancel) == MessageBoxResult.OK;

            if (noAuth)
            {
                // Disable browsing maps from your ArcGIS Online account
                BrowseMyMaps.IsEnabled = false;

                // Hide the OAuth input, show the search UI
                OAuthSettingsGrid.Visibility = Visibility.Collapsed;
                SearchUI.Visibility = Visibility.Visible;
            }
        }
    }
}