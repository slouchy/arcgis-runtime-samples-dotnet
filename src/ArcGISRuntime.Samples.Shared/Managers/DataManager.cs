// Copyright 2016 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using ArcGISRuntime.Samples.Shared.Models;
using Esri.ArcGISRuntime.Portal;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.Storage;
#endif

namespace ArcGISRuntime.Samples.Managers
{
    public class DataManager
    {
        private DataManager()
        {
        }

        private static readonly DataManager SingleInstance = new DataManager();

        private static async Task DownloadItem(PortalItem item)
        {
            // get sample data directory
            string data_dir = Path.Combine(GetDataFolder(), item.ItemId);

            // create directory matching item id
            if (!Directory.Exists(data_dir))
            {
                Directory.CreateDirectory(data_dir);
            }

            // Download item to the directory
            Task<Stream> downloadTask = item.GetDataAsync();

            string tempFile = Path.Combine(data_dir, item.Name);
            // Download the file
            using (var s = await downloadTask.ConfigureAwait(false))
            {
                using (var f = File.Create(tempFile))
                {
                    await s.CopyToAsync(f).ConfigureAwait(false);
                }
            }

            // Unzip the file if it is a zip file
            if (tempFile.EndsWith(".zip"))
            {
                UnpackData(tempFile, data_dir);
            }

            // Write the __sample.config file
            string configFilePath = Path.Combine(data_dir, "__sample.config");
            File.WriteAllText(configFilePath, DateTime.Now.ToString());
        }

        private static bool IsDataPresent(PortalItem item)
        {
            // get sample data directory
            string data_dir = GetDataFolder();
            // look for directory matching item id
            string dir = GetDataFolder(item.ItemId);
            if (!Directory.Exists(dir)) { return false; }
            // if data is present, look for __sample.config file
            string configPath = Path.Combine(dir, "__sample.config");
            if (!File.Exists(configPath)) { return false; }
            // Read __sample.config, extract data
            string body = File.ReadLines(configPath).First();
            DateTime downloadDate;
            bool dateExtractSuccess = DateTime.TryParse(body, out downloadDate);

            if (!dateExtractSuccess) { return false; }

            // Return false if data was updated since last download
            if (downloadDate < item.Modified) { return false; }

            // If we're still here, the sample data is valid
            return true;
        }

        public static async Task EnsureSampleDataPresent(SampleInfo sample)
        {
            // Return if there's nothing to do
            if (sample.OfflineDataItems == null || !sample.OfflineDataItems.Any()) { return; }

            // Hold list of download tasks (to enable parallel download)
            List<Task> downloads = new List<Task>();

            foreach (string itemId in sample.OfflineDataItems)
            {
                // Create ArcGIS portal item
                var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(false);
                var item = await PortalItem.CreateAsync(portal, itemId).ConfigureAwait(false);
                // Download item if not already present
                if (!IsDataPresent(item))
                {
                    Task downloadTask = DownloadItem(item);
                    downloads.Add(downloadTask);
                }
            }
            // Wait for all downloads to complete
            await Task.WhenAll(downloads);
        }

        /// <summary>
        /// Downloads data from ArcGIS Portal and unzips it to the local data folder
        /// </summary>
        /// <param name="path">The path to put the data in - if the folder already exists, the data is assumed to have been downloaded and this immediately returns</param>
        /// <returns></returns>
        public static async Task GetData(string itemId, string sampleName) // temporary rename before removal
        {
            // Method for creating directories as needed
            Action<DirectoryInfo> createDir = null;
            createDir = (s) =>
            {
                System.Diagnostics.Debug.WriteLine(s.FullName);
                if (Directory.Exists(s.FullName)) return;
                if (!Directory.Exists(s.Parent.FullName))
                    createDir(s.Parent);
                Directory.CreateDirectory(s.FullName);
            };

            // Create the portal
            var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(false);

            // Create the portal item
            var item = await PortalItem.CreateAsync(portal, itemId).ConfigureAwait(false);

            // Create the SampleData folder
            var tempFile = Path.Combine(GetDataFolder(), "SampleData");
            createDir(new DirectoryInfo(tempFile));

            // Create the sample-specific folder
            tempFile = Path.Combine(tempFile, sampleName);
            createDir(new DirectoryInfo(tempFile));

            // Get the full path to the specific file
            tempFile = Path.Combine(tempFile, item.Name);

            // Download the file
            using (var s = await item.GetDataAsync().ConfigureAwait(false))
            {
                var x = item.GetDataAsync();
                using (var f = File.Create(tempFile))
                {
                    await s.CopyToAsync(f).ConfigureAwait(false);
                }
            }

            // Unzip the file if it is a zip file
            if (tempFile.EndsWith(".zip"))
            {
                UnpackData(tempFile, Path.Combine(GetDataFolder(), "SampleData", sampleName));
            }
        }

        private static void UnpackData(string zipFile, string folder)
        {
            using (var archive = ZipFile.OpenRead(zipFile))
            {
                foreach (var entry in archive.Entries.Where(m => !String.IsNullOrWhiteSpace(m.Name)))
                {
                    var path = Path.Combine(folder, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    entry.ExtractToFile(path, true);
                }
            }
        }

        /// <summary>
        /// Gets the data folder where locally provisioned data is stored
        /// </summary>
        internal static string GetDataFolder()
        {
            string appDataFolder = "";
#if NETFX_CORE
            appDataFolder  = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#else
            appDataFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
#endif
            string sampleDataFolder = Path.Combine(appDataFolder, "ArcGISRuntimeSampleData");

            if (!Directory.Exists(sampleDataFolder)) { Directory.CreateDirectory(sampleDataFolder); }

            return sampleDataFolder;
        }

        internal static string GetDataFolder(string itemId)
        {
            return Path.Combine(GetDataFolder(), itemId);
        }
    }
}