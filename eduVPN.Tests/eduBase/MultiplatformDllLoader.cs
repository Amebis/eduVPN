/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace eduBase
{
    /// <summary>
    /// A helper class to register our custom assembly resolver
    /// </summary>
    /// <remarks>
    /// Based on this <a href="https://stackoverflow.com/a/9951658/2071884">example</a>.
    /// </remarks>
    class MultiplatformDllLoader
    {
        /// <summary>
        /// Property used to set or get registration status of our resolver
        /// </summary>
        public static bool Enable
        {
            get { return isEnabled; }
            set
            {
                lock (isEnabledLock)
                {
                    if (isEnabled != value)
                    {
                        if (value)
                            AppDomain.CurrentDomain.AssemblyResolve += Resolver;
                        else
                            AppDomain.CurrentDomain.AssemblyResolve -= Resolver;
                        isEnabled = value;
                    }
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static bool isEnabled;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly object isEnabledLock = new object();

        /// <summary>
        /// Resolve event handler that will attempt to load a missing assembly from either Win32 or x64 subdir
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="args">The event data</param>
        /// <returns>The assembly that resolves the type, assembly, or resource; or nullptr if the assembly cannot be resolved.</returns>
        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            var assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
            var archSpecificPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, Environment.Is64BitProcess ? "x64" : "x86", assemblyName);
            return File.Exists(archSpecificPath) ? Assembly.LoadFile(archSpecificPath) : null;
        }
    }
}
