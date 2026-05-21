/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2026 Open Text
 *
 * The only warranties for products and services of Open Text and
 * its affiliates and licensors ("Open Text") are as may be set forth
 * in the express warranty statements accompanying such products and services.
 * Nothing herein should be construed as constituting an additional warranty.
 * Open Text shall not be liable for technical or editorial errors or
 * omissions contained herein. The information contained herein is subject
 * to change without notice.
 *
 * Except as specifically indicated otherwise, this document contains
 * confidential information and a valid license is required for possession,
 * use or copying. If this work is provided to the U.S. Government,
 * consistent with FAR 12.211 and 12.212, Commercial Computer Software,
 * Computer Software Documentation, and Technical Data for Commercial Items are
 * licensed to the U.S. Government under vendor's standard commercial license.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ___________________________________________________________________
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HpToolsLauncher.Common
{
    public static class Encrypter
    {
        private const string USE_STDIN_KEY = "--use-stdin-key";
        private const string FTL_AES256_ENCRYPTION_KEY = "FTL_AES256_ENCRYPTION_KEY";
        private const string DEFAULT_KEY = "EncriptionPass4Java";

        private static readonly byte[] _legacyKey = DeriveLegacyKey();
        private static readonly byte[] _aesKey;
        private static readonly byte[] _hmacKey;

        static Encrypter()
        {
            string raw = ReadKeyFromStdInOrEnvVar();
            Environment.SetEnvironmentVariable(FTL_AES256_ENCRYPTION_KEY, null);

            if (raw is null) return;

            byte[] key = Convert.FromBase64String(raw);
            if (key.Length != 64)
                throw new CryptographicException("Invalid secure key length. Expected 64 bytes (base64-encoded).");

            _aesKey = new byte[32];
            _hmacKey = new byte[32];
            Buffer.BlockCopy(key, 0, _aesKey, 0, 32);
            Buffer.BlockCopy(key, 32, _hmacKey, 0, 32);
        }

        private static byte[] DeriveLegacyKey()
        {
            byte[] key = new byte[16];
            byte[] pwd = Encoding.UTF8.GetBytes(DEFAULT_KEY);
            Buffer.BlockCopy(pwd, 0, key, 0, Math.Min(pwd.Length, 16));
            return key;
        }

        /// <summary>
        /// Encrypts using AES-256-CBC + HMAC-SHA256 when a secure key is provided,
        /// otherwise falls back to legacy AES-128-CBC.
        /// </summary>
        public static string Encrypt(string plainText) =>
            _aesKey is null ? EncryptLegacy(plainText) : EncryptSecure(plainText);

        private static string EncryptSecure(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            byte[] iv = aes.IV; // 16 bytes

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] ciphertext = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Layout: [ IV (16) | ciphertext | HMAC (32) ]
            byte[] data = new byte[16 + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, data, 0, 16);
            Buffer.BlockCopy(ciphertext, 0, data, 16, ciphertext.Length);

            using var h = new HMACSHA256(_hmacKey);
            byte[] hmac = h.ComputeHash(data);

            byte[] result = new byte[data.Length + 32];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            Buffer.BlockCopy(hmac, 0, result, data.Length, 32);

            return Convert.ToBase64String(result);
        }

        private static string EncryptLegacy(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _legacyKey;
            aes.IV = _legacyKey; // ⚠️ legacy behavior preserved
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length));
        }

        public static string Decrypt(string cipherText)
        {
#if DEBUG
            return cipherText;
#endif
            if (cipherText.IsNullOrWhiteSpace())
                return cipherText;

            return _aesKey is null ? DecryptLegacy(cipherText) : DecryptSecure(cipherText);
        }

        // =========================================================
        // 🔐 SECURE MODE (AES-256-CBC + HMAC)
        // =========================================================
        private static string DecryptSecure(string input)
        {
            byte[] buffer = Convert.FromBase64String(input);
            // minimum: 16 (IV) + 1 block (16) + 32 (HMAC) = 64
            if (buffer.Length < 64)
                throw new CryptographicException("Invalid encrypted payload.");

            int ciphertextLen = buffer.Length - 16 - 32;

            byte[] iv = new byte[16];
            byte[] ciphertext = new byte[ciphertextLen];
            byte[] hmac = new byte[32];

            Buffer.BlockCopy(buffer, 0, iv, 0, 16);
            Buffer.BlockCopy(buffer, 16, ciphertext, 0, ciphertextLen);
            Buffer.BlockCopy(buffer, buffer.Length - 32, hmac, 0, 32);

            using var h = new HMACSHA256(_hmacKey);
            byte[] expected = h.ComputeHash(buffer, 0, buffer.Length - 32);

            if (!ConstantTimeEquals(expected, hmac))
                throw new CryptographicException("HMAC validation failed.");

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            return Encoding.UTF8.GetString(plain);
        }

        // =========================================================
        // 🔓 LEGACY MODE (unchanged behavior)
        // =========================================================
        private static string DecryptLegacy(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _legacyKey;
            aes.IV = _legacyKey; // ⚠️ legacy behavior preserved
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plain);
        }

        // =========================================================
        // 🔑 INPUT HANDLING
        // =========================================================
        private static string ReadKeyFromStdInOrEnvVar()
        {
            var args = Environment.GetCommandLineArgs();

            if (USE_STDIN_KEY.In(true, args))
            {
                using var reader = new StreamReader(Console.OpenStandardInput());
                var key = reader.ReadLine()?.Trim();
                if (key.IsNullOrWhiteSpace())
                    throw new CryptographicException($"{USE_STDIN_KEY} was specified but no key was provided via stdin.");
                return key;
            }

            var envKey = Environment.GetEnvironmentVariable(FTL_AES256_ENCRYPTION_KEY);
            return envKey.IsNullOrWhiteSpace() ? null : envKey.Trim();
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}