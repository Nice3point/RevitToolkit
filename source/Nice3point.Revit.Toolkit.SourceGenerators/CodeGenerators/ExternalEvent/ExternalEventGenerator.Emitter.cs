using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

partial class ExternalEventGenerator
{
    internal static class Emitter
    {
        private static readonly string AssemblyVersion = typeof(ExternalEventGenerator).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>
        ///     Generates the complete source code for an external event method,
        ///     including properties, records, and nested handler classes where needed.
        /// </summary>
        public static string Generate(ExternalEventInfo info, bool useFieldKeyword)
        {
            var writer = new CodeWriter();

            var namespaceBlock = EmitNamespaceBlock(writer, info);
            var typeBlocks = EmitTypeHierarchy(writer, info);

            EmitArgsRecord(writer, info);
            EmitProperties(writer, info, useFieldKeyword);

            CloseBlocks(typeBlocks);
            namespaceBlock?.Dispose();

            return writer.ToString();
        }

        /// <summary>
        ///     Opens the namespace block if the type has a containing namespace.
        /// </summary>
        private static IDisposable? EmitNamespaceBlock(CodeWriter writer, ExternalEventInfo info)
        {
            return !string.IsNullOrEmpty(info.Namespace)
                ? writer.BeginBlock($"namespace {info.Namespace}")
                : null;
        }

        /// <summary>
        ///     Emits the type hierarchy (nested partial types) and returns the disposable blocks
        ///     to be closed in reverse order after content generation.
        /// </summary>
        private static List<IDisposable> EmitTypeHierarchy(CodeWriter writer, ExternalEventInfo info)
        {
            var typeBlocks = new List<IDisposable>();
            for (var typeIndex = 0; typeIndex < info.TypeHierarchy.Length; typeIndex++)
            {
                var typeDeclaration = info.TypeHierarchy[typeIndex];
                var staticModifier = typeDeclaration.IsStatic ? "static " : "";
                writer.AppendLine("/// <inheritdoc/>");
                var block = writer.BeginBlock($"{staticModifier}partial {typeDeclaration.Keyword} {typeDeclaration.Name}");
                typeBlocks.Add(block);
            }

            return typeBlocks;
        }

        /// <summary>
        ///     Emits a sealed record for methods with 2+ extra parameters,
        ///     bundling them into a single argument type.
        /// </summary>
        private static void EmitArgsRecord(CodeWriter writer, ExternalEventInfo info)
        {
            if (info.ExtraParameters.Length < 2)
            {
                return;
            }

            var recordName = $"{info.MethodName}Args";
            var recordParameters = new List<string>();
            for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
            {
                var parameter = info.ExtraParameters[paramIndex];
                var pascalName = ToPascalCase(parameter.Name);
                recordParameters.Add($"{parameter.FullyQualifiedType} {pascalName}");
            }

            EmitGeneratedCodeAttributes(writer);
            writer.AppendLine($"public sealed record {recordName}({string.Join(", ", recordParameters)});");
            writer.AppendLine();
        }

        /// <summary>
        ///     Emits event properties (sync and/or async) based on method return type and parameters.
        /// </summary>
        private static void EmitProperties(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            if (info.IsVoidReturn)
            {
                EmitSyncProperty(writer, info, useFieldKeyword);

                if (info.ExtraParameters.Length == 0)
                {
                    writer.AppendLine();
                    EmitAsyncVoidProperty(writer, info, useFieldKeyword);
                }
            }
            else
            {
                EmitAsyncResultProperty(writer, info, useFieldKeyword);
            }
        }

        /// <summary>
        ///     Emits a synchronous property using <c>ExternalEvent</c> or <c>ExternalEvent&lt;T&gt;</c>.
        /// </summary>
        private static void EmitSyncProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";

            if (info.ExtraParameters.Length == 0)
            {
                // ExternalEvent (no args)
                var eventType = WellKnownFullyQualifiedClassNames.ExternalEvent.WithGlobalPrefix;
                EmitPropertyWithBackingField(writer, info, useFieldKeyword, staticModifier,
                    eventType, "Event",
                    $"new {eventType}({info.MethodName}{optionsArgument})");
            }
            else if (info.ExtraParameters.Length == 1)
            {
                // ExternalEvent<T> (single arg)
                var argType = info.ExtraParameters[0].FullyQualifiedType;
                var eventType = $"{WellKnownFullyQualifiedClassNames.ExternalEvent.WithGlobalPrefix}<{argType}>";
                var interfaceType = $"{WellKnownFullyQualifiedClassNames.ExternalEventInterface.WithGlobalPrefix}<{argType}>";
                EmitPropertyWithBackingField(writer, info, useFieldKeyword, staticModifier,
                    interfaceType, "Event",
                    $"new {eventType}({info.MethodName}{optionsArgument})");
            }
            else
            {
                // ExternalEvent<RecordType> (multi args, with lambda)
                var recordName = $"{info.MethodName}Args";
                var eventType = $"{WellKnownFullyQualifiedClassNames.ExternalEvent.WithGlobalPrefix}<{recordName}>";
                var interfaceType = $"{WellKnownFullyQualifiedClassNames.ExternalEventInterface.WithGlobalPrefix}<{recordName}>";
                var lambda = BuildRecordLambda(info, "args");
                EmitPropertyWithBackingField(writer, info, useFieldKeyword, staticModifier,
                    interfaceType, "Event",
                    $"new {eventType}(({BuildLambdaParams(info)}) => {lambda}{optionsArgument})");
            }
        }

