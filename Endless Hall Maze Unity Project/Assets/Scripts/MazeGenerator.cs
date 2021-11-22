using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static GameMacros;

public class MazeGenerator
{
    private bool restart = false;


    public Maze3D GenerateMaze(int mazeSize, int nbOfColors, int nbOfTraps)
    {
        Debug.Assert(nbOfColors * 2 <= (mazeSize * mazeSize) - nbOfTraps);

        Maze2D maze2D;
        Maze3D maze3D;

        do
        {
            restart = false;
            maze2D = CreateBasicStructure(mazeSize);
            RemoveWalls(maze2D);
            ConnectIsolatedRooms(maze2D);
            maze3D = CreateAdvancedStructure(maze2D);

            TransformCrossroadsIntoBridges(maze3D);
            while (!ConnectConnectedComponents(maze3D) && ! restart) ;

            if (!restart)
                PlaceItems(maze3D, nbOfColors);

            while (!ConnectConnectedComponents(maze3D) && ! restart) ;


            if (! restart)
                TestMaze(maze3D);
            
        } while (restart);
        // When connecting separated components, we only connect rooms that have less than 3 set edges.
        // That way, we avoid making bridges, which by their properties have a tendency to create additional separated components.
        // However, there exists a (really) small chance that we cannot connect components cause there are to many built bridges.
        // Since it's rare, we can just restart the whole maze creation process over.
        //
        // Btw, the more "int openChancePercentage" in "void RemoveWalls()" grows, the more the chance a restart happens grows.

        return maze3D;
    }

    public Maze2D CreateBasicStructure(int mazeSize)
    {
        Debug.Assert(mazeSize % 2 == 0);
        Room2D[,] roomArray = new Room2D[mazeSize, mazeSize];
        for (int j = 0; j < mazeSize; j++)
        {
            for (int i = 0; i < mazeSize; i++)
                roomArray[j, i] = new Room2D();
        }
        return new Maze2D(mazeSize, roomArray);
    }


    public Maze3D CreateAdvancedStructure(Maze2D maze2D)
    {
        return new Maze3D(maze2D);
    }

