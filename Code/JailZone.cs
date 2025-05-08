using System;
using Code;
using UnityEngine;

public class JailZone : MonoBehaviour
{
    public GameObject jail;
    
    // Transforms to act as start and end markers for the journey.
    public Transform startMarker;
    public Transform endMarker;

    public float speed = 1.0F;
    private float startTime;
    private float journeyLength;

    private void Start()
    {
        startTime = Time.time;
        journeyLength = Vector3.Distance(startMarker.position, endMarker.position);
    }

    private void OnTriggerEnter(Collider col)
    {
        Player player = col.GetComponent<Player>();
        if (player != null && !player.IsInJail)
        {
            player.EnterJail();
        }
        
        float distCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distCovered / journeyLength;
        jail.transform.position = Vector3.Lerp(startMarker.position, endMarker.position, fractionOfJourney);
    }
    private void OnTriggerExit(Collider col)
    {
        Player player = col.GetComponent<Player>();
        if (player != null && player.IsInJail)
        {
            player.LeaveJail();
        }
        
        float distCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distCovered / journeyLength;
        jail.transform.position = Vector3.Lerp(endMarker.position, startMarker.position, fractionOfJourney);

    }

    private void OnTriggerStay(Collider col)
    {
        if(col.GetComponent<Player>() != null)
            jail.transform.position = endMarker.position;
    }
}
