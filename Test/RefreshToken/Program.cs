using System.Text;
using System.Text.Json;

namespace RefreshToken;

/// <summary>
/// Refresh Token 刷新令牌测试
/// 验证使用 refresh_token 获取新的 access_token 的完整流程
/// </summary>
class Program
{
    // ──────── 配置 ────────
    private const string TokenEndpoint = "https://localhost:7044/connect/token";
    private const string ClientId = "test_authcode_pkce";

    static async Task Main(string[] args)
    {
        Console.WriteLine("===== Refresh Token 刷新令牌测试 =====\n");
        Console.WriteLine("说明: 此测试验证使用 refresh_token 获取新 access_token 的流程。");
        Console.WriteLine("前提: 需要先通过 Authorization Code 或 Device Code 流程获取 refresh_token。\n");

        var tokenEndpoint = args.Length > 0 ? args[0] : TokenEndpoint;
        var clientId = args.Length > 1 ? args[1] : ClientId;

        Console.WriteLine($"Token 端点: {tokenEndpoint}");
        Console.WriteLine($"Client ID:  {clientId}\n");

        // 获取 Refresh Token
        Console.Write("请输入 Refresh Token（直接回车使用交互模式）: ");
        var refreshToken = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(refreshToken))
        {
            Console.WriteLine("\n交互模式: 将引导你完成授权码+PKCE流程获取 Refresh Token\n");
            refreshToken = await GetRefreshTokenViaPKCE(tokenEndpoint, clientId);
            if (string.IsNullOrEmpty(refreshToken))
            {
                Console.WriteLine("未能获取 Refresh Token，测试终止。");
                return;
            }
        }

