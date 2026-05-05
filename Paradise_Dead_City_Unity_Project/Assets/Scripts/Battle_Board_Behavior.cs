using System.Collections.Generic;
using UnityEngine;

public class Battle_Board_Behavior : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private Material Tile_Material;
    [SerializeField] private float Tile_Size = 1.0f;
    [SerializeField] private float Y_Offset = 0.2f;
    [SerializeField] private Vector3 Board_Center = Vector3.zero;

    [Header("Drag Settings")]
    [SerializeField] private float Normal_Drag_Offset = 0.5f;
    [SerializeField] private float Altered_Drag_Offset = 1.0f;
    [SerializeField] private float Drag_Detect_Radius = 0.4f;

    [Header("Factions")]
    [SerializeField] private Faction_Data_SO Player_1_Faction;
    [SerializeField] private Faction_Data_SO Player_2_Faction;

    // Board Logic
    private const int Tile_Count_X = 8;
    private const int Tile_Count_Y = 8;
    private GameObject[,] Tiles;
    private Camera Main_Camera;
    private Vector2Int Current_Mouse_Hover;
    private Vector3 Bounds;

    // Models
    private Model_Standard_Behavior[,] Models;
    private Model_Standard_Behavior Is_Dragging;
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

            // If we press donw on the mouse
            if(Input.GetMouseButtonDown(0))
            {
                if (Models[Hit_Position.x, Hit_Position.y] != null)
                {
                    // Is it our turn?
                    if (true)

                    {
                        Is_Dragging = Models[Hit_Position.x, Hit_Position.y];
                    }
                }
            }

            // If we release the mouse button
            if (Is_Dragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int Previous_Position = new Vector2Int(Is_Dragging.Current_X, Is_Dragging.Current_Y);

                bool Valid_Move = Move_To(Is_Dragging, Hit_Position.x, Hit_Position.y); // Check if the move is valid based on game rules
                if (!Valid_Move)
                {
                    Is_Dragging.Set_Position(Get_Tile_Center(Previous_Position.x, Previous_Position.y)); // Move back to previous position
                    Is_Dragging = null;
                }
                else
                {
                    Is_Dragging = null;
                }
            }

            // If we are dragging a model, update its position to follow the mouse
            if (Is_Dragging)
            {
                Plane Horizontal_Plane = new Plane(Vector3.up, Vector3.up * Y_Offset);
                float Distance = 0.0f;
                if (Horizontal_Plane.Raycast(Ray, out Distance))
                {
                    Vector3 Mouse_Position = Ray.GetPoint(Distance);

                    // Determine which offset to use based on enviroment
                    float Current_Drag_Offset = Update_Drag_Offset(Mouse_Position);

                    Is_Dragging.Set_Position(Mouse_Position + Vector3.up * Current_Drag_Offset);
                }
            }

        }
        else
        {
            if (Current_Mouse_Hover != -Vector2Int.one)
            {
                Tiles[Current_Mouse_Hover.x, Current_Mouse_Hover.y].layer = LayerMask.NameToLayer("Tile");
                Current_Mouse_Hover = -Vector2Int.one;
            }

            if (Is_Dragging && Input.GetMouseButtonUp(0))
            {
                Is_Dragging.Set_Position(Get_Tile_Center(Is_Dragging.Current_X, Is_Dragging.Current_Y)); // Move back to previous position
                Is_Dragging = null;
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

    // SPAWN MODELS
    private void Spawn_All_Models()
    {
        Models = new Model_Standard_Behavior[Tile_Count_X, Tile_Count_Y];

        // Player 1
        Models[0, 0] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat, Player_1_Faction, 1);
        Models[0, 1] = Spawn_Single_Model(Model_Type.Specialist_B, Player_1_Mat, Player_1_Faction, 1);
        Models[0, 2] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat, Player_1_Faction, 1);
        Models[0, 3] = Spawn_Single_Model(Model_Type.Axillary, Player_1_Mat, Player_1_Faction, 1);
        Models[0, 4] = Spawn_Single_Model(Model_Type.DeathHead, Player_1_Mat, Player_1_Faction, 1);
        Models[0, 5] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat, Player_1_Faction, 1);
        Models[0, 6] = Spawn_Single_Model(Model_Type.Specialist_B, Player_1_Mat, Player_1_Faction, 1);
        Models[0, 7] = Spawn_Single_Model(Model_Type.Specialist_A, Player_1_Mat, Player_1_Faction, 1);
        for (int i = 0; i < Tile_Count_Y; i++)
            Models[1, i] = Spawn_Single_Model(Model_Type.Chaff, Player_1_Mat, Player_1_Faction, 1);

        // Player 2
        Models[7, 0] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat, Player_2_Faction, 2);
        Models[7, 1] = Spawn_Single_Model(Model_Type.Specialist_B, Player_2_Mat, Player_2_Faction, 2);
        Models[7, 2] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat, Player_2_Faction, 2);
        Models[7, 3] = Spawn_Single_Model(Model_Type.Axillary, Player_2_Mat, Player_2_Faction, 2);
        Models[7, 4] = Spawn_Single_Model(Model_Type.DeathHead, Player_2_Mat, Player_2_Faction, 2);
        Models[7, 5] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat, Player_2_Faction, 2);
        Models[7, 6] = Spawn_Single_Model(Model_Type.Specialist_B, Player_2_Mat, Player_2_Faction, 2);
        Models[7, 7] = Spawn_Single_Model(Model_Type.Specialist_A, Player_2_Mat, Player_2_Faction, 2);
        for (int i = 0; i < Tile_Count_Y; i++)
            Models[6, i] = Spawn_Single_Model(Model_Type.Chaff, Player_2_Mat, Player_2_Faction, 2);
    }

    private Model_Standard_Behavior Spawn_Single_Model(Model_Type Type, Material Team_Mat, Faction_Data_SO Faction, int Team_Number)
    {
        GameObject Prefab = Faction.Get_Prefab_By_Type(Type);

        if(Prefab == null)
        {
            Debug.LogError($"Prefab for {Type} not found in faction {Faction.Faction_Name}");
            return null;
        }

        Model_Standard_Behavior Model = Instantiate(Prefab, transform).GetComponent<Model_Standard_Behavior>();

        Model.Type = Type;
        Model.Team = Team_Number;

        // Find renderer with materials
        Renderer[] Renderers = Model.GetComponentsInChildren<Renderer>();

        foreach (Renderer Renderer in Renderers)
        {
            // Get materials array
            Material[] Materials = Renderer.materials;

            // If there are at least 2 materials (base color + texture)
            if (Materials.Length >= 2)
            {
                for (int i = 0; i < Materials.Length; i++)
                {
                    // Assign the team material to the first material slot
                    if (Materials[i] != null && Materials[i].name.Contains("Base"))
                    {
                        Materials[i] = new Material(Team_Mat);
                        Renderer.materials = Materials;
                    }
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
        if (Player_1_Faction == null || Player_2_Faction == null)
        {
            Debug.LogError("Factions not assigned!");
            return;
        }

        // Get team materials from the factions
        Material[] Player_1_Materials = Player_1_Faction.Team_Materials;
        Material[] Player_2_Materials = Player_2_Faction.Team_Materials;

        // Check if both players are using the same faction
        bool Same_Faction = Player_1_Faction == Player_2_Faction;

        if (Same_Faction)
        {
            if (Player_1_Materials.Length < 2)
            {
                Player_1_Mat = Player_1_Materials[0];
                Player_2_Mat = Player_2_Materials[0];
                return;
            }

            // Create a list of available indices for the materials
            List<int> Available_Indices = new List<int>();
            for (int i = 0; i < Player_1_Materials.Length; i++)
            {
                Available_Indices.Add(i);
            }

            // Player 1 randomly selects a material
            int Player_1_Index = Random.Range(0, Available_Indices.Count);
            Player_1_Mat = Player_1_Materials[Available_Indices[Player_1_Index]];
            Available_Indices.RemoveAt(Player_1_Index);

            // Player 2 randomly selects a material from the remaining options
            int Player_2_Index = Random.Range(0, Available_Indices.Count);
            Player_2_Mat = Player_2_Materials[Available_Indices[Player_2_Index]];
        }
        else
        {
            // If factions are different, just assign the first material from each faction
            Player_1_Mat = Player_1_Materials[0];
            Player_2_Mat = Player_2_Materials[0];
        }

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
        Models[x, y].Set_Position(Get_Tile_Center(x, y), Force);
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

    private bool Move_To(Model_Standard_Behavior Model, int X, int Y)
    {
        Vector2Int Previous_Position = new Vector2Int(Model.Current_X, Model.Current_Y);

        // Is there another peice in the way?
        if (Models[X, Y] != null)
        {
            return false;
        }

        Models[X,Y] = Model;
        Models[Previous_Position.x, Previous_Position.y] = null;

        Position_Single_Model(X, Y);

        return true; // For now, we assume all moves are valid. Implement game rules here.
    }

    private float Update_Drag_Offset(Vector3 Mouse_Position)
    {
        // Check if the mouse is over any other model
        foreach (Model_Standard_Behavior Model in Models)
        {
            if (Model == null || Model == Is_Dragging)
                continue;
            float Distance = Vector3.Distance(Mouse_Position, Model.transform.position);
            if (Distance < Drag_Detect_Radius)
            {
                return Altered_Drag_Offset; // Increase the drag offset to avoid overlapping
            }
        }
        return Normal_Drag_Offset; // Default drag offset
    }

    // Debugging
}