    public void RemoveWalls(Maze2D maze)
    {
        int openChancePercentage = 30;
        for (int j = 0; j < maze.MazeSize; j++)
        {
            for (int i = 0; i < maze.MazeSize; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    if (Random.Range(0, 100) < openChancePercentage)
                        LinkRooms(maze, j, i, k);
                }
            }
        }
    }


    public void TestMaze(Maze3D maze3D)
    {
        Debug.Assert(TestBridge(maze3D), "TestBridge");
    }

    public bool TestBridge(Maze3D maze3D)
    {
        for (int j = 0; j<maze3D.MazeSize; j++)
        {
            for (int i = 0; i<maze3D.MazeSize; i++)
            {
                if (maze3D.TestIsRoomBugged(j, i))
                    return false;
            }
        }
        return true;
    }


    public void LinkRooms(Maze2D maze, int j, int i, int side)
    {
        // -- Assertions -- //
        Debug.Assert(side >= 0 && side < 4);
        Debug.Assert(j >= 0 && j < maze.MazeSize);
        Debug.Assert(i >= 0 && i < maze.MazeSize);

        int tempY;
        int tempX;
        switch (side)
        {
            case 0: // North
                maze.GetRoom(j, i).IsNorthOpen = true;
                tempY = maze.GetNorthY(j, i);
                tempX = maze.GetNorthX(j, i);
                maze.GetRoom(tempY, tempX).IsSouthOpen = true;
                break;

            case 1: // South
                maze.GetRoom(j, i).IsSouthOpen = true;
                tempY = maze.GetSouthY(j, i);
                tempX = maze.GetSouthX(j, i);
                maze.GetRoom(tempY, tempX).IsNorthOpen = true;
                break;

            case 2: // West
                maze.GetRoom(j, i).IsWestOpen = true;
                tempY = maze.GetWestY(j, i);
                tempX = maze.GetWestX(j, i);
                maze.GetRoom(tempY, tempX).IsEastOpen = true;
                break;

            case 3: // East
                maze.GetRoom(j, i).IsEastOpen = true;
                tempY = maze.GetEastY(j, i);
                tempX = maze.GetEastX(j, i);
                maze.GetRoom(tempY, tempX).IsWestOpen = true;
                break;
        }
    }

    public bool TransformCrossroadsIntoBridges(Maze3D maze3D)
    {
        bool wasTransformed = false;
        List<(int, int)> crossroadRooms = new List<(int, int)>();
        for (int j = 0; j < maze3D.MazeSize; j++)
        {
            for (int i = 0; i < maze3D.MazeSize; i++)
                if (maze3D.IsRoomCrossroad(j, i))
                    crossroadRooms.Add((j, i));
        }
        while (crossroadRooms.Count > 0)
        {
            wasTransformed = true;
            int r = Random.Range(0, crossroadRooms.Count);
            int tempJ = crossroadRooms[r].Item1;
            int tempI = crossroadRooms[r].Item2;
            TransformCrossroadIntoBridge(maze3D, tempJ, tempI);
            crossroadRooms.RemoveAt(r);
        }
        return wasTransformed;
    }

    private void TransformCrossroadIntoBridge(Maze3D maze3D, int j, int i)
    {
        Room3D weBridge = new Room3D();
        weBridge.WestRoom = maze3D.GetWestRoom(0, j, i);
        weBridge.EastRoom = maze3D.GetEastRoom(0, j, i);
        maze3D.GetWestRoom(0, j, i).EastRoom = weBridge;
        maze3D.GetEastRoom(0, j, i).WestRoom = weBridge;
        maze3D.GetRoom(0, j, i).WestRoom = null;
        maze3D.GetRoom(0, j, i).EastRoom = null;
        maze3D.AddWestEastBridge(j, i, weBridge);
    }


    public void LinkRooms(Maze3D maze3D, (int, int, int) mainRoomCoords, (int, int, int) sideRoomCoords, int side)
    {
        int k1 = mainRoomCoords.Item1;
        int j1 = mainRoomCoords.Item2;
        int i1 = mainRoomCoords.Item3;
        int k2 = sideRoomCoords.Item1;
        int j2 = sideRoomCoords.Item2;
        int i2 = sideRoomCoords.Item3;

        if (side == NORTH)
        {
            maze3D.GetRoom(k1, j1, i1).NorthRoom = maze3D.GetRoom(k2, j2, i2);
            maze3D.GetRoom(k2, j2, i2).SouthRoom = maze3D.GetRoom(k1, j1, i1);
        }

        else if (side == SOUTH)
        {
            maze3D.GetRoom(k1, j1, i1).SouthRoom = maze3D.GetRoom(k2, j2, i2);
            maze3D.GetRoom(k2, j2, i2).NorthRoom = maze3D.GetRoom(k1, j1, i1);
        }

        else if (side == WEST)
        {
            maze3D.GetRoom(k1, j1, i1).WestRoom = maze3D.GetRoom(k2, j2, i2);
            maze3D.GetRoom(k2, j2, i2).EastRoom = maze3D.GetRoom(k1, j1, i1);
        }

        else if (side == EAST)
        {
            maze3D.GetRoom(k1, j1, i1).EastRoom = maze3D.GetRoom(k2, j2, i2);
            maze3D.GetRoom(k2, j2, i2).WestRoom = maze3D.GetRoom(k1, j1, i1);
        }
    }


    private List<HashSet<Room3D>> GetConnectedComponents(Maze3D maze3D)
    {
        List<HashSet<Room3D>> foundComponents = new List<HashSet<Room3D>>();

        List<Room3D> roomList = maze3D.GetRoomList();
        foreach (Room3D room3D in roomList)
        {
            if (! room3D.IsTrap)
            {
                HashSet<Room3D> visitedRoooms = new HashSet<Room3D>();
                ExploreRoom(room3D, visitedRoooms);
                bool found = false;
                foreach (HashSet<Room3D> roomSet in foundComponents)
                {
                    if (visitedRoooms.SetEquals(roomSet))
                        found = true;
                }
                if (!found)
                    foundComponents.Add(visitedRoooms);
            }
        }
        return foundComponents; 
    }

    private void ExploreRoom(Room3D roomToExplore, HashSet<Room3D> visitedRoooms)
    {
        if (roomToExplore.IsTrap)
            return;

        visitedRoooms.Add(roomToExplore);
        if (roomToExplore.IsNorthOpen() && ! visitedRoooms.Contains(roomToExplore.NorthRoom))
            ExploreRoom(roomToExplore.NorthRoom, visitedRoooms);
        if (roomToExplore.IsSouthOpen() && ! visitedRoooms.Contains(roomToExplore.SouthRoom))
            ExploreRoom(roomToExplore.SouthRoom, visitedRoooms);
        if (roomToExplore.IsWestOpen() && ! visitedRoooms.Contains(roomToExplore.WestRoom))
            ExploreRoom(roomToExplore.WestRoom, visitedRoooms);
        if (roomToExplore.IsEastOpen() && ! visitedRoooms.Contains(roomToExplore.EastRoom))
            ExploreRoom(roomToExplore.EastRoom, visitedRoooms);
    }


    private bool ConnectConnectedComponents(Maze3D maze3D)
    {
        bool finished = false;
        List<HashSet<Room3D>> connectedComponents = GetConnectedComponents(maze3D);

        if (connectedComponents.Count == 1)
            finished = true;

        if (! finished)
        {
            List<HashSet<Room3D>> sortedConnectedComponents = connectedComponents.OrderByDescending(o => o.Count).ToList();

            int c = 0;
            bool stop = false;
            while (!stop)
            {
                stop = ConnectComponents(maze3D, sortedConnectedComponents[0], sortedConnectedComponents[c + 1]);
                c++;

                if (! stop && c + 1 >= sortedConnectedComponents.Count)
                {
                    Debug.Log("Couldn't connect components, c out of range. (lol)");
                    restart = true;
                    break;
                }
            }
        }
       
        return finished;
    }


    private bool ConnectComponents(Maze3D maze3D, HashSet<Room3D> biggestComponent, HashSet<Room3D> component)
    {
        bool connected = false;
        List<Room3D> biggestComponentList = biggestComponent.ToList();
        List<Room3D> componentList = component.ToList();
        List<(int, int, int)> potentialRooms = new List<(int, int, int)>();

        for (int x = 0; x < biggestComponentList.Count; x++)
        {
            for (int y = 0; y < componentList.Count; y++)
            {
                if (maze3D.AreRoomsAdjacent(biggestComponentList[x], componentList[y]))
                {
                    if (! maze3D.IsRoomBridgeOrCross(biggestComponentList[x]) && ! maze3D.IsRoomBridgeOrCross(componentList[y]))
                    {
                        int size = biggestComponentList[x].GetNumberOfEdges() + componentList[y].GetNumberOfEdges();
                        potentialRooms.Add((size, x, y));
                    }
                }
            }
        }

        if (potentialRooms.Count > 0)
        {
            List<(int, int, int)> bestRooms = new List<(int, int, int)>();
            int smallestValue = int.MaxValue;
            for (int i = 0; i < potentialRooms.Count; i++)
            {
                if (potentialRooms[i].Item1 < smallestValue)
                    smallestValue = potentialRooms[i].Item1;
            }
            foreach ((int, int, int) roomCoords in potentialRooms)
            {
                if (roomCoords.Item1 == smallestValue)
                    bestRooms.Add(roomCoords);
            }

            int r = Random.Range(0, bestRooms.Count);
            Room3D room1 = biggestComponentList[bestRooms[r].Item2];
            Room3D room2 = componentList[bestRooms[r].Item3];
            int side = maze3D.WhereRoomsAdjacent(room1, room2);
            Debug.Assert(side != -1);
            (int, int, int) room1Coords = maze3D.GetRoomCoords(room1);
            (int, int, int) room2Coords = maze3D.GetRoomCoords(room2);
            LinkRooms(maze3D, room1Coords, room2Coords, side);
            connected = true;
        }
        return connected;
    }


    public void ConnectIsolatedRooms(Maze2D maze2D)
    {
        /* Check if exists way to (0,0) */
        /* If not, delete one wall and start the scan over (important to start over) */

        bool stop = false;
        while (!stop)
        {
            stop = true;
            for (int j = 0; j < maze2D.MazeSize; j++)
            {
                for (int i = 0; i < maze2D.MazeSize; i++)
                {
                    while (!IsConnectedToOrigin(maze2D, j, i) && !maze2D.GetRoom(j, i).IsCrossroad())
                    {
                        RemoveOneWall(maze2D, j, i);
                        stop = false;
                    }
                }
            }
        }
    }

    public bool IsConnectedToOrigin(Maze2D maze2D, int j, int i)
    {
        bool[,] visitedRooms = new bool[maze2D.MazeSize, maze2D.MazeSize];
        for (int b = 0; b < maze2D.MazeSize; b++)
        {
            for (int a = 0; a < maze2D.MazeSize; a++)
                visitedRooms[b, a] = false;
        }
        bool connected = ExploreRoom(maze2D, visitedRooms, j, i);
        return connected;
    }

    public bool ExploreRoom(Maze2D maze2D, bool[,] visitedRooms, int j, int i)
    {
        visitedRooms[j, i] = true;

        if (j == 0 && i == 0)
            return true;

        bool foundNorth = false;
        bool foundSouth = false;
        bool foundWest = false;
        bool foundEast = false;

        if (maze2D.GetRoom(j, i).IsNorthOpen && ! visitedRooms[maze2D.GetNorthY(j, i), maze2D.GetNorthX(j, i)])
        {
            foundNorth = ExploreRoom(maze2D, visitedRooms, maze2D.GetNorthY(j, i), maze2D.GetNorthX(j, i));
        }

        if (maze2D.GetRoom(j, i).IsSouthOpen && ! visitedRooms[maze2D.GetSouthY(j, i), maze2D.GetSouthX(j, i)])
        {
            foundSouth = ExploreRoom(maze2D, visitedRooms, maze2D.GetSouthY(j, i), maze2D.GetSouthX(j, i));
        }

        if (maze2D.GetRoom(j, i).IsWestOpen && ! visitedRooms[maze2D.GetWestY(j, i), maze2D.GetWestX(j, i)])
        {
            foundWest = ExploreRoom(maze2D, visitedRooms, maze2D.GetWestY(j, i), maze2D.GetWestX(j, i));
        }

        if (maze2D.GetRoom(j, i).IsEastOpen && ! visitedRooms[maze2D.GetEastY(j, i), maze2D.GetEastX(j, i)])
        {
            foundEast = ExploreRoom(maze2D, visitedRooms, maze2D.GetEastY(j, i), maze2D.GetEastX(j, i));
        }
        return foundNorth || foundSouth || foundWest || foundEast;
        
    }

    public void RemoveOneWall(Maze2D maze, int j, int i)
    {
        Debug.Assert(! maze.GetRoom(j, i).IsCrossroad());

        if (!maze.GetRoom(j, i).IsNorthOpen)
        {
            LinkRooms(maze, j, i, NORTH);
        }

        else if (!maze.GetRoom(j, i).IsSouthOpen)
        {
            LinkRooms(maze, j, i, SOUTH);
        }
        
        else if (!maze.GetRoom(j, i).IsWestOpen)
        {
            LinkRooms(maze, j, i, WEST);
        }
        
        else if (!maze.GetRoom(j, i).IsEastOpen)
        {
            LinkRooms(maze, j, i, EAST);
        }
    }
    


    public void PlaceItems(Maze3D maze3D, int nbOfColors)
    {
        int tempK, tempJ, tempI;

        List<(int, int, int)> availableRooms = new List<(int, int, int)>();
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < maze3D.MazeSize; j++)
            {
                for (int i = 0; i < maze3D.MazeSize; i++)
                    if (maze3D.GetRoom(k, j, i) != null)
                        availableRooms.Add((k,j,i));
            }
        }

        // -- TRAP ROOM -- //
        int r = Random.Range(0, availableRooms.Count);
        tempK = availableRooms[r].Item1;
        tempJ = availableRooms[r].Item2;
        tempI = availableRooms[r].Item3;
        maze3D.GetRoom(tempK, tempJ, tempI).IsTrap = true;
        availableRooms.RemoveAt(r);

        // -- OBJECTIVE ROOM -- //
        for (int i = 1; i < nbOfColors + 1; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                r = Random.Range(0, availableRooms.Count);
                tempK = availableRooms[r].Item1;
                tempJ = availableRooms[r].Item2;
                tempI = availableRooms[r].Item3;
                maze3D.GetRoom(tempK, tempJ, tempI).Color = i;
                if (j == 0)
                    maze3D.GetRoom(tempK, tempJ, tempI).HasOrb = true;
                else
                    maze3D.GetRoom(tempK, tempJ, tempI).HasRune = true;
                availableRooms.RemoveAt(r);
            }
        }
    }
}


