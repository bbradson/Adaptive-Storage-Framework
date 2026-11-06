using System;

namespace Multiplayer.API;

/// <summary>
/// An abstract class that can be both a reader and a writer depending on implementation.
/// </summary>
/// <remarks>See <see cref="ISynchronizable"/> and <see cref="SyncWorkerAttribute"/> for usage examples.</remarks>
public abstract class SyncWorker
{
    /// <summary><see langword="true"/> if is currently writing.</summary>
    public readonly bool isWriting;

    protected SyncWorker(bool isWriting)
    {
        this.isWriting = isWriting;
    }

    public void Write<T>(T obj, SyncType type)
    {
        if (isWriting)
        {
            Bind(ref obj, type);
        }
    }

    /// <summary>
    /// Write the specified obj, only active during writing.
    /// </summary>
    /// <param name="obj">Object to write.</param>
    /// <typeparam name="T">Type to write.</typeparam>
    public void Write<T>(T obj) {
        if (isWriting) {
            Bind(ref obj);
        }
    }

    public T Read<T>(SyncType type)
    {
        T obj = default(T);

        if (isWriting)
        {
            return obj;
        }

        Bind(ref obj, type);

        return obj;
    }

    /// <summary>
    /// Read the specified Type from the memory stream, only active during reading.
    /// </summary>
    /// <returns>The requested Type object. Null if writing.</returns>
    /// <typeparam name="T">The Type to read.</typeparam>
    public T Read<T>() {
        T obj = default(T);

        if (isWriting) {
            return obj;
        }

        Bind(ref obj);

        return obj;
    }

    public abstract void Bind<T>(ref T obj, SyncType type);

    /// <summary>Reads or writes a <see cref="Type"/> referenced by <paramref name="type"/>.</summary>
    /// <typeparam name="T">Base type that <paramref name="type"/> derives from.</typeparam>
    /// <param name="type">type to bind</param>
    public abstract void BindType<T>(ref Type type);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref byte obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref sbyte obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref short obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref ushort obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref int obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref uint obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref long obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref ulong obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref float obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref double obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref string obj);

    /// <summary>Reads or writes an object referenced by <paramref name="obj"/>.</summary>
    /// <param name="obj">object to bind</param>
    public abstract void Bind(ref bool obj);

    /// <summary>
    /// Reads or writes an object referenced by <paramref name="obj"/>
    /// </summary>
    /// <remarks>Can read/write types using user defined syncers, <see cref="ISynchronizable"/>s and readers/writers implemented by the multiplayer mod.</remarks>
    /// <typeparam name="T">type of the object to bind</typeparam>
    /// <param name="obj">object to bind</param>
    public abstract void Bind<T>(ref T obj);

    /// <summary>
    /// Uses reflection to bind a field or property
    /// </summary>
    /// <param name="obj">
    /// <para>object where the field or property can be found</para>
    /// <para>if null, <paramref name="name"/> will point at field from the global namespace</para>
    /// </param>
    /// <param name="name">path to the field or property</param>
    public abstract void Bind(object obj, string name);

    /// <summary>
    /// Reads or writes an object inheriting <see cref="ISynchronizable"/> interface. 
    /// </summary>
    /// <remarks>Does not create a new object.</remarks>
    /// <param name="obj">object to bind</param>
    public void Bind(ref ISynchronizable obj)
    {
        obj.Sync(this);
    }
}