        /// <summary>
        ///     Emits an asynchronous property for void methods without extra parameters,
        ///     using the built-in <c>AsyncExternalEvent</c> type.
        /// </summary>
        private static void EmitAsyncVoidProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";

            var eventType = WellKnownFullyQualifiedClassNames.AsyncExternalEvent.WithGlobalPrefix;
            EmitPropertyWithBackingField(writer, info, useFieldKeyword, staticModifier,
                eventType, "AsyncEvent",
                $"new {eventType}({info.MethodName}{optionsArgument})");
        }

        /// <summary>
        ///     Emits an asynchronous property for methods that return a value.
        ///     For 0 extra params, uses <c>AsyncExternalEvent&lt;TResult&gt;</c>.
        ///     For 1 extra param, uses <c>AsyncExternalEvent&lt;T, TResult&gt;</c>.
        ///     For 2+ extra params, uses <c>AsyncExternalEvent&lt;RecordType, TResult&gt;</c> with lambda.
        /// </summary>
        private static void EmitAsyncResultProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var returnType = info.ReturnTypeFullyQualified!;
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";

            if (info.ExtraParameters.Length == 0)
            {
                // AsyncExternalEvent<TResult> (no args)
                var eventType = $"{WellKnownFullyQualifiedClassNames.AsyncExternalEvent.WithGlobalPrefix}<{returnType}>";
                EmitPropertyWithBackingField(writer, info, useFieldKeyword, staticModifier,
                    eventType, "AsyncEvent",
                    $"new {eventType}({info.MethodName}{optionsArgument})");
            }
            else if (info.ExtraParameters.Length == 1)
            {
                // AsyncExternalEvent<T, TResult> (single arg)
                var argType = info.ExtraParameters[0].FullyQualifiedType;
                var eventType = $"{WellKnownFullyQualifiedClassNames.AsyncExternalEvent.WithGlobalPrefix}<{argType}, {returnType}>";
                var interfaceType = $"{WellKnownFullyQualifiedClassNames.AsyncExternalEventGenericInterface.WithGlobalPrefix}<{argType}, {returnType}>";
                EmitPropertyWithBackingField(writer, info, useFieldKeyword, staticModifier,
                    interfaceType, "AsyncEvent",
                    $"new {eventType}({info.MethodName}{optionsArgument})");
            }
            else
            {
                // AsyncExternalEvent<RecordType, TResult> (multi args, with lambda)
                var recordName = $"{info.MethodName}Args";
                var eventType = $"{WellKnownFullyQualifiedClassNames.AsyncExternalEvent.WithGlobalPrefix}<{recordName}, {returnType}>";
                var interfaceType = $"{WellKnownFullyQualifiedClassNames.AsyncExternalEventGenericInterface.WithGlobalPrefix}<{recordName}, {returnType}>";
                var lambda = BuildRecordLambda(info, "args");
                EmitPropertyWithBackingField(writer, info, useFieldKeyword, staticModifier,
                    interfaceType, "AsyncEvent",
                    $"new {eventType}(({BuildLambdaParams(info)}) => {lambda}{optionsArgument})");
            }
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
        ///     Emits a property with optional backing field, including generated code attributes.
        /// </summary>
        private static void EmitPropertyWithBackingField(
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
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{propertyType} {info.MethodName}{propertySuffix} => field ??= {initializer};");
            }
            else
            {
                var backingFieldName = BuildBackingFieldName(info.MethodName, propertySuffix);
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"private {staticModifier}{propertyType}? {backingFieldName};");
                writer.AppendLine();
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{propertyType} {info.MethodName}{propertySuffix} => {backingFieldName} ??= {initializer};");
            }
        }

        /// <summary>
        ///     Emits <c>[GeneratedCode]</c> and <c>[ExcludeFromCodeCoverage]</c> attributes
        ///     for generated members.
        /// </summary>
        private static void EmitGeneratedCodeAttributes(CodeWriter writer)
        {
            writer.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            writer.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{AssemblyVersion}\")]");
        }

        /// <summary>
        ///     Builds the lambda expression that unpacks a record into method arguments.
        /// </summary>
        private static string BuildRecordLambda(ExternalEventInfo info, string argsParamName)
        {
            var arguments = new List<string>();
            if (info.HasUiApplicationParam)
            {
                arguments.Add("application");
            }

            for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
            {
                var pascalName = ToPascalCase(info.ExtraParameters[paramIndex].Name);
                arguments.Add($"{argsParamName}.{pascalName}");
            }

            return $"{info.MethodName}({string.Join(", ", arguments)})";
        }

        /// <summary>
        ///     Builds the lambda parameter list for record-based event constructors.
        /// </summary>
        private static string BuildLambdaParams(ExternalEventInfo info)
        {
            if (info.HasUiApplicationParam)
            {
                return "application, args";
            }

            return "args";
        }

        /// <summary>
        ///     Builds the backing field name for a generated property using camelCase convention.
        /// </summary>
        private static string BuildBackingFieldName(string methodName, string suffix)
        {
            return $"_{char.ToLowerInvariant(methodName[0])}{methodName.Substring(1)}{suffix}";
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

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
    }
}
