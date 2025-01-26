using System;
using System.Runtime.CompilerServices;

namespace Fontisso.NET.Helpers;

public static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Replace(this Span<byte> target, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        var index = target.IndexOf(oldValue);
        if (index >= 0)
        {
            newValue.CopyTo(target.Slice(index, oldValue.Length));
        }
    }
}