        await TestRefreshToken(tokenEndpoint, clientId, refreshToken);
    }

    /// <summary>
    /// 通过 Authorization Code + PKCE 流程获取 Refresh Token（手动复制授权码方式）
    /// </summary>
    static async Task<string?> GetRefreshTokenViaPKCE(string tokenEndpoint, string clientId)
    {
        var redirectUri = "http://localhost:3000/";

        // 生成 PKCE
        var verifier = GenerateRandomString(64);
        var challenge = await Sha256Base64Url(verifier);

        var ssoUrl = "http://localhost:3010";
        var authUrl = $"{ssoUrl}/connect/authorize?" +
            $"client_id={clientId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString("openid profile email roles offline_access")}" +
            $"&code_challenge={challenge}" +
            $"&code_challenge_method=S256";

        Console.WriteLine("1. 请在浏览器中打开以下链接完成登录:");
        Console.WriteLine($"   {authUrl}\n");

        Console.WriteLine("2. 登录授权后，浏览器会跳转到回调地址。");
        Console.WriteLine("   请从浏览器地址栏复制 code 参数值，粘贴到下方：\n");
        Console.Write("   授权码 (code): ");
        var code = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(code))
        {
            Console.WriteLine("未输入授权码，测试终止。");
            return null;
        }

        // 用授权码换 Token
        var tokenParams = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = verifier
        };

        using var httpClient = CreateHttpClient();
        Console.WriteLine("\n3. 正在用授权码交换 Token...");
        var tokenResponse = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenParams));
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Token 交换失败: {tokenBody}");
            return null;
        }

        var tokenJson = JsonDocument.Parse(tokenBody);
        var refreshToken = tokenJson.RootElement.TryGetProperty("refresh_token", out var rtEl)
            ? rtEl.GetString() : null;

        if (refreshToken == null)
        {
            Console.WriteLine("响应中未包含 refresh_token。");
            Console.WriteLine("提示: 请求 scope 中需要包含 offline_access 才能获取 refresh_token。");
            Console.WriteLine(FormatJson(tokenBody));
            return null;
        }

        Console.WriteLine("4. 成功获取 Refresh Token!");
        Console.WriteLine($"   access_token:  {tokenJson.RootElement.GetProperty("access_token").GetString()![..Math.Min(30, tokenJson.RootElement.GetProperty("access_token").GetString()!.Length)]}...");
        Console.WriteLine($"   refresh_token: {refreshToken[..Math.Min(30, refreshToken.Length)]}...\n");

        return refreshToken;
    }

    /// <summary>
    /// 测试 Refresh Token 流程
    /// </summary>
    static async Task TestRefreshToken(string tokenEndpoint, string clientId, string refreshToken)
    {
        Console.WriteLine("===== 开始 Refresh Token 测试 =====\n");

        // 第一次刷新
        Console.WriteLine("1. 第一次刷新 Token...");
        var result1 = await DoRefresh(tokenEndpoint, clientId, refreshToken);

        if (result1 == null)
        {
            Console.WriteLine("刷新失败，测试终止。");
            return;
        }

        var newRefreshToken = result1.Value.RefreshToken ?? refreshToken;

        // 第二次刷新（使用新的 refresh_token）
        Console.WriteLine("\n2. 第二次刷新 Token（使用新 refresh_token）...");
        var result2 = await DoRefresh(tokenEndpoint, clientId, newRefreshToken);

        if (result2 == null)
        {
            Console.WriteLine("第二次刷新失败。");
            Console.WriteLine("可能原因: 服务端未启用 Refresh Token 轮换（Rotation）。");
            return;
        }

        // 尝试用旧的 refresh_token 刷新（应该失败，如果启用了轮换）
        Console.WriteLine("\n3. 尝试用旧的 refresh_token 刷新（测试令牌轮换）...");
        var result3 = await DoRefresh(tokenEndpoint, clientId, refreshToken);

        if (result3 == null)
        {
            Console.WriteLine("   旧 refresh_token 已失效 -> 令牌轮换（Rotation）已启用!");
        }
        else
        {
            Console.WriteLine("   旧 refresh_token 仍然有效 -> 令牌轮换未启用（建议启用以提高安全性）");
        }

        Console.WriteLine("\n===== 测试完成 =====");
    }

    /// <summary>
    /// 执行一次 Refresh Token 请求
    /// </summary>
    static async Task<(string AccessToken, string? RefreshToken)?> DoRefresh(string tokenEndpoint, string clientId, string refreshToken)
    {
        using var client = CreateHttpClient();

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId
        };

        Console.WriteLine($"   POST {tokenEndpoint}");
        Console.WriteLine($"   grant_type=refresh_token");
        Console.WriteLine($"   refresh_token={refreshToken[..Math.Min(20, refreshToken.Length)]}...");

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(parameters));
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = JsonDocument.Parse(body);
            var error = errorJson.RootElement.GetProperty("error").GetString();
            var desc = errorJson.RootElement.TryGetProperty("error_description", out var d) ? d.GetString() : "";
            Console.WriteLine($"   刷新失败: {error} - {desc}");
            return null;
        }

        var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var newRefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

        Console.WriteLine($"   刷新成功!");
        Console.WriteLine($"   新 access_token:  {accessToken[..Math.Min(30, accessToken.Length)]}...");
        if (newRefreshToken != null)
        {
            Console.WriteLine($"   新 refresh_token: {newRefreshToken[..Math.Min(30, newRefreshToken.Length)]}...");
        }

        // 解析新 Token 的过期时间
        var payloadStr = ParseJwtPayloadString(accessToken);
        if (payloadStr != null)
        {
            var payloadJson = JsonDocument.Parse(payloadStr);
            if (payloadJson.RootElement.TryGetProperty("exp", out var expEl))
            {
                var exp = DateTimeOffset.FromUnixTimeSeconds(expEl.GetInt64());
                Console.WriteLine($"   过期时间: {exp.LocalDateTime}");
            }
        }

        return (accessToken, newRefreshToken);
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

    // ──────── PKCE 工具 ────────

    static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    static async Task<string> Sha256Base64Url(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = await System.Security.Cryptography.SHA256.HashDataAsync(new MemoryStream(bytes));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>
    /// 解析 JWT Payload，返回 JSON 字符串
    /// </summary>
    static string? ParseJwtPayloadString(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            while (payload.Length % 4 != 0) payload += '=';
            var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            return Encoding.UTF8.GetString(jsonBytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 格式化 JSON
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
