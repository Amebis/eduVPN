﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Self-update wizard page
    /// </summary>
    public class SelfUpdateProgressPage : ConnectWizardPopupPage
    {
        #region Fields

        /// <summary>
        /// Self-update cancellation token
        /// </summary>
        private CancellationTokenSource SelfUpdateInProgress;

        /// <summary>
        /// Update file SHA-256 hash
        /// </summary>
        public byte[] Hash;

        /// <summary>
        /// List of update file download URIs
        /// </summary>
        /// <remarks>May contain absolute or relative to self-update-dicovery URIs.</remarks>
        public List<Uri> DownloadUris;

        /// <summary>
        /// Update file command line arguments
        /// </summary>
        public string Arguments;

        #endregion

        #region Properties

        /// <summary>
        /// Self-update progress value
        /// </summary>
        public Range<int> Progress { get; } = new Range<int>(0, 100);

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(
                        () =>
                        {
                            SelfUpdateInProgress?.Cancel();
                            if (base.NavigateBack.CanExecute())
                                base.NavigateBack.Execute();
                        });
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _NavigateBack;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelfUpdateProgressPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void OnActivate()
        {
            SelfUpdateInProgress?.Cancel();

            base.OnActivate();

            // Setup self-update.
            SelfUpdateInProgress = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(SelfUpdateInProgress.Token, Window.Abort.Token).Token;
            var selfUpdate = new BackgroundWorker() { WorkerReportsProgress = true };
            selfUpdate.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                selfUpdate.ReportProgress(0);
                var random = new Random();
                var tempFolder = Path.GetTempPath();
                var workingFolder = tempFolder + Path.GetRandomFileName() + "\\";
                Directory.CreateDirectory(workingFolder);
                try
                {
                    string installerFilename = null;
                    FileStream installerFile = null;

                    // Download installer.
                    while (DownloadUris.Count > 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        var uriIndex = random.Next(DownloadUris.Count);
                        try
                        {
                            var binaryUri = DownloadUris[uriIndex];
                            Trace.TraceInformation("Downloading installer file {0}", binaryUri.AbsoluteUri);
                            var request = Xml.Response.CreateRequest(
                                uri: binaryUri,
                                responseType: "application/vnd.microsoft.portable-executable,application/x-msdos-program,application/octet-stream,*/*;q=0.8");
                            if (request is HttpWebRequest httpRequest)
                            {
                                // Allow HTTP redirects, as GitHub downloads use them extensively.
                                // The integrity of the downloaded file is checked against trusted hash explicitly, so unsafe redirects to http:// may be tolerated.
                                httpRequest.AllowAutoRedirect = true;
                            }
                            using (var response = request.GetResponse())
                            {
                                // When request redirects are disabled, GetResponse() doesn't throw on 3xx status.
                                if (response is HttpWebResponse httpResponse && httpResponse.StatusCode != HttpStatusCode.OK)
                                    throw new WebException("Response status code not 200", null, WebExceptionStatus.UnknownError, response);

                                // 1. Get installer filename from Content-Disposition header.
                                // 2. Get installer filename from the last segment of URI path.
                                // 3. Fallback to a predefined installer filename.
                                try { installerFilename = Path.GetFullPath(workingFolder + new ContentDisposition(request.Headers["Content-Disposition"]).FileName); }
                                catch
                                {
                                    try { installerFilename = Path.GetFullPath(workingFolder + binaryUri.Segments[binaryUri.Segments.Length - 1]); }
                                    catch { installerFilename = Path.GetFullPath(workingFolder + Properties.Settings.Default.ClientTitle + " Client Setup.exe"); }
                                }

                                // Save response data to file.
                                installerFile = File.Open(installerFilename, FileMode.CreateNew, FileAccess.Write, FileShare.Read | FileShare.Inheritable);
                                try
                                {
                                    using (var stream = response.GetResponseStream())
                                    {
                                        installerFile.Seek(0, SeekOrigin.Begin);
                                        var hash = new eduLibsodium.SHA256();
                                        var buffer = new byte[1048576];
                                        long offset = 0, total = response.ContentLength;

                                        for (; ; )
                                        {
                                            // Wait for the data to arrive.
                                            ct.ThrowIfCancellationRequested();
                                            var bufferLength = stream.Read(buffer, 0, buffer.Length);
                                            if (bufferLength == 0)
                                                break;
                                            //ct.WaitHandle.WaitOne(100); // Mock a slow link for testing.

                                            // Append it to the file and hash it.
                                            ct.ThrowIfCancellationRequested();
                                            installerFile.Write(buffer, 0, bufferLength);
                                            hash.TransformBlock(buffer, 0, bufferLength, buffer, 0);

                                            // Report progress.
                                            offset += bufferLength;
                                            selfUpdate.ReportProgress((int)(offset * 100 / total));
                                        }

                                        hash.TransformFinalBlock(buffer, 0, 0);
                                        if (!hash.Hash.SequenceEqual(Hash))
                                            throw new DownloadedFileCorruptException(string.Format(Resources.Strings.ErrorDownloadedFileCorrupt, binaryUri.AbsoluteUri));

                                        installerFile.SetLength(installerFile.Position);
                                        break;
                                    }
                                }
                                catch
                                {
                                    // Close installer file.
                                    installerFile.Close();
                                    installerFile = null;

                                    // Delete installer file. If possible.
                                    Trace.TraceInformation("Deleting file {0}", installerFilename);
                                    try { File.Delete(installerFilename); }
                                    catch (Exception ex2) { Trace.TraceWarning("Deleting {0} file failed: {1}", installerFilename, ex2.ToString()); }
                                    installerFilename = null;

                                    throw;
                                }
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning("Error: {0}", ex.ToString());
                            DownloadUris.RemoveAt(uriIndex);
                        }
                    }

                    if (installerFilename == null || installerFile == null)
                    {
                        // The installer file is not ready.
                        throw new InstallerFileUnavailableException();
                    }

                    try
                    {
                        Trace.TraceInformation("Launching installer file {0}", installerFilename);
                        var process = new Process();
                        process.StartInfo.FileName = installerFilename;
                        if (!string.IsNullOrEmpty(Arguments))
                            process.StartInfo.Arguments = Arguments;
                        process.StartInfo.WorkingDirectory = workingFolder;

                        // Close installer file as late as possible to narrow the attack window.
                        // If Windows supported executing files that are locked for writing, we could leave those files open.
                        installerFile.Close();
                        process.Start();
                    }
                    catch
                    {
                        // Close installer file.
                        installerFile.Close();

                        // Delete installer file. If possible.
                        Trace.TraceInformation("Deleting file {0}", installerFilename);
                        try { File.Delete(installerFilename); }
                        catch (Exception ex2) { Trace.TraceWarning("Deleting {0} file failed: {1}", installerFilename, ex2.ToString()); }

                        throw;
                    }
                }
                catch
                {
                    // Delete working folder. If possible.
                    try { Directory.Delete(workingFolder); }
                    catch (Exception ex2) { Trace.TraceWarning("Deleting {0} folder failed: {1}", workingFolder, ex2.ToString()); }

                    throw;
                }
            };

            // Self-update progress.
            selfUpdate.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                Progress.Value = e.ProgressPercentage;
            };

            // Self-update completition.
            selfUpdate.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (e.Error == null)
                {
                    // Self-updating successfuly launched. Quit to release open files.
                    Wizard.OnQuitApplication(this);
                }
                else if (!(e.Error is OperationCanceledException))
                    Wizard.Error = e.Error;

                // Self-dispose.
                (sender as BackgroundWorker)?.Dispose();
            };

            selfUpdate.RunWorkerAsync();
        }

        #endregion
    }
}
