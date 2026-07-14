using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static string CanonicalObjectSha256(object value)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        WriteCanonical(writer, value, value.GetType());
        writer.Flush();
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void WriteCanonical(BinaryWriter writer, object? value, Type declaredType)
    {
        writer.Write(declaredType.FullName ?? declaredType.Name);
        if (value is null)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        var nullableType = Nullable.GetUnderlyingType(declaredType);
        if (nullableType is not null)
        {
            WriteCanonical(writer, value, nullableType);
            return;
        }

        if (declaredType.IsEnum)
        {
            writer.Write(Convert.ToInt64(value));
        }
        else if (value is float single)
        {
            writer.Write(BitConverter.SingleToInt32Bits(single));
        }
        else if (value is double doubleValue)
        {
            writer.Write(BitConverter.DoubleToInt64Bits(doubleValue));
        }
        else if (value is string text)
        {
            writer.Write(text);
        }
        else if (value is bool boolean)
        {
            writer.Write(boolean);
        }
        else if (value is int integer)
        {
            writer.Write(integer);
        }
        else if (value is long longInteger)
        {
            writer.Write(longInteger);
        }
        else if (value is uint unsignedInteger)
        {
            writer.Write(unsignedInteger);
        }
        else if (value is ulong unsignedLong)
        {
            writer.Write(unsignedLong);
        }
        else if (value is IEnumerable enumerable)
        {
            var items = enumerable.Cast<object?>().ToArray();
            writer.Write(items.Length);
            var itemType = declaredType.IsArray
                ? declaredType.GetElementType()!
                : declaredType.IsGenericType ? declaredType.GetGenericArguments()[0] : typeof(object);
            foreach (var item in items)
            {
                WriteCanonical(writer, item, itemType);
            }
        }
        else
        {
            var properties = declaredType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead)
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ToArray();
            writer.Write(properties.Length);
            foreach (var property in properties)
            {
                writer.Write(property.Name);
                WriteCanonical(writer, property.GetValue(value), property.PropertyType);
            }
        }
    }
}
