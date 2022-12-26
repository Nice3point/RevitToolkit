// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable once CheckNamespace

namespace System.Runtime.CompilerServices;

/// <summary>Specifies that a type has required members or that a member is required.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
public
#else
internal
#endif
    sealed class RequiredMemberAttribute : Attribute
{
}