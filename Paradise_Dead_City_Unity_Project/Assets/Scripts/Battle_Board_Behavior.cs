using System.Collections.Generic;
using UnityEngine;

public class Battle_Board_Behavior : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private Material Tile_Material;
    [SerializeField] private float Tile_Size = 1.0f;
    [SerializeField] private float Y_Offset = 0.2f;
    [SerializeField] private Vector3 Board_Center = Vector3.zero;

    [Header("Prefabs and Tags")]
    [SerializeField] private GameObject[] Prefabs;
    [SerializeField] private Material[] Team_Materials;

    // Board Logic
    private const int Tile_Count_X = 8;
    private const int Tile_Count_Y = 8;
    private GameObject[,] Tiles;
    private Camera Main_Camera;
    private Vector2Int Current_Mouse_Hover;
    private Vector3 Bounds;

    // Models
    private Model_Standard_Behavior[,] Models;
    private Material Player_1_Mat;
    private Material Player_2_Mat;

    private void Awake()
    {
        Generate_All_Tiles(Tile_Size, Tile_Count_X, Tile_Count_Y);
        Assign_Team_Colors();
        Spawn_All_Models();
        Position_All_Models();
    }

    private void Update()
    {
        if (!Main_Camera)
        {
            Main_Camera = Camera.main;
            return;
        }

        RaycastHit Info;
        Ray Ray = Main_Camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(Ray, out Info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            // Get the tile index of the tile that was hit by the raycast
            Vector2Int Hit_Position = Look_Up_Tile_Index(Info.transform.gameObject);

            // If hovering over a tile after not hovering over any tile, change the layer of the tile to "Hover"
            if (Current_Mouse_Hover == -Vector2Int.one)
            {
                Current_Mouse_Hover = Hit_Position;
                Tiles[Hit_Position.x, Hit_Position.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If we were already hovering over a tile, change the previous one back to "Tile" and change the new one to "Hover"
            if (Current_Mouse_Hover != Hit_Position)
            {
                Tiles[Current_Mouse_Hover.x, Current_Mouse_Hover.y].layer = LayerMask.NameToLayer("Tile");
                Current_Mouse_Hover = Hit_Position;
                Tiles[Current_Mouse_Hover.x, Hit_Position.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
        else
        {
            if (Current_Mouse_Hover != -Vector2Int.one)
            {
                Tiles[Current_Mouse_Hover.x, Current_Mouse_Hover.y].layer = LayerMask.NameToLayer("Tile");
                Current_Mouse_Hover = -Vector2Int.one;
            }
        }
    }

    // GENERATE BATTLE BOARD

    // Generate all the tiles in the board
    private void Generate_All_Tiles(float Tile_Size, int Tile_Count, int Tile_Count_Y)
    {
        Y_Offset += transform.position.y;
        Bounds = new Vector3((Tile_Count_X / 2.0f) * Tile_Size, 0, (Tile_Count_X / 2.0f) * Tile_Size) + Board_Center;

        Tiles = new GameObject[Tile_Count_X, Tile_Count_Y];
        for (int x = 0; x < Tile_Count_X; x++)
            for (int y = 0; y < Tile_Count_Y; y++)
                Tiles[x, y] = Generate_Single_Tiles(Tile_Size, x, y);
    }

    // Generate a single tile at the given x and y coordinates
    private GameObject Generate_Single_Tiles(float Tile_Size, int x, int y)
    {
        GameObject Tile_Object = new GameObject($"X:{x}, Y:{y}");
        Tile_Object.transform.parent = transform;

        Mesh Mesh = new Mesh();
        Tile_Object.AddComponent<MeshFilter>().mesh = Mesh;
        Tile_Object.AddComponent<MeshRenderer>().material = Tile_Material;

        // Add vertices to every corner of the square
        Vector3[] Verticies = new Vector3[4];
        Verticies[0] = new Vector3(x * Tile_Size, Y_Offset, y * Tile_Size) - Bounds;
        Verticies[1] = new Vector3(x * Tile_Size, Y_Offset, (y + 1) * Tile_Size) - Bounds;
        Verticies[2] = new Vector3((x + 1) * Tile_Size, Y_Offset, y * Tile_Size) - Bounds;
        Verticies[3] = new Vector3((x + 1) * Tile_Size, Y_Offset, (y + 1) * Tile_Size) - Bounds;

        int[] Tris = new int[] { 0, 1, 2, 1, 3, 2 };

        Mesh.vertices = Verticies;
        Mesh.triangles = Tris;

        Mesh.RecalculateNormals();

        Tile_Object.layer = LayerMask.NameToLayer("Tile");
        Tile_Object.AddComponent<BoxCollider>();

        return Tile_Object;
    }

    // Spawn models
    private void Spawn_All_Models()
    {
        Models = new Model_Standard_Behavior[Tile_Count_X, Tile_Count_Y];

        // Player 1
        Models[0, 0] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat);
        Models[0, 1] = Spawn_Single_Model(Model_Type.Specialist_B, Player_1_Mat);
        Models[0, 2] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat);
        Models[0, 3] = Spawn_Single_Model(Model_Type.Axillary, Player_1_Mat);
        Models[0, 4] = Spawn_Single_Model(Model_Type.DeathHead, Player_1_Mat);
        Models[0, 5] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat);
        Models[0, 6] = Spawn_Single_Model(Model_Type.Specialist_B, Player_1_Mat);
        Models[0, 7] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat);
        for (int i = 0; i < Tile_Count_Y; i++)
            Models[1, i] = Spawn_Single_Model(Model_Type.Chaff, Player_1_Mat);

        // Player 2
        Models[7, 0] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat);
        Models[7, 1] = Spawn_Single_Model(Model_Type.Specialist_B, Player_2_Mat);
        Models[7, 2] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat);
        Models[7, 3] = Spawn_Single_Model(Model_Type.Axillary, Player_2_Mat);
        Models[7, 4] = Spawn_Single_Model(Model_Type.DeathHead, Player_2_Mat);
        Models[7, 5] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat);
        Models[7, 6] = Spawn_Single_Model(Model_Type.Specialist_B, Player_2_Mat);
        Models[7, 7] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat);
        for (int i = 0; i < Tile_Count_Y; i++)
            Models[6, i] = Spawn_Single_Model(Model_Type.Chaff, Player_2_Mat);
    }

    private Model_Standard_Behavior Spawn_Single_Model(Model_Type Type, Material Team_Mat)
    {
        Model_Standard_Behavior Model = Instantiate(Prefabs[(int)Type], transform).GetComponent<Model_Standard_Behavior>();

        Model.Type = Type;

        // Find renderer with materials
        Renderer[] Renderers = Model.GetComponentsInChildren<Renderer>();

        foreach (Renderer Renderer in Renderers)
        {
            // Get materials array
            Material[] Materials = Renderer.materials;

            // If there are at least 2 materials (base color + texture)
            if (Materials.Length >= 2)
            {
                if (Materials[1] != null && Materials[1].name.Contains("Base"))
                {
                    Materials[1] = new Material(Team_Mat);
                    Renderer.materials = Materials;
                    break;
                }
                
            }
            else if (Materials.Length == 1)
            {
                Renderer.material = Team_Mat;
            }
        }

            return Model;
    }

    // Model Colors
    private void Assign_Team_Colors()
    {
        if (Team_Materials == null || Team_Materials.Length < 2)
        {
            Debug.LogError("Team materials not assigned or insufficient materials.");
            return;
        }

        // Create list of avaliable materials
        List<int> Available_Mats = new List<int>();
        for (int i = 0; i < Team_Materials.Length; i++)
            Available_Mats.Add(i);

        int Player_1_Mat_Index = Random.Range(0, Available_Mats.Count);
        Player_1_Mat = Team_Materials[Available_Mats[Player_1_Mat_Index]];
        Available_Mats.RemoveAt(Player_1_Mat_Index);

        int Player_2_Mat_Index = Random.Range(0, Available_Mats.Count);
        Player_2_Mat = Team_Materials[Available_Mats[Player_2_Mat_Index]];
    }


    // Positioning
    private void Position_All_Models()
    {
        for (int x = 0; x < Tile_Count_X; x++)
            for (int y = 0; y < Tile_Count_Y; y++)
                if (Models[x, y] != null)
                    Position_Single_Model(x, y, true);
    }

    private void Position_Single_Model(int x, int y, bool Force = false)
    {
        Models[x, y].Current_X = x;
        Models[x, y].Current_Y = y;
        Models[x, y].transform.position = Get_Tile_Center(x, y);
    }

    private Vector3 Get_Tile_Center(int x, int y)
    {
        return new Vector3(x * Tile_Size, Y_Offset, y * Tile_Size) - Bounds + new Vector3(Tile_Size / 2, 0, Tile_Size / 2);
    }

    // Operations
    private Vector2Int Look_Up_Tile_Index(GameObject Hit_Info)
    {
        for (int x = 0; x < Tile_Count_X; x++)
            for (int y = 0; y < Tile_Count_Y; y++)
                if (Tiles[x, y] == Hit_Info)
                    return new Vector2Int(x, y);

        return -Vector2Int.one; // Invalid index if the tile is not found
    }

    // Debugging
}