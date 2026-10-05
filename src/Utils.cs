using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Text;

namespace PSLauncher;

public static class Utils
{
    private static Dictionary<string, string> TypeAbbreviations = new Dictionary<string, string> {
            { "System.Management.Automation.AliasAttribute" , "Alias" },
            { "System.Management.Automation.AllowEmptyCollectionAttribute" , "AllowEmptyCollection" },
            { "System.Management.Automation.AllowEmptyStringAttribute" , "AllowEmptyString" },
            { "System.Management.Automation.AllowNullAttribute" , "AllowNull" },
            { "System.Management.Automation.ArgumentCompleterAttribute" , "ArgumentCompleter" },
            { "System.Management.Automation.ArgumentCompletionsAttribute" , "ArgumentCompletions" },
            { "System.Array" , "array" },
            { "System.Boolean" , "bool" },
            { "System.Byte" , "byte" },
            { "System.Char" , "char" },
            { "System.Management.Automation.CmdletBindingAttribute" , "CmdletBinding" },
            { "System.DateTime" , "datetime" },
            { "System.Decimal" , "decimal" },
            { "System.Double" , "double" },
            { "System.Management.Automation.DscResourceAttribute" , "DscResource" },
            { "System.Management.Automation.ExperimentAction" , "ExperimentAction" },
            { "System.Management.Automation.ExperimentalAttribute" , "Experimental" },
            { "System.Management.Automation.ExperimentalFeature" , "ExperimentalFeature" },
            { "System.Single" , "float" },
            { "System.Guid" , "guid" },
            { "System.Collections.Hashtable" , "hashtable" },
            { "System.Int32" , "int" },
            { "System.Int16" , "short" },
            { "System.Int64" , "long" },
            { "Microsoft.Management.Infrastructure.CimInstance" , "ciminstance" },
            { "Microsoft.Management.Infrastructure.CimClass" , "cimclass" },
            { "Microsoft.Management.Infrastructure.CimType" , "cimtype" },
            { "Microsoft.Management.Infrastructure.CimConverter" , "cimconverter" },
            { "System.Net.IPEndPoint" , "IPEndpoint" },
            { "System.Management.Automation.Language.NoRunspaceAffinityAttribute" , "NoRunspaceAffinity" },
            { "System.Management.Automation.Language.NullString" , "NullString" },
            { "System.Management.Automation.OutputTypeAttribute" , "OutputType" },
            { "System.Security.AccessControl.ObjectSecurity" , "ObjectSecurity" },
            { "System.Collections.Specialized.OrderedDictionary" , "ordered" },
            { "System.Management.Automation.ParameterAttribute" , "Parameter" },
            { "System.Net.NetworkInformation.PhysicalAddress" , "PhysicalAddress" },
            { "System.Management.Automation.PSCredential" , "pscredential" },
            { "System.Management.Automation.PSDefaultValueAttribute" , "PSDefaultValue" },
            { "System.Management.Automation.PSListModifier" , "pslistmodifier" },
            { "System.Management.Automation.PSObject" , "psobject" },
            { "System.Management.Automation.PSPrimitiveDictionary" , "psprimitivedictionary" },
            { "System.Management.Automation.PSReference" , "ref" },
            { "System.Management.Automation.PSTypeNameAttribute" , "PSTypeNameAttribute" },
            { "System.Text.RegularExpressions.Regex" , "regex" },
            { "System.Management.Automation.DscPropertyAttribute" , "DscProperty" },
            { "System.SByte" , "sbyte" },
            { "System.String" , "string" },
            { "System.Management.Automation.SupportsWildcardsAttribute" , "SupportsWildcards" },
            { "System.Management.Automation.SwitchParameter" , "switch" }, // Should this be SwitchParameter?
            { "System.Globalization.CultureInfo" , "cultureinfo" },
            { "System.Numerics.BigInteger" , "bigint" },
            { "System.Security.SecureString" , "securestring" },
            { "System.TimeSpan" , "timespan" },
            { "System.UInt16" , "ushort" },
            { "System.UInt32" , "uint" },
            { "System.UInt64" , "ulong" },
            { "System.Uri" , "uri" },
            { "System.Management.Automation.ValidateCountAttribute" , "ValidateCount" },
            { "System.Management.Automation.ValidateDriveAttribute" , "ValidateDrive" },
            { "System.Management.Automation.ValidateLengthAttribute" , "ValidateLength" },
            { "System.Management.Automation.ValidateNotNullAttribute" , "ValidateNotNull" },
            { "System.Management.Automation.ValidateNotNullOrEmptyAttribute" , "ValidateNotNullOrEmpty" },
            { "System.Management.Automation.ValidateNotNullOrWhiteSpaceAttribute" , "ValidateNotNullOrWhiteSpace" },
            { "System.Management.Automation.ValidatePatternAttribute" , "ValidatePattern" },
            { "System.Management.Automation.ValidateRangeAttribute" , "ValidateRange" },
            { "System.Management.Automation.ValidateScriptAttribute" , "ValidateScript" },
            { "System.Management.Automation.ValidateSetAttribute" , "ValidateSet" },
            { "System.Management.Automation.ValidateTrustedDataAttribute" , "ValidateTrustedData" },
            { "System.Management.Automation.ValidateUserDriveAttribute" , "ValidateUserDrive" },
            { "System.Version" , "version" },
            { "System.Void" , "void" },
            { "System.Net.IPAddress" , "ipaddress" },
            { "System.Management.Automation.DscLocalConfigurationManagerAttribute" , "DscLocalConfigurationManager" },
            { "System.Management.Automation.WildcardPattern" , "WildcardPattern" },
            { "System.Security.Cryptography.X509Certificates.X509Certificate" , "X509Certificate" },
            { "System.Security.Cryptography.X509Certificates.X500DistinguishedName" , "X500DistinguishedName" },
            { "System.Xml.XmlDocument" , "xml" },
            { "Microsoft.Management.Infrastructure.CimSession" , "CimSession" },
            { "System.Net.Mail.MailAddress" , "mailaddress" },
            { "System.Management.Automation.SemanticVersion" , "semver" },
            { "System.Management.Automation.ScriptBlock" , "scriptblock" },
            { "Microsoft.PowerShell.Commands.PSPropertyExpression" , "pspropertyexpression" },
            { "System.Management.Automation.PSVariable" , "psvariable" },
            { "System.Type" , "type" },
            { "System.Management.Automation.PSModuleInfo" , "psmoduleinfo" },
            { "System.Management.Automation.PowerShell" , "powershell" },
            { "System.Management.Automation.Runspaces.RunspaceFactory" , "runspacefactory" },
            { "System.Management.Automation.Runspaces.Runspace" , "runspace" },
            { "System.Management.Automation.Runspaces.InitialSessionState" , "initialsessionstate" },
            { "System.Management.Automation.PSScriptMethod" , "psscriptmethod" },
            { "System.Management.Automation.PSScriptProperty" , "psscriptproperty" },
            { "System.Management.Automation.PSNoteProperty" , "psnoteproperty" },
            { "System.Management.Automation.PSAliasProperty" , "psaliasproperty" },
            { "System.Management.Automation.PSVariableProperty" , "psvariableproperty" },
        };


