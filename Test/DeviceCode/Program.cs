using System.Net;
using System.Text;
using System.Text.Json;

namespace DeviceCode;

/// <summary>
/// Device Authorization Grant（设备码授权）测试
/// 适用于 IoT 设备、智能电视、CLI 工具等无浏览器或输入受限的场景
/// </summary>
class Program
{
    // ──────── 配置 ────────
    private const string DeviceAuthEndpoint = "https://localhost:7044/connect/device";
    private const string TokenEndpoint = "https://localhost:7044/connect/token";
    private const string VerifyUrl = "http://localhost:3010/device-verify";
    private const string ClientId = "test_device_code";
    private const string Scope = "openid profile email roles";

    static async Task Main(string[] args)
    {
        Console.WriteLine("===== Device Authorization Grant（设备码授权）测试 =====\n");
        Console.WriteLine("说明: 此模式适用于 IoT 设备、CLI 工具等无浏览器场景。");
        Console.WriteLine("前提: 需要在 TPSSO 中注册一个客户端，授权类型包含 device_code。\n");

        var deviceAuthEndpoint = args.Length > 0 ? args[0] : DeviceAuthEndpoint;
        var tokenEndpoint = args.Length > 1 ? args[1] : TokenEndpoint;
        var clientId = args.Length > 2 ? args[2] : ClientId;
        var scope = args.Length > 3 ? args[3] : Scope;

        Console.WriteLine($"Device 端点: {deviceAuthEndpoint}");
        Console.WriteLine($"Token 端点:   {tokenEndpoint}");
        Console.WriteLine($"Client ID:    {clientId}");
        Console.WriteLine($"Scope:        {scope}\n");

        await TestDeviceCode(deviceAuthEndpoint, tokenEndpoint, clientId, scope);
    }

    /// <summary>
    /// 执行设备码授权流程
    /// </summary>
    static async Task TestDeviceCode(string deviceAuthEndpoint, string tokenEndpoint, string clientId, string scope)
    {
        using var client = CreateHttpClient();

        // ── 步骤 1: 请求设备码 ──
        Console.WriteLine("步骤 1: 请求设备码...");
        var deviceRequest = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = scope
        };

        Console.WriteLine($"   POST {deviceAuthEndpoint}");
        Console.WriteLine($"   client_id={clientId}");
        Console.WriteLine();

        var deviceResponse = await client.PostAsync(deviceAuthEndpoint, new FormUrlEncodedContent(deviceRequest));
        var deviceBody = await deviceResponse.Content.ReadAsStringAsync();

