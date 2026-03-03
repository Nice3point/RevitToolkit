using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

partial class ExternalEventGenerator
{
    internal static class Emitter
    {
        private static readonly string AssemblyVersion = typeof(ExternalEventGenerator).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>
        ///     Generates the complete source code for an external event method,
        ///     including properties and nested handler classes.
        /// </summary>
        public static string Generate(ExternalEventInfo info, bool useFieldKeyword)
        {
            var writer = new CodeWriter();

            var namespaceBlock = EmitNamespaceBlock(writer, info);
            var typeBlocks = EmitTypeHierarchy(writer, info);

            EmitProperties(writer, info, useFieldKeyword);
            EmitNestedClasses(writer, info);

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
        ///     Emits event properties (sync and/or async) based on method return type and parameters.
        /// </summary>
        private static void EmitProperties(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var hasExtraParameters = info.ExtraParameters.Length > 0;

            if (info.IsVoidReturn)
            {
                if (hasExtraParameters)
                {
                    EmitCustomSyncProperty(writer, info, useFieldKeyword);
                }
                else
                {
                    EmitSimpleSyncProperty(writer, info, useFieldKeyword);
                }

                writer.AppendLine();

                if (hasExtraParameters)
                {
                    EmitCustomAsyncProperty(writer, info, useFieldKeyword);
                }
                else
                {
                    EmitSimpleAsyncProperty(writer, info, useFieldKeyword);
                }
            }
            else
            {
                if (hasExtraParameters)
                {
                    EmitCustomAsyncProperty(writer, info, useFieldKeyword);
                }
                else
                {
                    EmitSimpleAsyncGenericProperty(writer, info, useFieldKeyword);
                }
            }
        }

        /// <summary>
        ///     Emits nested event handler classes for methods with extra parameters.
        /// </summary>
        private static void EmitNestedClasses(CodeWriter writer, ExternalEventInfo info)
        {
            if (info.ExtraParameters.Length == 0)
            {
                return;
            }

            writer.AppendLine();
            if (info.IsVoidReturn)
            {
                EmitCustomSyncClass(writer, info);
                writer.AppendLine();
                EmitCustomAsyncVoidClass(writer, info);
            }
            else
            {
                EmitCustomAsyncGenericClass(writer, info);
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
        ///     Emits a synchronous property using built-in <see cref="WellKnownFullyQualifiedClassNames.ExternalEvent"/> type
        ///     for methods without extra parameters.
        /// </summary>
        private static void EmitSimpleSyncProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";
            var externalEvent = WellKnownFullyQualifiedClassNames.ExternalEvent.WithGlobalPrefix;

            if (useFieldKeyword)
            {
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{externalEvent} {info.MethodName}Event => field ??= new {externalEvent}({info.MethodName}{optionsArgument});");
            }
            else
            {
                var backingFieldName = BuildBackingFieldName(info.MethodName, "Event");
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"private {staticModifier}{externalEvent}? {backingFieldName};");
                writer.AppendLine();
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{externalEvent} {info.MethodName}Event => {backingFieldName} ??= new {externalEvent}({info.MethodName}{optionsArgument});");
            }
        }

        /// <summary>
        ///     Emits an asynchronous property using built-in <see cref="WellKnownFullyQualifiedClassNames.AsyncExternalEvent"/> type
        ///     for void methods without extra parameters.
        /// </summary>
        private static void EmitSimpleAsyncProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";
            var asyncExternalEvent = WellKnownFullyQualifiedClassNames.AsyncExternalEvent.WithGlobalPrefix;

            if (useFieldKeyword)
            {
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{asyncExternalEvent} {info.MethodName}AsyncEvent => field ??= new {asyncExternalEvent}({info.MethodName}{optionsArgument});");
            }
            else
            {
                var backingFieldName = BuildBackingFieldName(info.MethodName, "AsyncEvent");
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"private {staticModifier}{asyncExternalEvent}? {backingFieldName};");
                writer.AppendLine();
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{asyncExternalEvent} {info.MethodName}AsyncEvent => {backingFieldName} ??= new {asyncExternalEvent}({info.MethodName}{optionsArgument});");
            }
        }

