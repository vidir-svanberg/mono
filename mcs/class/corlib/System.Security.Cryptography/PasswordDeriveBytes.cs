//
// PasswordDeriveBytes.cs: Handles PKCS#5 key derivation using password
//
// Author:
//	Sebastien Pouliot (sebastien@ximian.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2004 Novell (http://www.novell.com)
//

using System;
using System.Globalization;
using System.Text;

namespace System.Security.Cryptography {

// References:
// a.	PKCS #5 - Password-Based Cryptography Standard 
//	http://www.rsasecurity.com/rsalabs/pkcs/pkcs-5/index.html
// b.	IETF RFC2898: PKCS #5: Password-Based Cryptography Specification Version 2.0
//	http://www.rfc-editor.org/rfc/rfc2898.txt

public class PasswordDeriveBytes : DeriveBytes {

	private string HashNameValue;
	private byte[] SaltValue;
	private int IterationsValue;

	private HashAlgorithm hash;
	private int state;
	private byte[] password;
	private byte[] initial;
	private byte[] output;
	private int position;
	private int hashnumber;
	private int globalPos;

	public PasswordDeriveBytes (string strPassword, byte[] rgbSalt) 
	{
		Prepare (strPassword, rgbSalt, "SHA1", 1);
	}

	public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, CspParameters cspParams) 
	{
		throw new NotSupportedException (
			Locale.GetText ("CspParameters not supported by Mono"));
	}

	public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, string strHashName, int iterations) 
	{
		Prepare (strPassword, rgbSalt, strHashName, iterations);
	}

	public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, string strHashName, int iterations, CspParameters cspParams) 
	{
		throw new NotSupportedException (
			Locale.GetText ("CspParameters not supported by Mono"));
	}

	~PasswordDeriveBytes () 
	{
		// zeroize buffer
		if (initial != null) {
			Array.Clear (initial, 0, initial.Length);
			initial = null;
		}
		// zeroize temporary password storage
		Array.Clear (password, 0, password.Length);
	}

	private void Prepare (string strPassword, byte[] rgbSalt, string strHashName, int iterations) 
	{
		HashNameValue = strHashName;
		SaltValue = rgbSalt;
		IterationsValue = iterations;
		state = 0;
		password = Encoding.UTF8.GetBytes (strPassword);
	}

	public string HashName {
		get { return HashNameValue; } 
		set {
			if (state != 0) {
				throw new CryptographicException (
					Locale.GetText ("Can't change this property at this stage"));
			}
			HashNameValue = value;
		}
	}

	public int IterationCount {
		get { return IterationsValue; }
		set {
			if (state != 0) {
				throw new CryptographicException (
					Locale.GetText ("Can't change this property at this stage"));
			}
			IterationsValue = value;
		}
	}

	public byte[] Salt {
		get { return (byte[]) SaltValue.Clone ();  }
		set {
			if (state != 0) {
				throw new CryptographicException (
					Locale.GetText ("Can't change this property at this stage"));
			}

// For Fx 1.0/1.1 compatibility
//			if (value != null)
				SaltValue = (byte[]) value.Clone ();
//			else
//				SaltValue = null;
		}
	}

	public byte[] CryptDeriveKey (string algname, string alghashname, int keySize, byte[] rgbIV) 
	{
		if (keySize > 128) {
			throw new CryptographicException (
				Locale.GetText ("Key Size can't be greater than 128 bits"));
		}
		throw new NotSupportedException (
			Locale.GetText ("CspParameters not supported by Mono"));
	}

	// note: Key is returned - we can't zeroize it ourselve :-(
	public override byte[] GetBytes (int cb) 
	{
		if (cb < 1)
			throw new IndexOutOfRangeException ("cb");

		if (state == 0) {
			state = 1;
			// it's now impossible to change the HashName, Salt
			// and IterationCount
			Reset ();
		}

		byte[] result = new byte [cb];
		int cpos = 0;
		// the initial hash (in reset) + at least one iteration
		int iter = Math.Max (1, IterationsValue - 1);

		// start with the PKCS5 key
		if (output == null) {
			// calculate the PKCS5 key
			output = initial;

			// generate new key material
			for (int i = 0; i < iter - 1; i++)
				output = hash.ComputeHash (output);
		}

		while (cpos < cb) {
			byte[] output2 = null;
			if (hashnumber == 0) {
				// last iteration on output
				output2 = hash.ComputeHash (output);
			}
			else if (hashnumber < 1000) {
				string n = Convert.ToString (hashnumber);
				output2 = new byte [output.Length + n.Length];
				for (int j=0; j < n.Length; j++)
					output2 [j] = (byte)(n [j]);
				Buffer.BlockCopy (output, 0, output2, n.Length, output.Length);
				// don't update output
				output2 = hash.ComputeHash (output2);
			}
			else {
				throw new CryptographicException (
					Locale.GetText ("too long"));
			}

			int l = Math.Min (cb - cpos, output2.Length);
			Buffer.BlockCopy (output2, position, result, cpos, l);
			cpos += l;
			position += l;
			while (position >= output2.Length) {
				position -= output2.Length;
				hashnumber++;
			}
			globalPos += l;
		}
		return result;
	}

	public override void Reset () 
	{
		// note: Reset doesn't change state
		globalPos = 0;
		position = 0;
		hashnumber = 0;

		hash = HashAlgorithm.Create (HashNameValue);
		if (SaltValue != null) {
			hash.TransformBlock (password, 0, password.Length, password, 0);
			hash.TransformFinalBlock (SaltValue, 0, SaltValue.Length);
			initial = hash.Hash;
		}
		else
			initial = hash.ComputeHash (password);
	}
} 
	
} 
