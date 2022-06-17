/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Xml;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Self-update wizard page
    /// </summary>
    public class SelfUpdateProgressPage : ConnectWizardPopupPage
    {
        #region Fields

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
            base.OnActivate();

            // Setup self-update.
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
                        Window.Abort.Token.ThrowIfCancellationRequested();
                        var uriIndex = random.Next(DownloadUris.Count);
                        try
                        {
                            var binaryUri = DownloadUris[uriIndex];
                            Trace.TraceInformation("Downloading installer file {0}", binaryUri.AbsoluteUri);
                            var request = Xml.Response.CreateRequest(
                                uri: binaryUri,
                                responseType: "application/vnd.microsoft.portable-executable,application/x-msdos-program,application/octet-stream,*/*;q=0.8");
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
                                            Window.Abort.Token.ThrowIfCancellationRequested();
                                            var bufferLength = stream.Read(buffer, 0, buffer.Length);
                                            if (bufferLength == 0)
                                                break;
                                            //Window.Abort.Token.WaitHandle.WaitOne(100); // Mock a slow link for testing.

                                            // Append it to the file and hash it.
                                            Window.Abort.Token.ThrowIfCancellationRequested();
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
                        var updaterFilename = Path.GetFullPath(workingFolder + Properties.Settings.Default.ClientTitle + " Client Setup and Relaunch.wsf");
                        var updaterFile = File.Open(updaterFilename, FileMode.CreateNew, FileAccess.Write, FileShare.Read | FileShare.Inheritable);
                        try
                        {
                            // Prepare WSF file.
                            var writer = new XmlTextWriter(updaterFile, null);
                            writer.WriteStartDocument();
                            writer.WriteStartElement("package");
                            writer.WriteStartElement("job");

                            writer.WriteStartElement("reference");
                            writer.WriteAttributeString("object", "WScript.Shell");
                            writer.WriteEndElement(); // reference

                            writer.WriteStartElement("reference");
                            writer.WriteAttributeString("object", "Scripting.FileSystemObject");
                            writer.WriteEndElement(); // reference

                            writer.WriteStartElement("script");
                            writer.WriteAttributeString("language", "JScript");
                            var installerArgumentsEsc = string.IsNullOrEmpty(Arguments) ? "" : " " + HttpUtility.JavaScriptStringEncode(Arguments);
                            var argv = Environment.GetCommandLineArgs();
                            var arguments = new StringBuilder();
                            for (long i = 1, n = argv.LongLength; i < n; i++)
                            {
                                if (i > 1) arguments.Append(" ");
                                arguments.Append("\"");
                                arguments.Append(argv[i].Replace("\"", "\"\""));
                                arguments.Append("\"");
                            }
                            var script = new StringBuilder();
                            script.AppendLine("var wsh = WScript.CreateObject(\"WScript.Shell\");");
                            script.AppendLine("wsh.Run(\"\\\"" + HttpUtility.JavaScriptStringEncode(installerFilename.Replace("\"", "\"\"")) + "\\\"" + installerArgumentsEsc + "\", 0, true);");
                            script.AppendLine("var fso = WScript.CreateObject(\"Scripting.FileSystemObject\");");
                            script.AppendLine("try { fso.DeleteFile(\"" + HttpUtility.JavaScriptStringEncode(installerFilename) + "\", true); } catch (err) {}");
                            script.AppendLine("try { fso.DeleteFile(\"" + HttpUtility.JavaScriptStringEncode(updaterFilename) + "\", true); } catch (err) {}");
                            script.AppendLine("try { fso.DeleteFolder(\"" + HttpUtility.JavaScriptStringEncode(workingFolder.TrimEnd(Path.DirectorySeparatorChar)) + "\", true); } catch (err) {}");
                            writer.WriteCData(script.ToString());
                            writer.WriteEndElement(); // script

                            writer.WriteEndElement(); // job
                            writer.WriteEndElement(); // package
                            writer.WriteEndDocument();
                            writer.Flush();

                            // Prepare WSF launch parameters.
                            Trace.TraceInformation("Launching update script file {0}", updaterFilename);
                            var process = new Process();
                            process.StartInfo.FileName = "wscript.exe";
                            process.StartInfo.Arguments = "\"" + updaterFilename + "\"";
                            process.StartInfo.WorkingDirectory = workingFolder;

                            // Close WSF and installer files as late as possible to narrow the attack window.
                            // If Windows supported executing files that are locked for writing, we could leave those files open.
                            updaterFile.Close();
                            installerFile.Close();
                            process.Start();
                        }
                        catch
                        {
                            // Close WSF file.
                            updaterFile.Close();

                            // Delete WSF file. If possible.
                            Trace.TraceInformation("Deleting file {0}", updaterFilename);
                            try { File.Delete(updaterFilename); }
                            catch (Exception ex2) { Trace.TraceWarning("Deleting {0} file failed: {1}", updaterFilename, ex2.ToString()); }

                            throw;
                        }
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
                else
                    Wizard.Error = e.Error;

                // Self-dispose.
                (sender as BackgroundWorker)?.Dispose();
            };

            selfUpdate.RunWorkerAsync();
        }

        #endregion
    }
}
