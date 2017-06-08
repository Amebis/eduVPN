/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.IO;
using System.Reflection;

namespace eduVPNTests
{
    /// <summary>
    /// A helper class to register our custom assembly resolver
    /// </summary>
    /// <remarks>Based on code found at <see cref="<see cref="https://stackoverflow.com/a/9951658/2071884"/>"/></remarks>
    class MultiplatformDllLoader
    {
        private static bool is_enabled;

        /// <summary>
        /// Property used to set or get registration status of our resolver
        /// </summary>
        public static bool Enable
        {
            get { return is_enabled; }
            set
            {
                lock (typeof(MultiplatformDllLoader))
                {
                    if (is_enabled != value)
                    {
                        if (value)
                            AppDomain.CurrentDomain.AssemblyResolve += Resolver;
                        else
                            AppDomain.CurrentDomain.AssemblyResolve -= Resolver;
                        is_enabled = value;
                    }
                }
            }
        }

        /// <summary>
        /// Resolve event handler that will attempt to load a missing assembly from either Win32 or x64 subdir
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="args">The event data</param>
        /// <returns>The assembly that resolves the type, assembly, or resource; or nullptr if the assembly cannot be resolved.</returns>
        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            string assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
            string archSpecificPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, Environment.Is64BitProcess ? "x64" : "Win32", assemblyName);
            return File.Exists(archSpecificPath) ? Assembly.LoadFile(archSpecificPath) : null;
        }
    }
}
