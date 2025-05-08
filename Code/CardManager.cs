using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code
{
    public class CardManager : MonoBehaviour
    {
        public static CardManager Instance;
        private static readonly int Spin = Animator.StringToHash("Spin");

        public List<Card> chanceCards; // List of Chance cards
        public List<Card> communityChestCards; // List of Community Chest cards
        
        public Transform chanceCardStack; // Parent object for visual stack of chance cards
        public Transform communityChestCardStack; // Parent object for visual stack of community chest cards
        public Transform chanceCardDisplay; // Position where the Chance card is shown when drawn
        public Transform communityChestDisplay; // Position where the Com Chest card is shown when drawn

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public Card DrawCard(TileType tileType)
        {
            List<Card> cardPool = tileType == TileType.Chance ? chanceCards : communityChestCards;

            if (cardPool == null || cardPool.Count == 0)
            {
                Debug.LogError($"No cards available for {tileType}!");
                return null;
            }

            // Randomly pick a card
            int randomIndex = Random.Range(0, cardPool.Count);
            Card drawnCard = cardPool[randomIndex];

            Debug.Log($"Drawn Card: {drawnCard.cardName} - {drawnCard.cardDescription}");
            
            // Display the corresponding card GameObject
            DisplayCardGameObject(drawnCard, tileType == TileType.Chance);
            
            return drawnCard;
        }
        
        private void DisplayCardGameObject(Card drawnCard, bool isChance)
        {
            if (drawnCard.cardPrefab is null)
            {
                Debug.LogError($"No prefab assigned for card: {drawnCard.cardName}!");
                return;
            }
        
            // Determine the correct display position and stack
            Transform displayPosition = isChance ? chanceCardDisplay : communityChestDisplay;
            Transform stack = isChance ? chanceCardStack : communityChestCardStack;
        
            // Instantiate the card prefab at the correct display position
            GameObject cardObject = Instantiate(
                drawnCard.cardPrefab,
                displayPosition.position,
                Quaternion.identity
            );
        
            // Optionally parent it to the display position (for organization)
            cardObject.transform.SetParent(displayPosition);
        
            // Adjust the card's scale and rotation for proper display
            cardObject.transform.localRotation = Quaternion.identity;
        
            // Trigger a spin animation if Animator is attached
            Animator animator = cardObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(Spin);
            }
        
            // Return the card to the correct stack after a short delay
            StartCoroutine(ReturnCardToStackAfterDelay(cardObject, stack));
        }


        private System.Collections.IEnumerator ReturnCardToStackAfterDelay(GameObject cardObject, bool isChance)
        {
            yield return new WaitForSeconds(3f); // Wait before returning the card to the stack

            Transform stack = isChance ? chanceCardStack : communityChestCardStack;

            // Move the card back to the stack
            cardObject.transform.SetParent(stack);
            cardObject.transform.localPosition = new Vector3(0, -stack.childCount * 0.01f, 0);
            cardObject.transform.localRotation = Quaternion.identity;
            
            //Delete clone after some period of time
            Destroy(cardObject, 2f);
        }

        public void ApplyCardEffect(Player player, Card card)
        {
            PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
            if (playerManager == null)
            {
                Debug.LogError("PlayerManager not found!");
                return;
            }

            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();

            if (playerMovement == null)
            {
                Debug.LogError("PlayerMovement script not found!");
                return;
            }

            // Resolve current player information
            Transform playerTransform = player.transform;
            List<Transform> waypoints = playerManager._playerWaypoints[playerManager.currentPlayerIndex];
            int currentPos = playerManager._playerPositions[playerTransform];

            switch (card.effectType)
            {
                case CardEffect.GainMoney:
                    player.money += card.moneyChange;
                    Debug.Log($"{player.playerName} gains {card.moneyChange} money!");
                    PlayerManager.Instance.UpdatePlayerUI();
                    break;

                case CardEffect.LoseMoney:
                    player.money += card.moneyChange; // card.moneyChange is negative for losing money
                    Debug.Log($"{player.playerName} loses {Mathf.Abs(card.moneyChange)} money!");
                    PlayerManager.Instance.UpdatePlayerUI();
                    break;

                case CardEffect.MoveTiles:
                    int tilesToMove = Mathf.RoundToInt(card.moveToTile.x); // Positive = forward, Negative = backward
                    int targetPosition = (currentPos + tilesToMove + waypoints.Count) % waypoints.Count;
                    
                    Debug.Log($"Current Position: {currentPos}, Tiles to Move: {tilesToMove}, Target Position: {targetPosition}");

                    StartCoroutine(playerMovement.MovePlayer(
                        playerTransform,
                        waypoints,
                        currentPos,
                        tilesToMove,
                        () => OnMovementComplete(playerTransform, targetPosition)
                    ));
                    break;

                case CardEffect.GoToTile:
                    int destinationTileIndex = Mathf.RoundToInt(card.moveToTile.x); // Assuming x stores the tile index
                    int tilesToMoveGoTo = (destinationTileIndex - currentPos + waypoints.Count) % waypoints.Count;
                    StartCoroutine(playerMovement.MovePlayer(
                        playerTransform,
                        waypoints,
                        currentPos,
                        tilesToMoveGoTo,
                        () => OnMovementComplete(playerTransform, destinationTileIndex)
                    ));
                    break;
                
                case CardEffect.JailFree:
                    player.getOutOfJailFreeCards++;
                    Debug.Log($"{player.playerName} jail free card acquired!");
                    break;

                case CardEffect.PayAllPlayers:
                    Debug.Log($"{player.playerName} pays all other players {card.moneyChange}!");
                    // Implement logic to pay all players
                    break;

                case CardEffect.CollectFromAllPlayers:
                    Debug.Log($"{player.playerName} collects {card.moneyChange} from each player!");
                    // Implement logic to collect from all players
                    break;

                default:
                    Debug.LogError("Unknown card effect!");
                    break;
            }
        }

        private void OnMovementComplete(Transform playerTransform, int newTileIndex)
        {
            PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
            if (playerManager == null)
            {
                Debug.LogError("PlayerManager not found!");
                return;
            }

            // Update the player's current position in PlayerManager
            playerManager._playerPositions[playerTransform] = newTileIndex;

            // Retrieve the new tile and trigger any necessary logic
            Transform newTile = playerManager._playerWaypoints[playerManager.currentPlayerIndex][newTileIndex];
            Debug.Log($"{playerTransform.name} moved to tile: {newTile.name}");

            // If there's a PropertyTrigger or special tile logic, handle it
            PropertyTrigger propertyTrigger = newTile.GetComponent<PropertyTrigger>();
            if (propertyTrigger != null)
            {
                propertyTrigger.OnTriggerEnter(playerTransform.GetComponent<Collider>());
            }
        }
    }
}