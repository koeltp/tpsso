using System.Text.Json;
using System.Text.Json.Serialization;

namespace TPSSO.Application.Models;

/// <summary>
/// DateTime UTC 转换器
/// EF Core 从数据库读取的 DateTime.Kind 为 Unspecified，序列化时不会带 Z 后缀
/// 导致前端 new Date() 将其当作本地时间解析，显示时间偏移 8 小时
/// 此转换器将 Unspecified 的 DateTime 视为 UTC，序列化时输出 Z 后缀
/// </summary>
public class DateTimeUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        // 反序列化时确保 Kind 为 UTC
        if (value.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Unspecified 视为 UTC，确保输出带 Z 后缀
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value;
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
