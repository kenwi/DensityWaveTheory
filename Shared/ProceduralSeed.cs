namespace DensityWaveTheory.Shared;

/// <summary>
/// Deterministic seed mixing shared by system / surface / focus generators.
/// Values must stay stable - changing primes or tags reshuffles content.
/// </summary>
public static class ProceduralSeed
{
    public const int MixPrime = 397;
    public const int IndexPrime = 7919;
    public const int ChildPrime = 31;
    public const int SiblingPrime = 577;

    /// <summary>ASCII "PLNT" - planet-focus moon layout salt.</summary>
    public const int PlanetTag = 0x504C4E54;

    /// <summary>ASCII "SURF" - planet-focus surface salt.</summary>
    public const int SurfaceTag = 0x53555246;

    /// <summary>ASCII "MOON" - moon-focus surface salt.</summary>
    public const int MoonTag = 0x4D4F4F4E;

    /// <summary>hash = hash * MixPrime ^ value</summary>
    public static int Mix(int hash, int value) => unchecked(hash * MixPrime ^ value);

    /// <summary>hash = hash * ChildPrime ^ value (moon/child surfaces)</summary>
    public static int MixChild(int hash, int value) => unchecked(hash * ChildPrime ^ value);

    public static int Xor(int hash, int value) => unchecked(hash ^ value);

    public static int NonZero(int hash) => hash == 0 ? 1 : hash;

    /// <summary>Stable string hash (not <see cref="string.GetHashCode"/>).</summary>
    public static int StableHash(string s)
    {
        unchecked
        {
            var h = 23;
            foreach (var c in s)
                h = h * ChildPrime + c;
            return h;
        }
    }
}
