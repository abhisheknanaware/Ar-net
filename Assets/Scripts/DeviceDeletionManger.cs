using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro; // Added so we can change the UI text!

public class DeviceDeletionManager : MonoBehaviour
{
    [Header("State")]
    public bool isDeletionMode = false;
    public Camera mainCamera;

    [Header("UI Elements")]
    public TMP_Text deleteButtonText; // Drop your button's Text (TMP) here

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public void ToggleDeletionMode()
    {
        isDeletionMode = !isDeletionMode;
        
        // Safety: Turn off connection mode if we are trying to delete
        DeviceConnector connector = FindFirstObjectByType<DeviceConnector>();
        if (connector != null && isDeletionMode && connector.isConnectingMode)
        {
            // Toggle it off properly so its button UI resets back to normal too!
            connector.ToggleConnectingMode(); 
        }

        UpdateUIButton();
        Debug.Log("Deletion Mode is now: " + (isDeletionMode ? "ON" : "OFF"));
    }

    public void UpdateUIButton()
    {
        if (deleteButtonText != null)
        {
            if (isDeletionMode)
            {
                deleteButtonText.text = "CANCEL DELETE";
                deleteButtonText.color = Color.red; // Turns red when active
            }
            else
            {
                deleteButtonText.text = "DELETE DEVICE";
                deleteButtonText.color = Color.black; // Default color
            }
        }
    }

    void Update()
    {
        // --- DESKTOP ONLY: Quick-key fallback ---
        if (Keyboard.current != null && (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
        {
            TryDeleteDeviceFromScreen();
        }

        // --- DESKTOP / MOUSE CLICK IN MODE ---
        if (isDeletionMode && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryDeleteDeviceFromScreen();
        }
    }

    void TryDeleteDeviceFromScreen()
    {
        if (mainCamera == null || Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            NetworkDevice device = hit.collider.GetComponentInParent<NetworkDevice>();
            if (device != null && !device.isPreviewModel)
            {
                ExecuteDeletion(device);
            }
        }
    }

    public void DeleteDevice(NetworkDevice device)
    {
        // Only allow direct HoloLens deletion if we are in deletion mode
        if (!isDeletionMode) return;
        ExecuteDeletion(device);
    }

    private void ExecuteDeletion(NetworkDevice device)
    {
        string deletedName = device.deviceName;

        // 1. Clean up cables
        NetworkConnection[] allCables = FindObjectsByType<NetworkConnection>(FindObjectsSortMode.None);
        foreach (NetworkConnection cable in allCables)
        {
            if (cable.deviceA == device || cable.deviceB == device)
            {
                Destroy(cable.gameObject);
            }
        }

        // 2. Destroy device
        Destroy(device.gameObject);
        Debug.Log("Deleted device: " + deletedName);

        // --- THE FIX ---
        // I removed 'isDeletionMode = false;' from here! 
        // Now the script stays in Deletion Mode until you click the Cancel button!

        // 3. Refresh UIs
        StartCoroutine(RefreshUIsNextFrame());
    }

    IEnumerator RefreshUIsNextFrame()
    {
        yield return new WaitForEndOfFrame();

        PacketUIManager packetUI = FindFirstObjectByType<PacketUIManager>();
        if (packetUI != null) packetUI.LoadDevices();

        ConnectionUIManager connectionUI = FindFirstObjectByType<ConnectionUIManager>();
        if (connectionUI != null) connectionUI.LoadConnections();
    }
}