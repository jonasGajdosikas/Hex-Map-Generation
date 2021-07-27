using System;
using coordLibrary;
using GridLibrary;

namespace Program
{
    class Program
    {
        static void Main()
        {
            int width = 60;
            int height = 50;
            HexGrid grid = new(width, height);
            grid.Randomize(51);
            for (int i = 0; i < 10; i++)
            {
                grid.Smooth();
                grid.Export("map " + i + ".png");
            }
        }
    }

    class HexGrid : Grid
    {
        public HexGrid(int _width, int _height) : base(_width, _height) { }

        public void Randomize(int percentFilled)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1) this[x, y] = 1;
                    else this[x, y] = (rand.Next(100) < percentFilled) ? 1 : 0;
                }
            }
        }

        public void Process(int smoothingSteps)
        {
            for (int i = 0; i < smoothingSteps; i++)
            {
                Smooth();
            }
        }
        public void Smooth()
        {
            int[,] neighbors = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    foreach (Coord neighbor in new Coord(x, y).Neighbors())
                    {
                        if (InMap(neighbor)) neighbors[x, y] += this[neighbor];
                        else neighbors[x, y] += 1;
                    }
                }
            }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (neighbors[x, y] < 3) this[x, y] = 0;
                    else if (neighbors[x, y] > 3) this[x, y] = 1;
                }
            }
        }
    }
}
