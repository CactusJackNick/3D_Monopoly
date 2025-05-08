using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Code
{
    public enum BoardSide
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public class Property : MonoBehaviour
    {
        public BoardSide boardSide;
        public PropertyData propertyData; // Assign in Inspector
        public Player owner; // Now the runtime component tracks ownership
    
        public int houseCount = 0;
        public bool hasHotel = false;
        public bool isMortgaged = false;
        public int houseCost = 100; // Example house price
        public int hotelCost = 1000; // Hotel price reference
    
        public GameObject banner; // The banner GameObject on the Avenue
        public Renderer bannerRenderer; // Renderer for changing banner color from that Avenue
        private Text bannerText; // Use Text for legacy UI

    
        [Header("Building Settings")]
        public Transform[] housePoints; // Assign HousePoint1, HousePoint2, etc., in Inspector
        public Transform hotelPoint; // Assign HotelPoint in Inspector
        public GameObject housePrefab; // Assign House prefab in Inspector
        public GameObject hotelPrefab; // Assign Hotel prefab in Inspector

        public bool IsOwned => owner != null; // Helper to check ownership
        public GameObject buildEffect;
        public bool IsMaxedOut() => hasHotel; // If it has a hotel, it's maxed out
        private List<GameObject> instantiatedHouses = new List<GameObject>();
        private GameObject currentHotel; // Track the hotel instance
        private PropertyManager _propertyManager;
        [SerializeField] private GameObject bannerPrefab; // Drag your banner prefab here in the Inspector

        private void Awake()
        {
            bannerText = banner.GetComponentInChildren<Text>();
            _propertyManager = FindFirstObjectByType<PropertyManager>();
        }

        private void Start()
        {
            PropertyManager.Instance.allProperties.Add(this);
            Debug.Log($"📌 Registered {propertyData.propertyName} in {propertyData.colorGroup}.");
        }

        public void BuyProperty(Player player)  
        {
            if (IsOwned)
            {
                Debug.Log("Property already owned!");
                return;
            }

            if (player.money >= propertyData.price)
            {
                player.money -= propertyData.price;
                owner = player;  // Set owner in runtime Property, not in ScriptableObject
                player.ownedProperties.Add(this); // Now the player's list updates correctly
                Debug.Log($"{player.playerName} bought {propertyData.propertyName}!");
            }
            else
            {
                Debug.Log("Not enough money to buy this property!");
            }
            if (banner == null && bannerPrefab != null)
            {
                // Instantiate the banner from a prefab
                banner = Instantiate(bannerPrefab, transform.position, Quaternion.identity);
                banner.transform.SetParent(transform); // Optional: Set the property as the parent
                bannerRenderer = banner.GetComponent<Renderer>();

                if (bannerRenderer == null)
                {
                    Debug.LogError("Banner Renderer component is missing on the instantiated prefab!");
                    return;
                }
            }

            UpdateBanner();
            PlayerManager.Instance.UpdatePlayerUI();
        }
    
        public void MortgageProperty()
        {
            if (houseCount > 0 || !_propertyManager.AllHousesSoldInColorGroup(this))
            {
                Debug.Log("Cannot mortgage - all houses/hotels in the color group must be sold first.");
                return;
            }

            if (isMortgaged)
            {
                Debug.Log("This property is already mortgaged!");
                return;
            }

            isMortgaged = true;
            owner.money += (propertyData.price / 2);

            Debug.Log($"{owner.playerName} mortgaged {propertyData.propertyName}.");
            UpdateBanner();
            _propertyManager.RefreshPropertyUI();
        }


        public void UnmortgageProperty()
        {
            if (!isMortgaged)
            {
                Debug.Log("This property is not mortgaged!");
                return;
            }
        
            isMortgaged = false;
            owner.money -= propertyData.price / 2 + (propertyData.price * 10 / 100);
            Debug.Log($"{owner.playerName} unmortgaged {propertyData.propertyName}."); 
            UpdateBanner();
            _propertyManager.RefreshPropertyUI();
        }
    
        private void UpdateBanner()
        {
            if (banner is null)
                return;

            if (!banner.activeInHierarchy)
                banner.SetActive(true);

            // Ensure the Renderer component is properly initialized
            if (bannerRenderer == null)
            {
                bannerRenderer = banner.GetComponent<Renderer>();
                if (bannerRenderer == null)
                {
                    Debug.LogError("Banner Renderer component not found!");
                    return;
                }
            }

            if (owner != null && !isMortgaged)
            {
                // Set material and color for the banner
                if (!bannerRenderer.material.name.Contains("Instance"))
                    bannerRenderer.material = new Material(bannerRenderer.sharedMaterial);

                bannerRenderer.material.color = owner.playerColor;
                // Find all child objects with a Renderer component and change their material color
                foreach (Renderer childRenderer in bannerRenderer.GetComponentsInChildren<Renderer>())
                {
                    childRenderer.material.color = owner.playerColor;
                }
                bannerText.text = owner.playerName;
            }
            else
                banner.SetActive(false); // Hide banner if no owner or mortgaged
        }

        public void SellPropertyFunction() // SELL THE ACTUAL PROPERTY ITSELF
        {
            if (owner is null)
            {
                Debug.Log("This property has no owner to sell!");
                return;
            }

            owner.money += propertyData.price / 2; 
            owner.ownedProperties.Remove(this); // Remove this property from the owner's list of owned properties
            owner = null; // Set the owner to null
            houseCount = 0;
            hasHotel = false;

            Debug.Log($"{propertyData.propertyName} was sold!");
            UpdateBanner();
            _propertyManager.RefreshPropertyUI();
        }
    
        public void Upgrade()
        {
            if (IsMaxedOut())
            {
                Debug.Log("This property is fully upgraded!");
                return;
            }

            if (houseCount < 4)
            {
                // Instantiate a house at the next available house point
                if (houseCount < housePoints.Length)
                {
                    //Instantiate(housePrefab, housePoints[houseCount].position, Quaternion.identity, this.transform);
                    GameObject house = Instantiate(housePrefab, housePoints[houseCount].position, housePoints[houseCount].rotation, this.transform);
                    GameObject effect = Instantiate(buildEffect, housePoints[houseCount].position, housePoints[houseCount].rotation);
                    instantiatedHouses.Add(house);
                    houseCount++;
                    owner.money -= houseCount * houseCost;
                    Destroy(effect, 2f);
                    Debug.Log($"House built on {propertyData.propertyName}. Total houses: {houseCount}");
               
                }
            }
            else if (!hasHotel)
            {
                hasHotel = true; // Upgrade to a hotel
                owner.money -= hotelCost; // Deduct the hotel cost

                // Make all houses invisible
                foreach (var house in instantiatedHouses)
                    house.SetActive(false);

                // Instantiate the hotel depending on the side of the board
                Quaternion rotation = Quaternion.identity;
                switch (boardSide)
                {
                    case BoardSide.Top:
                        rotation = Quaternion.Euler(0, -90, 0);
                        break;
                    case BoardSide.Bottom:
                        rotation = Quaternion.Euler(0, 90, 0);
                        break;
                    case BoardSide.Left:
                        rotation = Quaternion.Euler(0, 0, 0);
                        break;
                    case BoardSide.Right:
                        rotation = Quaternion.Euler(0, 180, 0);
                        break;
                }
                currentHotel = Instantiate(hotelPrefab, hotelPoint.position, rotation, this.transform);
                GameObject effect = Instantiate(buildEffect, currentHotel.transform);
                Destroy(effect, 2f);
                Debug.Log($"Hotel built on {propertyData.propertyName}!");
            }
        }
    
        public void DowngradeHotel() // Downgrades a hotel back to 4 houses
        {
            if (hasHotel)
            {
                // Destroy the hotel
                currentHotel.SetActive(false);
                hasHotel = false;

                // Make all 4 houses visible again
                for (int i = 0; i < 4; i++)
                {
                    if (i <= instantiatedHouses.Count)
                        instantiatedHouses[i].SetActive(true);
                }

                // Refund part of the hotel cost
                owner.money += hotelCost / 2;
                Debug.Log($"Hotel downgraded on {propertyData.propertyName}. Houses restored. Current houses: {houseCount}");
            }
            else
            {
                Debug.Log("No hotel to downgrade!");
            }
        }
    
        public void SellHouse(Property property, Player player)
        {
            List<Property> colorGroupProperties = player.ownedProperties.FindAll(p => p.propertyData.colorGroup == property.propertyData.colorGroup);

            // Get the property with the maximum house count
            int maxHouses = int.MinValue;
            foreach (var prop in colorGroupProperties)
            {
                maxHouses = Mathf.Max(maxHouses, prop.houseCount);
            }

            // Ensure the player is selling houses evenly
            if (property.houseCount < maxHouses)
            {
                Debug.Log("You must sell houses evenly across all properties in this color group!");
                return;
            }

            if (property.houseCount > 0)
            {
                // Remove one house
                property.houseCount--;
                player.money += property.houseCost / 2; // Half the cost is returned when selling
                property.UpdateVisuals(); // Update house visuals on the board
                Debug.Log($"Sold one house from {property.propertyData.propertyName}. Total houses now: {property.houseCount}");
            }
            else
            {
                Debug.Log("No houses to sell on this property!");
            }
            _propertyManager.RefreshPropertyUI();
        }
    
        private void UpdateVisuals()
        {
            // Show or hide houses
            for (int i = 0; i < housePoints.Length; i++)
                instantiatedHouses[i].SetActive(i < houseCount); // Make the house visible and hidden

            // Show or hide the hotel
            if (hasHotel)
            {
                if (currentHotel is null)
                {
                    currentHotel = Instantiate(hotelPrefab, hotelPoint.position, Quaternion.Euler(0, 90, 0), transform);
                }
                currentHotel.SetActive(true);
            }
            else if (currentHotel is not null)
            {
                currentHotel.SetActive(false);
            }
        }
    
        public int GetRent()
        {
            if (isMortgaged) // If mortgaed do not collect rent
                return 0;
        
            if (hasHotel)
            {
                Debug.Log($"Hotel rent for {propertyData.propertyName}: {propertyData.hotelRent}");
                return propertyData.hotelRent; // Return hotel rent
            }
            
            if (propertyData.colorGroup == ColorGroup.Railroads)
            {
                int railroadsOwned = owner.CountRailroads();
                return 25 *(int)Mathf.Pow(2, railroadsOwned - 1);
            }

            switch (houseCount)
            {
                case 1:
                    return propertyData.OneHouseRent;
                case 2:
                    return propertyData.TwoHousesRent;
                case 3:
                    return propertyData.ThreeHousesRent;
                case 4:
                    return propertyData.FourHousesRent;
                default:
                    return propertyData.rent; // Base rent if no houses/hotel
            }
        }
        
        public int CalculateRent(int diceRoll, Player player)
        {
            if (propertyData.tileType == TileType.Utility)
            {
                int utilitiesOwned = player.GetOwnedUtilityCount();
                int multiplier = (utilitiesOwned == 2) ? propertyData.utilityFullMultiplier : propertyData.utilityBaseMultiplier;
                return diceRoll * multiplier;
            }
            //return propertyData.rent; // Default rent for other properties
            return GetRent();
        }

    
        //Winner of the AUCTION
        public void AssignOwner(Player newOwner)
        {
            if (newOwner is null) return;

            if (!IsOwned)
            {
                owner = newOwner;
                Debug.Log($"{owner.playerName} is now the owner of {propertyData.propertyName}.");
                UpdateBanner();
            }
        }

    }
}