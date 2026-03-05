using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;

namespace OsuFramework.Unity.Allocation.Tests
{
    public class DependencyInjectionTests
    {
        private class ParentBehaviour : DependencyNodeBehaviour
        {
            [Cached]
            private string myString = "Hello";
            
            [Cached]
            private int myInt = 42;
        }

        private class ChildBehaviour : DependencyBehaviour
        {
            [Resolved]
            public string InjectedString { get; private set; }
            
            [Resolved]
            public int InjectedInt { get; private set; }

            public bool LoaderCalled { get; private set; }

            [BackgroundDependencyLoader]
            private void load()
            {
                LoaderCalled = true;
            }
        }

        [Test]
        public void TestHierarchyInjection()
        {
            var parentGo = new GameObject("Parent");
            var parent = parentGo.AddComponent<ParentBehaviour>();

            var childGo = new GameObject("Child");
            childGo.transform.SetParent(parentGo.transform);
            
            // AddComponent triggers Awake which triggers injection via reflection pathway
            var child = childGo.AddComponent<ChildBehaviour>();

            Assert.AreEqual("Hello", child.InjectedString);
            Assert.AreEqual(42, child.InjectedInt);
            Assert.IsTrue(child.LoaderCalled);

            Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void TestDynamicInstantiation()
        {
            var parentGo = new GameObject("Parent");
            var parent = parentGo.AddComponent<ParentBehaviour>();

            // Create a prefab-like object (inactive so Awake doesn't fire prematurely)
            var prefabGo = new GameObject("Prefab");
            prefabGo.SetActive(false);
            var prefabChild = prefabGo.AddComponent<ChildBehaviour>();

            // Instantiate under parent
            var instanceGo = Object.Instantiate(prefabGo, parentGo.transform);
            instanceGo.SetActive(true); // Awake fires here, looking up the new parent hierarchy
            var instanceChild = instanceGo.GetComponent<ChildBehaviour>();

            Assert.AreEqual("Hello", instanceChild.InjectedString);
            Assert.AreEqual(42, instanceChild.InjectedInt);
            Assert.IsTrue(instanceChild.LoaderCalled);

            Object.DestroyImmediate(parentGo);
            Object.DestroyImmediate(prefabGo);
        }
    }
}
