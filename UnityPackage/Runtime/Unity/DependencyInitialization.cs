using System;
using osu.Framework.Allocation;
using UnityEngine;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Runtime helpers for applying explicit Init(args)-style contracts on top of osu!framework dependency scopes.
    /// </summary>
    public static class DependencyInitialization
    {
        public static void Initialize<T>(IInitializable<T> target, T argument)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            target.Init(argument);
        }

        public static void Initialize<T1, T2>(IInitializable<T1, T2> target, T1 first, T2 second)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            target.Init(first, second);
        }

        public static void Initialize<T1, T2, T3>(IInitializable<T1, T2, T3> target, T1 first, T2 second, T3 third)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            target.Init(first, second, third);
        }

        public static void Initialize<T1, T2, T3, T4>(IInitializable<T1, T2, T3, T4> target, T1 first, T2 second, T3 third, T4 fourth)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            target.Init(first, second, third, fourth);
        }

        public static void InitializeFromHierarchy<T>(Component target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            Initialize((IInitializable<T>)target, ResolveFromHierarchy<T>(target.transform, target.GetType()));
        }

        public static void InitializeFromHierarchy<T1, T2>(Component target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            Initialize(
                (IInitializable<T1, T2>)target,
                ResolveFromHierarchy<T1>(target.transform, target.GetType()),
                ResolveFromHierarchy<T2>(target.transform, target.GetType()));
        }

        public static void InitializeFromHierarchy<T1, T2, T3>(Component target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            Initialize(
                (IInitializable<T1, T2, T3>)target,
                ResolveFromHierarchy<T1>(target.transform, target.GetType()),
                ResolveFromHierarchy<T2>(target.transform, target.GetType()),
                ResolveFromHierarchy<T3>(target.transform, target.GetType()));
        }

        public static void InitializeFromHierarchy<T1, T2, T3, T4>(Component target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            Initialize(
                (IInitializable<T1, T2, T3, T4>)target,
                ResolveFromHierarchy<T1>(target.transform, target.GetType()),
                ResolveFromHierarchy<T2>(target.transform, target.GetType()),
                ResolveFromHierarchy<T3>(target.transform, target.GetType()),
                ResolveFromHierarchy<T4>(target.transform, target.GetType()));
        }

        public static T ResolveFromHierarchy<T>(Transform transform, Type requestingType = null)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            var parentContainer = transform.GetParentNode()?.Dependencies ?? new DependencyContainer();
            var dependency = parentContainer.Get(typeof(T));

            if (dependency == null)
                throw new DependencyNotRegisteredException(requestingType ?? typeof(T), typeof(T));

            return (T)dependency;
        }
    }
}