public class Maze3D
{
    /*
     * Rooms in [0, MazeSize * MazeSize[ : normal and crossroad NS
     * Rooms in [MazeSize * MazeSize, MazeSize * MazeSize * 2[ : crossroad WE
    */

    readonly Room3D[,,] rooms;
    readonly private int mazeSize;
    public int MazeSize
    {
        get { return mazeSize; }
    }

    public Room3D GetRoom(int k, int j, int i)
    {
        return rooms[k, j, i];
    }

    public (int, int, int) GetRoomCoords(Room3D room)
    {
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < mazeSize; j++)
            {
                for (int i = 0; i < mazeSize; i++)
                {
                    if (GetRoom(k, j, i) == room)
                        return (k, j, i);
                }
            }
        }
        return (-1, -1, -1);
    }

    public void AddWestEastBridge(int j, int i, Room3D room)
    {
        rooms[1, j, i] = room;
    }


    public bool IsRoomBridgeOrCross(Room3D room3D)
    {
        bool ans = false;
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < mazeSize; j++)
            {
                for (int i = 0; i < mazeSize; i++)
                {
                    if (rooms[k,j,i] == room3D)
                    {
                        if (k == 1 || rooms[1, j, i] != null)
                        {
                            ans = true;
                            break;
                        }
                            
                    }
                }
            }
        }
        return ans;
    }

    public bool AreRoomsAdjacent(Room3D room1, Room3D room2)
    {
        return WhereRoomsAdjacent(room1, room2) != -1;
    }

    public int WhereRoomsAdjacent(Room3D room1, Room3D room2)
    {
        int ans = -1;
        (int, int, int) coord1 = GetRoomCoords(room1);
        (int, int, int) coord2 = GetRoomCoords(room2);
        if ((GetNorthY(coord1.Item2, coord1.Item3), GetNorthX(coord1.Item2, coord1.Item3)) == (coord2.Item2, coord2.Item3))
            ans = NORTH;
        else if ((GetSouthY(coord1.Item2, coord1.Item3), GetSouthX(coord1.Item2, coord1.Item3)) == (coord2.Item2, coord2.Item3))
            ans = SOUTH;
        else if ((GetWestY(coord1.Item2, coord1.Item3), GetWestX(coord1.Item2, coord1.Item3)) == (coord2.Item2, coord2.Item3))
            ans = WEST;
        else if ((GetEastY(coord1.Item2, coord1.Item3), GetEastX(coord1.Item2, coord1.Item3)) == (coord2.Item2, coord2.Item3))
            ans = EAST;
        return ans;
    }

    public Room3D GetNorthRoom(int k, int j, int i)
    {
        return rooms[k, j, i].NorthRoom;
    }
    public Room3D GetSouthRoom(int k, int j, int i)
    {
        return rooms[k, j, i].SouthRoom;
    }
    public Room3D GetWestRoom(int k, int j, int i)
    {
        return rooms[k, j, i].WestRoom;
    }
    public Room3D GetEastRoom(int k, int j, int i)
    {
        return rooms[k, j, i].EastRoom;
    }
    public void SetNorthRoom(int k, int j, int i, Room3D room)
    {
        rooms[k, j, i].NorthRoom = room;
    }
    public void SetSouthRoom(int k, int j, int i, Room3D room)
    {
        rooms[k, j, i].SouthRoom = room;
    }
    public void SetWestRoom(int k, int j, int i, Room3D room)
    {
        rooms[k, j, i].WestRoom = room;
    }
    public void SetEastRoom(int k, int j, int i, Room3D room)
    {
        rooms[k, j, i].EastRoom = room;
    }

    public bool IsRoomBridge(int j, int i)
    {
        return rooms[1, j, i] != null;
    }

    public bool IsRoomCrossroad(int j, int i)
    {
        return rooms[0, j, i].IsNorthOpen() &&
            rooms[0, j, i].IsSouthOpen() &&
            rooms[0, j, i].IsWestOpen() &&
            rooms[0, j, i].IsEastOpen();
    }


    public bool TestIsRoomBugged(int j, int i)
    {
        return IsRoomBridge(j, i) &&
            (GetRoom(0, j, i).IsWestOpen() ||
            GetRoom(0, j, i).IsEastOpen() ||
            GetRoom(1, j, i).IsNorthOpen() ||
            GetRoom(1, j, i).IsSouthOpen());
    }


    public List<Room3D> GetRoomList()
    {
        List<Room3D> roomList = new List<Room3D>();
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < mazeSize; j++)
            {
                for (int i = 0; i < mazeSize; i++)
                {
                    if (GetRoom(k, j, i) != null)
                        roomList.Add(rooms[k, j, i]);
                }
                    
            }
        }
        return roomList;
    }

    public List<(int, int, int)> GetRoomCoordList()
    {
        List<(int, int, int)> roomCoordList = new List<(int, int, int)>();
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < MazeSize; j++)
            {
                for (int i = 0; i < MazeSize; i++)
                {
                    if (GetRoom(k, j, i) != null)
                        roomCoordList.Add((k, j, i));
                }
            }
        }
        return roomCoordList;
    }
    

    

    public Maze3D(Maze2D maze2D)
    {
        // -- ROOM LIST INIT -- //
        mazeSize = maze2D.MazeSize;
        rooms = new Room3D[2, mazeSize, mazeSize];
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < mazeSize; j++)
            {
                for (int i = 0; i < mazeSize; i++)
                    rooms[k, j, i] = null;
            }
        }

        // -- FIRST LEVEL INIT -- //
        for (int j = 0; j < mazeSize; j++)
        {
            for (int i = 0; i < mazeSize; i++)
                rooms[0, j, i] = new Room3D();
        }


        // -- SECOND LEVEL INIT (WE CROSSROAD) -- //
        for (int j = 0; j < mazeSize; j++)
        {
            for (int i = 0; i < mazeSize; i++)
            {
                if (maze2D.GetRoom(j, i).IsCrossroad())
                    rooms[1, j, i] = new Room3D();
            }
        }

        // -- LINKING ROOMS -- //
        for (int j = 0; j < maze2D.MazeSize; j++)
        {
            for (int i = 0; i < maze2D.MazeSize; i++)
            {

                // NORTH
                if (maze2D.GetRoom(j, i).IsNorthOpen)
                    rooms[0, j, i].NorthRoom = rooms[0, GetNorthY(j, i), GetNorthX(j, i)];

                // SOUTH
                if (maze2D.GetRoom(j, i).IsSouthOpen)
                    rooms[0, j, i].SouthRoom = rooms[0, GetSouthY(j, i), GetSouthX(j, i)];


                if (maze2D.GetRoom(j, i).IsCrossroad())
                {
                    // WEST
                    if (maze2D.GetWestRoom(j, i).IsCrossroad())
                    {
                        rooms[1, j, i].WestRoom = rooms[1, GetWestY(j, i), GetWestX(j, i)];
                    } 
                    else
                    {
                        rooms[1, j, i].WestRoom = rooms[0, GetWestY(j, i), GetWestX(j, i)];
                    }
                        

                    // EAST
                    if (maze2D.GetEastRoom(j, i).IsCrossroad())
                    {
                        rooms[1, j, i].EastRoom = rooms[1, GetEastY(j, i), GetEastX(j, i)];
                    } 
                    else
                    {
                        rooms[1, j, i].EastRoom = rooms[0, GetEastY(j, i), GetEastX(j, i)];
                    }
                        
                }
                else
                {
                    // WEST
                    if (maze2D.GetWestRoom(j, i).IsCrossroad())
                    {
                        rooms[0, j, i].WestRoom = rooms[1, GetWestY(j, i), GetWestX(j, i)];
                    } 
                    else
                    {
                        if (maze2D.GetRoom(j, i).IsWestOpen)
                            rooms[0, j, i].WestRoom = rooms[0, GetWestY(j, i), GetWestX(j, i)];
                    }
                        

                    // EAST
                    if (maze2D.GetEastRoom(j, i).IsCrossroad())
                    {
                        rooms[0, j, i].EastRoom = rooms[1, GetEastY(j, i), GetEastX(j, i)];
                    }  
                    else
                    {
                        if (maze2D.GetRoom(j, i).IsEastOpen)
                            rooms[0, j, i].EastRoom = rooms[0, GetEastY(j, i), GetEastX(j, i)];
                    }
                        
                }
            }
        }
    }

    // -- UTILITIES -- //
    public int GetNorthY(int y, int x)
    {
        if (y == 0)
            return MazeSize - 1;
        else
            return y - 1;
    }
    public int GetNorthX(int y, int x)
    {
        if (y == 0)
        {
            if (x < MazeSize / 2)
                return x + (MazeSize / 2);
            else
                return x - (MazeSize / 2);
        }
        else
            return x;

    }
    public int GetSouthY(int y, int x)
    {
        if (y == MazeSize - 1)
            return 0;
        else
            return y + 1;
    }
    public int GetSouthX(int y, int x)
    {
        if (y == MazeSize - 1)
        {
            if (x < MazeSize / 2)
                return x + (MazeSize / 2);
            else
                return x - (MazeSize / 2);
        }
        else
            return x;
    }
    public int GetWestY(int y, int x)
    {
        if (x == 0)
        {
            if (y < MazeSize / 2)
                return y + (MazeSize / 2);
            else
                return y - (MazeSize / 2);
        }
        else
            return y;
    }
    public int GetWestX(int y, int x)
    {
        if (x == 0)
            return MazeSize - 1;
        else
            return x - 1;
    }
    public int GetEastY(int y, int x)
    {
        if (x == MazeSize - 1)
        {
            if (y < MazeSize / 2)
                return y + (MazeSize / 2);
            else
                return y - (MazeSize / 2);
        }
        else
            return y;
    }
    public int GetEastX(int y, int x)
    {
        if (x == MazeSize - 1)
            return 0;
        else
            return x + 1;
    }


    // -- Debug -- //
    public void PrintInConsole()
    {
        string textMaze = "";
        for (int j = 0; j < MazeSize; j++)
        {
            for (int s = 0; s < 3; s++)
            {
                string line = "";
                for (int i = 0; i < MazeSize; i++)
                {

                    if (IsRoomBridge(j, i))
                    {
                        if (s == 0)
                            line += "X";
                        if (s == 1)
                        {
                            if (GetRoom(0, j, i).IsTrap || GetRoom(1, j, i).IsTrap)
                                line += "<color=yellow>";
                            else if (GetRoom(0, j, i).IsSpecial() || GetRoom(1, j, i).IsSpecial())
                                line += "<color=red>";
                            line += "X";
                            if (GetRoom(0, j, i).IsSpecial() || GetRoom(1, j, i).IsSpecial())
                                line += "</color>";
                        }
                            
                        if (s == 2)
                            line += "X";
                    }
                    else
                    {
                        if (s == 0)
                        {
                            if (GetRoom(0, j, i).IsNorthOpen()) { line += "N"; }
                            else { line += "O"; }
                        }
                        if (s == 2)
                        {
                            if (GetRoom(0, j, i).IsSouthOpen()) { line += "S"; }
                            else { line += "O"; }
                        }
                        if (s == 1)
                        {
                            if (GetRoom(0, j, i).IsTrap)
                                line += "<color=yellow>";
                            else if (GetRoom(0, j, i).IsSpecial())
                                line += "<color=red>";

                            if (GetRoom(0, j, i).IsWestOpen() && GetRoom(0, j, i).IsEastOpen())
                            { line += "B"; }
                            else if (GetRoom(0, j, i).IsWestOpen())
                            { line += "W"; }
                            else if (GetRoom(0, j, i).IsEastOpen())
                            { line += "E"; }
                            else
                            { line += "O"; }

                            if (GetRoom(0, j, i).IsSpecial())
                                line += "</color>";
                        }
                    }

                    
                }
                textMaze += line;
                textMaze += "\n";
            }
            textMaze += "\n";
        }
        Debug.Log(textMaze);
    }
}


