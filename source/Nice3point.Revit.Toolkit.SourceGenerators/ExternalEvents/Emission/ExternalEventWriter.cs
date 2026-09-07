using Nice3point.Revit.Toolkit.SourceGenerators.CSharp;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Emission;

internal static class ExternalEventWriter
{
    private const string GeneratorName = "Nice3point.Revit.Toolkit.SourceGenerators.ExternalEventGenerator";

    private static readonly string AssemblyVersion = typeof(ExternalEventGenerator).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    public static string GenerateSource(ExternalEventDefinition eventDefinition, bool useFieldKeyword, bool useLockType)
    {
        var writer = new CodeWriter();

        var namespaceBlock = WriteNamespaceBlock(writer, eventDefinition);
        var typeBlocks = WriteTypeHierarchy(writer, eventDefinition);

        WriteProperties(writer, eventDefinition, useFieldKeyword, useLockType);
        WriteArgumentsRecord(writer, eventDefinition);
        CloseBlocks(typeBlocks);
        WriteExtensionClass(writer, eventDefinition);

        namespaceBlock?.Dispose();
        return writer.ToString();
    }

    private static IDisposable? WriteNamespaceBlock(CodeWriter writer, ExternalEventDefinition eventDefinition)
    {
        return !string.IsNullOrEmpty(eventDefinition.Namespace)
            ? writer.BeginBlock($"namespace {eventDefinition.Namespace}")
            : null;
    }

    private static List<IDisposable> WriteTypeHierarchy(CodeWriter writer, ExternalEventDefinition eventDefinition)
    {
        var typeBlocks = new List<IDisposable>();
        foreach (var typeDeclaration in eventDefinition.TypeHierarchy)
        {
            var staticModifier = typeDeclaration.IsStatic ? "static " : string.Empty;
            var typeName = FormatGenericType(typeDeclaration.Name, FormatTypeParameters(typeDeclaration.TypeParameters));
            var block = writer.BeginBlock($"{typeDeclaration.Accessibility} {staticModifier}partial {typeDeclaration.Keyword} {typeName}");
            typeBlocks.Add(block);
        }

        return typeBlocks;
    }

    private static void WriteProperties(CodeWriter writer, ExternalEventDefinition eventDefinition, bool useFieldKeyword, bool useLockType)
    {
        if (eventDefinition.ReturnsVoid)
        {
            WriteEventProperty(writer, eventDefinition, useFieldKeyword, useLockType, EventPropertyKind.Sync);
            writer.AppendLine();
            WriteEventProperty(writer, eventDefinition, useFieldKeyword, useLockType, EventPropertyKind.Async);
        }
        else
        {
            WriteEventProperty(writer, eventDefinition, useFieldKeyword, useLockType, EventPropertyKind.AsyncResult);
        }
    }

    private static void WriteArgumentsRecord(CodeWriter writer, ExternalEventDefinition eventDefinition)
    {
        if (eventDefinition.ExtraParameters.Length < 2)
        {
            return;
        }

        writer.AppendLine();

        var recordName = $"{eventDefinition.MethodName}Args";
        var recordParameters = new List<string>();
        foreach (var parameter in eventDefinition.ExtraParameters)
        {
            recordParameters.Add($"{parameter.FullyQualifiedType} {CSharpIdentifier.Escape(parameter.RecordPropertyName)}");
        }

        WriteExcludeFromCodeCoverageAttributes(writer);
        WriteGeneratedCodeAttributes(writer);
        writer.AppendLine($"public sealed record {recordName}({string.Join(", ", recordParameters)});");
    }

    private static void WriteExtensionClass(CodeWriter writer, ExternalEventDefinition eventDefinition)
    {
        if (eventDefinition.ExtraParameters.Length < 2)
        {
            return;
        }

        if (eventDefinition.TypeHierarchy.Any(type => type.Accessibility is "private" or "protected" or "private protected"))
        {
            return;
        }

        var outermostTypeName = eventDefinition.TypeHierarchy[0].Name;
        var qualifiedRecordType = eventDefinition.Extension.QualifiedRecordType;

        writer.AppendLine();

        using (writer.BeginBlock($"{eventDefinition.Extension.ClassAccessibility} static partial class {outermostTypeName}Extensions"))
        {
            if (eventDefinition.ReturnsVoid)
            {
                WriteRaiseExtensionMethod(writer, eventDefinition, qualifiedRecordType, false);
                writer.AppendLine();
            }

            WriteRaiseExtensionMethod(writer, eventDefinition, qualifiedRecordType, true);
        }
    }

