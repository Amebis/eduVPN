/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Xml;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace eduVPN.PrepopulateResponseCache
{
    class Program
    {
        /// <summary>
        /// Retrieves server and organization discovery files and pre-caches them in the eduVPN.Client's app.config file.
        /// </summary>
        /// <param name="args">An array of strings with the first string containing the path to eduVPN.Client\app.config file</param>
        static void Main(string[] args)
        {
            if (args.Length < 1)
                throw new ArgumentException("Missing app.config filename.");

            var doc = new XmlDocument();
            using (var reader = new XmlTextReader(args[0]))
                doc.Load(reader);
            if (!(doc.SelectSingleNode("/configuration/userSettings/eduVPN.Properties.Settings") is XmlElement settings))
                throw new ArgumentException(string.Format("{0} has no userSettings/eduVPN.Properties.Settings element.", args[0]));
            if (settings.SelectSingleNode("setting[@name='ResponseCache']") is XmlElement el)
                el.SetAttribute("serializeAs", "Xml");
            else
            {
                el = doc.CreateElement("setting");
                el.SetAttribute("name", "ResponseCache");
                el.SetAttribute("serializeAs", "Xml");
                settings.AppendChild(el);
            }

            var sb = new StringBuilder();
            using (var textWriter = new StringWriter(sb))
            using (var writer = new XmlTextWriter(textWriter))
            {
                writer.WriteStartElement("value");
                writer.WriteStartElement(nameof(JSONResponseDictionary));
                var responseCache = new JSONResponseDictionary();
                responseCache.GetSeq(Properties.Settings.Default.ServersDiscovery);
                responseCache.GetSeq(Properties.Settings.Default.OrganizationsDiscovery);
                responseCache.WriteXml(writer);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            el.InnerXml = sb.ToString();
            doc.Save(args[0]);
        }
    }
}
