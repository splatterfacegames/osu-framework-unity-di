using System;
using System.Reflection;
using UniRx;

namespace OsuFramework.Unity.Allocation
{
    public interface IHasDependencyDisposable
    {
        CompositeDisposable DependenciesDisposable { get; }
    }

    public static class UniRxDependencyUtils
    {
        public static bool IsReactiveProperty(Type type, out Type innerType, out bool isReadOnly)
        {
            innerType = null;
            isReadOnly = false;
            
            if (!type.IsGenericType) return false;

            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(IReactiveProperty<>) || genericType == typeof(ReactiveProperty<>))
            {
                innerType = type.GetGenericArguments()[0];
                return true;
            }
            if (genericType == typeof(IReadOnlyReactiveProperty<>) || genericType == typeof(ReadOnlyReactiveProperty<>))
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
                throw new InvalidOperationException($"Target {target.GetType().Name} must implement IHasDependencyDisposable to inject UniRx reactive properties.");
            }

            // Reflection-based creation of the bound copy.
            // In a fully source-generated environment, this could be emitted directly.
            var method = typeof(UniRxDependencyUtils).GetMethod(nameof(CreateBoundCopyGeneric), BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(innerType);

            return method?.Invoke(null, new object[] { original, disposables, isReadOnly });
        }

        private static object CreateBoundCopyGeneric<T>(object original, CompositeDisposable disposables, bool isReadOnly)
        {
            var originalReadOnly = original as IReadOnlyReactiveProperty<T>;
            if (originalReadOnly == null) return null;

            if (isReadOnly)
            {
                // If they only asked for IReadOnlyReactiveProperty, we can just return the original since they can't mutate it.
                // However, if we want to ensure isolation or specific binding logic, we could wrap it.
                // For now, passing the reference is safe and zero-allocation.
                return original;
            }

            var originalReactive = original as IReactiveProperty<T>;
            if (originalReactive == null)
            {
                throw new InvalidOperationException("Cannot inject IReactiveProperty<T> when the cached value is only IReadOnlyReactiveProperty<T>.");
            }

            // Create a new ReactiveProperty that acts as a two-way bound copy.
            var copy = new ReactiveProperty<T>(originalReactive.Value);

            // Two-way binding
            bool isUpdating = false;

            originalReactive.Subscribe(x =>
            {
                if (isUpdating) return;
                isUpdating = true;
                copy.Value = x;
                isUpdating = false;
            }).AddTo(disposables);

            copy.Subscribe(x =>
            {
                if (isUpdating) return;
                isUpdating = true;
                originalReactive.Value = x;
                isUpdating = false;
            }).AddTo(disposables);

            // The copy itself must be disposed when the target is disposed.
            copy.AddTo(disposables);

            return copy;
        }
    }
}