        if (!deviceResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"   请求失败: {(int)deviceResponse.StatusCode} {deviceResponse.ReasonPhrase}");
            Console.WriteLine($"   {FormatJson(deviceBody)}");
            Console.WriteLine();
            Console.WriteLine("可能原因:");
            Console.WriteLine("  - 客户端未注册或 Client ID 错误");
            Console.WriteLine("  - 客户端未启用 device_code 授权类型");
            Console.WriteLine("  - 客户端未审核通过");
            return;
        }

        // 解析设备码响应
        var deviceJson = JsonDocument.Parse(deviceBody);
        var deviceRoot = deviceJson.RootElement;

        var deviceCode = deviceRoot.GetProperty("device_code").GetString()!;
        var userCode = deviceRoot.GetProperty("user_code").GetString()!;
        // 使用前端验证页面地址，而非 OpenIddict 返回的后端端点地址
        var verificationUri = VerifyUrl;
        var verificationUriComplete = $"{VerifyUrl}?user_code={Uri.EscapeDataString(userCode)}";
        var expiresIn = deviceRoot.GetProperty("expires_in").GetInt32();
        var interval = deviceRoot.TryGetProperty("interval", out var intervalEl)
            ? intervalEl.GetInt32() : 5;

        Console.WriteLine("   设备码请求成功!");
        Console.WriteLine($"\n步骤 2: 请在浏览器中访问以下地址并输入验证码");
        Console.WriteLine($"   ┌─────────────────────────────────────────────┐");
        Console.WriteLine($"   │  验证地址: {verificationUri,-30} │");
        Console.WriteLine($"   │  验证码:   {userCode,-30} │");
        Console.WriteLine($"   │  有效期:   {expiresIn} 秒{(expiresIn >= 60 ? $" ({expiresIn / 60} 分钟)" : ""),-24} │");
        Console.WriteLine($"   └─────────────────────────────────────────────┘");

        if (verificationUriComplete != null)
        {
            Console.WriteLine($"\n   或直接访问完整链接: {verificationUriComplete}");
        }

        // ── 步骤 3: 轮询 Token 端点 ──
        Console.WriteLine($"\n步骤 3: 轮询 Token 端点（每 {interval} 秒）...");

        var startTime = DateTime.UtcNow;
        var deadline = startTime.AddSeconds(expiresIn);

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(interval * 1000);

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["device_code"] = deviceCode,
                ["client_id"] = clientId
            };

            var tokenResponse = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
            var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

            if (tokenResponse.IsSuccessStatusCode)
            {
                // 授权成功，获取到 Token
                Console.WriteLine("\n   授权成功! Token 已获取。\n");

                var tokenJson = JsonDocument.Parse(tokenBody);
                var tokenRoot = tokenJson.RootElement;

                Console.WriteLine("步骤 4: Token 信息:");
                Console.WriteLine(FormatJson(tokenBody));

                // 解析 Access Token
                if (tokenRoot.TryGetProperty("access_token", out var accessTokenEl))
                {
                    Console.WriteLine("\n步骤 5: 解析 Access Token (JWT Payload):");
                    Console.WriteLine(ParseJwtPayload(accessTokenEl.GetString()!));
                }

                // 显示 Refresh Token 信息
                if (tokenRoot.TryGetProperty("refresh_token", out var refreshTokenEl))
                {
                    Console.WriteLine($"\nRefresh Token: {refreshTokenEl.GetString()![..30]}...");
                    Console.WriteLine("提示: 可使用 Refresh Token 测试令牌刷新流程");
                }

                return;
            }

            // 解析错误
            var errorJson = JsonDocument.Parse(tokenBody);
            var errorRoot = errorJson.RootElement;
            var error = errorRoot.GetProperty("error").GetString()!;

            switch (error)
            {
                case "authorization_pending":
                    // 用户尚未完成授权，继续等待
                    Console.Write(".");
                    break;

                case "slow_down":
                    // 请求过于频繁，增加轮询间隔
                    interval += 5;
                    Console.WriteLine($"\n   请求过于频繁，轮询间隔调整为 {interval} 秒");
                    break;

                case "expired_token":
                    Console.WriteLine("\n   设备码已过期，请重新发起授权请求。");
                    return;

                case "access_denied":
                    Console.WriteLine("\n   用户拒绝了授权请求。");
                    return;

                default:
                    Console.WriteLine($"\n   未知错误: {error}");
                    if (errorRoot.TryGetProperty("error_description", out var descEl))
                    {
                        Console.WriteLine($"   描述: {descEl.GetString()}");
                    }
                    return;
            }
        }

        Console.WriteLine("\n   设备码已过期（超时），请重新发起授权请求。");
    }

    /// <summary>
    /// 创建 HttpClient（绕过开发环境自签名 SSL 证书验证）
    /// </summary>
    static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new HttpClient(handler);
    }

    /// <summary>
    /// 解析 JWT Payload 部分
    /// </summary>
    static string ParseJwtPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return "(无效的 JWT 格式)";

            var payload = parts[1];
            while (payload.Length % 4 != 0) payload += '=';
            var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var json = Encoding.UTF8.GetString(jsonBytes);
            return FormatJson(json);
        }
        catch (Exception ex)
        {
            return $"(JWT 解析失败: {ex.Message})";
        }
    }

    /// <summary>
    /// 格式化 JSON 输出
    /// </summary>
    static string FormatJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}
