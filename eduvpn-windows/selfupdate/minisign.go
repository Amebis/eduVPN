/*
	eduVPN - VPN for education and research

	Copyright: 2023 The Commons Conservancy
	SPDX-License-Identifier: GPL-3.0+
*/

package selfupdate

import (
	"errors"
	"fmt"

	"github.com/jedisct1/go-minisign"
)

// Bitmask of signing algorithms
type AlgorithmMask int

const (
	LegacyAlgorithm    AlgorithmMask = 1 << iota // BLAKE2b EdDSA
	PrehashedAlgorithm                           // BLAKE2b-prehashed EdDSA

	AnyAlgorithm = LegacyAlgorithm | PrehashedAlgorithm
)

// Who and how is the signer we trust
type TrustedSigner struct {
	PublicKey     minisign.PublicKey // Public key of the trusted signer
	AlgorithmMask AlgorithmMask      // Bitmask of algorithms trusted signer is using to sign content
}

// verifySignature validates the content was signed by a signature from one of the allowedSigners.
func verifySignature(
	content []byte,
	signature string,
	allowedSigners []TrustedSigner) error {
	sig, err := minisign.DecodeSignature(signature)
	if err != nil {
		return fmt.Errorf("invalid signature format: %w", err)
	}
	for i := range allowedSigners {
		if sig.KeyId != allowedSigners[i].PublicKey.KeyId {
			continue
		}
		if (allowedSigners[i].AlgorithmMask&LegacyAlgorithm == 0 || sig.SignatureAlgorithm != [2]byte{'E', 'd'}) &&
			(allowedSigners[i].AlgorithmMask&PrehashedAlgorithm == 0 || sig.SignatureAlgorithm != [2]byte{'E', 'D'}) {
			return fmt.Errorf("invalid signature algorithm '%s'", sig.SignatureAlgorithm[:])
		}
		valid, err := allowedSigners[i].PublicKey.Verify(content, sig)
		if !valid {
			return fmt.Errorf("invalid signature: %w", err)
		}
		return nil
	}
	return errors.New("signer is not trusted")
}
