using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Code
{
    public class PropertyTrigger : MonoBehaviour
    {
        private Property _property;
        private PropertyManager _propertyManager;
        private Player _currentPlayer; // Track the player inside
        private readonly HashSet<Player> _playersWhoPaidRent = new HashSet<Player>(); // Tracks players who paid rent this turn
        private DiceManager _diceManager;
        
        private void Awake()
        {
            _propertyManager = FindFirstObjectByType<PropertyManager>();
            if (_propertyManager == null)
            {
                Debug.LogError("PropertyManager NOT FOUND in the scene! Make sure it exists.");
            }
        }

        private void Start()
        {
            _property = GetComponent<Property>(); // Get property component
        }

        public void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Player {other.name} entered the {gameObject.name}");

            // Try to get the Player component
            Player player = other.GetComponent<Player>();
            if (player == null) return; // Ignore non-player objects
        
            PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
            if (playerManager == null) return;

            PlayerMovement playerMovement = playerManager.GetComponent<PlayerMovement>();
            if (playerMovement == null) return;

            // Store the current player
            _currentPlayer = player;

            // Defer Chance/Community Chest or Property logic until movement stops
            StartCoroutine(WaitForPlayerToStopMoving(player, playerMovement));
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && other.GetComponent<Player>() == _currentPlayer)
            {
                Debug.Log($"⛔ Player {other.name} exited {gameObject.name}");
                _currentPlayer = null; // Reset tracked player
                _playersWhoPaidRent.Remove(other.GetComponent<Player>()); // Allow rent payment on next entry
            }
        }
    
        private IEnumerator WaitForPlayerToStopMoving(Player player, PlayerMovement playerMovement)
        {
            // Wait until the player finishes moving
            while (playerMovement._isMoving)
            {
                yield return null; // Check again in the next frame
            }

            yield return new WaitForSeconds(0.5f); // Short delay after movement ends

            // Ensure the player is still on this tile
            if (_currentPlayer == player)
            {
                if (_property.propertyData.tileType == TileType.Chance)
                {
                    HandleChanceTile();
                }
                else if (_property.propertyData.tileType == TileType.CommunityChest)
                {
                    HandleCommunityChestTile();
                }
                else
                {
                    HandlePropertyTile(player);
                }
            }
        }

        private void HandlePropertyTile(Player player)
        {
            StartCoroutine(HandleLanding(player, FindFirstObjectByType<PlayerMovement>()));
        }
        
        private IEnumerator HandleLanding(Player player, PlayerMovement playerMovement)
        {
            if (_property.propertyData.tileType == TileType.Tax)
            {
                HandleTaxTile(player);
                PlayerManager.Instance.UpdatePlayerUI();
                yield break;
            }
            
            if (_property.IsOwned)
            {
                if (_property.owner == player)
                {
                    _propertyManager.ShowPropertyUI(_property, player);
                }
                else
                {
                    if (!_playersWhoPaidRent.Contains(player))
                    {
                        int rent;

                        if (_property.propertyData.tileType == TileType.Utility)
                        {
                            // Roll separate dice for rent calculation
                            yield return StartCoroutine(InstantiateAndRollRentDice(player));
                        }
                        else
                        {
                            rent = _property.GetRent();
                            player.money -= rent;
                            _property.owner.money += rent;
                            _playersWhoPaidRent.Add(player);

                            PlayerManager.Instance.UpdatePlayerUI();
                            StartCoroutine(PlayerManager.Instance.ShowRentPopup($"{player.playerName} paid ${rent} to {_property.owner.playerName}!"));
                        }
                    }
                }
            }
            else
            {
                _propertyManager.ShowPropertyUI(_property, player);
            }

            yield return null;
        }

        private IEnumerator InstantiateAndRollRentDice(Player player)
        {
            // Ensure DiceManager is assigned properly
            if (_diceManager is null)
            {
                _diceManager = FindFirstObjectByType<DiceManager>(); // Get the DiceManager if not assigned
                if (_diceManager is null)
                    yield break; // Exit coroutine to prevent further errors
            }

            Debug.Log("Spawning and rolling dice for rent calculation...");
            CameraManager.Instance.SwitchToDiceCamera();

            _diceManager.SpawnDice();
            yield return new WaitForSeconds(0.5f);
            _diceManager.RollAllDice();
            yield return new WaitForSeconds(6.5f);
    
            int diceRoll = _diceManager.GetDiceSum();
            Debug.Log($"Rent Dice Roll Result: {diceRoll}");

            int rent = _property.CalculateRent(diceRoll, _property.owner);
            Debug.Log($"{player.playerName} pays ${rent} rent to {_property.owner.playerName}");

            player.money -= rent;
            _property.owner.money += rent;
            _playersWhoPaidRent.Add(player);
            
            Transform playerCameraTarget = _currentPlayer.transform.Find("PlayerTargetCamera");
            CameraManager.Instance.SwitchToPlayer(playerCameraTarget);
            PlayerManager.Instance.UpdatePlayerUI();
            StartCoroutine(PlayerManager.Instance.ShowRentPopup($"{player.playerName} paid ${rent} to {_property.owner.playerName}!"));
    
            _diceManager.DestroyUnusedDice();
        }

        private void HandleTaxTile(Player player)
        {
            if (_property.CompareTag("TaxDepartment")) // Correcting possible typo
            {
                int taxAmount = (player.money >= 2000) ? (player.money * 10) / 100 : 200;
                player.money -= taxAmount;
                StartCoroutine(PlayerManager.Instance.ShowRentPopup($"{player.playerName} paid ${taxAmount} tax to the Government!"));
            }
            else if (_property.CompareTag("LuxuryTax"))
            {
                player.money -= 75;
                StartCoroutine(PlayerManager.Instance.ShowRentPopup($"{player.playerName} paid $75 Luxury Tax to the Government!"));
            }
        }

        
        private void HandleChanceTile()
        {
            // Ensure CameraManager instance is not null
            if (CameraManager.Instance == null)
            {
                return;
            }

            StartCoroutine(ShowChanceCard());
        }

        private void HandleCommunityChestTile()
        {
            // Ensure CameraManager instance is not null
            if (CameraManager.Instance == null)
            {
                return;
            }

            StartCoroutine(ShowCommunityChestCard());
        }

        private IEnumerator ShowChanceCard()
        {
            // Switch to the Chance camera
            Transform chanceCameraTarget = CameraManager.Instance.chanceCameraTarget;
            if (chanceCameraTarget != null)
            {
                CameraManager.Instance.SwitchToTarget(chanceCameraTarget);
                yield return new WaitForSeconds(1.5f); // Simulate delay for camera transition
            }
            else
            {
                Debug.LogError("Chance Camera Target is not set in CameraManager!");
            }
        
            // Draw and display a Chance card
            Card drawnCard = CardManager.Instance.DrawCard(TileType.Chance);
            if (drawnCard != null)
            {
                Debug.Log($"Chance Card: {drawnCard.cardName} - {drawnCard.cardDescription}");
        
                // Simulate card effect logic
                Debug.Log("Chance card effect triggered!");
                yield return new WaitForSeconds(2f); // Simulate card display delay
        
                // Apply the card's effect
                CardManager.Instance.ApplyCardEffect(_currentPlayer, drawnCard);
            }
        
            // Return to the current player's camera
            Transform playerCameraTarget = _currentPlayer.transform.Find("PlayerTargetCamera");
            if (playerCameraTarget != null)
            {
                CameraManager.Instance.SwitchToPlayer(playerCameraTarget);
            }
            else
            {
                Debug.LogError("PlayerTargetCamera is not set for the current player!");
            }
        }

        private IEnumerator ShowCommunityChestCard()
        {
            // Switch to the Chance camera
            Transform communityChestTarget = CameraManager.Instance.communityChestCameraTarget;
            if (communityChestTarget != null)
            {
                CameraManager.Instance.SwitchToTarget(communityChestTarget);
                yield return new WaitForSeconds(1.5f); // Simulate delay for camera transition
            }
            else
            {
                Debug.LogError("Chance Camera Target is not set in CameraManager!");
            }

            // Draw and display a Community Chest card
            Card drawnCard = CardManager.Instance.DrawCard(TileType.CommunityChest);
            if (drawnCard != null)
            {
                Debug.Log($"Community Chest Card: {drawnCard.cardName} - {drawnCard.cardDescription}");
                // Simulate card display delay (replace with your UI logic if necessary)
                yield return new WaitForSeconds(2f);

                // Apply the card's effect
                CardManager.Instance.ApplyCardEffect(_currentPlayer, drawnCard);
            }

            // Return to the current player's camera
            Transform playerCameraTarget = _currentPlayer.transform.Find("PlayerTargetCamera");
            if (playerCameraTarget is not null)
            {
                CameraManager.Instance.SwitchToPlayer(playerCameraTarget);
            }
            else
            {
                Debug.LogError("PlayerTargetCamera is not set for the current player!");
            }
        }
    }
}

