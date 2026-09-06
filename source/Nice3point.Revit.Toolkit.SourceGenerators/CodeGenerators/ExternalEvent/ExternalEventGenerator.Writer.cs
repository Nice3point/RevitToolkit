using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

partial class ExternalEventGenerator
{
    internal static class Writer
    {
        private static readonly string AssemblyVersion = typeof(ExternalEventGenerator).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>
        ///     Generates the complete source code for an external event method,
        ///     including properties, records, and nested handler classes where needed.
        /// </summary>
        public static string GenerateSource(ExternalEventInfo info, bool useFieldKeyword)
        {
            var writer = new CodeWriter();

            var namespaceBlock = WriteNamespaceBlock(writer, info);
            var typeBlocks = WriteTypeHierarchy(writer, info);

            WriteProperties(writer, info, useFieldKeyword);
            WriteArgumentsRecord(writer, info);
            CloseBlocks(typeBlocks);
            WriteExtensionClass(writer, info);

            namespaceBlock?.Dispose();
            return writer.ToString();
        }

        /// <summary>
        ///     Opens the namespace block if the type has a containing namespace.
        /// </summary>
        private static IDisposable? WriteNamespaceBlock(CodeWriter writer, ExternalEventInfo info)
        {
            return !string.IsNullOrEmpty(info.Namespace)
                ? writer.BeginBlock($"namespace {info.Namespace}")
                : null;
        }

        /// <summary>
        ///     Writes the type hierarchy (nested partial types) and returns the disposable blocks
        ///     to be closed in reverse order after content generation.
        /// </summary>
        private static List<IDisposable> WriteTypeHierarchy(CodeWriter writer, ExternalEventInfo info)
        {
            var typeBlocks = new List<IDisposable>();
            foreach (var typeDeclaration in info.TypeHierarchy)
            {
                var staticModifier = typeDeclaration.IsStatic ? "static " : string.Empty;
                var block = writer.BeginBlock($"{typeDeclaration.Accessibility} {staticModifier}partial {typeDeclaration.Keyword} {typeDeclaration.Name}");
                typeBlocks.Add(block);
            }

            return typeBlocks;
        }

        /// <summary>
        ///     Writes event properties (sync and/or async) based on method return type and parameters.
        /// </summary>
        private static void WriteProperties(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            if (info.ReturnsVoid)
            {
                WriteEventProperty(writer, info, useFieldKeyword, EventPropertyKind.Sync);
                writer.AppendLine();
                WriteEventProperty(writer, info, useFieldKeyword, EventPropertyKind.Async);
            }
            else
            {
                WriteEventProperty(writer, info, useFieldKeyword, EventPropertyKind.AsyncResult);
            }
        }

        /// <summary>
        ///     Writes a sealed record for methods with 2+ extra parameters, bundling them into a single argument type.
        /// </summary>
        private static void WriteArgumentsRecord(CodeWriter writer, ExternalEventInfo info)
        {
            if (info.ExtraParameters.Length < 2)
            {
                return;
            }

            writer.AppendLine();

            var recordName = $"{info.MethodName}Args";
            var recordParameters = new List<string>();
            foreach (var parameter in info.ExtraParameters)
            {
                var pascalName = ToPascalCase(parameter.Name);
                recordParameters.Add($"{parameter.FullyQualifiedType} {pascalName}");
            }

            WriteExcludeFromCodeCoverageAttributes(writer);
            WriteGeneratedCodeAttributes(writer);
            writer.AppendLine($"public sealed record {recordName}({string.Join(", ", recordParameters)});");
        }

        /// <summary>
        ///     Writes a static partial extension class with convenience <c>Raise</c> / <c>RaiseAsync</c>
        ///     overloads that accept individual parameters instead of the generated record.
        /// </summary>
        private static void WriteExtensionClass(CodeWriter writer, ExternalEventInfo info)
        {
            if (info.ExtraParameters.Length < 2)
            {
                return;
            }

            var outermostTypeName = info.TypeHierarchy[0].Name;
            var recordName = $"{info.MethodName}Args";
            var qualifiedRecordType = BuildQualifiedRecordType(info, recordName);

            writer.AppendLine();

            var extensionAccessibility = GetMostRestrictiveAccessibility(info.TypeHierarchy);
            using (writer.BeginBlock($"{extensionAccessibility} static partial class {outermostTypeName}Extensions"))
            {
                if (info.ReturnsVoid)
                {
                    WriteRaiseExtensionMethod(writer, info, qualifiedRecordType, false);
                    writer.AppendLine();
                    WriteRaiseExtensionMethod(writer, info, qualifiedRecordType, true);
                }
                else
                {
                    WriteRaiseExtensionMethod(writer, info, qualifiedRecordType, true);
                }
            }
        }

