/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN
{
    /// <summary>
    /// Signature and padding algorithm type
    /// </summary>
    public enum SignAlgorithmType
    {
        /// <summary>
        /// Undefined signature and padding (default)
        /// </summary>
        [ParameterValue("UNDEFINED")]
        Undefined = 0,

        /// <summary>
        /// RSA signature with PKCS1 padding
        /// </summary>
        [ParameterValue("RSA_PKCS1_PADDING")]
        RSASignaturePKCS1Padding = 1,

        /// <summary>
        /// RSA signature with no padding
        /// </summary>
        [ParameterValue("RSA_NO_PADDING")]
        RSASignatureNoPadding = 2,

        /// <summary>
        /// EC signature
        /// </summary>
        [ParameterValue("ECDSA")]
        ECDSA = 3,
    }
}
