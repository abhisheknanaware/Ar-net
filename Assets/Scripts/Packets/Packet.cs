using System.Collections.Generic;
using UnityEngine;

public class Packet : MonoBehaviour
{
    public float speed = 3f;
    private NetworkDevice source;
    private NetworkDevice destination;
    private List<NetworkDevice> path;
    
    private int currentTargetIndex = 0;
    private float heightOffset = 0.3f; 
    
    private NetworkLogUI logUI;
    private NetworkConnection currentCable;

    public void Initialize(NetworkDevice src, NetworkDevice dest, List<NetworkDevice> route)
    {
        source = src;
        destination = dest;
        path = route;
        
        logUI = FindFirstObjectByType<NetworkLogUI>();

        if (logUI != null)
        {
            logUI.StartPacket(source, destination);
        }

        transform.position = path[0].transform.position + (Vector3.up * heightOffset);
        currentTargetIndex = 1; 

        // --- NEW LED TRIGGER: Flash the source device when the packet spawns! ---
        DeviceLED sourceLED = source.GetComponentInChildren<DeviceLED>();
        if (sourceLED != null) sourceLED.BlinkData();
        
        UpdateCurrentCable();
    }

    void Update()
    {
        if (path == null || path.Count == 0 || currentTargetIndex >= path.Count) return;

        if (currentCable != null && !currentCable.isConnected)
        {
            Debug.LogWarning("PACKET DROPPED! Cable was disconnected mid-flight.");
            
            currentCable.SetActive(false); 
            
            if (logUI != null)
            {
                logUI.FailedPacket("Cable cut while traveling from " + path[currentTargetIndex - 1].deviceName + " to " + path[currentTargetIndex].deviceName);
            }
            
            if (NetworkStatistics.Instance != null)
            {
                NetworkStatistics.Instance.PacketFailed();
            }

            // --- NEW LED TRIGGER: Flash red on the device the packet just left! ---
            DeviceLED errorLED = path[currentTargetIndex - 1].GetComponentInChildren<DeviceLED>();
            if (errorLED != null) errorLED.BlinkError();
            
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = path[currentTargetIndex].transform.position + (Vector3.up * heightOffset);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Did we reach the next device?
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            if (logUI != null)
            {
                logUI.AddHop(path[currentTargetIndex - 1], path[currentTargetIndex]);
            }

            if (NetworkStatistics.Instance != null)
            {
                NetworkStatistics.Instance.AddHop();
            }

            // Turn OFF the glow for the cable we just finished crossing
            if (currentCable != null)
            {
                currentCable.SetActive(false);
            }

            // --- NEW LED TRIGGER: Flash the device we just arrived at! ---
            DeviceLED arriveLED = path[currentTargetIndex].GetComponentInChildren<DeviceLED>();
            if (arriveLED != null) arriveLED.BlinkData();

            currentTargetIndex++; 

            if (currentTargetIndex >= path.Count)
            {
                CompleteTransfer();
            }
            else
            {
                UpdateCurrentCable();
            }
        }
    }
    
    private void UpdateCurrentCable()
    {
        if (currentTargetIndex >= path.Count) return;
        
        NetworkDevice nodeA = path[currentTargetIndex - 1];
        NetworkDevice nodeB = path[currentTargetIndex];
        
        NetworkConnection[] allCables = FindObjectsByType<NetworkConnection>(FindObjectsSortMode.None);
        foreach(var cable in allCables)
        {
            if ((cable.deviceA == nodeA && cable.deviceB == nodeB) || 
                (cable.deviceA == nodeB && cable.deviceB == nodeA))
            {
                currentCable = cable;
                currentCable.SetActive(true);
                break;
            }
        }
    }

    private void CompleteTransfer()
    {
        Debug.Log($"SUCCESS: Packet delivered from {source.deviceName} to {destination.deviceName}!");
        
        if (logUI != null)
        {
            logUI.CompletePacket(destination);
        }

        if (NetworkStatistics.Instance != null)
        {
            NetworkStatistics.Instance.PacketDelivered();
        }

        Destroy(gameObject);
    }
}