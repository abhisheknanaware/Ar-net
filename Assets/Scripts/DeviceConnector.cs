using UnityEngine;
using TMPro;

public class DeviceConnector : MonoBehaviour
{
    [Header("Core References")]
    public NetworkManager networkManager;
    public GameObject cablePrefab;
    
    [Header("UI Elements")]
    public TMP_Text connectButtonText; 

    [Header("State")]
    public bool isConnectingMode = false;
    private NetworkDevice selectedDeviceA;

    public void ToggleConnectingMode()
    {
        isConnectingMode = !isConnectingMode;
        selectedDeviceA = null; // Always clear half-selections if user cancels
        
        // Safety feature: Turn OFF Delete mode if we are trying to Connect
        DeviceDeletionManager deleteManager = FindFirstObjectByType<DeviceDeletionManager>();
        if (deleteManager != null && isConnectingMode && deleteManager.isDeletionMode)
        {
            deleteManager.ToggleDeletionMode(); 
        }

        UpdateUIButton();

        Debug.Log("Connecting Mode is now: " + (isConnectingMode ? "ON" : "OFF"));
    }

    public void UpdateUIButton()
    {
        if (connectButtonText != null)
        {
            if (isConnectingMode)
            {
                connectButtonText.text = "CANCEL CONNECT";
                connectButtonText.color = Color.red; 
            }
            else
            {
                connectButtonText.text = "CONNECT DEVICES";
                connectButtonText.color = Color.black; 
            }
        }
    }

    public void SelectDevice(NetworkDevice device)
    {
        if (!isConnectingMode) return;

        // User clicks the FIRST device
        if (selectedDeviceA == null)
        {
            selectedDeviceA = device;
            Debug.Log("Selected First Device: " + selectedDeviceA.deviceName);
            
            if (connectButtonText != null)
            {
                connectButtonText.text = "SELECT 2ND DEVICE";
            }
        }
        // User clicks the SECOND device
        else if (selectedDeviceA != device)
        {
            NetworkDevice selectedDeviceB = device;

            // Check for existing cables
            NetworkConnection existingCable = null;
            NetworkConnection[] allCables = FindObjectsByType<NetworkConnection>(FindObjectsSortMode.None);
            
            foreach (var cable in allCables)
            {
                if ((cable.deviceA == selectedDeviceA && cable.deviceB == selectedDeviceB) || 
                    (cable.deviceA == selectedDeviceB && cable.deviceB == selectedDeviceA))
                {
                    existingCable = cable;
                    break;
                }
            }

            if (existingCable != null)
            {
                if (!existingCable.isConnected)
                {
                    existingCable.Reconnect();
                    Debug.Log("Reconnected existing cable.");
                }
                else
                {
                    Debug.LogWarning("These devices are already connected!");
                }
            }
            else
            {
                if (networkManager != null && cablePrefab != null)
                {
                    networkManager.ConnectDevices(selectedDeviceA, selectedDeviceB, cablePrefab);
                }
            }
            
            // Auto-refresh the connection UI
            ConnectionUIManager connectionUI = FindFirstObjectByType<ConnectionUIManager>();
            if (connectionUI != null)
            {
                connectionUI.LoadConnections();
            }

            // --- THE FIX FOR CONTINUOUS CONNECTION ---
            // Reset the selection for the NEXT connection, but DO NOT turn off the mode!
            selectedDeviceA = null; 
            
            // Reset the button back to the "CANCEL CONNECT" state
            UpdateUIButton(); 
            
            Debug.Log("Connection complete. Connecting Mode is STILL ON for the next cable.");
        }
    }
}