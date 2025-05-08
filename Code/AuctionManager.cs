using System.Collections.Generic;
using Code;
using UnityEngine;

public class AuctionManager : MonoBehaviour
{
    public Property auctionedProperty;
    public List<Player> activeBidders = new List<Player>();
    private int _currentBidderIndex = 0; // Tracks the current player's turn
    private int _currentBid;
    private readonly int _bidIncrement = 10;
    private Player _currentBidder;

    private AuctionUI _auctionUI;

    private HashSet<Player> playersWhoPassed = new HashSet<Player>(); // Tracks players who passed

    private void Awake()
    {
        _auctionUI = FindFirstObjectByType<AuctionUI>();
    }

    public void StartAuction(Property property, List<Player> players)
    {
        // Set up the initial auction
        auctionedProperty = property;
        activeBidders = new List<Player>(players);
        _currentBidderIndex = 0;
        _currentBid = 0;
        _currentBidder = null;

        // Clear pass tracking
        playersWhoPassed.Clear();

        // Initialize UI
        _auctionUI.UpdatePropertyName(property.propertyData.propertyName);
        _auctionUI.UpdateBidInfo(_currentBid, null);
        _auctionUI.ToggleAuctionUI(true);

        // Add UI button listeners
        _auctionUI.bidButton.onClick.RemoveAllListeners();
        _auctionUI.bidButton.onClick.AddListener(PlaceBid);

        _auctionUI.passButton.onClick.RemoveAllListeners();
        _auctionUI.passButton.onClick.AddListener(PassTurn);

        UpdateUIForCurrentBidder();
    }

    private void PlaceBid()
    {
        if (activeBidders.Count == 0) return;

        _currentBid += _bidIncrement;
        _currentBidder = activeBidders[_currentBidderIndex];
        Debug.Log($"{_currentBidder.playerName} placed a bid of ${_currentBid}.");

        // Reset pass tracking because a bid resets the decision-making cycle
        playersWhoPassed.Clear();

        // Update UI
        _auctionUI.UpdateBidInfo(_currentBid, _currentBidder.playerName);

        // Move to the next bidder
        NextBidder();
    }

    private void PassTurn()
    {
        if (activeBidders.Count == 0) return;

        Player passingPlayer = activeBidders[_currentBidderIndex];
        Debug.Log($"{passingPlayer.playerName} passed.");

        activeBidders.RemoveAt(_currentBidderIndex);

        // Check if there are no active bidders left
        if (activeBidders.Count == 0)
        {
            Debug.Log("All players have passed. The property remains unowned.");
            EndAuction();
            return;
        }

        // Adjust currentBidderIndex to wrap around to the next player
        _currentBidderIndex %= activeBidders.Count;

        // Update the auction UI for the next bidder
        _auctionUI.UpdateTurnInfo(activeBidders[_currentBidderIndex].playerName);
    }

    private void NextBidder()
    {
        if (activeBidders.Count == 0) return;

        // Move to the next bidder in the list
        _currentBidderIndex++;
        if (_currentBidderIndex >= activeBidders.Count)
        {
            _currentBidderIndex = 0; // Wrap around to the first bidder
        }

        // Skip players who have already passed
        while (playersWhoPassed.Contains(activeBidders[_currentBidderIndex]))
        {
            _currentBidderIndex++;
            if (_currentBidderIndex >= activeBidders.Count)
            {
                _currentBidderIndex = 0; // Wrap around
            }
        }

        UpdateUIForCurrentBidder();
    }

    private void UpdateUIForCurrentBidder()
    {
        if (activeBidders.Count == 0) return;

        Player currentBidder = activeBidders[_currentBidderIndex];
        Debug.Log($"It's now {currentBidder.playerName}'s turn to bid.");

        // Update the auction UI for the current bidder
        _auctionUI.UpdateTurnInfo(currentBidder.playerName);
    }

    private void EndAuction()
    {
        if (_currentBidder is null)
        {
            Debug.Log("No one bid on the property.");
        }
        else
        {
            Debug.Log($"{_currentBidder.playerName} won the auction for ${_currentBid}.");
            auctionedProperty.AssignOwner(_currentBidder);
            _currentBidder.money -= _currentBid;
        }

        // Reset and hide UI
        _auctionUI.ToggleAuctionUI(false);
        auctionedProperty = null;
        activeBidders.Clear();
        playersWhoPassed.Clear();
    }
}
