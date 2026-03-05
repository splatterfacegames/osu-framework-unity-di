using System;
using System.Reflection;
using System.Collections.Generic;
using R3;

namespace OsuFramework.Unity.Allocation
{
    public interface IHasDependencyDisposable
    {
        List<IDisposable> DependenciesDisposable { get; }
    }

    public static class DisposableExtensions
    {
        public static void AddTo(this IDisposable disposable, ICollection<IDisposable> collection)
        {
            collection.Add(disposable);
        }
    }

    public static class R3DependencyUtils
    {
        public static bool IsReactiveProperty(Type type, out Type innerType, out bool isReadOnly)
        {
            innerType = null;
            isReadOnly = false;
            
            if (!type.IsGenericType) return false;

            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(ReactiveProperty<>))
            {
                innerType = type.GetGenericArguments()[0];
                return true;
            }
            if (genericType == typeof(ReadOnlyReactiveProperty<>))
            {
                innerType = type.GetGenericArguments()[0];
                isReadOnly = true;
                return true;
            }

            return false;
        }

        public static object CreateBoundCopy(object original, Type type, Type innerType, bool isReadOnly, object target)
        {
            var disposableTarget = target as IHasDependencyDisposable;
            var disposables = disposableTarget?.DependenciesDisposable;

            if (disposables == null)
            {
                throw new InvalidOperationException($"Target {target.GetType().Name} must implement IHasDependencyDisposable to inject R3 reactive properties.");
            }

            // Reflection-based creation of the bound copy.
            var method = typeof(R3DependencyUtils).GetMethod(nameof(CreateBoundCopyGeneric), BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(innerType);

            return method?.Invoke(null, new object[] { original, disposables, isReadOnly });
        }

        private static object CreateBoundCopyGeneric<T>(object original, List<IDisposable> disposables, bool isReadOnly)
        {
            var originalReadOnly = original as ReadOnlyReactiveProperty<T>;
            if (originalReadOnly == null) return null;

            if (isReadOnly)
            {
                return original;
            }

            var originalReactive = original as ReactiveProperty<T>;
            if (originalReactive == null)
            {
                throw new InvalidOperationException("Cannot inject ReactiveProperty<T> when the cached value is only ReadOnlyReactiveProperty<T>.");
            }

            var copy = new ReactiveProperty<T>(originalReactive.Value);

            bool isUpdating = false;

            disposables.Add(originalReactive.Subscribe(x =>
            {
                if (isUpdating) return;
                isUpdating = true;
                copy.Value = x;
                isUpdating = false;
            }));

            disposables.Add(copy.Subscribe(x =>
            {
                if (isUpdating) return;
                isUpdating = true;
                originalReactive.Value = x;
                isUpdating = false;
            }));

            disposables.Add(copy);

            return copy;
        }
    }
}
