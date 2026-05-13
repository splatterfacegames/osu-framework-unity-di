using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;

namespace OsuFramework.Unity.Allocation.Editor
{
    internal static class DependencyValidation
    {
        public enum Severity
        {
            Info,
            Warning,
            Error
        }

        public readonly struct Message
        {
            public readonly Severity Severity;
            public readonly string Text;

            public Message(Severity severity, string text)
            {
                Severity = severity;
                Text = text;
            }
        }

        public static IReadOnlyList<Message> Validate(DependencyBehaviour behaviour)
        {
            var messages = new List<Message>();

            string sceneState = DependencyInspectorUtility.GetSceneStateText(behaviour);
            if (sceneState != null)
                messages.Add(new Message(Severity.Info, sceneState));

            foreach (var resolvedMember in DependencyInspectorUtility.GetResolvedMembers(behaviour.GetType()))
            {
                string modifierError = DependencyInspectorUtility.GetResolvedPropertyValidationError(resolvedMember.Property);
                if (modifierError != null)
                    messages.Add(new Message(Severity.Error, $"[Resolved] {resolvedMember.DisplayName}: {modifierError}."));

                var resolution = DependencyInspectorUtility.ResolveFromAncestors(behaviour, resolvedMember);
                if (resolution.Source == DependencyInspectorUtility.DependencySource.Missing && !resolvedMember.CanBeNull)
                {
                    string selfHint = DependencyInspectorUtility.HasSelfProvider(behaviour, resolvedMember, out var selfProvider)
                        ? $" This object provides {selfProvider.DisplayName}, but own [Cached] members are visible only to children."
                        : string.Empty;

                    messages.Add(new Message(Severity.Warning, $"[Resolved] {resolvedMember.DisplayName}: missing {resolvedMember.DisplayKey}.{selfHint}"));
                }
            }

            foreach (var cachedMember in DependencyInspectorUtility.GetCachedMembers(behaviour))
            {
                if (behaviour is not IDependencyNode)
                {
                    messages.Add(new Message(Severity.Warning, $"[Cached] {cachedMember.DisplayName}: this component is not an IDependencyNode, so the value is not exposed to children by the Unity hierarchy adapter."));
                    continue;
                }

                if (cachedMember.InvalidReason == null)
                    continue;

                messages.Add(new Message(Severity.Warning, $"[Cached] {cachedMember.DisplayName}: {cachedMember.InvalidReason}."));
            }

            validateInitializableContracts(behaviour, messages);
            return messages;
        }

        private static void validateInitializableContracts(DependencyBehaviour behaviour, List<Message> messages)
        {
            foreach (var contract in getInitializableContracts(behaviour.GetType()))
            {
                foreach (Type argumentType in contract.GetGenericArguments())
                {
                    var syntheticMember = createSyntheticResolvedMember(contract, argumentType);
                    var resolution = DependencyInspectorUtility.ResolveFromAncestors(behaviour, syntheticMember);

                    if (resolution.Source == DependencyInspectorUtility.DependencySource.Missing)
                    {
                        messages.Add(new Message(
                            Severity.Warning,
                            $"{contract.Name} argument {DependencyInspectorUtility.FormatType(argumentType)} is not provided by ancestor dependency nodes."));
                    }
                }
            }
        }

        private static DependencyInspectorUtility.ResolvedMember createSyntheticResolvedMember(Type contract, Type argumentType)
        {
            PropertyInfo placeholder = typeof(DependencyValidation).GetProperty(nameof(SyntheticDependency), BindingFlags.Static | BindingFlags.NonPublic);
            return new DependencyInspectorUtility.ResolvedMember(placeholder, argumentType, null, null, false);
        }

        private static object SyntheticDependency => null;

        private static IEnumerable<Type> getInitializableContracts(Type type)
            => type.GetInterfaces().Where(isInitializableContract);

        private static bool isInitializableContract(Type type)
        {
            if (!type.IsGenericType)
                return false;

            Type definition = type.GetGenericTypeDefinition();
            string fullName = definition.FullName ?? string.Empty;
            string name = definition.Name;

            return name.StartsWith("IInitializable`", StringComparison.Ordinal)
                   || name.StartsWith("IAsyncInitializable`", StringComparison.Ordinal)
                   || fullName.EndsWith(".IInitializable`" + type.GetGenericArguments().Length, StringComparison.Ordinal)
                   || fullName.EndsWith(".IAsyncInitializable`" + type.GetGenericArguments().Length, StringComparison.Ordinal);
        }
    }
}
