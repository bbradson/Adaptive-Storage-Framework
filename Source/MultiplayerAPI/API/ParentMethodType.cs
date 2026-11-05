namespace Multiplayer.API;

/// <summary>Specifies the type of method. Those values are identical to Harmony's MethodType enum, and exist here to prevent reliance of this API on Harmony.</summary>
public enum ParentMethodType
{
    /// <summary>This is a normal method</summary>
    Normal,
    /// <summary>This is a getter</summary>
    Getter,
    /// <summary>This is a setter</summary>
    Setter,
    /// <summary>This is a constructor</summary>
    Constructor,
    /// <summary>This is a static constructor</summary>
    StaticConstructor,
    /// <summary>This targets the MoveNext method of the enumerator result</summary>
    Enumerator,
}