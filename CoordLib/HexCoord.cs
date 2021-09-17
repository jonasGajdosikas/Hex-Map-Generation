using System;

namespace coordLibrary
{

    public class Coord
    {
        public int X;
        public int Y;
        public int Z => 0 - X - Y;
        public double PosY => (double)Y + (X % 2) / 2f;
        public double PosX => HexW * (double)X;
        public const double HexW = 0.866025403784438646763;
        public Coord Cubic => new(X, Y - X / 2);
        public Coord InGrid => new(X, Y + X / 2);
        public Coord ToPixel => new(X * 2, Y * 2 + X % 2);
        public Coord(int _x, int _y)
        {
            X = _x;
            Y = _y;
        }
        public Coord(double radius, double angle)
        {
            double pX = Math.Floor(radius * Math.Cos(angle));
            double pY = Math.Floor(radius * Math.Sin(angle));
            X = (int)Math.Round(pX / HexW);
            Y = (int)Math.Round(pY + ((X % 2 == 1) ? 0.5f : 0f));
        }
        public Coord()
        {
            X = 0;
            Y = 0;
        }
        public Coord[] Neighbors => new Coord[]
            {
                new Coord(X, Y - 1),
                new Coord(X + 1, Y - 1 + (X % 2)),
                new Coord(X + 1, Y + (X % 2)),
                new Coord(X, Y + 1),
                new Coord(X - 1, Y + (X % 2)),
                new Coord(X - 1, Y - 1 + (X % 2))
            };
        
        public static double DotProduct(Coord first, Coord second)
        {
            return first.Dot(second);
        }
        public static double CrossProduct(Coord first, Coord second)
        {
            return first.Cross(second);
        }
        public int DistHex(Coord other)
        {
            Coord diff = this.Cubic - other.Cubic;
            return (Math.Abs(diff.X) + Math.Abs(diff.Y) + Math.Abs(diff.Z)) / 2;
        }
        public double DistDir(Coord other)
        {
            double dx = this.PosX - other.PosX;
            double dy = this.PosY - other.PosY;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        public Coord Interpolate(Coord other, int percentage)
        {
            return (percentage * this + (100 - percentage) * other) / 100;
        }
        public static Coord Lerp(Coord first, Coord second, int percentage)
        {
            return first.Interpolate(second, percentage);
        }
        public static Coord operator +(Coord left, Coord right)
        {
            return new Coord
            {
                X = left.X + right.X,
                Y = left.Y + right.Y
            };
        }
        public static Coord operator -(Coord left, Coord right)
        {
            return new Coord
            {
                X = left.X - right.X,
                Y = left.Y - right.Y
            };
        }
        public static Coord operator *(Coord left, int right)
        {
            return right * left;
        }
        public static Coord operator *(int left, Coord right)
        {
            return new Coord
            {
                X = left * right.X,
                Y = left * right.Y
            };
        }
        public static Coord operator /(Coord left, int right)
        {
            return new Coord
            {
                X = left.X / right,
                Y = left.Y / right
            };
        }
        public static bool operator >(Coord left, Coord right)
        {
            return (left.X > right.X && left.Y > right.Y);
        }
        public static bool operator <(Coord left, Coord right)
        {
            return (left.X < right.X && left.Y < right.Y);
        }
        public static bool operator >=(Coord left, Coord right)
        {
            return (left.X >= right.X && left.Y >= right.Y);
        }
        public static bool operator <=(Coord left, Coord right)
        {
            return (left.X <= right.X && left.Y <= right.Y);
        }
        /*
         * Code for equality based on stored values from
         * https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type
         * Class example. The code there was adapted only with a few changes in variable name
         * 
         */
        public override bool Equals(object obj) => this.Equals(obj as Coord);
        public bool Equals(Coord p)
        {
            if (p is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, p))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != p.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (X == p.X) && (Y == p.Y);
        }
        public override int GetHashCode() => (X, Y).GetHashCode();
        public static bool operator ==(Coord left, Coord right)
        {
            if (left is null)
            {
                if (right is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return left.Equals(right);
        }
        public static bool operator !=(Coord left, Coord right) => !(left == right);
        public override string ToString()
        {
            return "(" + X.ToString() + "," + Y.ToString() + ")";
        }
        public double Dot(Coord other)
        {
            return this.PosX * other.PosX + this.PosY * other.PosY;
        }
        public double Cross(Coord other)
        {
            return this.PosX * other.PosY - other.PosX * this.PosY;
        }
    }
}
