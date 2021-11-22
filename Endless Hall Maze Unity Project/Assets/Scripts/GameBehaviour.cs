using System.Collections.Generic;
using UnityEngine;
using TMPro;

using static GameMacros;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class GameBehaviour : MonoBehaviour
{
    public bool debug;
    public int mazeSize = 8;
    public int nbOfColors = 5;

    public GameObject northGate;
    public GameObject southGate;
    public GameObject westGate;
    public GameObject eastGate;

    public GameObject orb;
    public GameObject rune;

    public TMP_Text timeText;
    public TMP_Text progressText;
    public TMP_Text equipText;

    private Maze3D maze;
    private Player player;
    private PlayerHandler playerHandler;
    private MazeHandler mazeHandler;

    private float elapsedTime;
    private float endTime;

    private bool finished = false;
    private bool lost = false;

    public int seed = -1;

    // Start is called before the first frame update
    void Start()
    {
        Init();
        elapsedTime = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateUI();
    }

    public void Init()
    {
        LoadSettings();
        SetRandomSeed();
        maze = new MazeGenerator().GenerateMaze(mazeSize, nbOfColors, 1);
        player = new Player(0, Random.Range(0, mazeSize - 1), Random.Range(0, mazeSize - 1));
        playerHandler = new PlayerHandler(maze, player);
        mazeHandler = new MazeHandler(maze);

        if (debug)
        {
            maze.PrintInConsole();
            Debug.Log("Player start position :" + player.YPos + " " + player.XPos);
        }

        UpdateRoom();
        UpdateUI();
    }


    public void LoadSettings()
    {
        if (File.Exists(Application.persistentDataPath + "/playerInfo.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/playerInfo.dat", FileMode.Open);
            GameSettings data = (GameSettings) bf.Deserialize(file);
            file.Close();
            if (data.MazeSize >= 0) // TODO verify aberrant values 
                mazeSize = data.MazeSize;
            if (data.NbOfColors >= 0) // TODO verify aberrant values 
                nbOfColors = data.NbOfColors;
            if (data.Seed >= 0)
                seed = data.Seed;
        }

        // https://answers.unity.com/questions/1457625/saving-data-for-a-class.html
    }

    private void SetRandomSeed()
    {
        if (seed == -1)
            seed = Random.Range(0, 99999);
        Random.InitState(seed);
    }

    public void FinishGame()
    {
        endTime = elapsedTime;
        finished = true;
        Debug.Log("GG WP!");
    }

    public void YouLoose()
    {
        lost = true;
        Debug.Log("You n00b!");
    }


    public void UpdateUI()
    {
        int timeSeconds = (int) elapsedTime;
        if (timeSeconds >= 1)
        {
            int equippedColor = playerHandler.GetEquippedOrb();
            if (finished)
                timeText.text = "" + (int) endTime;
            else if (! lost)
                timeText.text = "" + timeSeconds;
            progressText.text = nbOfColors - mazeHandler.GetNumberLeftColors() + "/" + nbOfColors;
            if (finished)
                equipText.text = "GG WP!";
            else if (lost)
                equipText.text = "You lost lol. Here's the seed if you want to start over: " + seed;
            else if (equippedColor == COLOR_NULL)
                equipText.text = "";
            else
                equipText.text = ColorHandler.GetColorName(equippedColor);
        } 
    }


    public void MovePlayer(int dir)
    {
        if (dir == NORTH)
            playerHandler.MovePlayerNorth();
        else if (dir == SOUTH)
            playerHandler.MovePlayerSouth();
        else if (dir == WEST)
            playerHandler.MovePlayerWest();
        else if (dir == EAST)
            playerHandler.MovePlayerEast();
        else
            Debug.Assert(false, "MovePlayer: unknown direction");
        if (playerHandler.GetCurrentRoom().IsTrap)
            playerHandler.RandomlyTeleport();
        UpdateRoom();
        if (debug)
            playerHandler.PrintInConsole();
    }

    public void CollectOrb(int color)
    {
        playerHandler.EquipOrb(color);
        Debug.Log("CollectOrb: Equipped " + color);
    }

    public void FillRune(int color)
    {
        Debug.Log("FillRune: Rune" + color + " Orb" + playerHandler.GetEquippedOrb());
        if (playerHandler.GetEquippedOrb() == color)
        {
            Debug.Log("FillRune: MATCH");
            mazeHandler.DeleteColor(color);
            playerHandler.DropOrb();
            UpdateRoom();
            if (mazeHandler.GetNumberLeftColors() == 0)
                FinishGame();       
        }
        else
        {
            Debug.Log("FillRune: NO MATCH");
        }
    }

    public void UpdateRoom()
    {
        // ROOM RESET //
        EnableAllWalls();
        DisableObjective();
        // ADAPTING ROOM //
        DisableWalls();
        EnableObjective();
    }

    public void DisableWalls()
    {
        if (playerHandler.IsOpen(NORTH))
            northGate.SetActive(false);
        if (playerHandler.IsOpen(SOUTH))
            southGate.SetActive(false);
        if (playerHandler.IsOpen(WEST))
            westGate.SetActive(false);
        if (playerHandler.IsOpen(EAST))
            eastGate.SetActive(false);
    }

    public void EnableAllWalls()
    {
        northGate.SetActive(true);
        southGate.SetActive(true);
        westGate.SetActive(true);
        eastGate.SetActive(true);
    }
    public void EnableObjective()
    {
        if (playerHandler.GetCurrentRoom().Color > COLOR_NULL)
        {
            if (playerHandler.GetCurrentRoom().HasOrb)
            {
                orb.gameObject.SetActive(true);
                ColorHandler.ChangeColor(orb, playerHandler.GetCurrentRoom().Color);
            }
            else if (playerHandler.GetCurrentRoom().HasOrb)
            {
                rune.gameObject.SetActive(true);
                ColorHandler.ChangeColor(rune, playerHandler.GetCurrentRoom().Color);
            }  
        }
    }
    public void DisableObjective()
    {
        orb.gameObject.SetActive(false);
        rune.gameObject.SetActive(false);
    }
}

