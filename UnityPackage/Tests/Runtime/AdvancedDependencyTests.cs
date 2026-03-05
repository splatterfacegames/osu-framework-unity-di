using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;
using UniRx;

namespace OsuFramework.Unity.Allocation.Tests
{
    public class AdvancedDependencyTests
    {
        private partial class GrandparentBehaviour : DependencyNodeBehaviour
        {
            [Cached]
            private string globalConfig = "GrandparentConfig";

            [Cached]
            private int overrideableValue = 10;
        }

        private partial class ParentBehaviour : DependencyNodeBehaviour
        {
            // Overrides the Grandparent's integer dependency for its own children
            [Cached]
            private int overrideableValue = 99;
        }

        private partial class ChildBehaviour : DependencyBehaviour
        {
            [Resolved]
            public string Config { get; private set; }

            [Resolved]
            public int Value { get; private set; }

            [Resolved(CanBeNull = true)]
            public string OptionalConfig { get; private set; }
        }

        private partial class ReadOnlyReactiveParent : DependencyNodeBehaviour
        {
            private ReactiveProperty<float> internalHealth = new ReactiveProperty<float>(100f);

            // Expose only the read-only interface to children
            [Cached]
            public IReadOnlyReactiveProperty<float> Health => internalHealth;

            public void TakeDamage(float amount) => internalHealth.Value -= amount;
        }

        private partial class ReactiveChild : DependencyBehaviour
        {
            [Resolved]
            public IReadOnlyReactiveProperty<float> PlayerHealth { get; private set; }
        }

        [Test]
        public void TestHierarchyOverride()
        {
            var grandparentGo = new GameObject("Grandparent");
            var grandparent = grandparentGo.AddComponent<GrandparentBehaviour>();

            var parentGo = new GameObject("Parent");
            parentGo.transform.SetParent(grandparentGo.transform);
            var parent = parentGo.AddComponent<ParentBehaviour>();

            var childGo = new GameObject("Child");
            childGo.transform.SetParent(parentGo.transform);
            var child = childGo.AddComponent<ChildBehaviour>();

            // The string should come from the Grandparent (passes through Parent)
            Assert.AreEqual("GrandparentConfig", child.Config);
            
            // The int should come from the Parent (overriding the Grandparent)
            Assert.AreEqual(99, child.Value);

            // Optional dependency should be null without throwing an exception
            Assert.IsNull(child.OptionalConfig);

            UnityEngine.Object.DestroyImmediate(grandparentGo);
        }

        [Test]
        public void TestMissingDependencyThrows()
        {
            var childGo = new GameObject("ChildWithoutParent");
            
            // Adding the behaviour will trigger Awake -> DependencyActivator.Activate
            // Since there is no parent providing the mandatory 'string' and 'int' dependencies, it should throw.
            Assert.Throws<DependencyNotRegisteredException>(() => 
            {
                childGo.AddComponent<ChildBehaviour>();
            });

            UnityEngine.Object.DestroyImmediate(childGo);
        }

        [Test]
        public void TestReadOnlyReactivePropertyInjection()
        {
            var parentGo = new GameObject("Parent");
            var parent = parentGo.AddComponent<ReadOnlyReactiveParent>();

            var childGo = new GameObject("Child");
            childGo.transform.SetParent(parentGo.transform);
            var child = childGo.AddComponent<ReactiveChild>();

            // Ensure the child received the read-only property
            Assert.IsNotNull(child.PlayerHealth);
            Assert.AreEqual(100f, child.PlayerHealth.Value);

            // Ensure updates from the parent propagate to the child's read-only view
            parent.TakeDamage(25f);
            Assert.AreEqual(75f, child.PlayerHealth.Value);

            UnityEngine.Object.DestroyImmediate(parentGo);
        }
    }
}
