using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSettings
{
    public GameSettings(int mazeSize, int nbOfColors, int seed)
    {
        _mazeSize = mazeSize;
        _nbOfColors = nbOfColors;
        _seed = seed;
    }

    public GameSettings(){}

    private int _seed;
    private int _mazeSize;
    private int _nbOfColors;

    public int Seed
    {
        get { return _seed; }
        set { _seed = value; }
    }
    public int MazeSize
    {
        get { return _mazeSize; }
        set { _mazeSize = value; }
    }
    public int NbOfColors
    {
        get { return _nbOfColors; }
        set { _nbOfColors = value; }
    }
}