public class MazeHandler
{
    readonly Maze3D m;
    public MazeHandler(Maze3D maze)
    {
        m = maze;
    }

    public void DeleteColor(int color)
    {
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < m.MazeSize; j++)
            {
                for (int i = 0; i < m.MazeSize; i++)
                {
                    if (m.GetRoom(k, j, i) != null && m.GetRoom(k, j, i).Color == color)
                    {
                        m.GetRoom(k, j, i).HasOrb = false;
                        m.GetRoom(k, j, i).HasRune = false;
                    }
                }
            }
        }    
    }

    public int GetNumberLeftColors()
    {
        HashSet<int> foundColors = new HashSet<int>();
        for (int k = 0; k < 2; k++)
        {
            for (int j = 0; j < m.MazeSize; j++)
            {
                for (int i = 0; i < m.MazeSize; i++)
                {
                    if (m.GetRoom(k, j, i) != null)
                        foundColors.Add(m.GetRoom(k, j, i).Color);
                }   
            }
        }
            
        foundColors.Remove(COLOR_NULL);
        return foundColors.Count;
    }

}

public class PlayerHandler
{
    readonly Maze3D m;
    readonly Player p;
    public PlayerHandler(Maze3D maze, Player player)
    {
        m = maze;
        p = player;
    }

    public Room3D GetCurrentRoom()
    {
        return m.GetRoom(p.ZPos, p.YPos, p.XPos);
    }

    public bool IsOpen(int dir)
    {
        if (dir == NORTH)
            return m.GetRoom(p.ZPos, p.YPos, p.XPos).IsNorthOpen();
        else if (dir == SOUTH)
            return m.GetRoom(p.ZPos, p.YPos, p.XPos).IsSouthOpen();
        else if (dir == WEST)
            return m.GetRoom(p.ZPos, p.YPos, p.XPos).IsWestOpen();
        else
            return m.GetRoom(p.ZPos, p.YPos, p.XPos).IsEastOpen();
    }

    
    public void MovePlayerNorth()
    {
        (int, int, int) newCoords = m.GetRoomCoords(GetCurrentRoom().NorthRoom);
        p.ZPos = newCoords.Item1;
        p.YPos = newCoords.Item2;
        p.XPos = newCoords.Item3;

    }
    public void MovePlayerSouth()
    {
        (int, int, int) newCoords = m.GetRoomCoords(GetCurrentRoom().SouthRoom);
        p.ZPos = newCoords.Item1;
        p.YPos = newCoords.Item2;
        p.XPos = newCoords.Item3;
    }
    public void MovePlayerWest()
    {
        (int, int, int) newCoords = m.GetRoomCoords(GetCurrentRoom().WestRoom);
        p.ZPos = newCoords.Item1;
        p.YPos = newCoords.Item2;
        p.XPos = newCoords.Item3;
    }
    public void MovePlayerEast()
    {
        (int, int, int) newCoords = m.GetRoomCoords(GetCurrentRoom().EastRoom);
        p.ZPos = newCoords.Item1;
        p.YPos = newCoords.Item2;
        p.XPos = newCoords.Item3;
    }

    public void RandomlyTeleport()
    {
        List<Room3D> roomList = m.GetRoomList();
        roomList.Remove(GetCurrentRoom());
        int r = Random.Range(0, roomList.Count);
        (int,int,int) newCoords = m.GetRoomCoords(roomList[r]);
        p.ZPos = newCoords.Item1;
        p.YPos = newCoords.Item2;
        p.XPos = newCoords.Item3;
    }

    public void DropOrb()
    {
        p.EquippedOrb = COLOR_NULL;
    }

    public void EquipOrb(int color)
    {
        p.EquippedOrb = color;
    }

