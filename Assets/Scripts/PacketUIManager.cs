using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PacketUIManager : MonoBehaviour
{
    public TMP_Dropdown sourceDropdown;
    public TMP_Dropdown destinationDropdown;
    public NetworkLogUI networkLog;

    private NetworkManager networkManager;
    private List<NetworkDevice> devices = new List<NetworkDevice>();

    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();

        if (networkManager == null)
        {
            Debug.LogError("PacketUIManager: NetworkManager not found.");
            return;
        }

        if (networkLog == null)
        {
            networkLog = FindFirstObjectByType<NetworkLogUI>();
        }

        // Initial load
        LoadDevices();
    }

    void OnEnable()
    {
        LoadDevices();
    }

    public void LoadDevices()
    {
        if (sourceDropdown == null || destinationDropdown == null) return;

        string currentSource = sourceDropdown.options.Count > 0 ? sourceDropdown.options[sourceDropdown.value].text : "";
        string currentDest = destinationDropdown.options.Count > 0 ? destinationDropdown.options[destinationDropdown.value].text : "";

        devices.Clear();

        NetworkDevice[] foundDevices = FindObjectsByType<NetworkDevice>(FindObjectsSortMode.None);

        foreach (NetworkDevice device in foundDevices)
        {
            if (device != null && !device.isPreviewModel)
            {
                devices.Add(device);
            }
        }

        devices.Sort((a, b) => a.deviceName.CompareTo(b.deviceName));

        List<string> deviceNames = new List<string>();
        deviceNames.Add("-- Select Device --");

        foreach (NetworkDevice device in devices)
        {
            deviceNames.Add(device.deviceName);
        }

        sourceDropdown.ClearOptions();
        destinationDropdown.ClearOptions();

        sourceDropdown.AddOptions(deviceNames);
        destinationDropdown.AddOptions(deviceNames);

        sourceDropdown.value = 0;
        destinationDropdown.value = 0;

        for (int i = 0; i < sourceDropdown.options.Count; i++)
        {
            if (sourceDropdown.options[i].text == currentSource) sourceDropdown.value = i;
            if (destinationDropdown.options[i].text == currentDest) destinationDropdown.value = i;
        }

        sourceDropdown.RefreshShownValue();
        destinationDropdown.RefreshShownValue();
    }

    public void SendPacket()
    {
        if (sourceDropdown.value == 0 || destinationDropdown.value == 0)
        {
            Debug.LogWarning("Please select both a valid source and destination.");
            return;
        }

        if (sourceDropdown.value == destinationDropdown.value)
        {
            Debug.LogWarning("Source and Destination cannot be the same device.");
            return;
        }

        NetworkDevice source = devices[sourceDropdown.value - 1];
        NetworkDevice destination = devices[destinationDropdown.value - 1];

        if (source == null || destination == null)
        {
            Debug.LogError("Source or destination is null.");
            return;
        }

        // --- NEW: Strict Layer 3 Two-Way Validation ---
        if (!source.IsInSameSubnet(destination.ipAddress))
        {
            NetworkDevice[] allDevicesInScene = FindObjectsByType<NetworkDevice>(FindObjectsSortMode.None);
            
            // 1. Verify Source Gateway IS A ROUTER
            bool sourceGatewayValid = false;
            if (!string.IsNullOrEmpty(source.gateway))
            {
                foreach (NetworkDevice dev in allDevicesInScene)
                {
                    if (dev.ipAddress == source.gateway && dev.deviceType.ToLower() == "router") 
                    {
                        sourceGatewayValid = true;
                        break;
                    }
                }
            }

            if (!sourceGatewayValid)
            {
                Debug.LogWarning("DESTINATION UNREACHABLE: Source PC has missing or invalid Gateway Router.");
                if (networkLog != null) networkLog.ShowFailedPacket(source, destination, "Failed: Source Gateway Unreachable.");
                if (NetworkStatistics.Instance != null) { NetworkStatistics.Instance.PacketSent(); NetworkStatistics.Instance.PacketFailed(); }
                
                DeviceLED errorLed = source.GetComponentInChildren<DeviceLED>();
                if (errorLed != null) errorLed.BlinkError();
                return;
            }

            // 2. Verify Destination Gateway IS A ROUTER
            bool destGatewayValid = false;
            if (destination.deviceType.ToLower() == "router")
            {
                destGatewayValid = true;
            }
            else if (!string.IsNullOrEmpty(destination.gateway))
            {
                foreach (NetworkDevice dev in allDevicesInScene)
                {
                    if (dev.ipAddress == destination.gateway && dev.deviceType.ToLower() == "router") 
                    {
                        destGatewayValid = true;
                        break;
                    }
                }
            }

            if (!destGatewayValid)
            {
                Debug.LogWarning("REQUEST TIMED OUT: Target PC cannot route the reply back!");
                if (networkLog != null) networkLog.ShowFailedPacket(source, destination, "Timeout: Target Gateway Invalid.");
                if (NetworkStatistics.Instance != null) { NetworkStatistics.Instance.PacketSent(); NetworkStatistics.Instance.PacketFailed(); }
                
                DeviceLED errorLed = source.GetComponentInChildren<DeviceLED>();
                if (errorLed != null) errorLed.BlinkError();
                return;
            }

            Debug.Log("Cross-subnet traffic authorized. Both devices have valid gateway routers.");
        }
        // --------------------------------------

        // --- PATHFINDING & LOGGING ---
        List<NetworkDevice> path = networkManager.FindPath(source, destination);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("PacketUIManager: NO ROUTE AVAILABLE.");
            
            if (networkLog != null) 
            {
                networkLog.ShowFailedPacket(source, destination, "No physical cable connection found.");
            }
            
            if (NetworkStatistics.Instance != null)
            {
                NetworkStatistics.Instance.PacketSent();
                NetworkStatistics.Instance.PacketFailed();
            }
            
            return;
        }

        // Spawn the packet
        GameObject packetObj = Instantiate(networkManager.packetPrefab, source.transform.position, Quaternion.identity);
        Packet packet = packetObj.GetComponent<Packet>();
        
        if (packet != null)
        {
            packet.Initialize(source, destination, path);
            
            if (NetworkStatistics.Instance != null)
            {
                NetworkStatistics.Instance.PacketSent();
            }
        }
    }
}