        /// <summary>
        ///     Writes a single event property (sync, async, or async-with-result) based on the specified kind.
        /// </summary>
        private static void WriteEventProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword, EventPropertyKind kind)
        {
            var staticModifier = info.IsStatic ? "static " : string.Empty;
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : string.Empty;

            var (eventTypeName, interfaceTypeName, propertySuffix) = GetEventTypeNames(kind);
            var typeArguments = BuildTypeArguments(info, kind);

            var eventType = FormatGenericType(eventTypeName.WithGlobalPrefix, typeArguments);
            var interfaceType = FormatGenericType(interfaceTypeName.WithGlobalPrefix, typeArguments);
            var initializer = BuildPropertyInitializer(info, eventType, optionsArgument);

            WritePropertyWithBackingField(writer, info, useFieldKeyword, staticModifier, interfaceType, propertySuffix, initializer);
        }

        /// <summary>
        ///     Writes a property with optional backing field, including generated code attributes.
        /// </summary>
        private static void WritePropertyWithBackingField(
            CodeWriter writer,
            ExternalEventInfo info,
            bool useFieldKeyword,
            string staticModifier,
            string propertyType,
            string propertySuffix,
            string initializer)
        {
            if (useFieldKeyword)
            {
                WriteExcludeFromCodeCoverageAttributes(writer);
                WriteGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{propertyType} {info.MethodName}{propertySuffix} => field ??= {initializer};");
            }
            else
            {
                var backingFieldName = BuildBackingFieldName(info.MethodName, propertySuffix);
                writer.AppendLine($"private {staticModifier}{propertyType}? {backingFieldName};");
                writer.AppendLine();

                WriteExcludeFromCodeCoverageAttributes(writer);
                WriteGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{propertyType} {info.MethodName}{propertySuffix} => {backingFieldName} ??= {initializer};");
            }
        }

        /// <summary>
        ///     Writes a <c>Raise</c> or <c>RaiseAsync</c> extension method for the generated record type.
        /// </summary>
        private static void WriteRaiseExtensionMethod(CodeWriter writer, ExternalEventInfo info, string qualifiedRecordType, bool isAsync)
        {
            string interfaceType;
            string returnType;
            string methodName;

            if (!isAsync)
            {
                interfaceType = $"{WellKnownFullyQualifiedClassNames.ExternalEventInterface.WithGlobalPrefix}<{qualifiedRecordType}>";
                returnType = WellKnownFullyQualifiedClassNames.ExternalEventRequest.WithGlobalPrefix;
                methodName = "Raise";
            }
            else if (info.ReturnsVoid)
            {
                interfaceType = $"{WellKnownFullyQualifiedClassNames.AsyncExternalEventInterface.WithGlobalPrefix}<{qualifiedRecordType}>";
                returnType = WellKnownFullyQualifiedClassNames.Task.WithGlobalPrefix;
                methodName = "RaiseAsync";
            }
            else
            {
                var resultType = info.FullyQualifiedReturnType!;
                interfaceType = $"{WellKnownFullyQualifiedClassNames.AsyncRequestExternalEventInterface.WithGlobalPrefix}<{qualifiedRecordType}, {resultType}>";
                returnType = $"{WellKnownFullyQualifiedClassNames.Task.WithGlobalPrefix}<{resultType}>";
                methodName = "RaiseAsync";
            }

            var parameters = BuildExtensionMethodParameters(info);
            var recordArguments = BuildRecordConstructorArguments(info);

            WriteExcludeFromCodeCoverageAttributes(writer);
            WriteGeneratedCodeAttributes(writer);
            using (writer.BeginBlock($"public static {returnType} {methodName}(this {interfaceType} externalEvent, {parameters})"))
            {
                writer.AppendLine($"return externalEvent.{methodName}(new {qualifiedRecordType}({recordArguments}));");
            }
        }

        /// <summary>
        ///     Writes <c>[ExcludeFromCodeCoverage]</c> attribute for generated members.
        /// </summary>
        private static void WriteExcludeFromCodeCoverageAttributes(CodeWriter writer)
        {
            writer.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        }

