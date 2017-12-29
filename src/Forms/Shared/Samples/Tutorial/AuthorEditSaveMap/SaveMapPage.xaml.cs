using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ArcGISRuntime.Samples.TutorialSamples
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Author, edit, and save a map",
        "This sample demonstrates how to author and save a map as an ArcGIS portal item (web map). It is also the solution to the [Author, edit, and save maps to your portal tutorial](https://developers.arcgis.com/net/latest/forms/guide/author-edit-and-save-maps-to-your-portal.htm). Saving a map to arcgis.com requires an ArcGIS Online login.",
        "1. Pan and zoom to the extent you would like for your map.\n2. Choose a basemap from the list of available basemaps.\n3. Click 'Save ...' and provide info for the new portal item (Title, Description, and Tags).\n4. Click 'Save Map to Portal'.\n5. After successfully logging in to your ArcGIS Online account, the map will be saved to your default folder.\n6. You can make additional changes, update the map, and then re-save to store changes in the portal item.")]
	public partial class SaveMapPage : ContentPage
	{
        // Raise an event so the listener can access input values when the form has been completed
        public event EventHandler<SaveMapEventArgs> OnSaveClicked;

        public SaveMapPage ()
		{
			InitializeComponent ();
		}

        // A click handler for the save map button
        private void SaveButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Get information for the new portal item
                var title = MapTitleEntry.Text;
                var description = MapDescriptionEntry.Text;
                var tags = MapTagsEntry.Text.Split(',');

                // Make sure all required info was entered
                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description) || tags.Length == 0)
                {
                    throw new Exception("Please enter a title, description, and some tags to describe the map.");
                }

                // Create a new SaveMapEventArgs object to store the information entered by the user
                var mapSavedArgs = new SaveMapEventArgs(title, description, tags);

                // Raise the OnSaveClicked event so the main page can handle the event and save the map
                OnSaveClicked(this, mapSavedArgs);

                // Close the dialog
                Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                // Show the exception message (dialog will stay open so user can try again)
                DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void CancelButtonClicked(object sender, EventArgs e)
        {
            // If the user cancels, just navigate back to the previous page
            Navigation.PopAsync();
        }
    }

    // Custom EventArgs class for containing portal item properties when saving a map
    public class SaveMapEventArgs : EventArgs
    {
        // Portal item title
        public string MapTitle { get; set; }

        // Portal item description
        public string MapDescription { get; set; }

        // Portal item tags
        public string[] Tags { get; set; }

        public SaveMapEventArgs(string title, string description, string[] tags) : base()
        {
            MapTitle = title;
            MapDescription = description;
            Tags = tags;
        }
    }
}
