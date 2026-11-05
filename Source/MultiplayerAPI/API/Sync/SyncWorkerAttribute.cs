using System;

namespace Multiplayer.API;

/// <summary>
/// An attribute that marks a method as a SyncWorker for a type specified in its second parameter.
/// </summary>
/// <remarks>
/// Method with this attribute has to be static.
/// </remarks>
/// <example>
/// <para>An implementation that manually constructs an object.</para>
/// <code>
///    [SyncWorkerAttribute]
///    public static void MySyncWorker(SyncWorker sync, ref MyClass inst)
///    {
///        if(!sync.isWriting)
///            inst = new MyClass("hello");
///        
///        sync.bind(ref inst.myField);
///    }
/// </code>
/// <para>An implementation that instead of creating a new object, references its existing one which resides in MyThingComp that inherits ThingComp class.</para>
/// <para>Subclasses of ThingComp are sent as a reference by the multiplayer mod itself.</para>
/// <code>
///    [SyncWorkerAttribute]
///    public static void MySyncWorker(SyncWorker sync, ref MyClass inst)
///    {
///        if(!sync.isWriting)
///            MyThingComp parent = null;
///            sync.Bind(ref parent);    // Receive its parent
///            inst = new MyClass(parent);
///        else
///            sync.Bind(ref inst.parent);    // Send its parent
///        
///        sync.bind(ref inst.myField);
///    }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class SyncWorkerAttribute : Attribute
{
    /// <summary>Decides if the type specified in the second parameter should also be used as a syncer for all of its subclasses.</summary>
    public bool isImplicit = false;

    /// <summary>Decides if the method should get an already constructed object in case of reading data.</summary>
    public bool shouldConstruct = false;
}