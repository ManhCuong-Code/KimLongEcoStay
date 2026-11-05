using System;
using System.Collections.Generic;
using System.IO;

namespace KimLongEcoStay.Web.Services
{
    /// <summary>
    /// Nạp biến môi trường từ file .env (nằm trong thư mục gốc dự án hoặc thư mục Web)
    /// Đảm bảo tuyệt đối không hardcode API Key hoặc mật khẩu SQL trong mã nguồn.
    /// </summary>
    public static class EnvConfigurationLoader
    {
        public static Dictionary<string, string> LoadDotEnv()
        {
            var loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var candidatePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
                Path.Combine(AppContext.BaseDirectory, ".env"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")
            };

            foreach (var rawPath in candidatePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(rawPath);
                    if (!File.Exists(fullPath)) continue;

                    foreach (var line in File.ReadAllLines(fullPath))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                        int eqIdx = trimmed.IndexOf('=');
                        if (eqIdx <= 0) continue;

                        string key = trimmed.Substring(0, eqIdx).Trim();
                        string val = trimmed.Substring(eqIdx + 1).Trim();

                        if ((val.StartsWith("\"") && val.EndsWith("\"")) ||
                            (val.StartsWith("'") && val.EndsWith("'")))
                        {
                            val = val.Substring(1, val.Length - 2);
                        }

                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            loaded[key] = val;
                            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                            {
                                Environment.SetEnvironmentVariable(key, val);
                            }
                        }
                    }
                    break;
                }
                catch
                {
                    // Bỏ qua nếu không đọc được đường dẫn ứng viên
                }
            }

            return loaded;
        }
    }
}

