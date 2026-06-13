using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClientCredentials;

/// <summary>
/// Client Credentials 授权模式测试
/// 适用于 M2M（机器对机器）场景，无用户参与
/// </summary>
class Program
{
    // ──────── 配置 ────────
    private const string TokenEndpoint = "https://localhost:7044/connect/token";
    private const string ClientId = "test_client_credentials";
    private const string ClientSecret = "test_secret_123";
    private const string Scope = "openid profile email roles";

    static async Task Main(string[] args)
    {
        Console.WriteLine("===== Client Credentials 授权模式测试 =====\n");
        Console.WriteLine("说明: 此模式适用于服务间调用（M2M），无需用户参与。");
        Console.WriteLine("前提: 需要在 TPSSO 中注册一个机密客户端，授权类型包含 client_credentials。\n");

        // 使用配置值或命令行参数
        var tokenEndpoint = args.Length > 0 ? args[0] : TokenEndpoint;
        var clientId = args.Length > 1 ? args[1] : ClientId;
        var clientSecret = args.Length > 2 ? args[2] : ClientSecret;
        var scope = args.Length > 3 ? args[3] : Scope;

        Console.WriteLine($"Token 端点: {tokenEndpoint}");
        Console.WriteLine($"Client ID:  {clientId}");
        Console.WriteLine($"Scope:      {scope}\n");

        await TestClientCredentials(tokenEndpoint, clientId, clientSecret, scope);
    }

    /// <summary>
    /// 执行 Client Credentials 授权流程
    /// </summary>
    static async Task TestClientCredentials(string tokenEndpoint, string clientId, string clientSecret, string scope)
    {
        using var client = CreateHttpClient();

        // 构造请求参数
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = scope
        };

        Console.WriteLine("1. 发送 Token 请求...");
        Console.WriteLine($"   POST {tokenEndpoint}");
        Console.WriteLine($"   grant_type=client_credentials");
        Console.WriteLine($"   client_id={clientId}");
        Console.WriteLine();

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(parameters));
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"2. 响应状态: {(int)response.StatusCode} {response.ReasonPhrase}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("   请求失败！");
            Console.WriteLine($"   响应: {FormatJson(responseBody)}");
            Console.WriteLine();
            Console.WriteLine("可能原因:");
            Console.WriteLine("  - 客户端未注册或 Client ID 错误");
            Console.WriteLine("  - Client Secret 不匹配");
            Console.WriteLine("  - 客户端未启用 client_credentials 授权类型");
            Console.WriteLine("  - 客户端未审核通过");
            return;
        }

        // 解析响应
        var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Console.WriteLine("\n3. Token 信息:");
        Console.WriteLine(FormatJson(responseBody));

        // 提取 Access Token
        if (root.TryGetProperty("access_token", out var accessTokenEl))
        {
            var accessToken = accessTokenEl.GetString()!;
            Console.WriteLine("\n4. 解析 Access Token (JWT Payload):");
            Console.WriteLine(ParseJwtPayload(accessToken));

            // 尝试用 Token 调用受保护 API
            Console.WriteLine("\n5. 测试调用受保护 API...");
            await TestProtectedApi(accessToken);
        }

        // 注意: Client Credentials 模式不返回 refresh_token 和 id_token
        if (!root.TryGetProperty("refresh_token", out _))
        {
            Console.WriteLine("\n注意: Client Credentials 模式不返回 refresh_token");
        }
        if (!root.TryGetProperty("id_token", out _))
        {
            Console.WriteLine("注意: Client Credentials 模式不返回 id_token（无用户身份）");
        }
    }

    /// <summary>
    /// 使用 Access Token 调用受保护的 API
    /// </summary>
    static async Task TestProtectedApi(string accessToken)
    {
        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // 尝试调用 Admin API 的用户信息接口
        var apiUrl = "https://localhost:7045/api/account/me";
        Console.WriteLine($"   GET {apiUrl}");
        Console.WriteLine($"   Authorization: Bearer {accessToken[..30]}...");

        try
        {
            var response = await client.GetAsync(apiUrl);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"   响应: {(int)response.StatusCode} {response.ReasonPhrase}");
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"   {FormatJson(body)}");
            }
            else
            {
                Console.WriteLine($"   {body}");
                Console.WriteLine("   (此为预期结果: Client Credentials 获取的 Token 无用户身份，可能无法访问需要用户身份的 API)");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"   请求失败: {ex.Message}");
            Console.WriteLine("   (Admin API 可能未启动，这不影响 Token 验证)");
        }
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
            // 补齐 Base64 填充
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
