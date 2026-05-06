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
    [SerializeField] private float Drag_Hold_Time = 0.2f;

    [Header("Factions")]
    [SerializeField] private Faction_Data_SO Player_1_Faction;
    [SerializeField] private Faction_Data_SO Player_2_Faction;

    [Header("Spawn Settings")]
    [SerializeField] private int Spawn_Zone_Width = 5;
    [SerializeField] private int Spawn_Zone_Depth = 2;

    [Header("Highlight Materials")]
    [SerializeField] private Material Valid_Spawn_Tile_Locations_Material;
    [SerializeField] private Material Model_Placement_Material;
    [SerializeField] private Material Movement_Range_Material;
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
    private Vector3 Board_Origin; // NEW: Stores the world position of tile (0,0)

    // Models
    private Model_Standard_Behavior[,] Models;
    private Model_Standard_Behavior Is_Dragging;
    private Material Player_1_Mat;
    private Material Player_2_Mat;

    // Dragging Logic
    private Model_Standard_Behavior Selected_Model;
    private float Current_Hold_Time = 0f;
    private bool Is_Holding = false;
    private Vector2Int Hold_Start_Position;

    // Spawn System
    private GameObject Player_1_Spawn_Tile;
    private GameObject Player_2_Spawn_Tile;
    private Vector2Int Player_1_Spawn_Position;
    private Vector2Int Player_2_Spawn_Position;
    private List<Vector2Int> Spawn_Tile_Highlights = new List<Vector2Int>();
    private List<Vector2Int> Model_Placement_Highlights = new List<Vector2Int>();
    private List<Vector2Int> Movement_Range_Highlights = new List<Vector2Int>();
    private Material Current_Flash_Material;
    private Model_Standard_Behavior Current_Selected_Model;

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
            // CHANGED: Use math-based tile detection instead of iterating through all tiles
            Vector2Int Hit_Position = Get_Tile_From_World_Position(Info.point);

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

    private Vector2Int Get_Tile_From_World_Position(Vector3 World_Position)
    {
        // Convert world position to position relative to the board origin (bottom-left of tile 0,0)
        Vector3 Relative_Pos = World_Position - Board_Origin;

        // Divide by tile size to get the tile index
        int X = Mathf.FloorToInt(Relative_Pos.x / Tile_Size);
        int Y = Mathf.FloorToInt(Relative_Pos.z / Tile_Size);

        // Clamp to board bounds
        X = Mathf.Clamp(X, 0, Tile_Count_X - 1);
        Y = Mathf.Clamp(Y, 0, Tile_Count_Y - 1);

        return new Vector2Int(X, Y);
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

        int Start_X;
        int Start_Y;

        if (Player == 1)
        {
            Start_X = Mathf.Max(0, Spawn_Position.x - (Spawn_Zone_Depth - 1));
        }
        else
        {
            Start_X = Mathf.Max(6, Spawn_Position.x - (Spawn_Zone_Depth - 1));
        }

        int Half_Width = Spawn_Zone_Width / 2;
        Start_Y = Spawn_Position.y - Half_Width;

        if (Start_Y < 0)
            Start_Y = 0;
        if (Start_Y + Spawn_Zone_Width > Tile_Count_Y)
            Start_Y = Tile_Count_Y - Spawn_Zone_Width;

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

        // Handle different highlight types based on current game phase
        if (Current_Phase == Game_State.Player_1_Place_Spawn || Current_Phase == Game_State.Player_2_Place_Spawn)
        {
            Flash_Highlight_Group(Spawn_Tile_Highlights, Alpha);
        }
        else if (Current_Phase == Game_State.Player_1_Place_Models || Current_Phase == Game_State.Player_2_Place_Models)
        {
            Flash_Highlight_Group(Model_Placement_Highlights, Alpha);
        }
        else if (Current_Phase == Game_State.Gameplay && Movement_Range_Highlights.Count > 0)
        {
            Flash_Highlight_Group(Movement_Range_Highlights, Alpha);
        }
    }

    private void Flash_Highlight_Group(List<Vector2Int> Highlight_Group, float Alpha)
    {
        foreach (Vector2Int Tile in Highlight_Group)
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

    // Model Placement
    private void Spawn_Model(Vector2Int Hit_Position, int Player, Army_Composition_SO Army, Material Team_Mat)
    {
        if (!Model_Placement_Highlights.Contains(Hit_Position))
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (Models[Hit_Position.x, Hit_Position.y] != null)
            {
                Debug.Log("Tile already occupied!");
                return;
            }

            if (Army == null || !Army.Has_Models_Left())
            {
                Debug.Log($"No more models to place for Player {Player}!");
                return;
            }

            Model_Type Random_Type = Army.Get_Random_Model_Type();
            Model_Standard_Behavior Model = Spawn_Single_Model(Random_Type, Team_Mat,
                Player == 1 ? Player_1_Faction : Player_2_Faction, Player);

            if (Model != null)
            {
                Models[Hit_Position.x, Hit_Position.y] = Model;
                Position_Single_Model(Hit_Position.x, Hit_Position.y, true);

                Debug.Log($"Player {Player} placed {Random_Type}. Remaining: {Army.Get_Remaining_Count()} models");

                if (!Army.Has_Models_Left())
                {
                    Debug.Log($"Player {Player} has no more models to place!");

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
        // LEFT CLICK - Select/Deselect or Start Drag
        if (Input.GetMouseButtonDown(0))
        {
            // If clicking on a model
            if (Models[Hit_Position.x, Hit_Position.y] != null)
            {
                Model_Standard_Behavior Clicked_Model = Models[Hit_Position.x, Hit_Position.y];

                // If clicking the already selected model, start hold-to-drag instead of deselecting
                if (Clicked_Model == Selected_Model)
                {
                    // Don't deselect - just start the hold timer for dragging
                    Is_Holding = true;
                    Current_Hold_Time = 0f;
                    Hold_Start_Position = Hit_Position;
                    Debug.Log($"Clicked selected model again. Hold to drag.");
                    return; // Exit early, don't reselect
                }

                // Deselect previous model before selecting new one
                if (Selected_Model != null)
                {
                    Clear_Movement_Range_Flash();
                    Is_Dragging = null;
                }

                // Select new model (but don't start dragging yet)
                Selected_Model = Clicked_Model;
                Show_Movement_Range(Clicked_Model);

                // Start tracking hold time
                Is_Holding = true;
                Current_Hold_Time = 0f;
                Hold_Start_Position = Hit_Position;

                Debug.Log($"Selected {Clicked_Model.Type} at ({Clicked_Model.Current_X}, {Clicked_Model.Current_Y}). Hold to drag.");
            }
            // If clicking on an empty tile, deselect current model
            else
            {
                if (Selected_Model != null)
                {
                    Deselect_Model();
                }
            }
        }

        // Track holding left mouse button
        if (Input.GetMouseButton(0) && Is_Holding && Selected_Model != null)
        {
            Current_Hold_Time += Time.deltaTime;

            // Check if we've held long enough and haven't started dragging yet
            if (Current_Hold_Time >= Drag_Hold_Time && Is_Dragging == null)
            {
                // Start dragging the model
                Is_Dragging = Selected_Model;
                Debug.Log("Now dragging model!");
            }
        }

        // RIGHT CLICK - Quick Deselect
        if (Input.GetMouseButtonDown(1))
        {
            if (Selected_Model != null)
            {
                Deselect_Model();
            }
            return; // Prevent further processing on right-click
        }

        // LEFT MOUSE BUTTON UP - Complete move or cancel hold
        if (Input.GetMouseButtonUp(0))
        {
            // If we were holding but never started dragging (quick click)
            if (Is_Holding && Is_Dragging == null)
            {
                // This was just a click, not a drag - keep the model selected
                Debug.Log("Quick click - model remains selected");
            }

            // If we were dragging, complete the move
            if (Is_Dragging != null)
            {
                Vector2Int Previous_Position = new Vector2Int(Is_Dragging.Current_X, Is_Dragging.Current_Y);

                bool Valid_Move = Move_To(Is_Dragging, Hit_Position.x, Hit_Position.y);
                if (!Valid_Move)
                {
                    // Invalid move - snap back to original position
                    Is_Dragging.Set_Position(Get_Tile_Center(Previous_Position.x, Previous_Position.y));
                    Debug.Log("Invalid move - snapping back");

                    // Keep model selected after failed move
                    Is_Holding = false;
                    Current_Hold_Time = 0f;
                    Is_Dragging = null;
                    // Don't deselect - let the player try again
                }
                else
                {
                    // SUCCESSFUL MOVE
                    Debug.Log($"Model moved from {Previous_Position} to {Hit_Position}");

                    // Keep the model selected and show new movement range
                    Show_Movement_Range(Is_Dragging); // Show movement range from new position

                    Is_Dragging = null;
                    Is_Holding = false;
                    Current_Hold_Time = 0f;
                    // Selected_Model stays the same - model remains selected
                }
            }

            // Reset hold tracking (only if not dragging or after handling drag)
            if (Is_Dragging == null)
            {
                Is_Holding = false;
                Current_Hold_Time = 0f;
            }
        }

        // While dragging, update position
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

    // Updated Deselect_Model method
    private void Deselect_Model()
    {
        if (Selected_Model != null)
        {
            // Snap the model back to its original position if it was being dragged
            if (Is_Dragging != null)
            {
                Is_Dragging.Set_Position(Get_Tile_Center(Is_Dragging.Current_X, Is_Dragging.Current_Y));
            }
        }

        Clear_Movement_Range_Flash();
        Selected_Model = null;
        Is_Dragging = null;
        Is_Holding = false;
        Current_Hold_Time = 0f;
        Debug.Log("Model deselected");
    }

    // GENERATE BATTLE BOARD
    private void Generate_All_Tiles(float Tile_Size, int Tile_Count, int Tile_Count_Y)
    {
        Y_Offset += transform.position.y;
        Bounds = new Vector3((Tile_Count_X / 2.0f) * Tile_Size, 0, (Tile_Count_X / 2.0f) * Tile_Size) + Board_Center;

        // Store the world position of the bottom-left corner of tile (0,0)
        // This is the position of the first vertex of tile (0,0)
        Board_Origin = new Vector3(0, Y_Offset, 0) - Bounds;

        // Create individual tile meshes (no colliders)
        Tiles = new GameObject[Tile_Count_X, Tile_Count_Y];
        for (int x = 0; x < Tile_Count_X; x++)
            for (int y = 0; y < Tile_Count_Y; y++)
                Tiles[x, y] = Generate_Single_Tiles(Tile_Size, x, y);

        Create_Board_Collider();
    }

    private void Create_Board_Collider()
    {
        GameObject Collider_Object = new GameObject("Board_Collider");
        Collider_Object.transform.parent = transform;
        Collider_Object.layer = LayerMask.NameToLayer("Tile");

        BoxCollider Board_Collider = Collider_Object.AddComponent<BoxCollider>();

        // Calculate the center of the entire board in world space
        Vector3 Board_Center_World = Get_Tile_Center(3, 3);

        // Size covers all tiles plus some margin
        float Width = Tile_Count_X * Tile_Size;
        float Depth = Tile_Count_Y * Tile_Size;

        Board_Collider.center = new Vector3(0, Y_Offset, 0) - Bounds + new Vector3(Width / 2f, 0, Depth / 2f);
        Board_Collider.size = new Vector3(Width, 0.01f, Depth);
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
        // REMOVED: Tile_Object.AddComponent<BoxCollider>();

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

        // Assign stats
        Model.Stats = Faction.Get_Stats_By_Type(Type);
        if (Model.Stats != null)
        {
            Model.Current_Health = Model.Stats.Health;
        }

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

    private void Show_Movement_Range(Model_Standard_Behavior Model)
    {
        Clear_Movement_Range_Flash();

        if (Model.Stats == null)
            return;
        
        int Movement_Range = Model.Stats.Movement_Range;
        int Start_X = Model.Current_X;
        int Start_Y = Model.Current_Y;

        for (int X = 0; X < Tile_Count_X; X++)
        {
            for (int Y = 0; Y < Tile_Count_Y; Y++)
            {
                int Distance = Mathf.Abs(X - Start_X) + Mathf.Abs(Y - Start_Y);

                if (Distance <= Movement_Range && Distance > 0)
                {
                    Movement_Range_Highlights.Add(new Vector2Int(X, Y));

                    if (Movement_Range_Material != null)
                        Tiles[X, Y].GetComponent<MeshRenderer>().material = Movement_Range_Material;
                }
            }
        }

        if (Movement_Range_Highlights.Count > 0)
        {
            Current_Flash_Material = Movement_Range_Material;
        }

    }

    private void Clear_Movement_Range_Flash()
    {
        foreach (Vector2Int Tile in Movement_Range_Highlights)
        {
            Tiles[Tile.x, Tile.y].GetComponent<MeshRenderer>().material = Tile_Material;
        }
        Movement_Range_Highlights.Clear();
        Current_Flash_Material = null;
    }

    // Operations
    private bool Move_To(Model_Standard_Behavior Model, int X, int Y)
    {
        Vector2Int Previous_Position = new Vector2Int(Model.Current_X, Model.Current_Y);

        // Check if model can move (respects turn-based movement)
        if (Model.Has_Moved_This_Turn)
        {
            Debug.Log($"{Model.Type} has already moved this turn!");
            return false;
        }

        // Is tile occupied?
        if (Models[X, Y] != null)
        {
            // Check if it's an enemy (future: implement attack logic)
            if (Models[X, Y].Team != Model.Team)
            {
                Debug.Log("Cannot move onto enemy tile!");
            }
            else
            {
                Debug.Log("Tile occupied by friendly model!");
            }
            return false;
        }

        // Is within movement range?
        if (Model.Stats != null)
        {
            if (!Model.Can_Move_To(X, Y))
            {
                Debug.Log($"Target is outside movement range of {Model.Stats.Movement_Range}!");
                return false;
            }
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
