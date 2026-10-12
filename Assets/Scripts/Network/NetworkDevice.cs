using UnityEngine;
using UnityEngine.EventSystems;
using System.Net; 

public class NetworkDevice : MonoBehaviour, IPointerClickHandler 
{
    public string deviceName;
    public string deviceType;
    public string macAddress;
    
    [Header("Network Info")]
    public string ipAddress = ""; // Now starts empty!
    public string subnetMask = ""; 
    public string gateway = "";

    public bool isOnline = true;
    public bool isPreviewModel = false;

    // --- REMOVED Awake() with random IP logic ---

    public void SetNetworkConfiguration(string ip, string subnet, string gatewayAddress)
    {
        ipAddress = ip;
        subnetMask = subnet;
        gateway = gatewayAddress;
    }

    public void SetStatus(bool status)
    {
        isOnline = status;
    }

    public bool IsInSameSubnet(string targetIP)
    {
        // If either IP is completely blank, they cannot communicate!
        if (string.IsNullOrEmpty(this.ipAddress) || string.IsNullOrEmpty(targetIP)) return false;

        try
        {
            IPAddress ip1 = IPAddress.Parse(this.ipAddress);
            IPAddress ip2 = IPAddress.Parse(targetIP);
            IPAddress mask = IPAddress.Parse(this.subnetMask);

            byte[] ip1Bytes = ip1.GetAddressBytes();
            byte[] ip2Bytes = ip2.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();

            for (int i = 0; i < ip1Bytes.Length; i++)
            {
                if ((ip1Bytes[i] & maskBytes[i]) != (ip2Bytes[i] & maskBytes[i]))
                {
                    return false; 
                }
            }
            return true; 
        }
        catch
        {
            Debug.LogError("Invalid IP Address Format!");
            return false; 
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        DeviceConnector connector = FindFirstObjectByType<DeviceConnector>();
        if (connector != null && connector.isConnectingMode)
        {
            connector.SelectDevice(this);
            return; 
        }

        PacketSender sender = FindFirstObjectByType<PacketSender>();
        if (sender != null && sender.isSendingMode)
        {
            sender.SelectDevice(this);
            return; 
        }

        // --- NEW: Only open the terminal if it is NOT a switch ---
        // Make sure your Switch prefab has "Switch" typed into the Device Type box in the Inspector!
        if (deviceType.ToLower() != "switch")
        {
            DeviceConfigUI configUI = FindFirstObjectByType<DeviceConfigUI>();
            if (configUI != null)
            {
                configUI.OpenTerminal(this);
            }
        }
    }

    void Start()
    {
        TriggerUIRefresh();
    }

    void OnDestroy()
    {
        TriggerUIRefresh();
    }

    private void TriggerUIRefresh()
    {
        PacketUIManager uiManager = FindFirstObjectByType<PacketUIManager>();
        if (uiManager != null)
        {
            uiManager.LoadDevices();
        }
    }
}