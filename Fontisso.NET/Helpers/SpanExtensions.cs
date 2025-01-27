using System;
using System.Runtime.CompilerServices;

namespace Fontisso.NET.Helpers;

public static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReplace(this Span<byte> target, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        var index = target.IndexOf(oldValue);
        if (index >= 0)
        {
            var replacementSlice = target.Slice(index, oldValue.Length);
            newValue.CopyTo(replacementSlice);
            
            if (newValue.Length < oldValue.Length)
            {
                replacementSlice.Slice(newValue.Length).Clear();
            }

            return true;
        }

        return false;
    }
}