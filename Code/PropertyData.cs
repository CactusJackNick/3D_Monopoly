using UnityEngine;

[CreateAssetMenu(fileName = "NewProperty", menuName = "Property")]
public class PropertyData : ScriptableObject
{
    public string propertyName;
    public int price;
    public int rent;
    public int OneHouseRent;
    public int TwoHousesRent;
    public int ThreeHousesRent;
    public int FourHousesRent;
    public int hotelRent;
    public ColorGroup colorGroup;
    public TileType tileType;
    
    // ✅ Only for utilities
    public int utilityBaseMultiplier = 4;
    public int utilityFullMultiplier = 10;
}

public enum ColorGroup
{
    Blue, LightBlue, Pink, Orange, Red, Yellow, Green, Purple, None, Railroads
}

public enum TileType
{
    Go,
    Property,
    Chance,
    CommunityChest,
    Jail,
    FreeParking,
    GoToJail,
    Utility,
    Railroad,
    Tax
}

