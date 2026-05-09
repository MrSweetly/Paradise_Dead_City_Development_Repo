using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Army Composition", menuName = "Scriptable Objects/Army Composition")]
public class Army_Composition_SO : ScriptableObject
{
    public string Composition_Name;

    [System.Serializable]
    public class Model_Entry
    {
        public Model_Type Type;
        public int Count;
    }

    [Header("Army Roster")]
    public List<Model_Entry> Army_Roster = new List<Model_Entry>();

    [Header("Spawn Tile Prefab")]
    public GameObject Spawn_Tile_Prefab;

    // Helper method to get a random model type from roster
    public Model_Type Get_Random_Model_Type()
    {
        // Create a weighted list based on count
        List<Model_Type> Available_Types = new List<Model_Type>();
        foreach (Model_Entry Entry in Army_Roster)
        {
            for (int i = 0; i < Entry.Count; i++)
            {
                Available_Types.Add(Entry.Type);
            }
        }

        if (Available_Types.Count == 0)
        {
            Debug.LogError("No models available in army composition!");
            return Model_Type.Chaff; // Fallback, but shouldn't happen if Has_Models_Left() is checked first
        }

        int Random_Index = Random.Range(0, Available_Types.Count);
        Model_Type Selected_Type = Available_Types[Random_Index];

        // Decrease the count in the roster
        foreach (Model_Entry Entry in Army_Roster)
        {
            if (Entry.Type == Selected_Type && Entry.Count > 0)
            {
                Entry.Count--;
                break;
            }
        }

        return Selected_Type;
    }

    // Check if there are any models left to place
    public bool Has_Models_Left()
    {
        foreach (Model_Entry entry in Army_Roster)
        {
            if (entry.Count > 0)
                return true;
        }
        return false;
    }

    // Get total remaining model count
    public int Get_Remaining_Count()
    {
        int total = 0;
        foreach (Model_Entry entry in Army_Roster)
        {
            total += entry.Count;
        }
        return total;
    }
}
