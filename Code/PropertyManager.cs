using UnityEngine;
using System.Collections.Generic;
using Code;
using UnityEngine.UI;

public class PropertyManager : MonoBehaviour
{
    public static PropertyManager Instance;
    public List<Property> allProperties = new List<Property>();

    [Header("Panels")]
    public GameObject propertyUIPanel;
    public GameObject standardPropertyUIPanel;
    public GameObject railroadUIPanel;
    public GameObject utilityUIPanel;
    
    [Header("Texts")]
    public Text propertyNameText;
    public Text propertyPriceText;
    public Text ownerNameText;
    public Text rentText;
    
    [Header("Buttons")]
    public Button buyButton;
    public Button sellPropertyButton;
    public Button skipButton;
    public Button upgradeButton;
    public Button mortgageButton;
    public Button unmortgageButton; 
    public Button downgradeButton;

    private Property currentProperty;
    private Player currentPlayer;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    public bool AllHousesSoldInColorGroup(Property property)
    {
        if (property.propertyData is null) return false;

        List<Property> colorGroupProperties = currentPlayer.ownedProperties.FindAll(
            p => p.propertyData.colorGroup == property.propertyData.colorGroup
        );

        foreach (var groupProperty in colorGroupProperties)
        {
            if (groupProperty.houseCount > 0)
            {
                Debug.LogWarning($"Cannot mortgage {property.propertyData.propertyName} - all houses in the color group must be sold.");
                return false;
            }
        }
        return true;
    }
    
    public void RefreshPropertyUI()
    {
        if (currentProperty != null && currentPlayer != null)
        {
            ShowPropertyUI(currentProperty, currentPlayer);
        }
        
        buyButton.gameObject.SetActive(!currentProperty.IsOwned);
        sellPropertyButton.gameObject.SetActive(currentProperty.IsOwned && currentProperty.houseCount == 0);
    }
    
    public void ShowPropertyUI(Property property, Player player)
    {
        if (property is null || player is null) return;

        if (property.IsOwned && property.owner == player)
        {
            // Disable the upgrade button if the property is mortgaged
            upgradeButton.gameObject.SetActive(
                !property.isMortgaged && 
                player.OwnsFullColorGroup(property.propertyData.colorGroup));
            ownerNameText.text = property.owner.playerName;
        }
        else
        {
            upgradeButton.gameObject.SetActive(false); // Hide upgrade button for non-owners
        }

        if (property.IsOwned)
        {
            if (property.owner == player)
            {
                buyButton.gameObject.SetActive(false);
                downgradeButton.gameObject.SetActive(property.houseCount != 0);
                mortgageButton.gameObject.SetActive(property.houseCount == 0 && !property.isMortgaged);
            }
        }
        else if (!property.IsOwned)
        {
            ownerNameText.text = "Not owned";
            buyButton.gameObject.SetActive(true);
            downgradeButton.gameObject.SetActive(false);
            mortgageButton.gameObject.SetActive(false);
            unmortgageButton.gameObject.SetActive(false);
        }
            
        currentProperty = property;
        currentPlayer = player;

        propertyNameText.text = property.propertyData.propertyName;
        propertyPriceText.text = "Price: $" + property.propertyData.price;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(BuyProperty);

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(ClosePropertyUI);

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(UpgradeProperty);

        downgradeButton.onClick.RemoveAllListeners();
        downgradeButton.onClick.AddListener(DowngradeProperty);

        // Handle mortgage and unmortgage buttons
        if (property.IsOwned && property.houseCount == 0)
        {
            mortgageButton.gameObject.SetActive(!property.isMortgaged);
            mortgageButton.onClick.RemoveAllListeners();
            mortgageButton.onClick.AddListener(property.MortgageProperty);

            sellPropertyButton.gameObject.SetActive(true);
            sellPropertyButton.onClick.RemoveAllListeners();
            sellPropertyButton.onClick.AddListener(SellProperty);
        }
        else
            sellPropertyButton.gameObject.SetActive(false);

        if (property.isMortgaged && property.owner == currentPlayer)
        {
            unmortgageButton.gameObject.SetActive(true);
            unmortgageButton.onClick.RemoveAllListeners();
            unmortgageButton.onClick.AddListener(property.UnmortgageProperty);
        }
        else
        {
            unmortgageButton.gameObject.SetActive(false);
        }

        if (property.owner == player || !property.IsOwned)
        {
            propertyUIPanel.SetActive(true);
            if (property.propertyData.colorGroup == ColorGroup.Railroads)
            {
                utilityUIPanel.SetActive(false);
                standardPropertyUIPanel.SetActive(false);
                railroadUIPanel.SetActive(true);
            }
            else if (property.propertyData.tileType == TileType.Utility)
            {
                utilityUIPanel.SetActive(true);
                standardPropertyUIPanel.SetActive(false);
                railroadUIPanel.SetActive(false);
                rentText.text = "Depending on the amount of Utilities owned there is a respective rent cost";
                if(currentPlayer == currentProperty.owner)
                    rentText.text = "Rent: " + (property.owner.GetOwnedUtilityCount() == 2 ? "10× Dice Roll" : "4× Dice Roll");
            }
            else
            {
                standardPropertyUIPanel.SetActive(true);
                railroadUIPanel.SetActive(false);
                utilityUIPanel.SetActive(false);
            }

            
            
        }
    }
    
