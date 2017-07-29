/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace eduVPN.Models
{
    /// <summary>
    /// An eduVPN certificate with a key
    /// </summary>
    public class Certificate : JSON.ILoadableItem
    {
        #region Properties

        /// <summary>
        /// X509 Certificate
        /// </summary>
        public X509Certificate2 Value { get => _value; }
        private X509Certificate2 _value;

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads APIv2 from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>authorization_endpoint</c>, <c>token_endpoint</c> and other optional elements. All elements should be strings representing URI(s).</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public void Load(object obj)
        {
            if (obj is Dictionary<string, object> obj2)
            {
                // Load certificate.
                _value = new X509Certificate2(
                    GetBytesFromPEM(
                        eduJSON.Parser.GetValue<string>(obj2, "certificate"),
                        "CERTIFICATE"),
                    (string)null,
                    X509KeyStorageFlags.PersistKeySet);

                // Load private key parameters.
                try
                {
                    var key_pem = eduJSON.Parser.GetValue<string>(obj2, "private_key");
                    var key_der = GetBytesFromPEM(key_pem, "PRIVATE KEY");
                    if (key_der != null)
                        _value.PrivateKey = DecodePrivateKeyPKCS8(key_der);
                    else
                    {
                        key_der = GetBytesFromPEM(key_pem, "RSA PRIVATE KEY");
                        if (key_der != null)
                            _value.PrivateKey = DecodePrivateKeyPKCS1(key_der);
                        else
                            throw new InvalidDataException();
                    }
                }
                catch (Exception ex)
                {
                    throw new CertificatePrivateKeyException(Resources.Strings.ErrorInvalidPrivateKey, ex);
                }
            }
            else
                throw new eduJSON.InvalidParameterTypeException("obj", typeof(Dictionary<string, object>), obj.GetType());
        }

        /// <summary>
        /// Decodes PEM encoded BLOB
        /// </summary>
        /// <param name="pem">PEM encoded text</param>
        /// <param name="section">Section name to decode ("CERTIFICATE", "PRIVATE KEY", etc.)</param>
        /// <returns>BLOB</returns>
        /// <see cref="https://stackoverflow.com/a/10498045/2071884"/>
        private static byte[] GetBytesFromPEM(string pem, string section)
        {
            var header = String.Format("-----BEGIN {0}-----", section);
            var footer = String.Format("-----END {0}-----", section);

            var start = pem.IndexOf(header, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += header.Length;
            var end = pem.IndexOf(footer, start, StringComparison.Ordinal) - start;

            if (end < 0)
                return null;

            return Convert.FromBase64String(pem.Substring(start, end));
        }

        /// <summary>
        /// Reads RSA private key from ASN.1 data (PKCS#1)
        /// </summary>
        /// <param name="priv_key">ASN.1 data</param>
        /// <returns>Private key</returns>
        /// <see cref="https://tools.ietf.org/html/rfc3447#appendix-A.1.2"/>
        private static RSACryptoServiceProvider DecodePrivateKeyPKCS1(byte[] priv_key)
        {
            var stream = new MemoryStream(priv_key);
            using (var reader = new BinaryReader(stream))
            {
                // Create the private key.
                var rsa = new RSACryptoServiceProvider(new CspParameters()
                {
                    Flags = CspProviderFlags.UseNonExportableKey,
                    KeyContainerName = Guid.NewGuid().ToString().ToUpperInvariant(),
                });
                rsa.ImportParameters(reader.ReadASN1RSAPrivateKey());

                return rsa;
            }
        }

        /// <summary>
        /// Reads private key from ASN.1 data (PKCS#8)
        /// </summary>
        /// <param name="priv_key">ASN.1 data</param>
        /// <returns>Private key</returns>
        /// <remarks>Currently, only RSA private keys are supported.</remarks>
        /// <see cref="https://tools.ietf.org/html/rfc5208#section-5"/>
        private static AsymmetricAlgorithm DecodePrivateKeyPKCS8(byte[] priv_key)
        {
            var stream = new MemoryStream(priv_key);
            using (var reader = new BinaryReader(stream))
            {
                // SEQUENCE(PrivateKeyInfo)
                if (reader.ReadByte() != 0x30)
                    throw new InvalidDataException();
                long pki_end = reader.ReadASN1Length() + reader.BaseStream.Position;

                // INTEGER(Version)
                if (!reader.ReadASN1Integer().SequenceEqual(new byte[] { 0 }))
                    throw new InvalidDataException();

                // SEQUENCE(AlgorithmIdentifier)
                if (reader.ReadByte() != 0x30)
                    throw new InvalidDataException();
                long alg_id_end = reader.ReadASN1Length() + reader.BaseStream.Position;

                // OBJECT IDENTIFIER(1.2.840.113549.1.1.1)
                if (reader.ReadASN1ObjectID().Value != "1.2.840.113549.1.1.1")
                    throw new InvalidDataException();

                reader.BaseStream.Seek(alg_id_end, SeekOrigin.Begin);

                // OCTET STRING(PrivateKey)
                if (reader.ReadByte() != 0x04)
                    throw new InvalidDataException();
                long pk_end = reader.ReadASN1Length() + reader.BaseStream.Position;

                // Create the private key.
                var rsa = new RSACryptoServiceProvider(new CspParameters()
                {
                    Flags = CspProviderFlags.UseNonExportableKey,
                    KeyContainerName = Guid.NewGuid().ToString().ToUpperInvariant(),
                });
                rsa.ImportParameters(reader.ReadASN1RSAPrivateKey());

                return rsa;
            }
        }

        #endregion
    }
}
