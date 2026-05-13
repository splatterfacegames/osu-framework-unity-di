namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Declares that a Unity object has one explicit initialization argument.
    /// </summary>
    /// <typeparam name="T">The argument type.</typeparam>
    public interface IInitializable<in T>
    {
        void Init(T argument);
    }

    /// <summary>
    /// Declares that a Unity object has two explicit initialization arguments.
    /// </summary>
    public interface IInitializable<in T1, in T2>
    {
        void Init(T1 first, T2 second);
    }

    /// <summary>
    /// Declares that a Unity object has three explicit initialization arguments.
    /// </summary>
    public interface IInitializable<in T1, in T2, in T3>
    {
        void Init(T1 first, T2 second, T3 third);
    }

    /// <summary>
    /// Declares that a Unity object has four explicit initialization arguments.
    /// </summary>
    public interface IInitializable<in T1, in T2, in T3, in T4>
    {
        void Init(T1 first, T2 second, T3 third, T4 fourth);
    }
}
