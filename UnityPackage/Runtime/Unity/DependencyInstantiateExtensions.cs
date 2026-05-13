using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Unity factory helpers that apply Init(args)-style contracts before normal Awake-driven dependency activation.
    /// </summary>
    public static class DependencyInstantiateExtensions
    {
        public static TComponent AddComponentWithArguments<TComponent, T>(this GameObject gameObject, T argument)
            where TComponent : Component, IInitializable<T>
            => addComponentInitialized<TComponent>(gameObject, component => DependencyInitialization.Initialize(component, argument));

        public static TComponent AddComponentWithArguments<TComponent, T1, T2>(this GameObject gameObject, T1 first, T2 second)
            where TComponent : Component, IInitializable<T1, T2>
            => addComponentInitialized<TComponent>(gameObject, component => DependencyInitialization.Initialize(component, first, second));

        public static TComponent AddComponentWithArguments<TComponent, T1, T2, T3>(this GameObject gameObject, T1 first, T2 second, T3 third)
            where TComponent : Component, IInitializable<T1, T2, T3>
            => addComponentInitialized<TComponent>(gameObject, component => DependencyInitialization.Initialize(component, first, second, third));

        public static TComponent AddComponentWithArguments<TComponent, T1, T2, T3, T4>(this GameObject gameObject, T1 first, T2 second, T3 third, T4 fourth)
            where TComponent : Component, IInitializable<T1, T2, T3, T4>
            => addComponentInitialized<TComponent>(gameObject, component => DependencyInitialization.Initialize(component, first, second, third, fourth));

        public static TComponent AddComponentWithResolvedArguments<TComponent, T>(this GameObject gameObject)
            where TComponent : Component, IInitializable<T>
            => addComponentInitialized<TComponent>(gameObject, DependencyInitialization.InitializeFromHierarchy<T>);

        public static TComponent AddComponentWithResolvedArguments<TComponent, T1, T2>(this GameObject gameObject)
            where TComponent : Component, IInitializable<T1, T2>
            => addComponentInitialized<TComponent>(gameObject, DependencyInitialization.InitializeFromHierarchy<T1, T2>);

        public static TComponent AddComponentWithResolvedArguments<TComponent, T1, T2, T3>(this GameObject gameObject)
            where TComponent : Component, IInitializable<T1, T2, T3>
            => addComponentInitialized<TComponent>(gameObject, DependencyInitialization.InitializeFromHierarchy<T1, T2, T3>);

        public static TComponent AddComponentWithResolvedArguments<TComponent, T1, T2, T3, T4>(this GameObject gameObject)
            where TComponent : Component, IInitializable<T1, T2, T3, T4>
            => addComponentInitialized<TComponent>(gameObject, DependencyInitialization.InitializeFromHierarchy<T1, T2, T3, T4>);

        public static TComponent InstantiateWithArguments<TComponent, T>(this TComponent prefab, Transform parent, T argument, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T>
            => instantiateComponent(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, argument));

        public static TComponent InstantiateWithArguments<TComponent, T1, T2>(this TComponent prefab, Transform parent, T1 first, T2 second, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2>
            => instantiateComponent(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, first, second));

        public static TComponent InstantiateWithArguments<TComponent, T1, T2, T3>(this TComponent prefab, Transform parent, T1 first, T2 second, T3 third, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3>
            => instantiateComponent(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, first, second, third));

        public static TComponent InstantiateWithArguments<TComponent, T1, T2, T3, T4>(this TComponent prefab, Transform parent, T1 first, T2 second, T3 third, T4 fourth, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3, T4>
            => instantiateComponent(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, first, second, third, fourth));

        public static TComponent InstantiateWithResolvedArguments<TComponent, T>(this TComponent prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T>
            => instantiateComponent(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T>);

        public static TComponent InstantiateWithResolvedArguments<TComponent, T1, T2>(this TComponent prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2>
            => instantiateComponent(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T1, T2>);

        public static TComponent InstantiateWithResolvedArguments<TComponent, T1, T2, T3>(this TComponent prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3>
            => instantiateComponent(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T1, T2, T3>);

        public static TComponent InstantiateWithResolvedArguments<TComponent, T1, T2, T3, T4>(this TComponent prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3, T4>
            => instantiateComponent(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T1, T2, T3, T4>);

        public static TComponent InstantiateWithArguments<TComponent, T>(this GameObject prefab, Transform parent, T argument, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, argument));

        public static TComponent InstantiateWithArguments<TComponent, T1, T2>(this GameObject prefab, Transform parent, T1 first, T2 second, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, first, second));

        public static TComponent InstantiateWithArguments<TComponent, T1, T2, T3>(this GameObject prefab, Transform parent, T1 first, T2 second, T3 third, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, first, second, third));

        public static TComponent InstantiateWithArguments<TComponent, T1, T2, T3, T4>(this GameObject prefab, Transform parent, T1 first, T2 second, T3 third, T4 fourth, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3, T4>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, component => DependencyInitialization.Initialize(component, first, second, third, fourth));

        public static TComponent InstantiateWithResolvedArguments<TComponent, T>(this GameObject prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T>);

        public static TComponent InstantiateWithResolvedArguments<TComponent, T1, T2>(this GameObject prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T1, T2>);

        public static TComponent InstantiateWithResolvedArguments<TComponent, T1, T2, T3>(this GameObject prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T1, T2, T3>);

        public static TComponent InstantiateWithResolvedArguments<TComponent, T1, T2, T3, T4>(this GameObject prefab, Transform parent, bool worldPositionStays = false)
            where TComponent : Component, IInitializable<T1, T2, T3, T4>
            => instantiateGameObject<TComponent>(prefab, parent, worldPositionStays, DependencyInitialization.InitializeFromHierarchy<T1, T2, T3, T4>);

        private static TComponent addComponentInitialized<TComponent>(GameObject gameObject, Action<TComponent> initialize)
            where TComponent : Component
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            if (initialize == null) throw new ArgumentNullException(nameof(initialize));

            var wasActive = gameObject.activeSelf;

            if (wasActive)
                gameObject.SetActive(false);

            try
            {
                var component = gameObject.AddComponent<TComponent>();
                initialize(component);
                return component;
            }
            finally
            {
                if (wasActive)
                    gameObject.SetActive(true);
            }
        }

        private static TComponent instantiateComponent<TComponent>(TComponent prefab, Transform parent, bool worldPositionStays, Action<TComponent> initialize)
            where TComponent : Component
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (initialize == null) throw new ArgumentNullException(nameof(initialize));

            var component = instantiateInactive(prefab, parent, worldPositionStays);
            initialize(component);
            restoreInstanceActiveState(prefab.gameObject, component.gameObject);
            return component;
        }

        private static TComponent instantiateGameObject<TComponent>(GameObject prefab, Transform parent, bool worldPositionStays, Action<TComponent> initialize)
            where TComponent : Component
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (initialize == null) throw new ArgumentNullException(nameof(initialize));

            var instance = instantiateInactive(prefab, parent, worldPositionStays);
            var component = instance.GetComponent<TComponent>();

            if (component == null)
                throw new MissingComponentException($"{prefab.name} does not contain a {typeof(TComponent).Name} component.");

            initialize(component);
            restoreInstanceActiveState(prefab, instance);
            return component;
        }

        private static GameObject instantiateInactive(GameObject prefab, Transform parent, bool worldPositionStays)
        {
            var wasActive = prefab.activeSelf;

            if (wasActive)
                prefab.SetActive(false);

            try
            {
                return Object.Instantiate(prefab, parent, worldPositionStays);
            }
            finally
            {
                if (wasActive)
                    prefab.SetActive(true);
            }
        }

        private static TComponent instantiateInactive<TComponent>(TComponent prefab, Transform parent, bool worldPositionStays)
            where TComponent : Component
        {
            var wasActive = prefab.gameObject.activeSelf;

            if (wasActive)
                prefab.gameObject.SetActive(false);

            try
            {
                return Object.Instantiate(prefab, parent, worldPositionStays);
            }
            finally
            {
                if (wasActive)
                    prefab.gameObject.SetActive(true);
            }
        }

        private static void restoreInstanceActiveState(GameObject prefab, GameObject instance)
        {
            if (prefab.activeSelf)
                instance.SetActive(true);
        }
    }
}