    public int GetEquippedOrb()
    {
        return p.EquippedOrb;
    }


    // -- Debug -- //
    public void PrintInConsole()
    {
        string textMaze = "";
        for (int j = 0; j < m.MazeSize; j++)
        {
            for (int s = 0; s < 3; s++)
            {
                string line = "";
                for (int i = 0; i < m.MazeSize; i++)
                {

                    if (m.IsRoomBridge(j, i))
                    {
                        if (s == 0)
                            line += "X";
                        if (s == 1)
                        {
                            if (p.YPos == j && p.XPos == i)
                                line += "<color=green>";
                            else if (m.GetRoom(0, j, i).IsTrap || m.GetRoom(1, j, i).IsTrap)
                                line += "<color=yellow>";
                            else if (m.GetRoom(0, j, i).IsSpecial() || m.GetRoom(1, j, i).IsSpecial())
                                line += "<color=red>";
                            line += "X";
                            if (m.GetRoom(0, j, i).IsSpecial() || m.GetRoom(1, j, i).IsSpecial() || (p.YPos == j && p.XPos == i))
                                line += "</color>";
                        }
                        if (s == 2)
                            line += "X";
                    }
                    else
                    {
                        if (s == 0)
                        {
                            if (m.GetRoom(0, j, i).IsNorthOpen()) { line += "N"; }
                            else { line += "O"; }
                        }
                        if (s == 2)
                        {
                            if (m.GetRoom(0, j, i).IsSouthOpen()) { line += "S"; }
                            else { line += "O"; }
                        }
                        if (s == 1)
                        {
                            if (p.YPos == j && p.XPos == i)
                                line += "<color=green>";
                            else if (m.GetRoom(0, j, i).IsTrap)
                                line += "<color=yellow>";
                            else if (m.GetRoom(0, j, i).IsSpecial())
                                line += "<color=red>";

                            if (m.GetRoom(0, j, i).IsWestOpen() && m.GetRoom(0, j, i).IsEastOpen())
                            { line += "B"; }
                            else if (m.GetRoom(0, j, i).IsWestOpen())
                            { line += "W"; }
                            else if (m.GetRoom(0, j, i).IsEastOpen())
                            { line += "E"; }
                            else
                            { line += "O"; }

                            if (m.GetRoom(0, j, i).IsSpecial() || (p.YPos == j && p.XPos == i))
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


public class Player
{
    private int zPos;
    private int yPos;
    private int xPos;
    public int ZPos
    {
        get { return zPos; }
        set { zPos = value; }
    }
    public int YPos
    {
        get { return yPos; }
        set { yPos = value; }
    }
    public int XPos
    {
        get { return xPos; }
        set { xPos = value; }
    }

    public Player(int z, int y, int x)
    {
        ZPos = z;
        YPos = y;
        XPos = x;
    }

    private int equippedOrb = COLOR_NULL;
    public int EquippedOrb
    {
        get { return equippedOrb; }
        set { equippedOrb = value; }
    }

}

public static class ColorHandler
{
    public static void ChangeColor(GameObject gameObject, int color)
    {
        Debug.Assert(gameObject.GetComponent<ObjectInteraction>() != null);

        if (color == COLOR_ONE)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_ONE;
        }
        else if (color == COLOR_TWO)
        { 
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_TWO;
        }
        else if (color == COLOR_THREE)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.cyan);
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_THREE;
        }
        else if (color == COLOR_FOUR)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", new Color(0.93f, 0.509f, 0.74f, 1)); //pink
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_FOUR;
        }
        else if (color == COLOR_FIVE)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.magenta);
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_FIVE;
        }
        else if (color == COLOR_SIX)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 0.51f, 0f, 1)); //orange
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_SIX;
        }
        else if (color == COLOR_SEVEN)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_SEVEN;
        }
        else if (color == COLOR_EIGHT)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_EIGHT;
        }
        else if (color == COLOR_NINE)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", new Color(0.415f, 0.247f, 0f, 1)); //brown
            gameObject.GetComponent<ObjectInteraction>().Color = COLOR_NINE;
        }
    }

    public static string GetColorName(int color)
    {
        if (color == COLOR_ONE)
        {
            return "GREEN";
        }
        else if (color == COLOR_TWO)
        {
            return "YELLOW";
        }
        else if (color == COLOR_THREE)
        {
            return "CYAN";
        }
        else if (color == COLOR_FOUR)
        {
            return "PINK";
        }
        else if (color == COLOR_FIVE)
        {
            return "MAGENTA";
        }
        else if (color == COLOR_SIX)
        {
            return "ORANGE";
        }
        else if (color == COLOR_SEVEN)
        {
            return "BLUE";
        }
        else if (color == COLOR_EIGHT)
        {
            return "RED";
        }
        else if (color == COLOR_NINE)
        {
            return "BROWN";
        }
        else
        {
            return "ERROR: UNKNOW COLOR";
        }
    }
}