    private static void WriteEventProperty(CodeWriter writer, ExternalEventDefinition eventDefinition, bool useFieldKeyword, bool useLockType, EventPropertyKind propertyKind)
    {
        var staticModifier = eventDefinition.IsStatic ? "static " : string.Empty;
        var optionsArgument = eventDefinition.AllowDirectInvocation
            ? $", {ExternalEventTypeNames.ExternalEventOptions}.AllowDirectInvocation"
            : string.Empty;

        var (eventTypeName, interfaceTypeName, propertySuffix) = GetEventTypeNames(propertyKind);
        var typeArguments = BuildTypeArguments(eventDefinition, propertyKind);

        var eventType = FormatGenericType(eventTypeName.WithGlobalPrefix, typeArguments);
        var interfaceType = FormatGenericType(interfaceTypeName.WithGlobalPrefix, typeArguments);

        var captureHandler = eventDefinition is { IsStatic: false, ExtraParameters.Length: > 1 } && eventDefinition.TypeHierarchy[^1].Keyword is "struct" or "record struct";
        var reservedLocalNames = new HashSet<string>(eventDefinition.TypeHierarchy.SelectMany(type => type.TypeParameters), StringComparer.Ordinal)
        {
            eventDefinition.MethodName
        };

        var handlerName = captureHandler ? CSharpIdentifier.Reserve("handler", reservedLocalNames) : CSharpIdentifier.Escape(eventDefinition.MethodName);
        var applicationName = CSharpIdentifier.Reserve("application", reservedLocalNames);
        var argumentsName = CSharpIdentifier.Reserve("args", reservedLocalNames);

        var initializer = BuildPropertyInitializer(eventDefinition, eventType, optionsArgument, handlerName, applicationName, argumentsName);
        var eventProperty = new EventPropertyDefinition(staticModifier, interfaceType, propertySuffix, initializer, captureHandler ? handlerName : null);

        WritePropertyWithBackingField(writer, eventDefinition, useFieldKeyword, useLockType, eventProperty);
    }

    private static void WritePropertyWithBackingField(
        CodeWriter writer,
        ExternalEventDefinition eventDefinition,
        bool useFieldKeyword,
        bool useLockType,
        EventPropertyDefinition eventProperty)
    {
        var memberNames = new HashSet<string>(eventDefinition.ReservedMemberNames, StringComparer.Ordinal);
        var backingFieldName = useFieldKeyword ? "field" : CSharpIdentifier.Reserve($"_{eventDefinition.MethodName}{eventProperty.Suffix}BackingField", memberNames);
        var gateName = CSharpIdentifier.Reserve($"_{eventDefinition.MethodName}{eventProperty.Suffix}Lock", memberNames);

        var gateType = useLockType ? "global::System.Threading.Lock" : "object";

        writer.AppendLine($"private {eventProperty.StaticModifier}{gateType}? {gateName};");
        if (!useFieldKeyword)
        {
            writer.AppendLine($"private {eventProperty.StaticModifier}{eventProperty.Type}? {backingFieldName};");
        }

        writer.AppendLine();
        WriteExcludeFromCodeCoverageAttributes(writer);
        WriteGeneratedCodeAttributes(writer);
        using (writer.BeginBlock($"public {eventProperty.StaticModifier}{eventProperty.Type} {eventDefinition.MethodName}{eventProperty.Suffix}"))
        using (writer.BeginBlock("get"))
        using (writer.BeginBlock($"lock (global::System.Threading.LazyInitializer.EnsureInitialized(ref {gateName}))"))
        {
            if (eventProperty.CapturedHandlerName is null)
            {
                WritePropertyInitialization(writer, $"return {backingFieldName} ??= ", eventProperty.Initializer);
                return;
            }

            using (writer.BeginBlock($"if ({backingFieldName} is not null)"))
            {
                writer.AppendLine($"return {backingFieldName};");
            }

            writer.AppendLine();
            writer.AppendLine($"var {eventProperty.CapturedHandlerName} = new {eventDefinition.FullyQualifiedDelegateType}({CSharpIdentifier.Escape(eventDefinition.MethodName)});");
            WritePropertyInitialization(writer, $"return {backingFieldName} = ", eventProperty.Initializer);
        }
    }

