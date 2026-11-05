using System;

namespace Multiplayer.API;

public record Serializer<Live, Networked>(
    Func<Live, object, object[], Networked> Writer, // (live, target, args) => networked
    Func<Networked, Live> Reader // (networked) => live
);

public static class Serializer
{
    public static Serializer<Live, Networked> New<Live, Networked>(Func<Live, object, object[], Networked> writer, Func<Networked, Live> reader)
    {
        return new(writer, reader);
    }

    public static Serializer<Live, Networked> New<Live, Networked>(Func<Live, Networked> writer, Func<Networked, Live> reader)
    {
        return new((live, _, _) => writer(live), reader);
    }

    public static Serializer<Live, object> SimpleReader<Live>(Func<Live> reader)
    {
        return new((_, _, _) => null, _ => reader());
    }
}