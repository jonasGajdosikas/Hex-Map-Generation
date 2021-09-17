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
            
            int width = 180;
            int height = 150;
            HexGrid grid = new(width, height);
            grid.GenerateVoronoiRooms(60, 10, 1f, 50);
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
            //Export("output//SmoothedMap.png");
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

            //TODO: connect inner cell rooms with each other and make connections with outside
            //will do this operation in make voronoi rooms
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
                    if (addedToRegion[x, y]) continue;
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
            public bool isVoronoi;
            public int cellID;
            public Region(List<Coord> _tiles, List<Coord> _edgeTiles, bool _isVoronoi = false, int _cellID = -1)
            {
                tiles = _tiles;
                edgeTiles = _edgeTiles;
                isVoronoi = _isVoronoi;
                if (isVoronoi) cellID = _cellID;
            }
            public Region()
            {
                tiles = new();
                edgeTiles = new();
            }
        }
        public void ConnectAllRegions(List<List<Region>> ClustersToConnect)
        {
            while (ClustersToConnect.Count > 1)
            {
                int bestDistance = -1;
                bool connectionFound = false;
                List<Region> bestClusterA = new(), bestClusterB = new();
                Region BRA = new(), BRB = new();
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
                                            BRA = roomA;
                                            BRB = roomB;
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
                    if (BRA.isVoronoi && BRB.isVoronoi)
                    {
                        voronoiCells[BRA.cellID].neighbors.Add(BRB.cellID);
                        voronoiCells[BRB.cellID].neighbors.Add(BRA.cellID);
                    }
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
            public List<int> neighbors;
            public List<int> borderLenths;
            public bool isBorder;
            public VoronoiCell(int[,] tilemap, VoronoiPoint _origin, List<Coord> _tiles)
            {
                Origin = _origin;
                tiles = _tiles;
                borderTiles = new();
                isBorder = false;
                neighbors = new();
                borderLenths = new();
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
            public int SharedBorderLength(int[,] tilemap, VoronoiCell other)
            {
                int otherID = other.Origin.ID;
                if (!neighbors.Contains(otherID)) return 0;
                int count = 0;
                foreach (Coord tile in borderTiles)
                {
                    foreach (Coord neighbor in tile.Neighbors)
                    {
                        if (tilemap[neighbor.X, neighbor.Y] == otherID)
                        {
                            count++;
                            break;
                        }
                    }
                }
                return count;
            }
        }
        public List<VoronoiCell> voronoiCells;
        public List<VoronoiPoint> voronoiPoints;
        public void GenerateVoronoiRooms(int centerRadius, float pointDistance, float maxDistIncrease, int maxTries) {
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) this[x, y] = 1;
            int[,] cells = new int[width, height];
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) cells[x, y] = -1;
            voronoiPoints = PoissantSamplePoints(centerRadius, pointDistance, maxDistIncrease, maxTries);
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
                foreach (Coord borderTile in cell.borderTiles)
                {
                    this[borderTile] = 1;
                    foreach (Coord neighbor in borderTile.Neighbors)
                    {
                        addedToRegion[neighbor.X, neighbor.Y] = true;
                    }
                }
            }
            foreach (VoronoiCell cell in voronoiCells)
            {
                foreach (int neighborID in cell.neighbors)
                {
                    if (neighborID == -1) cell.borderLenths.Add(0);
                    else cell.borderLenths.Add(cell.SharedBorderLength(cells, voronoiCells[neighborID]));
                }
            }
            //modified room connection algorithm to connect by longest shared border
            List<List<int>> cellClusters = new();
            for (int i = 0; i < voronoiCells.Count; i++)
            {
                cellClusters.Add(new List<int> { i });
            }
            while(cellClusters.Count > 1)
            {
                List<int> bestClusterA = new(), bestClusterB = new();
                int bestCellAID = 0, bestCellBID = 0, bestSharedBorder = -1;
                foreach(List<int> cluster in cellClusters)
                {
                    foreach(int cell in cluster)
                    {
                        for (int i = 0; i < voronoiCells[cell].neighbors.Count; i++)
                        {
                            int neighborCell = voronoiCells[cell].neighbors[i];
                            if (neighborCell == -1 || cluster.Contains(neighborCell)) continue;
                            if (bestSharedBorder < voronoiCells[cell].borderLenths[i])
                            {
                                bestClusterA = cluster;
                                bestCellAID = cell;
                                bestCellBID = neighborCell;
                                bestSharedBorder = voronoiCells[cell].borderLenths[i];
                                foreach (List<int> clusterB in cellClusters) if (clusterB.Contains(neighborCell)) { bestClusterB = clusterB; break; }
                            }
                        }
                    }
                }
                if (bestSharedBorder != -1)
                {
                    cellClusters.Remove(bestClusterA);
                    cellClusters.Remove(bestClusterB);
                    bestClusterA.AddRange(bestClusterB);
                    cellClusters.Add(bestClusterA);
                    DrawLine(voronoiCells[bestCellAID].Origin.position, voronoiCells[bestCellBID].Origin.position);
                }
            }

        }
        public List<VoronoiPoint> PoissantSamplePoints(int maxDist, float distBetween, float maxDistIncrease, int maxTries)
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
        
        public void DrawHexagon(Coord pos, int radius, int value)
        {
            for(int X = pos.X - radius; X <= pos.X + radius; X++)
            {
                for (int Y = pos.Y - radius; Y <= pos.Y + radius; Y++)
                {
                    if (pos.DistHex(new(X, Y)) <= radius) this[X, Y] = value;
                }
            }
        }
    }
}
