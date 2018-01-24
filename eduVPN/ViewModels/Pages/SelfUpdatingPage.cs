/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Self-update wizard page
    /// </summary>
    public class SelfUpdatingPage : ConnectWizardPopupPage
    {
        #region Properties

        /// <inheritdoc/>
        public override string Title
        {
            get { return Resources.Strings.SelfUpdatingPageTitle; }
        }

        /// <summary>
        /// Self-updater JSON content retrieved from web
        /// </summary>
        public Dictionary<string, object> ObjWeb { get; set; }

        /// <summary>
        /// Self-update progress value
        /// </summary>
        public Range<int> Progress
        {
            get { return _progress; }
        }
        private Range<int> _progress = new Range<int>(0, 100);

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelfUpdatingPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream tolerates multiple disposes.")]
        public override void OnActivate()
        {
            base.OnActivate();

            // Setup self-update.
            var self_update = new BackgroundWorker() { WorkerReportsProgress = true };
            self_update.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                self_update.ReportProgress(0);
                var random = new Random();
                var discovery_uri = Properties.Settings.Default.SelfUpdateDescr.Uri;
                var working_folder = Path.GetTempPath();
                var installer_filename = Path.GetFullPath(working_folder + Path.GetRandomFileName() + ".exe");
                var installer_file = File.Open(installer_filename, FileMode.CreateNew, FileAccess.Write, FileShare.Read | FileShare.Inheritable);
                try
                {
                    // Download installer.
                    var installer_ready = false;
                    var repo_hash = ((string)ObjWeb["hash-sha256"]).FromHexToBin();
                    var binary_uris = (List<object>)ObjWeb["uri"];
                    while (binary_uris.Count > 0)
                    {
                        Window.Abort.Token.ThrowIfCancellationRequested();
                        var uri_idx = random.Next(binary_uris.Count);
                        try
                        {
                            var binary_uri = new Uri(discovery_uri, (string)binary_uris[uri_idx]);
                            Trace.TraceInformation("Downloading installer file from {0}...", binary_uri.AbsoluteUri);
                            var request = WebRequest.Create(binary_uri);
                            request.Proxy = null;
                            using (var response = request.GetResponse())
                            using (var stream = response.GetResponseStream())
                            {
                                installer_file.Seek(0, SeekOrigin.Begin);
                                var hash = new eduEd25519.SHA256();
                                var buffer = new byte[1048576];
                                long offset = 0, total = response.ContentLength;

                                for (; ; )
                                {
                                    // Wait for the data to arrive.
                                    Window.Abort.Token.ThrowIfCancellationRequested();
                                    var buffer_length = stream.Read(buffer, 0, buffer.Length);
                                    if (buffer_length == 0)
                                        break;

                                    // Append it to the file and hash it.
                                    Window.Abort.Token.ThrowIfCancellationRequested();
                                    installer_file.Write(buffer, 0, buffer_length);
                                    hash.TransformBlock(buffer, 0, buffer_length, buffer, 0);

                                    // Report progress.
                                    offset += buffer_length;
                                    self_update.ReportProgress((int)(offset * 100 / total));
                                }

                                hash.TransformFinalBlock(buffer, 0, 0);
                                if (!hash.Hash.SequenceEqual(repo_hash))
                                    throw new DownloadedFileCorruptException(string.Format(Resources.Strings.ErrorDownloadedFileCorrupt, binary_uri.AbsoluteUri));

                                installer_file.SetLength(installer_file.Position);
                                installer_ready = true;
                                break;
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning("Error: {0}", ex.ToString());
                            binary_uris.RemoveAt(uri_idx);
                        }
                    }

                    if (!installer_ready)
                    {
                        // The installer file is not ready.
                        throw new InstallerFileUnavailableException();
                    }

                    var updater_filename = Path.GetFullPath(working_folder + Path.GetRandomFileName() + ".wsf");
                    var updater_file = File.Open(updater_filename, FileMode.CreateNew, FileAccess.Write, FileShare.Read | FileShare.Inheritable);
                    try
                    {
                        // Prepare WSF file.
                        var writer = new XmlTextWriter(updater_file, null);
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
                        var installer_arguments_esc = eduJSON.Parser.GetValue(ObjWeb, "arguments", out string installer_arguments) ? " " + HttpUtility.JavaScriptStringEncode(installer_arguments) : "";
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
                        script.AppendLine("// This script was auto-generated.");
                        script.AppendLine("// Launch installer file and wait for the update to finish.");
                        script.AppendLine("var wsh = WScript.CreateObject(\"WScript.Shell\");");
                        script.AppendLine("if (wsh.Run(\"\\\"" + HttpUtility.JavaScriptStringEncode(installer_filename.Replace("\"", "\"\"")) + "\\\"" + installer_arguments_esc + "\", 0, true) == 0) {");
                        script.AppendLine("  // Installer succeeded. Relaunch the application.");
                        script.AppendLine("  var shl = WScript.CreateObject(\"Shell.Application\");");
                        script.AppendLine("  shl.ShellExecute(\"" + HttpUtility.JavaScriptStringEncode(argv[0]) + "\", \"" + HttpUtility.JavaScriptStringEncode(arguments.ToString()) + "\", \"" + HttpUtility.JavaScriptStringEncode(Environment.CurrentDirectory) + "\");");
                        script.AppendLine("}");
                        script.AppendLine("// Cleanup.");
                        script.AppendLine("var fso = WScript.CreateObject(\"Scripting.FileSystemObject\");");
                        script.AppendLine("try { fso.DeleteFile(\"" + HttpUtility.JavaScriptStringEncode(installer_filename) + "\", true); } catch (err) {}");
                        script.AppendLine("try { fso.DeleteFile(\"" + HttpUtility.JavaScriptStringEncode(updater_filename) + "\", true); } catch (err) {}");
                        writer.WriteCData(script.ToString());
                        writer.WriteEndElement(); // script

                        writer.WriteEndElement(); // job
                        writer.WriteEndElement(); // package
                        writer.WriteEndDocument();
                        writer.Flush();

                        // Prepare WSF launch parameters.
                        Trace.TraceInformation("Launching update script file {0}...", updater_filename);
                        var process = new Process();
                        process.StartInfo.FileName = "wscript.exe";
                        process.StartInfo.Arguments = "\"" + updater_filename + "\"";
                        process.StartInfo.WorkingDirectory = working_folder;

                        // Close WSF and installer files as late as possible to narrow the attack window.
                        // If Windows supported executing files that are locked for writing, we could leave those files open.
                        updater_file.Close();
                        installer_file.Close();
                        process.Start();
                    }
                    catch
                    {
                        // Close WSF file.
                        updater_file.Close();

                        // Delete WSF file. If possible.
                        Trace.TraceInformation("Deleting file {0}...", updater_filename);
                        try { File.Delete(updater_filename); }
                        catch (Exception ex2) { Trace.TraceWarning("Deleting {0} file failed: {1}", updater_filename, ex2.ToString()); }

                        throw;
                    }
                }
                catch
                {
                    // Close installer file.
                    installer_file.Close();

                    // Delete installer file. If possible.
                    Trace.TraceInformation("Deleting file {0}...", installer_filename);
                    try { File.Delete(installer_filename); }
                    catch (Exception ex2) { Trace.TraceWarning("Deleting {0} file failed: {1}", installer_filename, ex2.ToString()); }

                    throw;
                }
            };

            // Self-update progress.
            self_update.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                Progress.Value = e.ProgressPercentage;
            };

            // Self-update completition.
            self_update.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
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

            self_update.RunWorkerAsync();
        }

        #endregion
    }
}
