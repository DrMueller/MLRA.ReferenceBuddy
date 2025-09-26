namespace Mmu.Mlra.ReferenceBuddy.Common.Types.Maybes.Implementation
{
    public class None
    {
        private None()
        {
        }

        public static None Value { get; } = new None();
    }

    public sealed class None<T> : Maybe<T>
    {
        public override bool Equals(Maybe<T> other)
        {
            return other is None<T>;
        }

        public override bool Equals(T other)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}