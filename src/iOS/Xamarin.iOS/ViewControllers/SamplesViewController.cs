using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;
using ArcGISRuntime.Samples.Shared.Models;
using ArcGISRuntime.Samples.Managers;
using Foundation;

namespace ArcGISRuntimeXamarin
{
    public class SamplesViewController : UITableViewController
    {
        SearchableTreeNode category;

        public SamplesViewController(SearchableTreeNode category)
        {
            this.category = category;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.Title = "Samples";

            var listSampleItems = category.Items;

            this.TableView.Source = new SamplesDataSource(this, listSampleItems);

            this.TableView.ReloadData();
        }

        public class SamplesDataSource : UITableViewSource
        {
            private UITableViewController controller;
            private List<SampleInfo> data;

            public SamplesDataSource(UITableViewController controller, List<Object> data)
            {
                this.data = data.OfType<SampleInfo>().ToList();
                this.controller = controller;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var cell = new UITableViewCell();
                var item = data[indexPath.Row];
                cell.TextLabel.Text = (item as SampleInfo).SampleName;
                return cell;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return data.Count;
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                try
                {
                    var item = SampleManager.Current.SampleToControl(data[indexPath.Row]);

                    // Call a function to clear existing credentials
                    ClearCredentials();

                    controller.NavigationController.PushViewController((UIViewController)item, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            private void ClearCredentials()
            {
                // Clear credentials (if any) from previous sample runs
                var creds = Esri.ArcGISRuntime.Security.AuthenticationManager.Current.Credentials;
                for (var i = creds.Count() - 1; i >= 0; i--)
                {
                    var c = creds.ElementAtOrDefault(i);
                    if (c != null)
                    {
                        Esri.ArcGISRuntime.Security.AuthenticationManager.Current.RemoveCredential(c);
                    }
                }
            }
        }
    }
}