        /// <summary>
        ///     Emits an asynchronous generic property using <see cref="WellKnownFullyQualifiedClassNames.AsyncExternalEvent"/>
        ///     with a type parameter for methods that return a value, without extra parameters.
        /// </summary>
        private static void EmitSimpleAsyncGenericProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";
            var genericTypeName = $"{WellKnownFullyQualifiedClassNames.AsyncExternalEvent.WithGlobalPrefix}<{info.ReturnTypeFullyQualified}>";

            if (useFieldKeyword)
            {
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{genericTypeName} {info.MethodName}AsyncEvent => field ??= new {genericTypeName}({info.MethodName}{optionsArgument});");
            }
            else
            {
                var backingFieldName = BuildBackingFieldName(info.MethodName, "AsyncEvent");
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"private {staticModifier}{genericTypeName}? {backingFieldName};");
                writer.AppendLine();
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{genericTypeName} {info.MethodName}AsyncEvent => {backingFieldName} ??= new {genericTypeName}({info.MethodName}{optionsArgument});");
            }
        }

        /// <summary>
        ///     Emits a synchronous property using a generated nested class
        ///     for methods with extra parameters.
        /// </summary>
        private static void EmitCustomSyncProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var className = $"{info.MethodName}ExternalEvent";
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";

            if (useFieldKeyword)
            {
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{className} {info.MethodName}Event => field ??= new {className}({info.MethodName}{optionsArgument});");
            }
            else
            {
                var backingFieldName = BuildBackingFieldName(info.MethodName, "Event");
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"private {staticModifier}{className}? {backingFieldName};");
                writer.AppendLine();
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{className} {info.MethodName}Event => {backingFieldName} ??= new {className}({info.MethodName}{optionsArgument});");
            }
        }

        /// <summary>
        ///     Emits an asynchronous property using a generated nested class
        ///     for methods with extra parameters.
        /// </summary>
        private static void EmitCustomAsyncProperty(CodeWriter writer, ExternalEventInfo info, bool useFieldKeyword)
        {
            var staticModifier = info.IsStatic ? "static " : "";
            var className = $"{info.MethodName}AsyncExternalEvent";
            var optionsArgument = info.AllowDirectInvocation
                ? $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation"
                : "";

            if (useFieldKeyword)
            {
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{className} {info.MethodName}AsyncEvent => field ??= new {className}({info.MethodName}{optionsArgument});");
            }
            else
            {
                var backingFieldName = BuildBackingFieldName(info.MethodName, "AsyncEvent");
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"private {staticModifier}{className}? {backingFieldName};");
                writer.AppendLine();
                EmitGeneratedCodeAttributes(writer);
                writer.AppendLine($"public {staticModifier}{className} {info.MethodName}AsyncEvent => {backingFieldName} ??= new {className}({info.MethodName}{optionsArgument});");
            }
        }

        /// <summary>
        ///     Builds the fully qualified delegate type (Action or Func) for the method signature,
        ///     including UIApplication parameter and extra parameters.
        /// </summary>
        private static string BuildDelegateType(ExternalEventInfo info)
        {
            var parameterTypes = new List<string>();
            if (info.HasUiApplicationParam)
            {
                parameterTypes.Add(WellKnownFullyQualifiedClassNames.UiApplication.WithGlobalPrefix);
            }

            foreach (var parameter in info.ExtraParameters)
            {
                parameterTypes.Add(parameter.FullyQualifiedType);
            }

            if (info.IsVoidReturn)
            {
                return parameterTypes.Count == 0
                    ? WellKnownFullyQualifiedClassNames.Action.WithGlobalPrefix
                    : $"{WellKnownFullyQualifiedClassNames.Action.WithGlobalPrefix}<{string.Join(", ", parameterTypes)}>";
            }

            parameterTypes.Add(info.ReturnTypeFullyQualified!);
            return $"{WellKnownFullyQualifiedClassNames.Func.WithGlobalPrefix}<{string.Join(", ", parameterTypes)}>";
        }

        /// <summary>
        ///     Emits <c>[GeneratedCode]</c> and <c>[ExcludeFromCodeCoverage]</c> attributes
        ///     for generated members.
        /// </summary>
        private static void EmitGeneratedCodeAttributes(CodeWriter writer)
        {
            writer.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{AssemblyVersion}\")]");
            writer.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        }

        /// <summary>
        ///     Emits a synchronous nested handler class for void methods with extra parameters.
        /// </summary>
        private static void EmitCustomSyncClass(CodeWriter writer, ExternalEventInfo info)
        {
            var className = $"{info.MethodName}ExternalEvent";
            var delegateType = BuildDelegateType(info);
            var hasOptions = info.AllowDirectInvocation;

            var constructorParameters = $"{delegateType} handler";
            if (hasOptions)
            {
                constructorParameters += $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions} options";
            }

            EmitGeneratedCodeAttributes(writer);
            using (writer.BeginBlock($"public sealed class {className}({constructorParameters}) : {WellKnownFullyQualifiedClassNames.ExternalEventHandler}, {WellKnownFullyQualifiedClassNames.ExternalEventInterface}"))
            {
                for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
                {
                    writer.AppendLine($"private {info.ExtraParameters[paramIndex].FullyQualifiedType} _arg{paramIndex + 1};");
                }

                writer.AppendLine();

                using (writer.BeginBlock($"public override void Execute({WellKnownFullyQualifiedClassNames.UiApplication} uiApplication)"))
                {
                    var invokeArguments = BuildInvokeArguments(info, "uiApplication");
                    writer.AppendLine($"handler.Invoke({invokeArguments});");
                }

                writer.AppendLine();

                var raiseParameters = BuildRaiseParameters(info);
                using (writer.BeginBlock($"public void Raise({raiseParameters})"))
                {
                    EmitArgumentAssignments(writer, info);

                    if (hasOptions)
                    {
                        writer.AppendLine();
                        using (writer.BeginBlock($"if ((options & {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation) != 0 && {WellKnownFullyQualifiedClassNames.RevitContext}.IsRevitInApiMode)"))
                        {
                            writer.AppendLine($"Execute({WellKnownFullyQualifiedClassNames.RevitContext}.UiApplication);");
                            writer.AppendLine("return;");
                        }
                    }

                    writer.AppendLine();
                    writer.AppendLine("base.Raise();");
                }
            }
        }

        /// <summary>
        ///     Emits an asynchronous nested handler class for void methods with extra parameters.
        /// </summary>
        private static void EmitCustomAsyncVoidClass(CodeWriter writer, ExternalEventInfo info)
        {
            var className = $"{info.MethodName}AsyncExternalEvent";
            var delegateType = BuildDelegateType(info);
            var hasOptions = info.AllowDirectInvocation;

            var constructorParameters = $"{delegateType} handler";
            if (hasOptions)
            {
                constructorParameters += $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions} options";
            }

            EmitGeneratedCodeAttributes(writer);
            using (writer.BeginBlock($"public sealed class {className}({constructorParameters}) : {WellKnownFullyQualifiedClassNames.ExternalEventHandler}, {WellKnownFullyQualifiedClassNames.AsyncExternalEventInterface}"))
            {
                for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
                {
                    writer.AppendLine($"private {info.ExtraParameters[paramIndex].FullyQualifiedType} _arg{paramIndex + 1};");
                }

                writer.AppendLine($"private {WellKnownFullyQualifiedClassNames.TaskCompletionSource}? _taskCompletionSource;");
                writer.AppendLine();

                using (writer.BeginBlock($"public override void Execute({WellKnownFullyQualifiedClassNames.UiApplication} uiApplication)"))
                {
                    using (writer.BeginBlock("try"))
                    {
                        var invokeArguments = BuildInvokeArguments(info, "uiApplication");
                        writer.AppendLine($"handler.Invoke({invokeArguments});");
                        writer.AppendLine("_taskCompletionSource?.SetResult();");
                    }

                    using (writer.BeginBlock($"catch ({WellKnownFullyQualifiedClassNames.Exception} exception)"))
                    {
                        writer.AppendLine("_taskCompletionSource?.SetException(exception);");
                    }
                }

                writer.AppendLine();

                var raiseParameters = BuildRaiseParameters(info);
                using (writer.BeginBlock($"public {WellKnownFullyQualifiedClassNames.Task} RaiseAsync({raiseParameters})"))
                {
                    EmitArgumentAssignments(writer, info);

                    if (hasOptions)
                    {
                        writer.AppendLine();
                        using (writer.BeginBlock($"if ((options & {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation) != 0 && {WellKnownFullyQualifiedClassNames.RevitContext}.IsRevitInApiMode)"))
                        {
                            using (writer.BeginBlock("try"))
                            {
                                var directInvokeArguments = BuildInvokeArguments(info, $"{WellKnownFullyQualifiedClassNames.RevitContext}.UiApplication");
                                writer.AppendLine($"handler.Invoke({directInvokeArguments});");
                                writer.AppendLine($"return {WellKnownFullyQualifiedClassNames.Task}.CompletedTask;");
                            }

                            using (writer.BeginBlock($"catch ({WellKnownFullyQualifiedClassNames.Exception} exception)"))
                            {
                                writer.AppendLine($"return {WellKnownFullyQualifiedClassNames.Task}.FromException(exception);");
                            }
                        }
                    }

                    writer.AppendLine();
                    writer.AppendLine($"_taskCompletionSource = new {WellKnownFullyQualifiedClassNames.TaskCompletionSource}({WellKnownFullyQualifiedClassNames.TaskCreationOptions}.RunContinuationsAsynchronously);");
                    writer.AppendLine("base.Raise();");
                    writer.AppendLine("return _taskCompletionSource.Task;");
                }
            }
        }

        /// <summary>
        ///     Emits an asynchronous nested handler class for methods that return a value, with extra parameters.
        /// </summary>
        private static void EmitCustomAsyncGenericClass(CodeWriter writer, ExternalEventInfo info)
        {
            var className = $"{info.MethodName}AsyncExternalEvent";
            var delegateType = BuildDelegateType(info);
            var returnType = info.ReturnTypeFullyQualified!;
            var hasOptions = info.AllowDirectInvocation;
            var taskOfReturnType = $"{WellKnownFullyQualifiedClassNames.Task}<{returnType}>";
            var taskCompletionSourceOfReturnType = $"{WellKnownFullyQualifiedClassNames.TaskCompletionSource}<{returnType}>";

            var constructorParameters = $"{delegateType} handler";
            if (hasOptions)
            {
                constructorParameters += $", {WellKnownFullyQualifiedClassNames.ExternalEventOptions} options";
            }

            EmitGeneratedCodeAttributes(writer);
            using (writer.BeginBlock($"public sealed class {className}({constructorParameters}) : {WellKnownFullyQualifiedClassNames.ExternalEventHandler}, {WellKnownFullyQualifiedClassNames.AsyncExternalEventInterface}<{returnType}>"))
            {
                for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
                {
                    writer.AppendLine($"private {info.ExtraParameters[paramIndex].FullyQualifiedType} _arg{paramIndex + 1};");
                }

                writer.AppendLine($"private {taskCompletionSourceOfReturnType}? _taskCompletionSource;");
                writer.AppendLine();

                using (writer.BeginBlock($"public override void Execute({WellKnownFullyQualifiedClassNames.UiApplication} uiApplication)"))
                {
                    using (writer.BeginBlock("try"))
                    {
                        var invokeArguments = BuildInvokeArguments(info, "uiApplication");
                        writer.AppendLine($"var result = handler.Invoke({invokeArguments});");
                        writer.AppendLine("_taskCompletionSource?.SetResult(result);");
                    }

                    using (writer.BeginBlock($"catch ({WellKnownFullyQualifiedClassNames.Exception} exception)"))
                    {
                        writer.AppendLine("_taskCompletionSource?.SetException(exception);");
                    }
                }

                writer.AppendLine();

                var raiseParameters = BuildRaiseParameters(info);
                using (writer.BeginBlock($"public {taskOfReturnType} RaiseAsync({raiseParameters})"))
                {
                    EmitArgumentAssignments(writer, info);

                    if (hasOptions)
                    {
                        writer.AppendLine();
                        using (writer.BeginBlock($"if ((options & {WellKnownFullyQualifiedClassNames.ExternalEventOptions}.AllowDirectInvocation) != 0 && {WellKnownFullyQualifiedClassNames.RevitContext}.IsRevitInApiMode)"))
                        {
                            using (writer.BeginBlock("try"))
                            {
                                var directInvokeArguments = BuildInvokeArguments(info, $"{WellKnownFullyQualifiedClassNames.RevitContext}.UiApplication");
                                writer.AppendLine($"var result = handler.Invoke({directInvokeArguments});");
                                writer.AppendLine($"return {WellKnownFullyQualifiedClassNames.Task}.FromResult<{returnType}>(result);");
                            }

                            using (writer.BeginBlock($"catch ({WellKnownFullyQualifiedClassNames.Exception} exception)"))
                            {
                                writer.AppendLine($"return {WellKnownFullyQualifiedClassNames.Task}.FromException<{returnType}>(exception);");
                            }
                        }
                    }

                    writer.AppendLine();
                    writer.AppendLine($"_taskCompletionSource = new {taskCompletionSourceOfReturnType}({WellKnownFullyQualifiedClassNames.TaskCreationOptions}.RunContinuationsAsynchronously);");
                    writer.AppendLine("base.Raise();");
                    writer.AppendLine("return _taskCompletionSource.Task;");
                }
            }
        }

        /// <summary>
        ///     Builds the backing field name for a generated property using camelCase convention.
        /// </summary>
        private static string BuildBackingFieldName(string methodName, string suffix)
        {
            return $"_{char.ToLowerInvariant(methodName[0])}{methodName.Substring(1)}{suffix}";
        }

        /// <summary>
        ///     Builds the comma-separated invoke arguments including UIApplication and extra parameter fields.
        /// </summary>
        private static string BuildInvokeArguments(ExternalEventInfo info, string uiApplicationExpression)
        {
            var arguments = new List<string>();
            if (info.HasUiApplicationParam)
            {
                arguments.Add(uiApplicationExpression);
            }

            for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
            {
                arguments.Add($"_arg{paramIndex + 1}");
            }

            return string.Join(", ", arguments);
        }

        /// <summary>
        ///     Builds the comma-separated parameter declarations for the Raise/RaiseAsync methods.
        /// </summary>
        private static string BuildRaiseParameters(ExternalEventInfo info)
        {
            var parameters = new List<string>();
            for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
            {
                parameters.Add($"{info.ExtraParameters[paramIndex].FullyQualifiedType} arg{paramIndex + 1}");
            }

            return string.Join(", ", parameters);
        }

        /// <summary>
        ///     Emits assignment statements that copy Raise method arguments to backing fields.
        /// </summary>
        private static void EmitArgumentAssignments(CodeWriter writer, ExternalEventInfo info)
        {
            for (var paramIndex = 0; paramIndex < info.ExtraParameters.Length; paramIndex++)
            {
                writer.AppendLine($"_arg{paramIndex + 1} = arg{paramIndex + 1};");
            }
        }
    }
}
