using System.Collections.Generic;
using OsuFramework.Unity.Allocation;
using UnityEditor;
using UnityEngine;

namespace OsuFramework.Unity.Allocation.Editor
{
    [CustomEditor(typeof(DependencyBehaviour), true)]
    [CanEditMultipleObjects]
    internal sealed class DependencyBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length != 1)
            {
                EditorGUILayout.HelpBox("Dependency inspection is available when one object is selected.", MessageType.Info);
                return;
            }

            if (target is not DependencyBehaviour behaviour)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("osu! DI", EditorStyles.boldLabel);

            drawParentNode(behaviour);
            drawValidationMessages(behaviour);
            drawResolvedMembers(behaviour);
            drawCachedMembers(behaviour);
        }

        private static void drawParentNode(DependencyBehaviour behaviour)
        {
            string sceneState = DependencyInspectorUtility.GetSceneStateText(behaviour);
            if (sceneState != null)
            {
                EditorGUILayout.LabelField("Parent Dependency Node", "Unknown");
                return;
            }

            Component parentNode = DependencyInspectorUtility.GetParentDependencyNode(behaviour);
            if (parentNode == null)
            {
                EditorGUILayout.LabelField("Parent Dependency Node", "None");
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Parent Dependency Node");
                EditorGUILayout.ObjectField(parentNode, typeof(Component), true);
            }
        }

        private static void drawValidationMessages(DependencyBehaviour behaviour)
        {
            foreach (var message in DependencyValidation.Validate(behaviour))
            {
                MessageType messageType = MessageType.Info;

                if (message.Severity == DependencyValidation.Severity.Warning)
                    messageType = MessageType.Warning;
                else if (message.Severity == DependencyValidation.Severity.Error)
                    messageType = MessageType.Error;

                EditorGUILayout.HelpBox(message.Text, messageType);
            }
        }

        private static void drawResolvedMembers(DependencyBehaviour behaviour)
        {
            IReadOnlyList<DependencyInspectorUtility.ResolvedMember> resolvedMembers = DependencyInspectorUtility.GetResolvedMembers(behaviour.GetType());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("[Resolved] Members", EditorStyles.boldLabel);

            if (resolvedMembers.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                return;
            }

            foreach (var member in resolvedMembers)
            {
                var resolution = DependencyInspectorUtility.ResolveFromAncestors(behaviour, member);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(member.DisplayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Dependency", member.DisplayKey);
                    EditorGUILayout.LabelField("Status", DependencyInspectorUtility.FormatSource(resolution));

                    if (!string.IsNullOrEmpty(resolution.Detail))
                        EditorGUILayout.LabelField("Detail", resolution.Detail);

                    if (DependencyInspectorUtility.HasSelfProvider(behaviour, member, out var selfProvider))
                        EditorGUILayout.LabelField("Self Provider", $"{selfProvider.DisplayName} (children only)");
                }
            }
        }

        private static void drawCachedMembers(DependencyBehaviour behaviour)
        {
            IReadOnlyList<DependencyInspectorUtility.CachedMember> cachedMembers = DependencyInspectorUtility.GetCachedMembers(behaviour);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("[Cached] Members", EditorStyles.boldLabel);

            if (cachedMembers.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                return;
            }

            foreach (var member in cachedMembers)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(member.DisplayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Provides", member.DisplayKey);
                    EditorGUILayout.LabelField("Status", getCachedStatus(behaviour, member));

                    if (!member.IsSelfCache)
                        EditorGUILayout.LabelField("Value", member.Value == null ? "null" : member.Value.ToString());

                    if (!string.IsNullOrEmpty(member.InvalidReason))
                        EditorGUILayout.HelpBox(member.InvalidReason, MessageType.Warning);
                }
            }
        }

        private static string getCachedStatus(DependencyBehaviour behaviour, DependencyInspectorUtility.CachedMember member)
        {
            if (behaviour is not IDependencyNode)
                return "Not exposed";

            return member.IsAvailable ? "Self" : "Unavailable";
        }
    }
}
