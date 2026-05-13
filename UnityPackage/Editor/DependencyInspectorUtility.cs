using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;
using UnityEditor;
using UnityEngine;

namespace OsuFramework.Unity.Allocation.Editor
{
    internal static class DependencyInspectorUtility
    {
        private const BindingFlags declared_instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        public enum DependencySource
        {
            Self,
            Parent,
            Ancestor,
            Missing,
            Unknown
        }

        public readonly struct ResolvedMember
        {
            public readonly PropertyInfo Property;
            public readonly Type DependencyType;
            public readonly Type ParentType;
            public readonly string Name;
            public readonly bool CanBeNull;

            public ResolvedMember(PropertyInfo property, Type dependencyType, Type parentType, string name, bool canBeNull)
            {
                Property = property;
                DependencyType = dependencyType;
                ParentType = parentType;
                Name = name;
                CanBeNull = canBeNull;
            }

            public string DisplayName => $"{Property.DeclaringType?.Name}.{Property.Name}";
            public string DisplayKey => formatKey(DependencyType, ParentType, Name);
        }

        public readonly struct CachedMember
        {
            public readonly MemberInfo Member;
            public readonly Type CachedType;
            public readonly Type ParentType;
            public readonly string Name;
            public readonly object Value;
            public readonly bool IsSelfCache;
            public readonly bool IsAvailable;
            public readonly string InvalidReason;

            public CachedMember(MemberInfo member, Type cachedType, Type parentType, string name, object value, bool isSelfCache, bool isAvailable, string invalidReason)
            {
                Member = member;
                CachedType = cachedType;
                ParentType = parentType;
                Name = name;
                Value = value;
                IsSelfCache = isSelfCache;
                IsAvailable = isAvailable;
                InvalidReason = invalidReason;
            }

            public string DisplayName
            {
                get
                {
                    if (IsSelfCache)
                        return $"{Member.DeclaringType?.Name} self";

                    return $"{Member.DeclaringType?.Name}.{Member.Name}";
                }
            }

            public string DisplayKey => formatKey(CachedType, ParentType, Name);
        }

        public readonly struct Resolution
        {
            public readonly DependencySource Source;
            public readonly Component Provider;
            public readonly CachedMember CachedMember;
            public readonly string Detail;

            public Resolution(DependencySource source, Component provider, CachedMember cachedMember, string detail)
            {
                Source = source;
                Provider = provider;
                CachedMember = cachedMember;
                Detail = detail;
            }
        }

        public static IReadOnlyList<ResolvedMember> GetResolvedMembers(Type type)
        {
            var members = new List<ResolvedMember>();

            foreach (var currentType in getBaseTypesFirst(type))
            {
                foreach (var property in currentType.GetProperties(declared_instance))
                {
                    var attribute = property.GetCustomAttribute<ResolvedAttribute>();
                    if (attribute == null)
                        continue;

                    string name = attribute.Name;
                    if (attribute.Parent != null)
                        name ??= property.Name;

                    members.Add(new ResolvedMember(property, property.PropertyType, attribute.Parent, name, attribute.CanBeNull));
                }
            }

            return members;
        }

        public static IReadOnlyList<CachedMember> GetCachedMembers(Component component)
        {
            var members = new List<CachedMember>();

            if (component is DependencyPreviewContext previewContext)
            {
                foreach (var entry in previewContext.Entries)
                {
                    Type contractType = entry.ContractType;
                    if (contractType == null)
                        continue;

                    members.Add(new CachedMember(
                        previewContext.GetType(),
                        contractType,
                        null,
                        entry.Name,
                        entry.Value,
                        true,
                        true,
                        null));
                }

                return members;
            }

            Type type = component.GetType();

            foreach (var currentType in getBaseTypesFirst(type))
            {
                foreach (var attribute in currentType.GetCustomAttributes<CachedAttribute>(false))
                    members.Add(new CachedMember(currentType, attribute.Type ?? currentType, null, attribute.Name, component, true, true, null));

                foreach (var property in currentType.GetProperties(declared_instance))
                {
                    foreach (var attribute in property.GetCustomAttributes<CachedAttribute>())
                    {
                        string invalidReason = GetCachedPropertyValidationError(property);
                        object value = tryGetValue(property, component);
                        Type cachedType = attribute.Type ?? value?.GetType() ?? property.PropertyType;
                        bool available = invalidReason == null && value != null;

                        members.Add(new CachedMember(property, cachedType, null, attribute.Name, value, false, available, invalidReason ?? (value == null ? "value is null" : null)));
                    }
                }

                foreach (var field in currentType.GetFields(declared_instance))
                {
                    foreach (var attribute in field.GetCustomAttributes<CachedAttribute>())
                    {
                        string invalidReason = GetCachedFieldValidationError(field);
                        object value = tryGetValue(field, component);
                        Type cachedType = attribute.Type ?? value?.GetType() ?? field.FieldType;
                        bool available = invalidReason == null && value != null;

                        members.Add(new CachedMember(field, cachedType, null, attribute.Name, value, false, available, invalidReason ?? (value == null ? "value is null" : null)));
                    }
                }
            }

            foreach (var iface in type.GetInterfaces())
            {
                foreach (var attribute in iface.GetCustomAttributes<CachedAttribute>(false))
                    members.Add(new CachedMember(iface, attribute.Type ?? iface, null, attribute.Name, component, true, true, null));
            }

            return members;
        }

