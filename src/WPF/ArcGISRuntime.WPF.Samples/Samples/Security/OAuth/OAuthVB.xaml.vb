' Copyright 2016 Esri.
'
' Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
' You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
' "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
' language governing permissions and limitations under the License.

Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Navigation
Imports Esri.ArcGISRuntime.Mapping
Imports Esri.ArcGISRuntime.Portal
Imports Esri.ArcGISRuntime.Security

Namespace OAuth
    Partial Public Class OAuthVB
        ' Constants for OAuth-related values ...
        ' URL of the server to authenticate with
        Private Const ServerUrl As String = "https://www.arcgis.com/sharing/rest"

        ' TODO: Provide the client ID for your app (registered with the server)
        Private Const ClientId As String = "lgAdHkYZYlwwfAhC"

        ' TODO: [optional] Provide the client secret for the app (only needed for the OAuthAuthorizationCode auth type)
        Private Const ClientSecret As String = ""

        ' TODO: Provide a URL registered for the app for redirecting after a successful authorization
        Private Const RedirectUrl As String = "my-ags-app://auth"

        ' TODO: Provide an ID for a secured web map item hosted on the server
        Private Const WebMapId As String = "cbd8ac5252fa4cf8a55d8350265c531b"

        Public Sub New()
            InitializeComponent()

            ' Create the UI, setup the control references and execute initialization 
            Initialize()
        End Sub

        Private Async Sub Initialize()
            ' Set up the AuthenticationManager to authenticate requests for secure ArcGIS Online resources with OAuth
            SetOAuthInfo()

            ' Connect to a portal (ArcGIS Online, for example)
            Dim arcgisPortal As ArcGISPortal = Await ArcGISPortal.CreateAsync(New Uri(ServerUrl))

            ' Get a web map portal item using its ID
            ' (If the item contains layers not shared publicly, the user will be challenged for credentials at this point)
            Dim portalItem As PortalItem = Await PortalItem.CreateAsync(arcgisPortal, WebMapId)

            ' Create a New map with the portal item
            Dim myMap As Map = New Map(portalItem)

            ' Assign the map to the MapView.Map property to display it in the app
            MyMapView.Map = myMap
        End Sub

        Private Sub SetOAuthInfo()
            ' Register the server information with the AuthenticationManager
            Dim serverInfo As Esri.ArcGISRuntime.Security.ServerInfo = New ServerInfo With
                {
                    .ServerUri = New Uri(ServerUrl),
                    .TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit,
                    .OAuthClientInfo = New OAuthClientInfo With
                    {
                        .ClientId = ClientId,
                        .RedirectUri = New Uri(RedirectUrl)
                    }
                }

            ' If a client secret has been configured, set the authentication type to OAuthAuthorizationCode
            If Not String.IsNullOrEmpty(ClientSecret) Then
                ' Use OAuthAuthorizationCode if you need a refresh token (And have specified a valid client secret)
                serverInfo.TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode
                serverInfo.OAuthClientInfo.ClientSecret = ClientSecret
            End If

            ' Register this server with AuthenticationManager
            AuthenticationManager.Current.RegisterServer(serverInfo)

            ' Use the OAuthAuthorize class in this project to handle OAuth communication
            AuthenticationManager.Current.OAuthAuthorizeHandler = New OAuthAuthorize()

            ' Use a function in this class to challenge for credentials
            AuthenticationManager.Current.ChallengeHandler = New ChallengeHandler(AddressOf CreateCredentialAsync)
        End Sub

        Public Async Function CreateCredentialAsync(info As CredentialRequestInfo) As Task(Of Credential)
            ' ChallengeHandler function for AuthenticationManager that will be called whenever access to a secured
            ' resource Is attempted
            Dim cred As TokenCredential = Nothing

            Try
                ' AuthenticationManager will handle challenging the user for credentials
                cred = Await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri)
            Catch
                ' Exception will be reported in calling function
                Throw
            End Try

            Return cred
        End Function
    End Class

    ' In a desktop (WPF) app, an IOAuthAuthorizeHandler component is used to handle some of the OAuth details. Specifically, it
    '     implements AuthorizeAsync to show the login UI (generated by the server that hosts secure content) in a web control.
    '     When the user logs in successfully, cancels the login, or closes the window without continuing, the IOAuthAuthorizeHandler
    '     is responsible for obtaining the authorization from the server or raising an OperationCanceledException.
    ' Note: a custom IOAuthAuthorizeHandler component is not necessary when using OAuth in an ArcGIS Runtime Universal Windows app.
    '     The UWP AuthenticationManager uses a built-in IOAuthAuthorizeHandler that is based on WebAuthenticationBroker.
    Public Class OAuthAuthorize
        Implements IOAuthAuthorizeHandler
        ' Window to contain the OAuth UI
        Private _window As Window

        ' Use a TaskCompletionSource to track the completion of the authorization
        Private _tcs As TaskCompletionSource(Of IDictionary(Of String, String))

        ' URL for the authorization callback result (the redirect URI configured for your application)
        Private _callbackUrl As String

        ' URL that handles the OAuth request
        Private _authorizeUrl As String

        ' Function to handle authorization requests, takes the URIs for the secured service, the authorization endpoint, And the redirect URI
        Public Function AuthorizeAsync(serviceUri As Uri, authorizeUri As Uri, callbackUri As Uri) As Task(Of IDictionary(Of String, String)) Implements IOAuthAuthorizeHandler.AuthorizeAsync
            ' If the TaskCompletionSource.Task hasn't completed, authorization is still in progress
            If Not _tcs Is Nothing Or Not _window Is Nothing Then
                ' Allow only one authorization process at a time
                Throw New Exception("Task in progress")
            End If

            ' Store the authorization And redirect URLs
            _authorizeUrl = authorizeUri.AbsoluteUri
            _callbackUrl = callbackUri.AbsoluteUri

            ' Create a task completion source
            _tcs = New TaskCompletionSource(Of IDictionary(Of String, String))()

            ' Call a function to show the login controls, make sure it runs on the UI thread for this app
            Dim thisDispatcher As Threading.Dispatcher = Application.Current.Dispatcher
            If thisDispatcher Is Nothing Or thisDispatcher.CheckAccess() Then
                AuthorizeOnUIThread(_authorizeUrl)
            Else
                Dim authorizeSub As AuthorizeDelegate = New AuthorizeDelegate(AddressOf AuthorizeOnUIThread)
                thisDispatcher.BeginInvoke(authorizeSub, _authorizeUrl)
            End If

            ' Return the task associated with the TaskCompletionSource
            Return _tcs.Task
        End Function

        ' Delegate to handle calling AuthorizeOnUIThread from BeginInvoke
        Delegate Sub AuthorizeDelegate(url As String)

        ' Challenge for OAuth credentials on the UI thread
        Private Sub AuthorizeOnUIThread(authorizeUri As String)
            ' Create a WebBrowser control to display the authorize page
            Dim browser As WebBrowser = New WebBrowser()

            ' Handle the navigation event for the browser to check for a response to the redirect URL
            AddHandler browser.Navigating, AddressOf WebBrowserOnNavigating

            ' Display the web browser in a New window 
            _window = New Window With
                {
                    .Content = browser,
                    .Height = 430,
                    .Width = 395,
                    .WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    .Owner = Nothing
                }

            If Not Application.Current Is Nothing And Not Application.Current.MainWindow Is Nothing Then
                _window.Owner = Application.Current.MainWindow
            End If

            ' Handle the window closed event then navigate to the authorize url
            AddHandler _window.Closed, AddressOf OnWindowClosed
            browser.Navigate(authorizeUri)

            ' Display the Window
            If Not _window Is Nothing Then
                _window.ShowDialog()
            End If
        End Sub

        Private Sub OnWindowClosed(sender As Object, e As EventArgs)

            ' If the browser window closes, return the focus to the main window
            If Not _window Is Nothing AndAlso Not _window.Owner Is Nothing Then
                _window.Owner.Focus()
            End If

            If Not _tcs Is Nothing And Not _tcs.Task.IsCompleted Then
                ' The user closed the window
                _tcs.TrySetCanceled()
            End If

            ' Set the window and task completion source to null
            _window = Nothing
            _tcs = Nothing
        End Sub

        ' Handle browser navigation (content changing)
        Private Sub WebBrowserOnNavigating(sender As Object, e As NavigatingCancelEventArgs)
            ' Check for a response to the callback url
            Dim browser As WebBrowser = TryCast(sender, WebBrowser)
            Dim currentUri As Uri = e.Uri

            ' If no browser, uri, or an empty url, return
            If browser Is Nothing Or currentUri Is Nothing OrElse String.IsNullOrEmpty(currentUri.AbsoluteUri) Then Return

            ' Check for redirect
            Dim isRedirected As Boolean = currentUri.AbsoluteUri.StartsWith(_callbackUrl)

            If isRedirected Then
                ' If the web browser Is redirected to the callbackUrl:
                '    -close the window 
                '    -decode the parameters (returned as fragments Or query)
                '    -return these parameters as result of the Task
                e.Cancel = True

                Dim tcs As TaskCompletionSource(Of IDictionary(Of String, String)) = _tcs
                _tcs = Nothing

                ' Close the window
                If Not _window Is Nothing Then
                    _window.Close()
                End If

                ' Call a helper function to decode the response parameters
                Dim authResponse As IDictionary(Of String, String) = DecodeParameters(currentUri)

                ' Set the result for the task completion source
                tcs.SetResult(authResponse)
            End If
        End Sub

        ' Decodes the parameters returned when the browser Is redirected to the callback url
        Private Shared Function DecodeParameters(responseUri As Uri) As IDictionary(Of String, String)
            Dim answer As String = String.Empty
            ' Get the response text from the URI fragment or query string
            If Not String.IsNullOrEmpty(responseUri.Fragment) Then
                answer = responseUri.Fragment.Substring(1)
            Else
                If Not String.IsNullOrEmpty(responseUri.Query) Then
                    answer = responseUri.Query.Substring(1)
                End If
            End If

            ' Parse the response into a dictionary of key / value pairs
            Dim parametersDictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)()
            Dim keysAndValues As String() = answer.Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            For Each keyAndValueString As String In keysAndValues
                Dim keyAndValue As String() = keyAndValueString.Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                Dim key As String = keyAndValue(0)
                Dim value As String = String.Empty
                If keyAndValue.Length > 1 Then
                    value = Uri.UnescapeDataString(keyAndValue(1))
                End If

                parametersDictionary.Add(key, value)
            Next

            Return parametersDictionary
        End Function
    End Class
End Namespace
