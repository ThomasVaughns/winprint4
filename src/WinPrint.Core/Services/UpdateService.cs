using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using Serilog;

namespace WinPrint.Core.Services {
    /// <summary>
    /// Implements version checks, updated version downloads, and installs.
    /// </summary>
    public class UpdateService {

        /// <summary>
        /// Fired whenever a check for latest version has completed. 
        /// </summary>
        public event EventHandler<Version> GotLatestVersion;
        protected void OnGotLatestVersion(Version latestVersion) {
            GotLatestVersion?.Invoke(this, latestVersion);
        }

        /// <summary>
        /// Fired when a download kicked off by StartUpgrade completes
        /// </summary>
        public event EventHandler<string> DownloadComplete;
        protected void OnDownloadComplete(string path) {
            DownloadComplete?.Invoke(this, path);
        }

        public event EventHandler<int> DownloadProgressChanged;
        protected void OnDownloadProgressChanged(int percent)
        {
            DownloadProgressChanged?.Invoke(this, percent);
        }

        /// <summary>
        /// Any error messages from failed update checks or downloads
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Provides the current version number
        /// </summary>
        public static Version CurrentVersion {
            get {
                var filePath = Assembly.GetAssembly(typeof(UpdateService)).Location;
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                try {
                    return new Version(fileVersionInfo.ProductVersion.Split('-')[0]);
                } catch (Exception ex) {
                    // Log the error and fallback to a default version
                    Log.Error("Invalid ProductVersion format: {ProductVersion}. Exception: {Exception}", fileVersionInfo.ProductVersion, ex);
                    return new Version(0, 0);
                }
            }
        }

        /// <summary>
        /// Contains the version number of the latest version found online (only valid after GotLatestVersion)
        /// </summary>
        public Version LatestVersion { get; private set; }


        /// <summary>
        /// Uri to the release notes page (only valid after GotLatestVersion)
        /// </summary>
        public System.Uri ReleasePageUri { get; set; }

        /// <summary>
        /// Uri to the installer file (only valid after GotLatestVersion)
        /// </summary>
        public System.Uri InstallerUri { get; set; }

        private string _tempFilename;

        public UpdateService() {
            LatestVersion = new Version(0, 0);
        }

        /// <summary>
        /// Compares current version ot latest online version.
        /// > 0 - Current version is newer
        /// = 0 - Same version
        /// < 0 - A newer version available</summary>
        /// <returns></returns>
        public int CompareVersions() {
            return CurrentVersion.CompareTo(LatestVersion);
        }

        /// <summary>
        /// Checks for updated version online. 
        /// </summary>
        /// <returns></returns>
        public async Task<Version> GetLatestVersionAsync(CancellationToken token) {
            InstallerUri = new Uri("https://github.com/tig/winprint/releases");
            try {
                var github = new GitHubClient(new Octokit.ProductHeaderValue("tig-winprint"));
                var allReleases = await github.Repository.Release.GetAll("tig", "winprint").ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
#if DEBUG
                var releases = allReleases.Where(r => r.Prerelease).OrderByDescending(r => new Version(r.TagName.Replace('v', ' '))).ToArray();
#else
                var releases = allReleases.Where(r => !r.Prerelease).OrderByDescending(r => new Version(r.TagName.Replace('v', ' '))).ToArray();
#endif
                if (releases.Length > 0) {
                    Log.Debug("The latest release is tagged at {TagName} and is named {Name}. Download Url: {BrowserDownloadUrl}",
                        releases[0].TagName, releases[0].Name, releases[0].Assets[0].BrowserDownloadUrl);
                    LatestVersion = new Version(releases[0].TagName.Replace('v', ' '));
                    ReleasePageUri = new Uri(releases[0].HtmlUrl);
                    InstallerUri = new Uri(releases[0].Assets[0].BrowserDownloadUrl);
                }
                else {
                    ErrorMessage = "No release found.";
                }
            }
            catch (Exception e) {
                ErrorMessage = $"({ReleasePageUri}) {e.Message}";
                ServiceLocator.Current.TelemetryService.TrackException(e);
            }
            OnGotLatestVersion(LatestVersion);
            return LatestVersion;
        }

        /// <summary>
        /// Starts an upgrade. Must be called after GotLatestVersion has been fired.
        /// </summary>
        public async Task<string> StartUpgradeAsync()
        {
            Debug.WriteLine("StartUpgradeAsync");
            _tempFilename = Path.GetTempFileName() + ".msi";
            //Log.Information($"{this.GetType().Name}: Downloading {InstallerUri.AbsoluteUri} to {_tempFilename}...");

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(InstallerUri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new System.IO.FileStream(_tempFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
            var buffer = new byte[81920];
            long totalRead = 0;
            int read;
            int lastPercent = 0;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;
                if (canReportProgress)
                {
                    int percent = (int)((totalRead * 100) / totalBytes);
                    if (percent != lastPercent)
                    {
                        OnDownloadProgressChanged(percent);
                        lastPercent = percent;
                    }
                }
            }
            OnDownloadComplete(_tempFilename);
            return _tempFilename;
        }
    }
}