public class Room3D
{
    private int color = COLOR_NULL;

    private bool hasOrb = false;
    private bool hasRune = false;
    private bool isTrap = false;

    private Room3D northRoom = null;
    private Room3D southRoom = null;
    private Room3D westRoom = null;
    private Room3D eastRoom = null;

    public Room3D()
    {
    }

    public int GetNumberOfEdges()
    {
        int i = 0;

        if (NorthRoom != null)
            i++;
        if (SouthRoom != null)
            i++;
        if (WestRoom != null)
            i++;
        if (EastRoom != null)
            i++;

        return i;
    }

    public Room3D NorthRoom
    {
        get { return northRoom; }
        set { northRoom = value; }
    }
    public Room3D SouthRoom
    {
        get { return southRoom; }
        set { southRoom = value; }
    }
    public Room3D WestRoom
    {
        get { return westRoom; }
        set { westRoom = value; }
    }
    public Room3D EastRoom
    {
        get { return eastRoom; }
        set { eastRoom = value; }
    }

    public int Color
    {
        get { return color; }
        set { color = value; }
    }
    public bool HasOrb
    {
        get { return hasOrb; }
        set { hasOrb = value; }
    }
    public bool HasRune
    {
        get { return hasRune; }
        set { hasRune = value; }
    }
    public bool IsTrap
    {
        get { return isTrap; }
        set { isTrap = value; }
    }

