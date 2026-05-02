using UnityEngine;

public class Battle_Board_Behavior : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private Material Tile_Material;
    [SerializeField] private float Tile_Size = 1.0f;
    [SerializeField] private float Y_Offset = 0.2f;
    [SerializeField] private Vector3 Board_Center = Vector3.zero;

    // Board Logic
    private const int Tile_Count_X = 8;
    private const int Tile_Count_Y = 8;
    private GameObject[,] Tiles;
    private Camera Main_Camera;
    private Vector2Int Current_Mouse_Hover;
    private Vector3 Bounds;

    private void Awake()
    {
        Generate_All_Tiles(Tile_Size, Tile_Count_X, Tile_Count_Y);
    }

    private void Update()
    {
        if(!Main_Camera)
        {
            Main_Camera = Camera.main;
            return;
        }

        RaycastHit Info;
        Ray Ray = Main_Camera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(Ray, out Info, 100, LayerMask.GetMask("Tile","Hover")))
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
            if(Current_Mouse_Hover != -Vector2Int.one)
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

        int[] Tris = new int[] { 0,1,2,1,3,2 };

        Mesh.vertices= Verticies;
        Mesh.triangles= Tris;

        Mesh.RecalculateNormals();

        Tile_Object.layer = LayerMask.NameToLayer("Tile");
        Tile_Object.AddComponent<BoxCollider>();

        return Tile_Object;
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
}
