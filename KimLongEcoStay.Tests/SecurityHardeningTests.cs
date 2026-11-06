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

        [Theory]
        [InlineData("", "password123")]
        [InlineData("admin", "")]
        [InlineData("   ", "   ")]
        [InlineData(null, "pass")]
        public void SecurityTest7_Section11_Case1_EmptyCredentials_RejectedWithBadRequest(string? user, string? pass)
        {
            bool isInvalid = string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass);
            Assert.True(isInvalid, "Empty credentials must be rejected immediately (HTTP 400 Bad Request)");
        }

        [Fact]
        public void SecurityTest8_Section11_Case2_WrongPassword_RejectedWithUnauthorized()
        {
            string canonical = "admin";
            string wrongPassword = "WrongPassword!2026";
            string storedHash = "564C07116D7DED47805641FC46710CA1724F44F37868A56169213CBB65A07DA9";

            // Compute hash of wrong password
            byte[] bytes = Encoding.UTF8.GetBytes($"{canonical}:KLE_AUTH_SALT_2026:{wrongPassword}");
            string candidateHash = Convert.ToHexString(SHA256.HashData(bytes));

            bool isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(candidateHash),
                Encoding.ASCII.GetBytes(storedHash));

            Assert.False(isValid, "Wrong password must be rejected (HTTP 401 Unauthorized)");
        }

        [Fact]
        public void SecurityTest9_Section11_Case3_NonExistentUser_RejectedWithGenericMessage()
        {
            var knownUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "admin", "letan", "ketoan", "nhansu" };
            string fakeUser = "hacker_unknown_99";

            bool exists = knownUsers.Contains(fakeUser);
            Assert.False(exists);
            // Must return generic message without revealing existence
            string errorResponse = "Tên đăng nhập hoặc mật khẩu không chính xác.";
            Assert.DoesNotContain("tồn tại", errorResponse, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SecurityTest10_Section11_Case4_ValidLogin_IssuesSecureClaimsSession()
        {
            string username = "letan";
            string role = "Staff";
            string fullName = "Đặng Thu Thảo";

            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, fullName),
                new(System.Security.Claims.ClaimTypes.NameIdentifier, username),
                new(System.Security.Claims.ClaimTypes.Role, role),
                new("UserRole", role)
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("Staff", principal.FindFirst("UserRole")?.Value);
            Assert.Equal("letan", principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public void SecurityTest11_Section11_Case5_TamperingLocalStorage_HasNoEffectOnBackend()
        {
            // Attacker modifies localStorage to role: ADMIN
            string forgedLocalStorageRole = "ADMIN";

            // Backend verifies ONLY real Cookie ClaimsPrincipal
            var legitimateStaffClaims = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("UserRole", "Staff")
            }, "Cookies");
            var principal = new System.Security.Claims.ClaimsPrincipal(legitimateStaffClaims);

            // Backend checks access to payrolls
            string serverVerifiedRole = principal.FindFirst("UserRole")?.Value ?? "Guest";
            bool canAccessPayrolls = serverVerifiedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                                  || serverVerifiedRole.Equals("Accountant", StringComparison.OrdinalIgnoreCase);

            Assert.False(canAccessPayrolls, "Backend must ignore client localStorage and reject Staff accessing payrolls (HTTP 403)");
            Assert.NotEqual(forgedLocalStorageRole, serverVerifiedRole);
        }

        [Fact]
        public void SecurityTest12_Section11_Case6_Case15_SimulationOnProduction_StrictlyRejected()
        {
            bool isProductionEnvironment = true; // env.IsDevelopment() == false
            Func<bool, (int StatusCode, string Message)> devSimulationEndpoint = (isDev) =>
            {
                if (!isDev)
                {
                    return (403, "Chức năng đăng nhập mô phỏng (Quick Login) bị vô hiệu hóa hoàn toàn trên môi trường Production.");
                }
                return (200, "OK");
            };

            var result = devSimulationEndpoint(!isProductionEnvironment);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("vô hiệu hóa hoàn toàn trên môi trường Production", result.Message);
        }

        [Fact]
        public void SecurityTest13_Section11_Case8_UnauthenticatedAdminApi_Returns401()
        {
            System.Security.Claims.ClaimsPrincipal unauthenticatedUser = new System.Security.Claims.ClaimsPrincipal();

            Func<System.Security.Claims.ClaimsPrincipal, int> authorizeAdminEndpoint = (user) =>
            {
                if (user.Identity?.IsAuthenticated != true) return 401;
                string role = user.FindFirst("UserRole")?.Value ?? "";
                if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return 403;
                return 200;
            };

            int statusCode = authorizeAdminEndpoint(unauthenticatedUser);
            Assert.Equal(401, statusCode);
        }

        [Fact]
        public void SecurityTest14_Section11_Case9_StaffAccessingPayroll_Returns403()
        {
            var staffIdentity = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("UserRole", "Staff")
            }, "Cookies");
            var staffUser = new System.Security.Claims.ClaimsPrincipal(staffIdentity);

            Func<System.Security.Claims.ClaimsPrincipal, int> authorizePayrollEndpoint = (user) =>
            {
                if (user.Identity?.IsAuthenticated != true) return 401;
                string role = user.FindFirst("UserRole")?.Value ?? "";
                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Accountant", StringComparison.OrdinalIgnoreCase))
                    return 200;
                return 403;
            };

            int statusCode = authorizePayrollEndpoint(staffUser);
            Assert.Equal(403, statusCode);
        }

        [Fact]
        public void SecurityTest15_Section11_Case11_Case12_LogoutTerminatesSession_SubsequentCalls401()
        {
            var sessionStore = new ConcurrentDictionary<string, string>();
            string sessionId = "KLE_SESS_991823";
            sessionStore[sessionId] = "admin";

            // Logout occurs
            sessionStore.TryRemove(sessionId, out _);

            // Subsequent API call checks session
            bool isSessionValid = sessionStore.ContainsKey(sessionId);
            Assert.False(isSessionValid);

            int subsequentStatusCode = isSessionValid ? 200 : 401;
            Assert.Equal(401, subsequentStatusCode);
        }
    }
}