    public bool IsNorthOpen()
    {
        return NorthRoom != null;
    }
    public bool IsSouthOpen()
    {
        return SouthRoom != null;
    }
    public bool IsWestOpen()
    {
        return WestRoom != null;
    }
    public bool IsEastOpen()
    {
        return EastRoom != null;
    }

    public bool IsSpecial()
    {
        return HasOrb || HasRune || IsTrap;
    }
}



public class Maze2D
{
    readonly Room2D[,] roomArray;
    readonly int mazeSize;
    public int MazeSize
    {
        get { return mazeSize; }
    }

    // -- Constructor -- //
    public Maze2D(int size, Room2D[,] rooms)
    {
        mazeSize = size;
        roomArray = rooms;
    }

    // -- Methods -- //
    public Room2D GetRoom(int y, int x)
    {
        Debug.Assert(AreCoordsValid(y, x));
        return roomArray[y, x];
    }

    public Room2D GetNorthRoom(int y, int x)
    {
        return roomArray[GetNorthY(y,x), GetNorthX(y,x)];
    }
    public Room2D GetSouthRoom(int y, int x)
    {
        return roomArray[GetSouthY(y, x), GetSouthX(y, x)];
    }
    public Room2D GetWestRoom(int y, int x)
    {
        return roomArray[GetWestY(y, x), GetWestX(y, x)];
    }
    public Room2D GetEastRoom(int y, int x)
    {
        return roomArray[GetEastY(y, x), GetEastX(y, x)];
    }


