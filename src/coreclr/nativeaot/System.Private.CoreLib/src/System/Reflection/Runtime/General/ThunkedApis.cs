// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// The Reflection stack has grown a large legacy of apis that thunk others.
// Apis that do little more than wrap another api will be kept here to
// keep the main files less cluttered.
//

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection.Runtime.General;
using System.Text;

using Internal.LowLevelLinq;

namespace System.Reflection.Runtime.Assemblies
{
    internal partial class RuntimeAssemblyInfo
    {
        [RequiresUnreferencedCode("Types might be removed")]
        public sealed override Type[] GetExportedTypes() => ExportedTypes.ToArray();
        public sealed override Module[] GetLoadedModules(bool getResourceModules) => Modules.ToArray();
        public sealed override Module[] GetModules(bool getResourceModules) => Modules.ToArray();
        [RequiresUnreferencedCode("Types might be removed")]
        public sealed override Type[] GetTypes() => DefinedTypes.ToArray();

        // "copiedName" only affects whether CodeBase is set to the assembly location before or after the shadow-copy.
        // That concept is meaningless on .NET Native.
        public sealed override AssemblyName GetName(bool copiedName) => GetName();

        public sealed override Stream GetManifestResourceStream(Type type, string name)
        {
            StringBuilder sb = new StringBuilder();
            if (type == null)
            {
                ArgumentNullException.ThrowIfNull(name, nameof(type));
            }
            else
            {
                string? nameSpace = type.Namespace;
                if (nameSpace != null)
                {
                    sb.Append(nameSpace);
                    if (name != null)
                    {
                        sb.Append(Type.Delimiter);
                    }
                }
            }

            if (name != null)
            {
                sb.Append(name);
            }

            return GetManifestResourceStream(sb.ToString());
        }

        public override string Location
        {
            get
            {
                return string.Empty;
            }
        }

        [Obsolete("Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location.", DiagnosticId = "SYSLIB0012", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [RequiresAssemblyFiles("The code will throw for assemblies embedded in a single-file app")]
        public sealed override string? CodeBase
        {
            get
            {
                throw new NotSupportedException(SR.NotSupported_CodeBase);
            }
        }

        public sealed override Assembly GetSatelliteAssembly(CultureInfo culture) { throw new PlatformNotSupportedException(); }
        public sealed override Assembly GetSatelliteAssembly(CultureInfo culture, Version version) { throw new PlatformNotSupportedException(); }

        [RequiresUnreferencedCode("Assembly references might be removed")]
        public sealed override AssemblyName[] GetReferencedAssemblies() { throw new PlatformNotSupportedException(); }
    }
}

namespace System.Reflection.Runtime.MethodInfos
{
    internal abstract partial class RuntimeConstructorInfo
    {
        public sealed override MethodImplAttributes GetMethodImplementationFlags() => MethodImplementationFlags;

        // Partial trust doesn't exist in Aot so these legacy apis are meaningless. Will report everything as SecurityCritical by fiat.
        public sealed override bool IsSecurityCritical => true;
        public sealed override bool IsSecuritySafeCritical => false;
        public sealed override bool IsSecurityTransparent => false;

        public sealed override bool IsCollectible => false;
    }
}

namespace System.Reflection.Runtime.EventInfos
{
    internal abstract partial class RuntimeEventInfo
    {
        public sealed override MethodInfo GetAddMethod(bool nonPublic) => AddMethod.FilterAccessor(nonPublic);
        public sealed override MethodInfo GetRemoveMethod(bool nonPublic) => RemoveMethod.FilterAccessor(nonPublic);
        public sealed override MethodInfo GetRaiseMethod(bool nonPublic) => RaiseMethod?.FilterAccessor(nonPublic);

        public sealed override bool IsCollectible => false;
    }
}

namespace System.Reflection.Runtime.MethodInfos
{
    internal abstract partial class RuntimeMethodInfo
    {
        public sealed override MethodImplAttributes GetMethodImplementationFlags() => MethodImplementationFlags;
        public sealed override ICustomAttributeProvider ReturnTypeCustomAttributes => ReturnParameter;

        // Partial trust doesn't exist in Aot so these legacy apis are meaningless. Will report everything as SecurityCritical by fiat.
        public sealed override bool IsSecurityCritical => true;
        public sealed override bool IsSecuritySafeCritical => false;
        public sealed override bool IsSecurityTransparent => false;

        public sealed override bool IsCollectible => false;
    }
}

namespace System.Reflection.Runtime.FieldInfos
{
    internal abstract partial class RuntimeFieldInfo
    {
        public sealed override bool IsCollectible => false;
    }
}

namespace System.Reflection.Runtime.PropertyInfos
{
    internal abstract partial class RuntimePropertyInfo
    {
        public sealed override MethodInfo GetGetMethod(bool nonPublic) => Getter?.FilterAccessor(nonPublic);
        public sealed override MethodInfo GetSetMethod(bool nonPublic) => Setter?.FilterAccessor(nonPublic);
        public sealed override MethodInfo[] GetAccessors(bool nonPublic)
        {
            MethodInfo getter = GetGetMethod(nonPublic);
            MethodInfo setter = GetSetMethod(nonPublic);
            int count = 0;
            if (getter != null)
                count++;
            if (setter != null)
                count++;
            MethodInfo[] accessors = new MethodInfo[count];
            int index = 0;
            if (getter != null)
                accessors[index++] = getter;
            if (setter != null)
                accessors[index++] = setter;
            return accessors;
        }

        public sealed override bool IsCollectible => false;
    }
}

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:UnrecognizedReflectionPattern",
            Justification = "Analysis does not track annotations for RuntimeTypeInfo")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073:UnrecognizedReflectionPattern",
            Justification = "Analysis does not track annotations for RuntimeTypeInfo")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2121:UnrecognizedReflectionPattern",
            Justification = "Analysis does not track annotations for RuntimeTypeInfo")]
        public Type? GetInterface(string name, bool ignoreCase)
        {
            ArgumentNullException.ThrowIfNull(name, "fullname" /* Yep, CoreCLR names this different than the ref assembly */);

            SplitTypeName(name, out string simpleName, out string ns);

            Type? match = null;
            foreach (Type ifc in GetInterfaces())
            {
                string ifcSimpleName = ifc.Name;
                bool simpleNameMatches = ignoreCase
                    ? (0 == CultureInfo.InvariantCulture.CompareInfo.Compare(simpleName, ifcSimpleName, CompareOptions.IgnoreCase))  // @todo: This could be expressed simpler but the necessary parts of String api not yet ported.
                    : simpleName.Equals(ifcSimpleName);
                if (!simpleNameMatches)
                    continue;

                // This check exists for desktop compat:
                //   (1) caller can optionally omit namespace part of name in pattern- we'll still match.
                //   (2) ignoreCase:true does not apply to the namespace portion.
                if (ns != null && !ns.Equals(ifc.Namespace))
                    continue;
                if (match != null)
                    throw ThrowHelper.GetAmbiguousMatchException(match);
                match = ifc;
            }
            return match;

            static void SplitTypeName(string fullname, out string name, out string ns)
            {
                Debug.Assert(fullname != null);

                // Get namespace
                int nsDelimiter = fullname.LastIndexOf('.');
                if (nsDelimiter != -1)
                {
                    ns = fullname.Substring(0, nsDelimiter);
                    int nameLength = fullname.Length - ns.Length - 1;
                    name = fullname.Substring(nsDelimiter + 1, nameLength);
                    Debug.Assert(fullname.Equals(ns + "." + name));
                }
                else
                {
                    ns = null;
                    name = fullname;
                }
            }
        }
    }
}
