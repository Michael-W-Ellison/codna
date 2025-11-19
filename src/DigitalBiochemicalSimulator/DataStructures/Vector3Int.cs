using System;

namespace DigitalBiochemicalSimulator.DataStructures
{
    /// <summary>
    /// Represents a 3D integer position in the grid system.
    /// Used for cell positions and token locations.
    /// </summary>
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // Operators
        public static Vector3Int operator +(Vector3Int a, Vector3Int b)
            => new Vector3Int(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3Int operator -(Vector3Int a, Vector3Int b)
            => new Vector3Int(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static bool operator ==(Vector3Int a, Vector3Int b)
            => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

        public static bool operator !=(Vector3Int a, Vector3Int b)
            => !(a == b);

        // Common directions
        public static Vector3Int Zero => new Vector3Int(0, 0, 0);
        public static Vector3Int Up => new Vector3Int(0, 0, 1);
        public static Vector3Int Down => new Vector3Int(0, 0, -1);
        public static Vector3Int Left => new Vector3Int(-1, 0, 0);
        public static Vector3Int Right => new Vector3Int(1, 0, 0);
        public static Vector3Int Forward => new Vector3Int(0, 1, 0);
        public static Vector3Int Back => new Vector3Int(0, -1, 0);

        // Distance calculation
        public float Distance(Vector3Int other)
        {
            int dx = X - other.X;
            int dy = Y - other.Y;
            int dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public int ManhattanDistance(Vector3Int other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);
        }

        public override bool Equals(object? obj)
            => obj is Vector3Int other && Equals(other);

        public bool Equals(Vector3Int other)
            => X == other.X && Y == other.Y && Z == other.Z;

        public override int GetHashCode()
            => HashCode.Combine(X, Y, Z);

        public override string ToString()
            => $"({X}, {Y}, {Z})";
    }
}