    private static void WritePropertyInitialization(CodeWriter writer, string assignment, EventInitializer initializer)
    {
        if (initializer.LambdaBody is null)
        {
            writer.AppendLine($"{assignment}{initializer.Expression}{initializer.OptionsArgument});");
            return;
        }

        using (writer.BeginBlock($"{assignment}{initializer.Expression}", $"}}{initializer.OptionsArgument});"))
        {
            writer.AppendLine(initializer.LambdaBody);
        }
    }

    private static void WriteRaiseExtensionMethod(CodeWriter writer, ExternalEventDefinition eventDefinition, string qualifiedRecordType, bool isAsync)
    {
        string interfaceType;
        string returnType;
        string methodName;

        if (!isAsync)
        {
            interfaceType = $"{ExternalEventTypeNames.ExternalEventInterface.WithGlobalPrefix}<{qualifiedRecordType}>";
            returnType = ExternalEventTypeNames.ExternalEventRequest.WithGlobalPrefix;
            methodName = "Raise";
        }
        else if (eventDefinition.ReturnsVoid)
        {
            interfaceType = $"{ExternalEventTypeNames.AsyncExternalEventInterface.WithGlobalPrefix}<{qualifiedRecordType}>";
            returnType = ExternalEventTypeNames.Task.WithGlobalPrefix;
            methodName = "RaiseAsync";
        }
        else
        {
            var resultType = eventDefinition.Extension.FullyQualifiedReturnType!;
            interfaceType = $"{ExternalEventTypeNames.AsyncRequestExternalEventInterface.WithGlobalPrefix}<{qualifiedRecordType}, {resultType}>";
            returnType = $"{ExternalEventTypeNames.Task.WithGlobalPrefix}<{resultType}>";
            methodName = "RaiseAsync";
        }

        var parameters = BuildExtensionMethodParameters(eventDefinition);
        var recordArguments = BuildRecordConstructorArguments(eventDefinition);

        var typeParameters = FormatTypeParameters(eventDefinition.Extension.TypeParameters);
        var genericMethodName = FormatGenericType(methodName, typeParameters);
        var constraints = eventDefinition.Extension.Constraints;

        var parameterNames = new HashSet<string>(eventDefinition.ExtraParameters.Select(parameter => parameter.Name), StringComparer.Ordinal);
        parameterNames.UnionWith(eventDefinition.Extension.TypeParameters);
        var receiverName = CSharpIdentifier.Reserve("externalEvent", parameterNames);
        var accessibility = GetMostRestrictiveAccessibility(eventDefinition.TypeHierarchy);

        WriteExcludeFromCodeCoverageAttributes(writer);
        WriteGeneratedCodeAttributes(writer);
        using (writer.BeginBlock($"{accessibility} static {returnType} {genericMethodName}(this {interfaceType} {receiverName}, {parameters}){constraints}"))
        {
            writer.AppendLine($"return {receiverName}.{methodName}(new {qualifiedRecordType}({recordArguments}));");
        }
    }

    private static void WriteExcludeFromCodeCoverageAttributes(CodeWriter writer)
    {
        writer.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
    }

