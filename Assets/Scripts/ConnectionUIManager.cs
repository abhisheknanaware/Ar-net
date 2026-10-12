using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUIManager : MonoBehaviour
{
    public TMP_Dropdown connectionDropdown;
    public Button connectionButton;
    public TMP_Text connectionStatusText;

    private List<NetworkConnection> validConnections = new List<NetworkConnection>();
    private NetworkConnection selectedConnection;

    void Start()
    {
        if (connectionButton != null)
        {
            // Ensure we don't accidentally add the listener multiple times
            connectionButton.onClick.RemoveAllListeners();
            connectionButton.onClick.AddListener(ToggleConnection);
        }

        if (connectionDropdown != null)
        {
            connectionDropdown.onValueChanged.RemoveAllListeners();
            connectionDropdown.onValueChanged.AddListener(OnConnectionSelected);
        }
    }

    // --- THIS IS THE MAGIC FIX FOR DYNAMIC CABLES ---
    void OnEnable()
    {
        LoadConnections();
    }

    public void LoadConnections()
    {
        if (connectionDropdown == null)
        {
            Debug.LogError("ConnectionUIManager: Connection Dropdown reference is NULL.");
            return;
        }

        validConnections.Clear();
        connectionDropdown.ClearOptions();

        // Find every single cable in the scene
        NetworkConnection[] foundConnections = FindObjectsByType<NetworkConnection>(FindObjectsSortMode.None);

        List<string> options = new List<string>();
        
        // Add a placeholder so the UI looks clean
        options.Add("-- Select Connection --");

        foreach (NetworkConnection connection in foundConnections)
        {
            if (connection == null || connection.deviceA == null || connection.deviceB == null)
                continue;

            validConnections.Add(connection);
            string connectionName = connection.deviceA.deviceName + " ↔ " + connection.deviceB.deviceName;
            options.Add(connectionName);
        }

        connectionDropdown.AddOptions(options);

        // Reset the UI to the placeholder state
        connectionDropdown.value = 0;
        selectedConnection = null;
        
        // Hide button/status until a real connection is selected
        if (connectionButton != null) connectionButton.gameObject.SetActive(false);
        if (connectionStatusText != null) connectionStatusText.text = "Status: WAITING";
    }

    void OnConnectionSelected(int index)
    {
        // Index 0 is the "-- Select Connection --" placeholder
        if (index == 0)
        {
            selectedConnection = null;
            if (connectionButton != null) connectionButton.gameObject.SetActive(false);
            if (connectionStatusText != null) connectionStatusText.text = "Status: WAITING";
            return;
        }

        // Subtract 1 because of the placeholder
        int adjustedIndex = index - 1;

        if (adjustedIndex < 0 || adjustedIndex >= validConnections.Count)
            return;

        selectedConnection = validConnections[adjustedIndex];
        
        // Show the button now that a valid connection is selected
        if (connectionButton != null) connectionButton.gameObject.SetActive(true);

        UpdateUI();
    }

    void ToggleConnection()
    {
        if (selectedConnection == null)
            return;

        if (selectedConnection.isConnected)
        {
            selectedConnection.Disconnect();
        }
        else
        {
            selectedConnection.Reconnect();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (selectedConnection == null)
            return;

        if (connectionStatusText != null)
        {
            if (selectedConnection.isConnected)
            {
                connectionStatusText.text = "Status: CONNECTED";
                connectionStatusText.color = Color.green; // Optional: Adds a visual cue
            }
            else
            {
                connectionStatusText.text = "Status: DISCONNECTED";
                connectionStatusText.color = Color.red;
            }
        }

        if (connectionButton != null)
        {
            TMP_Text buttonText = connectionButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                if (selectedConnection.isConnected)
                {
                    buttonText.text = "DISCONNECT";
                }
                else
                {
                    buttonText.text = "RECONNECT";
                }
            }
        }
    }
}