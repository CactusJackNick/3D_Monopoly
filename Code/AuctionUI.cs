using UnityEngine;
using UnityEngine.UI;

public class AuctionUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject auctionUIPanel;
    public Text propertyNameText;
    public Text currentBidText;
    public Text currentBidderText;
    public Text turnInfoText;
    public Button bidButton;
    public Button passButton;

    public void UpdatePropertyName(string propertyName)
    {
        propertyNameText.text = propertyName;
    }

    public void UpdateBidInfo(int currentBid, string currentBidderName)
    {
        currentBidText.text = $"Current Bid: ${currentBid}";
        currentBidderText.text = currentBidderName != null ? $"Highest Bidder: {currentBidderName}" : "No Bids Yet";
    }
    
    public void UpdateTurnInfo(string playerName)
    {
        // Update UI to show whose turn it is
        turnInfoText.text = $"{playerName}'s Turn";
    }
    
    public void ToggleAuctionUI(bool isActive)
    {
        auctionUIPanel.SetActive(isActive);
    }
}