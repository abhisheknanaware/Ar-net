using UnityEngine;
using System.Collections.Generic;

public class PacketSender : MonoBehaviour
{
    [Header("Core References")]
    public NetworkManager networkManager;

    [Header("State")]
    public bool isSendingMode = false;
    private NetworkDevice selectedSource;

    // Hook this to a new UI Button called "Send Packet"
    public void ToggleSendingMode()
    {
        isSendingMode = !isSendingMode;
        selectedSource = null;
        Debug.Log("Send Packet Mode is now: " + (isSendingMode ? "ON" : "OFF"));
    }

    public void SelectDevice(NetworkDevice device)
    {
        if (!isSendingMode) return;

        if (selectedSource == null)
        {
            selectedSource = device;
            Debug.Log("Packet Source Selected: " + selectedSource.deviceName);
        }
        else if (selectedSource != device)
        {
            NetworkDevice target = device;
            bool canSend = false;

            // --- NEW: Strict Layer 3 Two-Way Validation ---
            if (selectedSource.IsInSameSubnet(target.ipAddress))
            {
                canSend = true; // Same network, good to go!
            }
            else 
            {
                // Different network! Both devices need a valid gateway to complete a Ping.
                bool sourceHasValidGateway = false;
                bool targetHasValidGateway = false;

                NetworkDevice[] allDevices = FindObjectsByType<NetworkDevice>(FindObjectsSortMode.None);
                
                // 1. Check if Source's Gateway exists AND is a Router
                if (!string.IsNullOrEmpty(selectedSource.gateway))
                {
                    foreach (NetworkDevice dev in allDevices)
                    {
                        if (dev.ipAddress == selectedSource.gateway && dev.deviceType.ToLower() == "router") 
                        {
                            sourceHasValidGateway = true;
                            break;
                        }
                    }
                }

                // 2. Check if Target's Gateway exists AND is a Router
                if (target.deviceType.ToLower() == "router")
                {
                    targetHasValidGateway = true; // Routers don't need a gateway to reply
                }
                else if (!string.IsNullOrEmpty(target.gateway))
                {
                    foreach (NetworkDevice dev in allDevices)
                    {
                        if (dev.ipAddress == target.gateway && dev.deviceType.ToLower() == "router") 
                        {
                            targetHasValidGateway = true;
                            break;
                        }
                    }
                }

                // 3. Final Verdict
                if (sourceHasValidGateway && targetHasValidGateway)
                {
                    canSend = true;
                }
                else
                {
                    if (!sourceHasValidGateway) Debug.LogWarning("DESTINATION UNREACHABLE: Source PC is missing a valid Gateway Router!");
                    if (!targetHasValidGateway) Debug.LogWarning("REQUEST TIMED OUT: Target PC has no Gateway Router to route the reply back!");
                    
                    DeviceLED led = selectedSource.GetComponentInChildren<DeviceLED>();
                    if (led != null) led.BlinkError();
                }
            }
            // --------------------------------------

            // 4. Actually send the packet if validated
            if (canSend)
            {
                List<NetworkDevice> path = networkManager.FindPath(selectedSource, target);
                if (path != null && path.Count > 0)
                {
                    networkManager.CreatePacket(selectedSource, target, path);
                }
                else
                {
                    Debug.LogWarning("NO PHYSICAL ROUTE AVAILABLE!");
                    DeviceLED led = selectedSource.GetComponentInChildren<DeviceLED>();
                    if (led != null) led.BlinkError();
                }
            }

            // Reset for the next transfer
            selectedSource = null;
            isSendingMode = false; 
            Debug.Log("Send Packet Mode is now OFF.");
        }
    }
}