        /// <summary>
        ///     Writes <c>[GeneratedCode]</c> attribute for generated members.
        /// </summary>
        private static void WriteGeneratedCodeAttributes(CodeWriter writer)
        {
            writer.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{AssemblyVersion}\")]");
        }

        /// <summary>
        ///     Builds the generic type argument string based on parameter count, return type, and event kind.
        /// </summary>
        private static string? BuildTypeArguments(ExternalEventInfo info, EventPropertyKind kind)
        {
            var argType = GetParameterTypeArgument(info);
            var returnType = kind == EventPropertyKind.AsyncResult ? info.FullyQualifiedReturnType : null;

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


        /// <summary>
        ///     Builds the lambda expression that unpacks a record into method arguments.
        /// </summary>
        private static string BuildRecordLambda(ExternalEventInfo info, string argsParamName)
        {
            var arguments = new List<string>();
            if (info.HasUiApplicationParameter)
            {
                arguments.Add("application");
            }

            foreach (var parameter in info.ExtraParameters)
            {
                var pascalName = ToPascalCase(parameter.Name);
                arguments.Add($"{argsParamName}.{pascalName}");
            }

            return $"{info.MethodName}({string.Join(", ", arguments)})";
        }

        /// <summary>
        ///     Builds the backing field name for a generated property using camelCase convention.
        /// </summary>
        private static string BuildBackingFieldName(string methodName, string suffix)
        {
            return $"_{char.ToLowerInvariant(methodName[0])}{methodName[1..]}{suffix}";
        }

        /// <summary>
        ///     Builds the property initializer expression, choosing between direct method reference and lambda for record types.
        /// </summary>
        private static string BuildPropertyInitializer(ExternalEventInfo info, string eventType, string optionsArgument)
        {
            if (info.ExtraParameters.Length < 2)
            {
                return $"new {eventType}(new {info.FullyQualifiedDelegateType}({info.MethodName}){optionsArgument})";
            }

            var lambda = BuildRecordLambda(info, "args");
            var lambdaParams = info.HasUiApplicationParameter ? "application, args" : "args";
            return $"new {eventType}(({lambdaParams}) => {lambda}{optionsArgument})";
        }

        /// <summary>
        ///     Builds the fully qualified record type path through the type hierarchy.
        /// </summary>
        private static string BuildQualifiedRecordType(ExternalEventInfo info, string recordName)
        {
            var parts = new List<string>();
            foreach (var typeDeclaration in info.TypeHierarchy)
            {
                parts.Add(typeDeclaration.Name);
            }

            parts.Add(recordName);
            return string.Join(".", parts);
        }

        /// <summary>
        ///     Builds the parameter list for extension method signatures using original parameter names.
        /// </summary>
        private static string BuildExtensionMethodParameters(ExternalEventInfo info)
        {
            var parameters = new List<string>();
            foreach (var parameter in info.ExtraParameters)
            {
                parameters.Add($"{parameter.FullyQualifiedType} {parameter.Name}");
            }

            return string.Join(", ", parameters);
        }

        /// <summary>
        ///     Builds the argument list for the record constructor call inside the extension method body.
        /// </summary>
        private static string BuildRecordConstructorArguments(ExternalEventInfo info)
        {
            var arguments = new List<string>();
            foreach (var parameter in info.ExtraParameters)
            {
                arguments.Add(parameter.Name);
            }

            return string.Join(", ", arguments);
        }

        /// <summary>
        ///     Determines the parameter type argument for generic event types.
        ///     Returns <c>null</c> for 0 params, the parameter type for 1 param, or the record name for 2+ params.
        /// </summary>
        private static string? GetParameterTypeArgument(ExternalEventInfo info)
        {
            return info.ExtraParameters.Length switch
            {
                0 => null,
                1 => info.ExtraParameters[0].FullyQualifiedType,
                _ => $"{info.MethodName}Args"
            };
        }

        /// <summary>
        ///     Returns the event type name, interface type name, and property suffix for the given event property kind.
        /// </summary>
        private static (FullyQualifiedTypeName EventType, FullyQualifiedTypeName InterfaceType, string PropertySuffix) GetEventTypeNames(EventPropertyKind kind)
        {
            return kind switch
            {
                EventPropertyKind.Sync => (
                    WellKnownFullyQualifiedClassNames.ExternalEvent,
                    WellKnownFullyQualifiedClassNames.ExternalEventInterface,
                    "Event"),
                EventPropertyKind.Async => (
                    WellKnownFullyQualifiedClassNames.AsyncExternalEvent,
                    WellKnownFullyQualifiedClassNames.AsyncExternalEventInterface,
                    "AsyncEvent"),
                EventPropertyKind.AsyncResult => (
                    WellKnownFullyQualifiedClassNames.AsyncRequestExternalEvent,
                    WellKnownFullyQualifiedClassNames.AsyncRequestExternalEventInterface,
                    "AsyncEvent"),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        /// <summary>
        ///     Returns the most restrictive accessibility from the type hierarchy,
        ///     clamped to "public" or "internal" (extension classes can only be top-level static).
        /// </summary>
        private static string GetMostRestrictiveAccessibility(EquatableArray<TypeDeclarationInfo> typeHierarchy)
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

        /// <summary>
        ///     Closes type hierarchy blocks in reverse order.
        /// </summary>
        private static void CloseBlocks(List<IDisposable> typeBlocks)
        {
            for (var typeIndex = typeBlocks.Count - 1; typeIndex >= 0; typeIndex--)
            {
                typeBlocks[typeIndex].Dispose();
            }
        }

        /// <summary>
        ///     Converts a camelCase parameter name to PascalCase for record properties.
        /// </summary>
        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        /// <summary>
        ///     Formats a type name with optional generic type arguments.
        /// </summary>
        private static string FormatGenericType(string typeName, string? typeArguments)
        {
            return typeArguments is not null ? $"{typeName}<{typeArguments}>" : typeName;
        }

        /// <summary>
        ///     Defines the kind of event property to generate.
        /// </summary>
        private enum EventPropertyKind
        {
            Sync,
            Async,
            AsyncResult
        }
    }
}