    private static void BuildTypeString(StringBuilder sb,
                                        Type? type,
                                        bool abbreviate,
                                        bool dropNamespace,
                                        bool fromNested = false)
    {
        if (type is null)
        {
            return;
        }
        var underlingType = Nullable.GetUnderlyingType(type);
        if (underlingType is not null)
        {
            BuildTypeString(sb, underlingType, abbreviate, dropNamespace, fromNested);
            sb.Append('?');
            return;
        }
        if (type.IsByRef)
        {
            BuildTypeString(sb, type.GetElementType(), abbreviate, dropNamespace);
            sb.Append('&');
            return;
        }
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            BuildTypeString(sb, type.GetGenericTypeDefinition(), abbreviate, dropNamespace);
            var genericArgs = type.GetGenericArguments();
            sb.Append('<');
            for (var i = 0; i < genericArgs.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                BuildTypeString(sb, genericArgs[i], abbreviate, dropNamespace);
            }
            sb.Append('>');
        }
        else if (type.IsArray)
        {
            BuildTypeString(sb, type.GetElementType(), abbreviate, dropNamespace);
            sb.Append('[')
              .Append(',', type.GetArrayRank() - 1)
              .Append(']');
        }
        else
        {
            if (abbreviate && TypeAbbreviations.TryGetValue(type.FullName ?? string.Empty, out var abbreviatedName))
            {
                sb.Append(abbreviatedName);
                return;
            }
            if (!dropNamespace && !string.IsNullOrEmpty(type.Namespace))
            {
                sb.Append(type.Namespace)
                  .Append('.');
            }
            bool canOmmitGenericTailingSign = true;
            if (type.IsNested)
            {
                var reflectedType = type.ReflectedType;
                BuildTypeString(sb, reflectedType, abbreviate, dropNamespace: true, fromNested: true);
                sb.Append('+');
                canOmmitGenericTailingSign = !(reflectedType?.IsGenericType ?? false);

            }
            if (!fromNested && canOmmitGenericTailingSign)
            {
                var backticPosition = type.Name.LastIndexOf('`');
                sb.Append(backticPosition > 0 ? type.Name.Remove(backticPosition) : type.Name);
            }
            else
            {
                sb.Append(type.Name);
            }
        }
    }


    [return: NotNullIfNotNull(nameof(value))]
    public static object? GetBaseObject(object? value)
    {
        if (value is null)
            return null;

        if (value is PSObject pso && pso.BaseObject is not PSCustomObject)
        {
            return pso.BaseObject;
        }
        return value;
    }

    public static IEnumerable<object?> GetBaseObjects(IEnumerable<object?> values)
        => values.Select(static value => value is null ? null : GetBaseObject(value));

    public static bool IsRefStructObject(object? value)
    {
        if (value is null)
            return false;
        var t = value.GetType();
        return t.IsValueType && t.IsByRefLike;
    }

    public static string GetTypeName(Type type, bool abbreviate = true, bool dropNamespace = true)
    {
        var sb = StringBuilderPool.Rent(512);
        try
        {
            BuildTypeString(sb, type, abbreviate: abbreviate, dropNamespace: dropNamespace);
            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Returns the string representation of an object, or an empty string if the object is null or does not override ToString().
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ObjectToString(object? value)
    {
        if (value is null)
            return string.Empty;
        var t = value.GetType();
        var toStringMethod = t.GetMethod("ToString", Type.EmptyTypes);
        return toStringMethod?.DeclaringType?.FullName is not "System.Object"
            ? toStringMethod?.Invoke(value, null) as string ?? string.Empty
            : string.Empty;
    }

    public static string QuotedPrintable(ReadOnlySpan<char> str, int maxLength = 255, string elipsis = "...")
    {
        var sb = StringBuilderPool.Rent(maxLength);
        var actualLength = maxLength - 2;
        try
        {
            sb.Append('"');
            int len = str.Length;
            bool useElipsis = false;
            if (len > actualLength)
            {
                useElipsis = true;
                len = actualLength - elipsis.Length;
            }
            for (var i = 0; i < len; i++)
            {
                char c = str[i];
                sb.Append(c switch
                {
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\e' => "\\e",
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    _ => c
                });
            }
            if (useElipsis)
            {
                sb.Append(elipsis);
            }
            sb.Append('"');
            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }
}
