using NUnit.Framework;
using osu.Framework.Allocation;
using UnityEngine;

namespace OsuFramework.Unity.Allocation.Tests
{
    public class ExplicitInitializationTests
    {
        private sealed class InitPayload
        {
            public InitPayload(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private partial class InitProvider : DependencyNodeBehaviour
        {
            [Cached]
            private InitPayload payload = new InitPayload("from parent");
        }

        private partial class ExplicitInitBehaviour : DependencyBehaviour, IInitializable<InitPayload>
        {
            public InitPayload Payload { get; private set; }
            public bool AwakeSawPayload { get; private set; }
            public bool LoaderSawPayload { get; private set; }

            public void Init(InitPayload argument)
            {
                Payload = argument;
            }

            protected override void Awake()
            {
                AwakeSawPayload = Payload != null;
                base.Awake();
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                LoaderSawPayload = Payload != null;
            }
        }

        private partial class MultiArgumentBehaviour : DependencyBehaviour, IInitializable<InitPayload, string>
        {
            public InitPayload Payload { get; private set; }
            public string Mode { get; private set; }
            public bool AwakeSawPayload { get; private set; }

            public void Init(InitPayload first, string second)
            {
                Payload = first;
                Mode = second;
            }

            protected override void Awake()
            {
                AwakeSawPayload = Payload != null && Mode != null;
                base.Awake();
            }
        }

        [Test]
        public void TestExplicitAddComponentInitializesBeforeAwakeAndLoader()
        {
            var host = new GameObject("Host");
            var payload = new InitPayload("explicit");

            var component = host.AddComponentWithArguments<ExplicitInitBehaviour, InitPayload>(payload);

            Assert.AreSame(payload, component.Payload);
            Assert.IsTrue(component.AwakeSawPayload);
            Assert.IsTrue(component.LoaderSawPayload);

            Object.DestroyImmediate(host);
        }

        [Test]
        public void TestExplicitPrefabInstantiationInitializesBeforeAwakeAndLoader()
        {
            var parent = new GameObject("Parent");
            var prefab = new GameObject("Prefab");
            var payload = new InitPayload("prefab");

            var prefabComponent = prefab.AddComponent<ExplicitInitBehaviour>();

            var instance = prefabComponent.InstantiateWithArguments(parent.transform, payload);

            Assert.AreSame(payload, instance.Payload);
            Assert.IsTrue(instance.AwakeSawPayload);
            Assert.IsTrue(instance.LoaderSawPayload);

            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void TestHierarchyResolvedAddComponentUsesNearestParentNode()
        {
            var parent = new GameObject("Parent");
            parent.AddComponent<InitProvider>();

            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);

            var component = child.AddComponentWithResolvedArguments<ExplicitInitBehaviour, InitPayload>();

            Assert.IsNotNull(component.Payload);
            Assert.AreEqual("from parent", component.Payload.Value);
            Assert.IsTrue(component.AwakeSawPayload);
            Assert.IsTrue(component.LoaderSawPayload);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void TestMultipleExplicitArguments()
        {
            var host = new GameObject("Host");
            var payload = new InitPayload("multi");

            var component = host.AddComponentWithArguments<MultiArgumentBehaviour, InitPayload, string>(payload, "shop");

            Assert.AreSame(payload, component.Payload);
            Assert.AreEqual("shop", component.Mode);
            Assert.IsTrue(component.AwakeSawPayload);

            Object.DestroyImmediate(host);
        }
    }
}
