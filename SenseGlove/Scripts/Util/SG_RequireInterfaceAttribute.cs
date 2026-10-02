using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class SG_RequireInterfaceAttribute : PropertyAttribute
{
    public Type InterfaceType { get; }

    public SG_RequireInterfaceAttribute(Type interfaceType)
    {
        if (interfaceType == null)
            throw new ArgumentNullException(nameof(interfaceType));

        if (!interfaceType.IsInterface)
            throw new ArgumentException(
                $"{interfaceType.FullName} must be an interface.",
                nameof(interfaceType)
            );

        InterfaceType = interfaceType;
    }
}