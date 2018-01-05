using ArcGISRuntime.Samples.Shared.Models;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace ArcGISRuntime.WPF.Viewer
{
    /// <summary>
    /// Interaction logic for CodeListing.xaml
    /// </summary>
    public partial class CodeListing : UserControl
    {
        private static string WrapCodeInHtml(string code)
        {
            // < conversion to &lt; is needed to prevent IE from interpreting xaml as a user control in the page
            return "<html><head><script src=\"https://cdn.rawgit.com/google/code-prettify/master/loader/run_prettify.js\"></script></head><body><pre class=\"prettyprint\">" + code.Replace("<", "&lt;") + "</pre></body></html>";
        }

        public CodeListing()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCodeFiles.SelectedIndex < 0) { return; }
            // Get datacontext
            SampleInfo sample = (SampleInfo)this.DataContext;

            if (sample == null) { return; }

            // Read file
            string content = File.ReadAllText(sample.CodeFiles.ElementAt(lstCodeFiles.SelectedIndex));
            txtCodeListing.NavigateToString(WrapCodeInHtml(content));
        }
    }
}