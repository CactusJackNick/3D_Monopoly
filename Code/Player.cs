using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Required for Count()
using UnityEngine.UI;

namespace Code
{
    public class Player : MonoBehaviour
    {
        [Header("Owned Properties")]
        public List<Property> ownedProperties;
        public int currentWaypointIndex;

        public Text playerTag;
    
        [Header("Player Stats")]
        public string playerName;
        public int money = 1500;
        public int jailFee = 50; // Fee to pay in order to get out of jail
        public int jailTurns; // Tracks how many turns the player has been in jail
        public int getOutOfJailFreeCards;
        public bool IsInJail => jailTurns > 0; // Checks if the player is in jail
        public PlayerState state; // Enum to track player state and set default to Playing
        public Color playerColor;
        public bool  preventGoBonus;

        public int diceTriesLeft;

        private void Start()
        {
            playerTag.text = playerName;
        }

        public Player(string name)
        {
            this.name = name;
            currentWaypointIndex = 0;
            jailTurns = 0;
            state = PlayerState.Playing;
        }
    
        public void GoToJail()
        {
            // Get the current player's waypoints
            List<Transform> waypoints = PlayerManager.Instance._playerWaypoints[PlayerManager.Instance.currentPlayerIndex];
            int jailWaypointIndex = 10; // Find the jail waypoint index (replace with actual board index)

            Transform currentPlayerTransform = PlayerManager.Instance.playersTransforms[PlayerManager.Instance.currentPlayerIndex];
            int currentPos = PlayerManager.Instance._playerPositions[currentPlayerTransform];// Get the player's current position in waypoints
            
            int stepsToJail = (jailWaypointIndex - currentPos + waypoints.Count) % waypoints.Count; // Calculate the steps to jail
            if (stepsToJail > 0)
            {
                // Move the player step-by-step like dice roll
                PlayerManager.Instance.StartPlayerMovement(stepsToJail);
            }
            preventGoBonus = true;
        }
        
        public void EnterJail()
        {
            if (state != PlayerState.InJail)
            {
                state = PlayerState.InJail;
                jailTurns = 3;
            }
        }

        public void LeaveJail()
        {
            state = PlayerState.Playing;
            jailTurns = 0;
        }
    
        public bool OwnsFullColorGroup(ColorGroup colorGroup)
        {
            int totalInGroup = PropertyManager.Instance.GetTotalPropertiesInGroup(colorGroup);
            int ownedInGroup = ownedProperties.FindAll(p => p.propertyData.colorGroup == colorGroup).Count;

            bool ownsAll = ownedInGroup == totalInGroup;
    
            Debug.Log($"{playerName} owns {ownedInGroup}/{totalInGroup} in {colorGroup} group. Full set? {ownsAll}");

            return ownsAll;
        }
        
        public int CountRailroads()
        {
            return ownedProperties.Count(p => p.propertyData.colorGroup == ColorGroup.Railroads);
        }
        
        public int GetOwnedUtilityCount()
        {
            return ownedProperties.Count(p => p.propertyData.tileType == TileType.Utility);
        }

    }
}