        public static IReadOnlyList<Component> GetAncestorDependencyNodes(DependencyBehaviour behaviour)
        {
            var nodes = new List<Component>();
            Transform current = behaviour.transform.parent;

            while (current != null)
            {
                foreach (var component in current.GetComponents<Component>())
                {
                    if (component is IDependencyNode || component is DependencyPreviewContext)
                        nodes.Add(component);
                }

                current = current.parent;
            }

            return nodes;
        }

        public static Component GetParentDependencyNode(DependencyBehaviour behaviour)
            => GetAncestorDependencyNodes(behaviour).FirstOrDefault();

        public static Resolution ResolveFromAncestors(DependencyBehaviour behaviour, ResolvedMember resolvedMember)
        {
            if (!isSceneResolvable(behaviour))
                return new Resolution(DependencySource.Unknown, null, default, "not in a loaded scene");

            var nodes = GetAncestorDependencyNodes(behaviour);
            if (nodes.Count == 0)
                return new Resolution(DependencySource.Missing, null, default, "no parent dependency node");

            for (int i = 0; i < nodes.Count; i++)
            {
                foreach (var cachedMember in GetCachedMembers(nodes[i]))
                {
                    if (!cachedMember.IsAvailable)
                        continue;

                    if (!matches(resolvedMember, cachedMember))
                        continue;

                    return new Resolution(i == 0 ? DependencySource.Parent : DependencySource.Ancestor, nodes[i], cachedMember, cachedMember.DisplayName);
                }
            }

            return new Resolution(DependencySource.Missing, null, default, "not provided by ancestor nodes");
        }

        public static bool HasSelfProvider(DependencyBehaviour behaviour, ResolvedMember resolvedMember, out CachedMember cachedMember)
        {
            foreach (var candidate in GetCachedMembers(behaviour))
            {
                if (!candidate.IsAvailable)
                    continue;

                if (!matches(resolvedMember, candidate))
                    continue;

                cachedMember = candidate;
                return true;
            }

            cachedMember = default;
            return false;
        }

        public static string GetResolvedPropertyValidationError(PropertyInfo property)
        {
            if (!property.CanWrite)
                return "property is not writable";

            MethodInfo setMethod = property.SetMethod;
            if (setMethod == null)
                return "property has no setter";

            if (!setMethod.IsPrivate)
                return "setter must be private";

            return null;
        }

        public static string GetCachedFieldValidationError(FieldInfo field)
        {
            if (!field.IsPrivate && !field.IsInitOnly)
                return "field must be private or readonly";

            return null;
        }

        public static string GetCachedPropertyValidationError(PropertyInfo property)
        {
            MethodInfo getMethod = property.GetMethod;
            if (getMethod == null)
                return "property has no getter";

            if (getMethod.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                return "property must be an auto-property";

            MethodInfo setMethod = property.SetMethod;
            if (setMethod == null)
                return null;

            if (!setMethod.IsPrivate)
                return "setter must be private or omitted";

            if (setMethod.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                return "property must be an auto-property";

            return null;
        }

        public static string GetSceneStateText(DependencyBehaviour behaviour)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(behaviour))
                return "Prefab asset: dependency source is unknown until instantiated.";

            if (!behaviour.gameObject.scene.IsValid() || !behaviour.gameObject.scene.isLoaded)
                return "Not in a loaded scene: dependency source is unknown.";

            return null;
        }

        public static string FormatType(Type type)
        {
            if (type == null)
                return "(none)";

            if (!type.IsGenericType)
                return type.Name;

            string genericName = type.Name;
            int tickIndex = genericName.IndexOf('`');
            if (tickIndex >= 0)
                genericName = genericName.Substring(0, tickIndex);

            return $"{genericName}<{string.Join(", ", type.GetGenericArguments().Select(FormatType))}>";
        }

        public static string FormatSource(Resolution resolution)
        {
            switch (resolution.Source)
            {
                case DependencySource.Parent:
                    return $"Parent: {resolution.Provider.name}";

                case DependencySource.Ancestor:
                    return $"Ancestor: {resolution.Provider.name}";

                case DependencySource.Missing:
                    return "Missing";

                case DependencySource.Unknown:
                    return "Unknown";

                case DependencySource.Self:
                    return "Self";

                default:
                    return resolution.Source.ToString();
            }
        }

        private static bool matches(ResolvedMember resolvedMember, CachedMember cachedMember)
            => cachedMember.CachedType != null
               && resolvedMember.DependencyType == cachedMember.CachedType
               && string.Equals(resolvedMember.Name, cachedMember.Name, StringComparison.Ordinal)
               && resolvedMember.ParentType == cachedMember.ParentType;

        private static bool isSceneResolvable(DependencyBehaviour behaviour)
            => behaviour != null
               && !PrefabUtility.IsPartOfPrefabAsset(behaviour)
               && behaviour.gameObject.scene.IsValid()
               && behaviour.gameObject.scene.isLoaded;

        private static object tryGetValue(PropertyInfo property, object instance)
        {
            try
            {
                return property.GetValue(instance);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object tryGetValue(FieldInfo field, object instance)
        {
            try
            {
                return field.GetValue(instance);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IEnumerable<Type> getBaseTypesFirst(Type type)
        {
            if (type == null || type == typeof(object))
                yield break;

            foreach (Type baseType in getBaseTypesFirst(type.BaseType))
                yield return baseType;

            yield return type;
        }

        private static string formatKey(Type type, Type parentType, string name)
        {
            string result = FormatType(type);

            if (!string.IsNullOrEmpty(name))
                result += $" named \"{name}\"";

            if (parentType != null)
                result += $" from {FormatType(parentType)}";

            return result;
        }
    }
}
