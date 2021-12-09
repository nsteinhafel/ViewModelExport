using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ViewModelExport.Writers;

/// <summary>Writer for conversion to TypeScript.</summary>
public class TypeScriptWriter : IWriter
{
    public string Process(Type t)
    {
        if (t == null) throw new ArgumentNullException(nameof(t));

        return t.IsEnum ? BuildEnum(t) : BuildInterface(t);
    }

    public string Name(Type t)
    {
        return t.IsEnum ? t.Name : $"I{t.Name}";
    }

    public string Extension => "ts";

    private string BuildInterface(Type t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"export interface {Name(t)} {{");
        foreach (var mi in GetInterfaceMembers(t))
        {
            sb.AppendLine($"    {ToCamelCase(mi.Name)}: {GetTypeName(mi)};");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static IEnumerable<MemberInfo> GetInterfaceMembers(IReflect type)
    {
        return type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(mi => mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property);
    }

    private string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length < 2) return s.ToLowerInvariant();
        return char.ToLowerInvariant(s[0]) + s.Substring(1);
    }

    private string GetTypeName(MemberInfo mi)
    {
        var t = mi is PropertyInfo info ? info.PropertyType : ((FieldInfo)mi).FieldType;
        return GetTypeName(t);
    }

    private string GetTypeName(Type t)
    {
        if (t.IsPrimitive)
        {
            if (t == typeof(bool)) return "boolean";
            if (t == typeof(char)) return "string";
            return "number";
        }

        if (t == typeof(decimal)) return "number";
        if (t == typeof(string)) return "string";
        if (t == typeof(Guid)) return "string";
        if (t.IsArray)
        {
            var at = t.GetElementType() ?? throw new InvalidOperationException();
            return GetTypeName(at) + "[]";
        }

        if (typeof(IEnumerable).IsAssignableFrom(t))
        {
            var collectionType =
                t.GetGenericArguments()[0];
            return GetTypeName(collectionType) + "[]";
        }

        var underlyingType = Nullable.GetUnderlyingType(t);
        if (underlyingType != null) return GetTypeName(underlyingType);
        return Name(t);
    }

    private string BuildEnum(Type t)
    {
        var sb = new StringBuilder();
        var values = (int[])Enum.GetValues(t);
        sb.AppendLine($"export enum {Name(t)} {{");
        foreach (var val in values)
        {
            var name = Enum.GetName(t, val);
            sb.AppendLine($"    {name} = {val},");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}