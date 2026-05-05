using UnityEngine;

[CreateAssetMenu(fileName = "New Faction", menuName = "Model Sets/Faction Data")]
public class Faction_Data_SO : ScriptableObject
{
    public string Faction_Name;

    [Header("Model Prefabs")]
    public GameObject Chaff_Prefab;
    public GameObject Specialist_A_Prefab;
    public GameObject Specialist_B_Prefab;
    public GameObject Axillary_Prefab;
    public GameObject Death_Head_Prefab;

    [Header("Model Materials")]
    public Material[] Team_Materials;

    // Healper method to get the prefab based on the model type
    public GameObject Get_Prefab_By_Type(Model_Type Type)
    {
        return Type switch
        {
            Model_Type.Chaff => Chaff_Prefab,
            Model_Type.Specialist_A => Specialist_A_Prefab,
            Model_Type.Specialist_B => Specialist_B_Prefab,
            Model_Type.Axillary => Axillary_Prefab,
            Model_Type.DeathHead => Death_Head_Prefab,
            _ => null
        };
    }
}
