using System.Collections.Generic;
using System.Linq;
using Mmu.Mlra.ReferenceBuddy.Common.Types.Maybes.Implementation;

namespace Mmu.Mlra.ReferenceBuddy.Common.Types.Maybes
{
    public static class MaybeAdapter
    {
        public static IEnumerable<T> SelectSome<T>(this IEnumerable<Maybe<T>> maybes)
        {
#pragma warning disable CA2021
            return maybes
                .OfType<Some<T>>()
#pragma warning restore CA2021
                .Select(some => (T)some)
                .ToList();
        }
    }
}