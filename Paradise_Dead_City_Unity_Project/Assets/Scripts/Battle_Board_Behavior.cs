using System.Collections.Generic;
using UnityEngine;

public enum Game_State
{
    Player_1_Place_Spawn,
    Player_1_Place_Models,
    Player_2_Place_Spawn,
    Player_2_Place_Models,
    Gameplay
}

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

    [Header("Spawn Settings")]
    [SerializeField] private int Spawn_Zone_Width = 5;
    [SerializeField] private int Spawn_Zone_Depth = 2;

    [Header("Highlight Materials")]
    [SerializeField] private Material Valid_Spawn_Tile_Locations_Material;
    [SerializeField] private Material Model_Placement_Material;
    [SerializeField] private float Flash_Speed = 2.0f;
    [SerializeField] private float Flash_Min_Alpha = 0.3f;
    [SerializeField] private float Flash_Max_Alpha = 1.0f;

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

    // Spawn System
    private GameObject Player_1_Spawn_Tile;
    private GameObject Player_2_Spawn_Tile;
    private Vector2Int Player_1_Spawn_Position;
    private Vector2Int Player_2_Spawn_Position;
    private List<Vector2Int> Spawn_Tile_Highlights = new List<Vector2Int>();
    private List<Vector2Int> Model_Placement_Highlights = new List<Vector2Int>();
    private Material Current_Flash_Material;

    // Game State
    private Game_State Current_Phase = Game_State.Player_1_Place_Spawn;
    private Army_Composition_SO Player_1_Army;
    private Army_Composition_SO Player_2_Army;

    private void Awake()
    {
        Generate_All_Tiles(Tile_Size, Tile_Count_X, Tile_Count_Y);
        Assign_Team_Colors();

        // Clone the army compositions to avoid modifying the original ScriptableObjects
        if (Player_1_Faction.Army_Composition != null)
            Player_1_Army = Instantiate(Player_1_Faction.Army_Composition);
        if (Player_2_Faction.Army_Composition != null)
            Player_2_Army = Instantiate(Player_2_Faction.Army_Composition);

        Models = new Model_Standard_Behavior[Tile_Count_X, Tile_Count_Y];

        // Show initial spawn zone for Player 1
        Flash_Spawn_Tile(1, 0, 1);

        Debug.Log($"Game Started! Phase: {Current_Phase}. Player 1 place your spawn tile (Rows 0-1)");
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
            Vector2Int Hit_Position = Look_Up_Tile_Index(Info.transform.gameObject);

            // Handle hover effects
            Mouse_Hover_Effects(Hit_Position);

            // Handle different game phases
            switch (Current_Phase)
            {
                case Game_State.Player_1_Place_Spawn:
                    Place_Spawn_Tile_Handler(Hit_Position, 1, 0, 1);
                    break;

                case Game_State.Player_2_Place_Spawn:
                    Place_Spawn_Tile_Handler(Hit_Position, 2, 6, 7);
                    break;

                case Game_State.Player_1_Place_Models:
                    Spawn_Model(Hit_Position, 1, Player_1_Army, Player_1_Mat);
                    break;

                case Game_State.Player_2_Place_Models:
                    Spawn_Model(Hit_Position, 2, Player_2_Army, Player_2_Mat);
                    break;

                case Game_State.Gameplay:
                    Mouse_Control(Hit_Position, Ray);
                    break;
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
                Is_Dragging.Set_Position(Get_Tile_Center(Is_Dragging.Current_X, Is_Dragging.Current_Y));
                Is_Dragging = null;
            }
        }

        // Apply flashing effect every frame
        Flash_Highlighted_Tiles();
    }

    // Hover Effects
    private void Mouse_Hover_Effects(Vector2Int Hit_Position)
    {
        if (Current_Mouse_Hover == -Vector2Int.one)
        {
            Current_Mouse_Hover = Hit_Position;
            Tiles[Hit_Position.x, Hit_Position.y].layer = LayerMask.NameToLayer("Hover");
        }

        if (Current_Mouse_Hover != Hit_Position)
        {
            Tiles[Current_Mouse_Hover.x, Current_Mouse_Hover.y].layer = LayerMask.NameToLayer("Tile");
            Current_Mouse_Hover = Hit_Position;
            Tiles[Current_Mouse_Hover.x, Hit_Position.y].layer = LayerMask.NameToLayer("Hover");
        }
    }

    // Spawn Tile Placement Handler (with row validation)
    private void Place_Spawn_Tile_Handler(Vector2Int Hit_Position, int Player, int Min_Row, int Max_Row)
    {
        // Only allow placement on specified rows
        if (Hit_Position.x < Min_Row || Hit_Position.x > Max_Row)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Place_Spawn_Tile(Hit_Position, Player);
        }
    }

    // Spawn Tile Placement (actual placement logic)
    private void Place_Spawn_Tile(Vector2Int Position, int Player)
    {
        Faction_Data_SO Faction = Player == 1 ? Player_1_Faction : Player_2_Faction;

        if (Faction == null || Faction.Army_Composition == null)
        {
            Debug.LogError($"Faction or Army Composition missing for Player {Player}!");
            return;
        }

        GameObject Spawn_Tile_Prefab = Faction.Army_Composition.Spawn_Tile_Prefab;

        if (Spawn_Tile_Prefab == null)
        {
            Debug.LogError($"Spawn tile prefab missing for Player {Player}!");
            return;
        }

        GameObject Spawn_Tile = Instantiate(Spawn_Tile_Prefab, Get_Tile_Center(Position.x, Position.y), Quaternion.identity, transform);

        if (Player == 1)
        {
            Player_1_Spawn_Tile = Spawn_Tile;
            Player_1_Spawn_Position = Position;

            Clear_Spawn_Tiles_Flash();
            Show_Model_Placement_Tiles(Position, Player);

            Current_Phase = Game_State.Player_1_Place_Models;
            Debug.Log($"Player 1 spawn tile placed at {Position}. Phase: Player 1 place your models");
        }
        else
        {
            Player_2_Spawn_Tile = Spawn_Tile;
            Player_2_Spawn_Position = Position;

            Clear_Spawn_Tiles_Flash();
            Show_Model_Placement_Tiles(Position, Player);

            Current_Phase = Game_State.Player_2_Place_Models;
            Debug.Log($"Player 2 spawn tile placed at {Position}. Phase: Player 2 place your models");
        }
    }

    // Show spawn tile placement zone
    private void Flash_Spawn_Tile(int Player, int Min_Row, int Max_Row)
    {
        Clear_Spawn_Tiles_Flash();

        Current_Flash_Material = Valid_Spawn_Tile_Locations_Material;

        for (int X = Min_Row; X <= Max_Row; X++)
        {
            for (int Y = 0; Y < Tile_Count_Y; Y++)
            {
                Spawn_Tile_Highlights.Add(new Vector2Int(X, Y));
                if (Valid_Spawn_Tile_Locations_Material != null)
                    Tiles[X, Y].GetComponent<MeshRenderer>().material = Valid_Spawn_Tile_Locations_Material;
            }
        }

        Debug.Log($"Spawn tile zone shown for Player {Player} on rows {Min_Row}-{Max_Row}");
    }

    // Show model placement zone
    private void Show_Model_Placement_Tiles(Vector2Int Spawn_Position, int Player)
    {
        Clear_Model_Tiles_Flash();

        Current_Flash_Material = Model_Placement_Material;

        int Start_X = Spawn_Position.x;
        if (Player==1)
        {
            // Player 1 (rows 0-1): spawn zone goes backward toward row 0
            // If spawn on row 1, start at row 0
            // If spawn on row 0, stay at row 0
            Start_X = Mathf.Max(0, Spawn_Position.x - (Spawn_Zone_Depth - 1));
        }
        else
        {
            // Player 2 (rows 6-7): spawn zone goes backward toward row 6
            // If spawn on row 7, start at row 6
            // If spawn on row 6, stay at row 6
            Start_X = Mathf.Max(6, Spawn_Position.x - (Spawn_Zone_Depth - 1));
        }    

        int Start_Y = Mathf.Max(0, Spawn_Position.y - 2);

        for (int X = Start_X; X < Start_X + Spawn_Zone_Depth && X < Tile_Count_X; X++)
        {
            for (int Y = Start_Y; Y < Start_Y + Spawn_Zone_Width && Y < Tile_Count_Y; Y++)
            {
                Model_Placement_Highlights.Add(new Vector2Int(X, Y));

                if (Model_Placement_Material != null)
                    Tiles[X, Y].GetComponent<MeshRenderer>().material = Model_Placement_Material;
            }
        }

        Debug.Log($"Model placement zone shown for Player {Player} with {Model_Placement_Highlights.Count} tiles");
    }

    // Clear spawn tile highlights
    private void Clear_Spawn_Tiles_Flash()
    {
        foreach (Vector2Int Tile in Spawn_Tile_Highlights)
        {
            Tiles[Tile.x, Tile.y].GetComponent<MeshRenderer>().material = Tile_Material;
        }
        Spawn_Tile_Highlights.Clear();
    }

    // Clear model placement highlights
    private void Clear_Model_Tiles_Flash()
    {
        foreach (Vector2Int Tile in Model_Placement_Highlights)
        {
            Tiles[Tile.x, Tile.y].GetComponent<MeshRenderer>().material = Tile_Material;
        }
        Model_Placement_Highlights.Clear();
    }

    // Clear all highlights
    private void Clear_All_Flashes()
    {
        Clear_Spawn_Tiles_Flash();
        Clear_Model_Tiles_Flash();
    }

    // Flashing effect
    private void Flash_Highlighted_Tiles()
    {
        if (Current_Flash_Material == null)
            return;

        float Alpha = Mathf.Lerp(Flash_Min_Alpha, Flash_Max_Alpha,
            (Mathf.Sin(Time.time * Flash_Speed) + 1.0f) * 0.5f);

        // Apply to spawn tile highlights if in spawn placement phase
        if (Current_Phase == Game_State.Player_1_Place_Spawn || Current_Phase == Game_State.Player_2_Place_Spawn)
        {
            if (Spawn_Tile_Highlights.Count > 0)
            {
                foreach (Vector2Int Tile in Spawn_Tile_Highlights)
                {
                    Renderer Renderer = Tiles[Tile.x, Tile.y].GetComponent<MeshRenderer>();
                    if (Renderer != null && Renderer.material != null)
                    {
                        Color Color = Renderer.material.color;
                        Color.a = Alpha;
                        Renderer.material.color = Color;
                    }
                }
            }
        }
        // Apply to model highlights if in model placement phase
        else if (Current_Phase == Game_State.Player_1_Place_Models || Current_Phase == Game_State.Player_2_Place_Models)
        {
            if (Model_Placement_Highlights.Count > 0)
            {
                foreach (Vector2Int Tile in Model_Placement_Highlights)
                {
                    Renderer Renderer = Tiles[Tile.x, Tile.y].GetComponent<MeshRenderer>();
                    if (Renderer != null && Renderer.material != null)
                    {
                        Color Color = Renderer.material.color;
                        Color.a = Alpha;
                        Renderer.material.color = Color;
                    }
                }
            }
        }
    }

    // Model Placement
    private void Spawn_Model(Vector2Int Hit_Position, int Player, Army_Composition_SO Army, Material Team_Mat)
    {
        // Check if the clicked tile is in the highlighted zone
        if (!Model_Placement_Highlights.Contains(Hit_Position))
            return;

        if (Input.GetMouseButtonDown(0))
        {
            // Check if tile is already occupied
            if (Models[Hit_Position.x, Hit_Position.y] != null)
            {
                Debug.Log("Tile already occupied!");
                return;
            }

            // Check if army has models left
            if (Army == null || !Army.Has_Models_Left())
            {
                Debug.Log($"No more models to place for Player {Player}!");
                return;
            }

            // Place a random model from the army composition
            Model_Type Random_Type = Army.Get_Random_Model_Type();
            Model_Standard_Behavior Model = Spawn_Single_Model(Random_Type, Team_Mat,
                Player == 1 ? Player_1_Faction : Player_2_Faction, Player);

            if (Model != null)
            {
                Models[Hit_Position.x, Hit_Position.y] = Model;
                Position_Single_Model(Hit_Position.x, Hit_Position.y, true);

                Debug.Log($"Player {Player} placed {Random_Type}. Remaining: {Army.Get_Remaining_Count()} models");

                // Check if this was the last model
                if (!Army.Has_Models_Left())
                {
                    Debug.Log($"Player {Player} has no more models to place!");

                    // Move to next phase immediately
                    if (Player == 1)
                    {
                        Current_Phase = Game_State.Player_2_Place_Spawn;
                        Clear_Model_Tiles_Flash();
                        Flash_Spawn_Tile(2, 6, 7);
                        Debug.Log("Player 1 finished placing models. Phase: Player 2 place your spawn tile (Rows 6-7)");
                    }
                    else
                    {
                        Current_Phase = Game_State.Gameplay;
                        Clear_All_Flashes();
                        Debug.Log("All models placed! Gameplay begins!");
                    }
                }
            }
        }
    }

    // Gameplay
    private void Mouse_Control(Vector2Int Hit_Position, Ray Ray)
    {
        // If we press down on the mouse
        if (Input.GetMouseButtonDown(0))
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

            bool Valid_Move = Move_To(Is_Dragging, Hit_Position.x, Hit_Position.y);
            if (!Valid_Move)
            {
                Is_Dragging.Set_Position(Get_Tile_Center(Previous_Position.x, Previous_Position.y));
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

                float Current_Drag_Offset = Update_Drag_Offset(Mouse_Position);

                Is_Dragging.Set_Position(Mouse_Position + Vector3.up * Current_Drag_Offset);
            }
        }
    }

    // GENERATE BATTLE BOARD
    private void Generate_All_Tiles(float Tile_Size, int Tile_Count, int Tile_Count_Y)
    {
        Y_Offset += transform.position.y;
        Bounds = new Vector3((Tile_Count_X / 2.0f) * Tile_Size, 0, (Tile_Count_X / 2.0f) * Tile_Size) + Board_Center;

        Tiles = new GameObject[Tile_Count_X, Tile_Count_Y];
        for (int x = 0; x < Tile_Count_X; x++)
            for (int y = 0; y < Tile_Count_Y; y++)
                Tiles[x, y] = Generate_Single_Tiles(Tile_Size, x, y);
    }

    private GameObject Generate_Single_Tiles(float Tile_Size, int x, int y)
    {
        GameObject Tile_Object = new GameObject($"X:{x}, Y:{y}");
        Tile_Object.transform.parent = transform;

        Mesh Mesh = new Mesh();
        Tile_Object.AddComponent<MeshFilter>().mesh = Mesh;
        Tile_Object.AddComponent<MeshRenderer>().material = Tile_Material;

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
    private Model_Standard_Behavior Spawn_Single_Model(Model_Type Type, Material Team_Mat, Faction_Data_SO Faction, int Team_Number)
    {
        GameObject Prefab = Faction.Get_Prefab_By_Type(Type);

        if (Prefab == null)
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
            Material[] Materials = Renderer.materials;

            if (Materials.Length >= 2)
            {
                for (int i = 0; i < Materials.Length; i++)
                {
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

        Material[] Player_1_Materials = Player_1_Faction.Team_Materials;
        Material[] Player_2_Materials = Player_2_Faction.Team_Materials;

        bool Same_Faction = Player_1_Faction == Player_2_Faction;

        if (Same_Faction)
        {
            if (Player_1_Materials.Length < 2)
            {
                Player_1_Mat = Player_1_Materials[0];
                Player_2_Mat = Player_2_Materials[0];
                return;
            }

            List<int> Available_Indices = new List<int>();
            for (int i = 0; i < Player_1_Materials.Length; i++)
            {
                Available_Indices.Add(i);
            }

            int Player_1_Index = Random.Range(0, Available_Indices.Count);
            Player_1_Mat = Player_1_Materials[Available_Indices[Player_1_Index]];
            Available_Indices.RemoveAt(Player_1_Index);

            int Player_2_Index = Random.Range(0, Available_Indices.Count);
            Player_2_Mat = Player_2_Materials[Available_Indices[Player_2_Index]];
        }
        else
        {
            Player_1_Mat = Player_1_Materials[0];
            Player_2_Mat = Player_2_Materials[0];
        }
    }

    // Positioning
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

        return -Vector2Int.one;
    }

    private bool Move_To(Model_Standard_Behavior Model, int X, int Y)
    {
        Vector2Int Previous_Position = new Vector2Int(Model.Current_X, Model.Current_Y);

        if (Models[X, Y] != null)
        {
            return false;
        }

        Models[X, Y] = Model;
        Models[Previous_Position.x, Previous_Position.y] = null;

        Position_Single_Model(X, Y);

        return true;
    }

    private float Update_Drag_Offset(Vector3 Mouse_Position)
    {
        foreach (Model_Standard_Behavior Model in Models)
        {
            if (Model == null || Model == Is_Dragging)
                continue;
            float Distance = Vector3.Distance(Mouse_Position, Model.transform.position);
            if (Distance < Drag_Detect_Radius)
            {
                return Altered_Drag_Offset;
            }
        }
        return Normal_Drag_Offset;
    }
}