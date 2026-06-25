using System.Security.Cryptography;
using System.Text;

namespace NexusCode.Domain;

public readonly struct GraphNodeId : IEquatable<GraphNodeId>
{
    public byte[] Hash { get; }
    public string FullName { get; }

    public GraphNodeId(byte[] hash, string fullName)
    {
        Hash = hash;
        FullName = fullName;
    }

    public static GraphNodeId FromFullName(string fullName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fullName));
        return new GraphNodeId(hash, fullName);
    }

    public static GraphNodeId FromName(string name)
    {
        return FromFullName(name);
    }

    public bool Equals(GraphNodeId other)
    {
        return Hash.AsSpan().SequenceEqual(other.Hash);
    }

    public override bool Equals(object? obj) => obj is GraphNodeId other && Equals(other);
    public override int GetHashCode() => Hash.GetHashCode();
    public static bool operator ==(GraphNodeId left, GraphNodeId right) => left.Equals(right);
    public static bool operator !=(GraphNodeId left, GraphNodeId right) => !left.Equals(right);

    public override string ToString() => FullName;

    public static implicit operator byte[](GraphNodeId id) => id.Hash;
}
