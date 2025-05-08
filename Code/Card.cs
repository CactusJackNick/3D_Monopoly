using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Monopoly/Card")]
public class Card : ScriptableObject
{
    public string cardName;
    public string cardDescription;
    public int moneyChange; // Positive for gain, negative for loss
    public Vector3 moveToTile; // Optional: Move player to a specific tile
    public CardEffect effectType;
    public GameObject cardPrefab; // Reference to the visual prefab of this card
}

public enum CardEffect
{
    GainMoney,
    LoseMoney,
    MoveTiles,
    PayAllPlayers,
    CollectFromAllPlayers,
    GoToTile,
    JailFree
}