    private static void WriteGeneratedCodeAttributes(CodeWriter writer)
    {
        writer.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{AssemblyVersion}\")]");
    }

    private static string? BuildTypeArguments(ExternalEventDefinition eventDefinition, EventPropertyKind propertyKind)
    {
        var argType = GetParameterTypeArgument(eventDefinition);
        var returnType = propertyKind == EventPropertyKind.AsyncResult ? eventDefinition.FullyQualifiedReturnType : null;

        if (argType is null && returnType is null)
        {
            return null;
        }

        if (argType is not null && returnType is not null)
        {
            return $"{argType}, {returnType}";
        }

        return argType ?? returnType;
    }

    private static string BuildHandlerInvocation(ExternalEventDefinition eventDefinition, string argumentsName, string handlerName, string applicationName)
    {
        var arguments = new List<string>();
        if (eventDefinition.HasUiApplicationParameter)
        {
            arguments.Add(applicationName);
        }

        foreach (var parameter in eventDefinition.ExtraParameters)
        {
            arguments.Add($"{argumentsName}.{CSharpIdentifier.Escape(parameter.RecordPropertyName)}");
        }

        return $"{handlerName}({string.Join(", ", arguments)})";
    }

    private static EventInitializer BuildPropertyInitializer(
        ExternalEventDefinition eventDefinition,
        string eventType,
        string optionsArgument,
        string handlerName,
        string applicationName,
        string argumentsName)
    {
        if (eventDefinition.ExtraParameters.Length < 2)
        {
            return new EventInitializer($"new {eventType}(new {eventDefinition.FullyQualifiedDelegateType}({handlerName})", null, optionsArgument);
        }

        var handlerInvocation = BuildHandlerInvocation(eventDefinition, argumentsName, handlerName, applicationName);
        var lambdaParameters = eventDefinition.HasUiApplicationParameter ? $"{applicationName}, {argumentsName}" : argumentsName;
        var returnKeyword = eventDefinition.ReturnsVoid ? string.Empty : "return ";
        return new EventInitializer($"new {eventType}(({lambdaParameters}) =>", $"{returnKeyword}{handlerInvocation};", optionsArgument);
    }

    private static string BuildExtensionMethodParameters(ExternalEventDefinition eventDefinition)
    {
        var parameters = new List<string>();
        for (var parameterIndex = 0; parameterIndex < eventDefinition.ExtraParameters.Length; parameterIndex++)
        {
            parameters.Add($"{eventDefinition.Extension.ParameterTypes[parameterIndex]} {CSharpIdentifier.Escape(eventDefinition.ExtraParameters[parameterIndex].Name)}");
        }

        return string.Join(", ", parameters);
    }

    private static string BuildRecordConstructorArguments(ExternalEventDefinition eventDefinition)
    {
        var arguments = new List<string>();
        foreach (var parameter in eventDefinition.ExtraParameters)
        {
            arguments.Add(CSharpIdentifier.Escape(parameter.Name));
        }

        return string.Join(", ", arguments);
    }

    private static string? GetParameterTypeArgument(ExternalEventDefinition eventDefinition)
    {
        return eventDefinition.ExtraParameters.Length switch
        {
            0 => null,
            1 => eventDefinition.ExtraParameters[0].FullyQualifiedType,
            _ => $"{eventDefinition.MethodName}Args"
        };
    }

    private static (FullyQualifiedTypeName EventType, FullyQualifiedTypeName InterfaceType, string PropertySuffix) GetEventTypeNames(EventPropertyKind propertyKind)
    {
        return propertyKind switch
        {
            EventPropertyKind.Sync => (ExternalEventTypeNames.ExternalEvent, ExternalEventTypeNames.ExternalEventInterface, "Event"),
            EventPropertyKind.Async => (ExternalEventTypeNames.AsyncExternalEvent, ExternalEventTypeNames.AsyncExternalEventInterface, "AsyncEvent"),
            EventPropertyKind.AsyncResult => (ExternalEventTypeNames.AsyncRequestExternalEvent, ExternalEventTypeNames.AsyncRequestExternalEventInterface, "AsyncEvent"),
            _ => throw new ArgumentOutOfRangeException(nameof(propertyKind))
        };
    }

    private static string GetMostRestrictiveAccessibility(EquatableArray<ContainingTypeDeclaration> typeHierarchy)
    {
        foreach (var type in typeHierarchy)
        {
            if (type.Accessibility != "public")
            {
                return "internal";
            }
        }

        return "public";
    }

    private static void CloseBlocks(List<IDisposable> typeBlocks)
    {
        for (var typeIndex = typeBlocks.Count - 1; typeIndex >= 0; typeIndex--)
        {
            typeBlocks[typeIndex].Dispose();
        }
    }

    private static string? FormatTypeParameters(IEnumerable<string> typeParameters)
    {
        var parameters = string.Join(", ", typeParameters);
        return parameters.Length == 0 ? null : parameters;
    }

    private static string FormatGenericType(string typeName, string? typeArguments)
    {
        return typeArguments is not null ? $"{typeName}<{typeArguments}>" : typeName;
    }

    private readonly record struct EventPropertyDefinition(string StaticModifier, string Type, string Suffix, EventInitializer Initializer, string? CapturedHandlerName);

    private readonly record struct EventInitializer(string Expression, string? LambdaBody, string OptionsArgument);

    private enum EventPropertyKind
    {
        Sync,
        Async,
        AsyncResult
    }
}