    public bool AreCoordsValid(int y, int x)
    {
        return y >= 0 && y < mazeSize && x >= 0 && x < mazeSize;
    }

    public int GetNorthY(int y, int x)
    {
        AreCoordsValid(y, x);
        if (y == 0)
            return MazeSize - 1;
        else
            return y - 1;
    }
    public int GetNorthX(int y, int x)
    {
        AreCoordsValid(y, x);
        if (y == 0)
        {
            if (x < MazeSize / 2)
                return x + (MazeSize / 2);
            else
                return x - (MazeSize / 2);
        }
        else
            return x;

    }
    public int GetSouthY(int y, int x)
    {
        AreCoordsValid(y, x);
        if (y == MazeSize - 1)
            return 0;
        else
            return y + 1;
    }
    public int GetSouthX(int y, int x)
    {
        AreCoordsValid(y, x);
        if (y == MazeSize - 1)
        {
            if (x < MazeSize / 2)
                return x + (MazeSize / 2);
            else
                return x - (MazeSize / 2);
        }
        else
            return x;
    }
    public int GetWestY(int y, int x)
    {
        AreCoordsValid(y, x);
        if (x == 0)
        {
            if (y < MazeSize / 2)
                return y + (MazeSize / 2);
            else
                return y - (MazeSize / 2);
        }
        else
            return y;
    }
    public int GetWestX(int y, int x)
    {
        AreCoordsValid(y, x);
        if (x == 0)
            return MazeSize - 1;
        else
            return x - 1;
    }
    public int GetEastY(int y, int x)
    {
        AreCoordsValid(y, x);
        if (x == MazeSize - 1)
        {
            if (y < MazeSize / 2)
                return y + (MazeSize / 2);
            else
                return y - (MazeSize / 2);
        }
        else
            return y;
    }
    public int GetEastX(int y, int x)
    {
        AreCoordsValid(y, x);
        if (x == MazeSize - 1)
            return 0;
        else
            return x + 1;
    }

}


public class Room2D
{
    // -- Methods -- //
    public bool IsCrossroad()
    {
        return IsNorthOpen && IsSouthOpen && IsWestOpen && IsEastOpen;
    }

    // -- Walls -- //
    private bool isNorthOpen = false;
    private bool isSouthOpen = false;
    private bool isWestOpen = false;
    private bool isEastOpen = false;
    public bool IsNorthOpen
    {
        get { return isNorthOpen; }
        set { isNorthOpen = value; }
    }
    public bool IsSouthOpen
    {
        get { return isSouthOpen; }
        set { isSouthOpen = value; }
    }
    public bool IsWestOpen
    {
        get { return isWestOpen; }
        set { isWestOpen = value; }
    }
    public bool IsEastOpen
    {
        get { return isEastOpen; }
        set { isEastOpen = value; }
    }

    // -- Constructor -- //
    public Room2D()
    {

    }
}
