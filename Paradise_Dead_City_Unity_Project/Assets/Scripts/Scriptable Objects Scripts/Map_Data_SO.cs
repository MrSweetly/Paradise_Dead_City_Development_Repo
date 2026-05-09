using System;
using UnityEngine;

public enum Board_Modifiers_Enum
{
    None,
    Cover,
    Wall,
    Terrain,
    Hazard,
    Spawn
}

[CreateAssetMenu(fileName = "New Map Data", menuName = "Scriptable Objects/Map Data")]
public class Map_Data_SO : ScriptableObject
{
    public string Map_Name;

    [Header("Map Layout (Rows 2-5 Only)")]
    public Map_Row[] Rows = new Map_Row[2];

    private Board_Modifiers[,] Full_Grid;

    public Board_Modifiers[,] Get_Full_Grid()
    {
        if (Full_Grid == null || Full_Grid.GetLength(0) != 8)
        {
            Generate_Full_Grid();
        }
        return Full_Grid;
    }

    public Board_Modifiers Get_Tile_Type(int X, int Y)
    {
        if (Full_Grid == null)
        {
            Generate_Full_Grid();
        }

        if (X < 0 || X >= 8 || Y < 0 || Y >= 8)
        {
            Debug.LogError($"Get_Tile_Type: Coordinates ({X}, {Y}) are out of bounds for map '{Map_Name}'.");
            return Board_Modifiers.None;
        }
        return Full_Grid[X, Y];
    }

    public bool Is_Tile_Passable(int X, int Y)
    {
        Board_Modifiers Type = Get_Tile_Type(X, Y);
        return Type != Board_Modifiers.Wall;
    }

    public bool Is_Tile_Hazardous(int X, int Y)
    {
        return Get_Tile_Type(X, Y) == Board_Modifiers.Hazard;
    }

    public bool Is_Tile_Cover(int X, int Y)
    {
        return Get_Tile_Type(X, Y) == Board_Modifiers.Cover;
    }

    public bool Is_Tile_Difficult_Terrain(int X, int Y)
    {
        return Get_Tile_Type(X, Y) == Board_Modifiers.Terrain;
    }

    private void Generate_Full_Grid()
    {
        Full_Grid = new Board_Modifiers[8, 8];

        for (int X = 0; X < 2; X++)
        {
            for (int Y = 0; Y < 8; Y++)
            {
                Full_Grid[X, Y] = Board_Modifiers.None;
            }
        }

        for (int X = 2; X < 4; X++)
        {
            int Designer_Row = X - 2;
            if (Designer_Row < Rows.Length && Rows[Designer_Row] != null && Rows[Designer_Row].Tiles != null)
            {
                for (int Y = 0; Y < 8; Y++)
                {
                    if (Y < Rows[Designer_Row].Tiles.Length)
                    {
                        Full_Grid[X, Y] = Rows[Designer_Row].Tiles[Y];
                    }
                    else
                    {
                        Full_Grid[X, Y] = Board_Modifiers.None;
                    }
                }
            }
            else
            {
                for (int Y = 0; Y < 8; Y++)
                {
                    Full_Grid[X, Y] = Board_Modifiers.None;
                }
            }
        }

        for (int X = 4; X < 6; X++)
        {
            int Mirrored_Row = 7 - X;
            for (int Y = 0; Y < 8; Y++)
            {
                Full_Grid[X, Y] = Full_Grid[Mirrored_Row, Y];
            }
        }

        for (int X = 6; X < 8; X++)
        {
            for (int Y = 0; Y < 8; Y++)
            {
                Full_Grid[X, Y] = Board_Modifiers.None;
            }
        }
    }

    public void Clear_Map()
    {
        Rows = new Map_Row[2];
        for (int i = 0; i < Rows.Length; i++)
        {
            Rows[i] = new Map_Row();
        }
        Full_Grid = null;
    }

    private void OnValidate()
    {
        if (Rows == null || Rows.Length != 2)
        {
            Rows = new Map_Row[2];
        }

        for (int i = 0; i < Rows.Length; i++)
        {
            if (Rows[i] == null)
            {
                Rows[i] = new Map_Row();
            }

            if (Rows[i].Tiles == null || Rows[i].Tiles.Length != 8)
            {
                Rows[i].Tiles = new Board_Modifiers[8];
            }
        }

        Full_Grid = null;
    }
}

[Serializable]
public class Map_Row
{
    public Board_Modifiers[] Tiles = new Board_Modifiers[8];
}