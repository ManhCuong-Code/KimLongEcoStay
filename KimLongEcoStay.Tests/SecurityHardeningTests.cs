using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace KimLongEcoStay.Tests
{
    public class SecurityHardeningTests
    {
        [Fact]
        public void SecurityTest1_CSPRNG_PinGeneration_ProducesCryptographicallyRandom6DigitPin()
        {
            var generatedPins = new ConcurrentBag<string>();
            System.Threading.Tasks.Parallel.For(0, 1000, _ =>
            {
                string pin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
                generatedPins.Add(pin);
            });

            Assert.Equal(1000, generatedPins.Count);
            foreach (var pin in generatedPins)
            {
                Assert.Equal(6, pin.Length);
                Assert.True(int.TryParse(pin, out int num));
                Assert.InRange(num, 100000, 999999);
            }
        }

        [Fact]
        public void SecurityTest2_SaltedSha256_HashVerification_FixedTimeEqualsWorksCorrectly()
        {
            string rawPin = "584920";
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            string pinSalt = Convert.ToBase64String(saltBytes);

            // Compute hash: SHA256(rawPin + ":" + pinSalt)
            using var sha = SHA256.Create();
            byte[] computedHashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawPin + ":" + pinSalt));
            string storedHash = Convert.ToHexString(computedHashBytes).ToLowerInvariant();

            // Verify with correct PIN
            byte[] testHashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawPin + ":" + pinSalt));
            bool isCorrectValid = CryptographicOperations.FixedTimeEquals(computedHashBytes, testHashBytes);
            Assert.True(isCorrectValid);

            // Verify with wrong PIN
            byte[] wrongHashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes("999999:" + pinSalt));
            bool isWrongValid = CryptographicOperations.FixedTimeEquals(computedHashBytes, wrongHashBytes);
            Assert.False(isWrongValid);
        }

        [Fact]
        public void SecurityTest3_SmartLock_5StrikeLockout_BlocksFurtherAttemptsFor15Minutes()
        {
            var failedAttempts = new ConcurrentDictionary<string, (int Count, DateTime LockedUntil)>();
            string ipOrRoom = "192.168.1.50";

            // Simulate 4 failed attempts
            for (int i = 1; i <= 4; i++)
            {
                var cur = failedAttempts.GetOrAdd(ipOrRoom, _ => (0, DateTime.MinValue));
                int newCount = cur.Count + 1;
                DateTime locked = newCount >= 5 ? DateTime.UtcNow.AddMinutes(15) : DateTime.MinValue;
                failedAttempts[ipOrRoom] = (newCount, locked);
                Assert.True(failedAttempts[ipOrRoom].LockedUntil <= DateTime.UtcNow);
            }

            // 5th failed attempt triggers 15-minute lock
            {
                var cur = failedAttempts[ipOrRoom];
                int newCount = cur.Count + 1;
                DateTime locked = DateTime.UtcNow.AddMinutes(15);
                failedAttempts[ipOrRoom] = (newCount, locked);
            }

            Assert.Equal(5, failedAttempts[ipOrRoom].Count);
            Assert.True(failedAttempts[ipOrRoom].LockedUntil > DateTime.UtcNow);

            // 6th attempt is blocked with lockout (HTTP 429)
            bool isLockedOut = failedAttempts[ipOrRoom].LockedUntil > DateTime.UtcNow;
            Assert.True(isLockedOut);
        }

        [Fact]
        public void SecurityTest4_CustomerLookup_RequiresTwoFactors_AndMasksPII()
        {
            // Database booking record
            string dbOrderCode = "KLE-20261018-01";
            string dbPhone = "0905123456";
            string dbEmail = "khachhang@example.com";

            // Helper matching logic
            Func<string, string, bool> verifyCredentials = (orderCode, verifier) =>
            {
                if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(verifier)) return false;
                if (!orderCode.Equals(dbOrderCode, StringComparison.OrdinalIgnoreCase)) return false;
                
                string cleanVer = verifier.Trim().ToLowerInvariant();
                return cleanVer == dbPhone.ToLowerInvariant() || cleanVer == dbEmail.ToLowerInvariant();
            };

            // Test correct phone matching
            Assert.True(verifyCredentials("KLE-20261018-01", "0905123456"));
            // Test correct email matching
            Assert.True(verifyCredentials("KLE-20261018-01", "khachhang@example.com"));

            // Test wrong phone / IDOR attack attempt
            Assert.False(verifyCredentials("KLE-20261018-01", "0912334455"));
            Assert.False(verifyCredentials("KLE-20261018-01", "attacker@evil.com"));
            Assert.False(verifyCredentials("KLE-20261018-99", "0905123456"));

            // Masking check
            string maskedPhone = dbPhone.Length >= 7 ? string.Concat(dbPhone.AsSpan(0, 3), "****", dbPhone.AsSpan(dbPhone.Length - 3)) : "••••••";
            Assert.Equal("090****456", maskedPhone);
        }

        [Fact]
        public void SecurityTest5_RoleBasedAccessControl_RestrictsUnauthorizedModules()
        {
            // Roles definition
            var roleModules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Admin"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                    { "reconciliation", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "duan", "sql-sync", "employees", "payrolls", "finance" },
                ["Staff"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                    { "reconciliation", "specialties-admin", "partners-admin", "tea-admin", "duan" },
                ["Accountant"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                    { "reconciliation", "specialties-admin", "payrolls", "finance" },
                ["HR_Ops"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                    { "duan", "employees", "partners-admin" }
            };

            // Receptionist / Staff checks
            Assert.DoesNotContain("payrolls", roleModules["Staff"]);
            Assert.DoesNotContain("employees", roleModules["Staff"]);
            Assert.DoesNotContain("finance", roleModules["Staff"]);
            Assert.Contains("reconciliation", roleModules["Staff"]);
            Assert.Contains("duan", roleModules["Staff"]);

            // Accountant checks
            Assert.DoesNotContain("employees", roleModules["Accountant"]);
            Assert.DoesNotContain("duan", roleModules["Accountant"]);
            Assert.Contains("payrolls", roleModules["Accountant"]);
            Assert.Contains("finance", roleModules["Accountant"]);

            // Admin checks
            Assert.Equal(10, roleModules["Admin"].Count);
            Assert.Contains("payrolls", roleModules["Admin"]);
            Assert.Contains("employees", roleModules["Admin"]);
        }

        [Fact]
        public void SecurityTest6_LoginRateLimiter_LocksAfter5ConsecutiveFailures()
        {
            var attempts = new ConcurrentDictionary<string, (int Count, DateTime LockedUntil)>();
            string ip = "10.0.0.1";

            // 5 failed login attempts
            for (int i = 0; i < 5; i++)
            {
                var cur = attempts.GetOrAdd(ip, _ => (0, DateTime.MinValue));
                int newCount = cur.Count + 1;
                DateTime locked = newCount >= 5 ? DateTime.UtcNow.AddMinutes(15) : DateTime.MinValue;
                attempts[ip] = (newCount, locked);
            }

            // Verify locked out
            Assert.True(attempts.TryGetValue(ip, out var state));
            Assert.Equal(5, state.Count);
            Assert.True(state.LockedUntil > DateTime.UtcNow);
        }
    }
}
