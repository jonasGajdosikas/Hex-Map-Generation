using System;
using System.Collections.Generic;
//personal libraries
using coordLibrary;
using GridLibrary;

namespace Program
{
    class Program
    {
        static void Main()
        {
            
            int width = 120;
            int height = 100;
            HexGrid grid = new(width, height);
            grid.GenerateVoronoiRooms(40, 8, 0.7f, 50);
            grid.Export("output//Map.png");
            /**
            grid.MakeCaverns(51, 4, 8);
            grid.Export("output//Map.png");
            /****/
            //Console.ReadKey();
        }
    }

    class HexGrid : Grid
    {
        public bool[,] addedToRegion;
        public HexGrid(int _width, int _height) : base(_width, _height)
        {
            addedToRegion = new bool[_width, _height];
        }
        public void MakeCaverns(int infill, int smoothingSteps, int minRoomSize)
        {
            Randomize(infill);
            Process(smoothingSteps, minRoomSize);
        }
        public void Randomize(int percentFilled)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (addedToRegion[x, y]) continue;
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1) this[x, y] = 1;
                    else this[x, y] = (rand.Next(100) < percentFilled) ? 1 : 0;
                }
            }
        }

        public void Process(int smoothingSteps, int minRoomsize)
        {
            for (int i = 0; i < smoothingSteps; i++)
            {
                Smooth();
            }
            Export("output//SmoothedMap.png");
            //find all regions; remove small ones; then connect the remaining ones
            List<Region> roomRegions = GetRegionsOfType(0);
            List<List<Region>> bigRooms = new();
            foreach (Region room in roomRegions)
            {
                if (room.tiles.Count < minRoomsize)
                {
                    foreach (Coord coord in room.tiles)
                    {
                        this[coord] = 1;
                    }
                }
                else
                {
                    bigRooms.Add(new List<Region> { room });
                }
            }
            ConnectAllRegions(bigRooms);
        }
        public void Smooth()
        {
            int[,] neighbors = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    foreach (Coord neighbor in new Coord(x, y).Neighbors)
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
        public List<Region> GetRegionsOfType(int type)
        {
            List<Region> regions = new();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!addedToRegion[x, y] && this[x, y] == type)
                    {
                        Region regionToAdd = GetRegion(new Coord(x, y));
                        regions.Add(regionToAdd);
                    }
                }
            }

            return regions;
        }
        public Region GetRegion(Coord start)
        {
            List<Coord> regionTiles = new();
            List<Coord> edgeTiles = new();

            int tileType = this[start];

            Queue<Coord> queue = new();
            queue.Enqueue(start);
            addedToRegion[start.X, start.Y] = true;

            while (queue.Count > 0)
            {
                Coord coord = queue.Dequeue();
                regionTiles.Add(coord);
                foreach (Coord neighbor in coord.Neighbors)
                {
                    if (addedToRegion[neighbor.X, neighbor.Y]) continue;
                    if (this[neighbor] == tileType)
                    {
                        queue.Enqueue(neighbor);
                        addedToRegion[neighbor.X, neighbor.Y] = true;
                    }
                }
            }

            foreach (Coord coord in regionTiles)
            {
                foreach (Coord neighbor in coord.Neighbors)
                {
                    if (this[neighbor] != tileType)
                    {
                        edgeTiles.Add(coord);
                        break;
                    }
                }
            }

            return new Region(regionTiles, edgeTiles);
        }
        public class Region
        {
            public List<Coord> tiles;
            public List<Coord> edgeTiles;
            public Region(List<Coord> _tiles, List<Coord> _edgeTiles)
            {
                tiles = _tiles;
                edgeTiles = _edgeTiles;
            }
        }
        public void ConnectAllRegions(List<List<Region>> ClustersToConnect)
        {
            while (ClustersToConnect.Count > 1)
            {
                int bestDistance = -1;
                bool connectionFound = false;
                List<Region> bestClusterA = new(), bestClusterB = new();
                Coord bestTileA = new(), bestTileB = new();
                for (int cA = 0; cA < ClustersToConnect.Count; cA++)
                {
                    List<Region> clusterA = ClustersToConnect[cA];
                    for (int cB = cA + 1; cB < ClustersToConnect.Count; cB++)
                    {
                        List<Region> clusterB = ClustersToConnect[cB];
                        foreach (Region roomA in clusterA)
                        {
                            foreach (Region roomB in clusterB)
                            {
                                foreach (Coord tileA in roomA.edgeTiles)
                                {
                                    foreach (Coord tileB in roomB.edgeTiles)
                                    {
                                        int distanceBetweenRooms = tileA.DistHex(tileB);
                                        if (distanceBetweenRooms < bestDistance || !connectionFound)
                                        {
                                            bestClusterA = clusterA;
                                            bestClusterB = clusterB;
                                            bestTileA = tileA;
                                            bestTileB = tileB;
                                            bestDistance = distanceBetweenRooms;
                                            connectionFound = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (connectionFound)
                {
                    ClustersToConnect.Remove(bestClusterA);
                    ClustersToConnect.Remove(bestClusterB);
                    bestClusterA.AddRange(bestClusterB);
                    ClustersToConnect.Add(bestClusterA);
                    DrawLine(bestTileA, bestTileB);
                }
            }
        }
        public void DrawLine(Coord from, Coord to, int value = 0)
        {
            List<Coord> line = GetLine(from, to);
            foreach (Coord coord in line)
            {
                this[coord] = value;
            }
        }
        public static List<Coord> GetLine(Coord from, Coord to)
        {
            List<Coord> line = new();
            Coord current = from;
            line.Add(current);

            while (current != to)
            {
                double dist = current.DistDir(to);
                double distToLine = double.MaxValue;
                Coord next = new();
                foreach (Coord neighbor in current.Neighbors)
                {
                    double dNext = neighbor.DistDir(to);
                    //Console.WriteLine("distance from {0} to {1} is {2}", neighbor, to, dNext);
                    if (dNext <= dist)
                    {
                        double ndtl = SqDistToLine(neighbor, from, to);
                        if (ndtl <= distToLine)
                        {
                            distToLine = ndtl;
                            next = neighbor;
                        }
                    }
                }
                //Console.WriteLine("{0} was chosen with a distance of {1}", next, dist);
                line.Add(next);
                current = next;
            }

            return line;
        }
        public static double SqDistToLine(Coord point, Coord lineA, Coord lineB)
        {
            return Math.Abs((point.PosX - lineA.PosX) * (-lineB.PosY + lineA.PosY) + (point.PosY - lineA.PosY) * (lineB.PosX - lineA.PosX));
        }
        //voronoi cell rooms
        public static int CloserOfTwo(Coord to, VoronoiPoint first, VoronoiPoint second)
        {
            bool firstCloser;
            if (to.DistHex(first.position) == to.DistHex(second.position)) firstCloser = (to.DistDir(first.position) <= to.DistDir(second.position));
            else firstCloser = (to.DistHex(first.position) <= to.DistHex(second.position));

            if (firstCloser) return first.ID;
            else return second.ID;
        }
        public class VoronoiPoint {
            public Coord position;
            public List<int> children;
            public int parent;
            public int level;
            public int ID;
            public VoronoiPoint(Coord _position, int _ID, int _parentID, int _level)
            {
                position = _position;
                ID = _ID;
                parent = _parentID;
                level = _level;
                children = new();
            }
        }
        public class VoronoiCell
        {
            public VoronoiPoint Origin;
            public List<Coord> tiles;
            public List<Coord> borderTiles;
            public int parent;
            public List<int> children;
            public int depth;
            public List<int> neighbors;
            public bool isBorder;
            public VoronoiCell(int[,] tilemap, VoronoiPoint _origin, List<Coord> _tiles)
            {
                Origin = _origin;
                tiles = _tiles;
                borderTiles = new();
                isBorder = false;
                children = new();
                neighbors = new();
                foreach (Coord tile in tiles)
                {
                    foreach (Coord neighbor in tile.Neighbors)
                    {
                        if (tilemap[neighbor.X, neighbor.Y] != Origin.ID)
                        {
                            if (!isBorder && tilemap[neighbor.X, neighbor.Y] == -1) isBorder = true;
                            borderTiles.Add(tile);
                            if (!neighbors.Contains(tilemap[neighbor.X, neighbor.Y])) neighbors.Add(tilemap[neighbor.X, neighbor.Y]);
                            break;
                        }
                    }
                }
            }
        }
        public List<VoronoiCell> voronoiCells;
        public List<VoronoiPoint> voronoiPoints;
        public void GenerateVoronoiRooms(int centerRadius, float pointDistance, float maxDistIncrease, int maxTries) {
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) this[x, y] = 1;
            int[,] cells = new int[width, height];
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) cells[x, y] = -1;
            voronoiPoints = GenerateVoronoiPoints(centerRadius, pointDistance, maxDistIncrease, maxTries);
            //make Voronoi cells out of the points
            //determine belonging of each coord
            Coord center = voronoiPoints[0].position;
            List<Coord>[] tileLists = new List<Coord>[voronoiPoints.Count];
            for (int i = 0; i < voronoiPoints.Count; i++) tileLists[i] = new();
            //make lists of coords according to the cells
            for (int x = center.X - centerRadius; x <= center.X + centerRadius; x++)
            {
                for (int y = center.Y - centerRadius; y <= center.Y + centerRadius; y++)
                {
                    if (center.DistHex(new(x, y)) > centerRadius) continue;
                    int closestPointID = 0;
                    for (int i = 1; i < voronoiPoints.Count; i++)
                    {
                        closestPointID = CloserOfTwo(new(x, y), voronoiPoints[closestPointID], voronoiPoints[i]);
                    }
                    cells[x, y] = closestPointID;
                    tileLists[closestPointID].Add(new Coord(x, y));
                }
            }
            //define the cells based on the found tiles
            voronoiCells = new();
            for (int i = 0; i < voronoiPoints.Count; i++)
            {
                voronoiCells.Add(new(cells, voronoiPoints[i], tileLists[i]));
            }
            //fill cells in with empty
            foreach (VoronoiCell cell in voronoiCells)
            {
                if (cell.isBorder) continue;
                foreach (Coord tile in cell.tiles)
                {
                    this[tile] = 0;
                    addedToRegion[tile.X, tile.Y] = true;
                }
                foreach (Coord borderTile in cell.borderTiles) this[borderTile] = 1;
            }

            //pathways
            voronoiCells[0].parent = -1;
            voronoiCells[0].depth = 0;
            for (int j = 0; j < voronoiCells.Count; j++){
                for (int i = 1; i < voronoiCells.Count; i++)
                {
                    int minDepth = -1;
                    int mdnID = -1;
                    foreach (int nID in voronoiCells[i].neighbors)
                    {
                        if (nID == -1) continue;
                        if (minDepth > voronoiCells[i].depth + 1 || (minDepth == -1 && voronoiCells[i].depth != -1))
                        {
                            minDepth = voronoiCells[i].depth + 1;
                            mdnID = nID;
                        }
                    }
                    voronoiCells[i].depth = minDepth;
                    voronoiCells[i].parent = mdnID;
                    if (j == voronoiCells.Count - 1) voronoiCells[mdnID].children.Add(i);
                }
            }

            foreach (VoronoiCell cell in voronoiCells)
            {
                foreach(int childID in cell.children)
                {
                    DrawLine(cell.Origin.position, voronoiPoints[childID].position, 0);
                    //Console.WriteLine("Drew line from {0} to {1}", point.position, voronoiPoints[childID].position);
                }
            }
            Console.WriteLine("made pathways");
        }
        public List<VoronoiPoint> GenerateVoronoiPoints(int maxDist, float distBetween, float maxDistIncrease, int maxTries)
        {
            bool[,] closeToPoint = new bool[width, height];
            VoronoiPoint StartPoint = new(new(width / 2, height / 2), 0, -1, 0);
            List<VoronoiPoint> PointList = new();
            PointList.Add(StartPoint);
            for (int x = StartPoint.position.X - (int)distBetween; x < StartPoint.position.X + distBetween; x++)
            {
                for (int y = StartPoint.position.Y - (int)distBetween; y < StartPoint.position.Y + distBetween; y++)
                {
                    if (StartPoint.position.DistDir(new(x, y)) < distBetween) closeToPoint[x, y] = true;
                }
            }
            closeToPoint[StartPoint.position.X, StartPoint.position.Y] = true;
            int fails = 0;
            while(fails < maxTries)
            {
                VoronoiPoint from = PointList[rand.Next(PointList.Count)];
                double radius = (rand.NextDouble() * maxDistIncrease + 1) * (distBetween);
                double angle = rand.NextDouble() * Math.PI * 2;
                Coord next = (from.position.Cubic + new Coord(radius, angle).Cubic).InGrid;
                bool invalidPoint = closeToPoint[next.X, next.Y] || (next.DistHex(StartPoint.position) > maxDist);
                if (invalidPoint) fails++;
                else
                {
                    for (int x = next.X - (int)distBetween; x < next.X + distBetween; x++)
                    {
                        for (int y = next.Y - (int)distBetween; y < next.Y + distBetween; y++)
                        {
                            if (next.DistDir(new(x, y)) < distBetween) closeToPoint[x, y] = true;
                        }
                    }
                    VoronoiPoint voronoiPoint = new(next, PointList.Count, from.ID, from.level + 1);
                    PointList[from.ID].children.Add(PointList.Count);
                    PointList.Add(voronoiPoint);
                    fails = 0;
                }

            }
            return PointList;
        }
    }
}