    private void BuyProperty()
    {
        currentProperty.BuyProperty(currentPlayer);
        ClosePropertyUI();
    }
    
    private void ClosePropertyUI()
    {
        propertyUIPanel.SetActive(false);
        if (currentProperty.IsOwned)
        {
            Debug.Log($"{currentProperty.propertyData.propertyName} has been purchased by {currentProperty.owner.playerName}. Auction will not start.");
            return;
        }
        
        // START AUCTION LOGIC if NOT PURCHASED
        List<Player> otherPlayers = new List<Player>(PlayerManager.Instance.players);
        otherPlayers.Remove(currentPlayer);
        
        FindFirstObjectByType<AuctionManager>().StartAuction(currentProperty, otherPlayers);
        
        // Reset current references
        currentProperty = null;
        currentPlayer = null;
    }

    public int GetTotalPropertiesInGroup(ColorGroup colorGroup)
    {
        return allProperties.FindAll(p => p.propertyData.colorGroup == colorGroup).Count;
    }
    
    private bool CanBuildHouse(Property property, Player player)
    {
        if (!player.OwnsFullColorGroup(property.propertyData.colorGroup))
        {
            Debug.Log("You must own all properties in this color group to build houses!");
            return false;
        }

        int minHouses = int.MaxValue;
        int maxHouses = int.MinValue;

        // Get all properties in the same color group owned by the player
        List<Property> colorGroupProperties = player.ownedProperties.FindAll(p => p.propertyData.colorGroup == property.propertyData.colorGroup);

        foreach (var prop in colorGroupProperties)
        {
            // Skip properties with hotels as they are already maxed out
            if (prop.hasHotel)
                continue;

            minHouses = Mathf.Min(minHouses, prop.houseCount);
            maxHouses = Mathf.Max(maxHouses, prop.houseCount);
        }

        // Ensure even building: a property cannot exceed the minimum house count in the group
        if (property.houseCount > minHouses)
        {
            Debug.Log("You must build evenly across all properties in this color group.");
            upgradeButton.gameObject.SetActive(false);
            return false;
        }

        // Check if the player has enough money for the house upgrade
        if (player.money < property.houseCost)
        {
            Debug.Log("You do not have enough money to build a house.");
            return false;
        }

        return true;
    }
    
    private void BuildHouse(Property property, Player player)
    { 
        if (!CanBuildHouse(property, player)) return;

        int houseCost = (property.houseCount + 1) * 100; // Increase cost per house
        if (player.money < houseCost)
        {
            Debug.Log("Not enough money to upgrade this property!");
            return;
        }
        
        property.Upgrade();
    }
    
    private void UpgradeProperty()
    {
        if (currentProperty is not null && currentPlayer is not null)
        {
            BuildHouse(currentProperty, currentPlayer);
            RefreshPropertyUI();
            PlayerManager.Instance.UpdatePlayerUI();
        }
        
        if(currentProperty.IsMaxedOut())
            upgradeButton.gameObject.SetActive(false);
    }

    private void DowngradeProperty()
    {
        if (currentProperty.hasHotel)
            currentProperty.DowngradeHotel(); // Downgrade hotel to 4 houses
        
        else if (currentProperty.houseCount > 0)
            currentProperty.SellHouse(currentProperty, currentPlayer);// Sell one house
        if (currentProperty.houseCount == 0)
        {
            mortgageButton.gameObject.SetActive(true);
            RefreshPropertyUI();
        }
        PlayerManager.Instance.UpdatePlayerUI();
    }

    private void SellProperty()
    {
        if (currentProperty.owner is null && currentProperty.houseCount > 0)
        {
            sellPropertyButton.gameObject.SetActive(false);
            buyButton.gameObject.SetActive(true);
            return;
        }
        
        currentProperty.isMortgaged = false;
        currentProperty.SellPropertyFunction();
        
        sellPropertyButton.gameObject.SetActive(false);
        buyButton.gameObject.SetActive(true);

        RefreshPropertyUI();
        PlayerManager.Instance.UpdatePlayerUI();
    }
}
