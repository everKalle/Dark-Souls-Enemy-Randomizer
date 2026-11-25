using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EnemyRandomizer.Core.Randomization;

/// <summary>Source of randomness for a run (strategy; tests use a scripted fake).</summary>
public interface IRandomSource
{
    /// <summary>Random integer in [<paramref name="min"/>, <paramref name="max"/>], both inclusive (Python <c>randint</c>).</summary>
    int Between(int min, int max);

    /// <summary>Random number in [0, 100) (Python <c>uniform(0, 100)</c>).</summary>
    double NextPercent();
}

/// <summary>
/// Deterministic random numbers from a text seed: the same seed gives the same layout with the same settings and
/// data. The seed text is hashed (SHA-256) into the <see cref="Random"/> seed. Seeds of v0.4.x (Python Mersenne
/// Twister) give different layouts; use "Copy enemy placement" to reproduce an old layout.
/// </summary>
public sealed class SeededRandom : IRandomSource
{
    private readonly Random _random;

    public SeededRandom(string seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        Seed = seed;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        _random = new Random(BinaryPrimitives.ReadInt32LittleEndian(hash));
    }

    public string Seed { get; }

    /// <summary>A new random seed text (used when the seed box is empty).</summary>
    public static string NewSeed() =>
        RandomNumberGenerator.GetInt32(int.MaxValue).ToString(CultureInfo.InvariantCulture);

    public int Between(int min, int max) => _random.Next(min, max + 1);

    public double NextPercent() => _random.NextDouble() * 100;
}
