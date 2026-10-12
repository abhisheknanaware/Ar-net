using UnityEngine;
using TMPro;

public class DeviceConfigUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject configPanel; 
    public TMP_Text titleText;     
    
    [Header("Input Fields")]
    public TMP_InputField ipInputField;
    public TMP_InputField subnetInputField;
    public TMP_InputField gatewayInputField;

    private NetworkDevice activeDevice;

    void Start()
    {
        if (configPanel != null) configPanel.SetActive(false);

        // --- NEW: Listen for when the user finishes typing the IP ---
        if (ipInputField != null)
        {
            ipInputField.onEndEdit.AddListener(AutoFillSubnetMask);
        }
    }

    public void OpenTerminal(NetworkDevice device)
    {
        activeDevice = device;
        titleText.text = device.deviceName + " Terminal";
        
        ipInputField.text = device.ipAddress;
        subnetInputField.text = device.subnetMask;
        gatewayInputField.text = device.gateway;

        configPanel.SetActive(true);
    }

    // --- NEW: Subnet Auto-Fill Logic based on IP Classes ---
    private void AutoFillSubnetMask(string ipEntered)
    {
        // Don't overwrite if the user already manually typed a subnet mask
        if (!string.IsNullOrEmpty(subnetInputField.text)) return;

        string[] octets = ipEntered.Split('.');
        
        if (octets.Length >= 1 && int.TryParse(octets[0], out int firstOctet))
        {
            if (firstOctet >= 1 && firstOctet <= 126)
            {
                subnetInputField.text = "255.0.0.0"; // Class A
            }
            else if (firstOctet >= 128 && firstOctet <= 191)
            {
                subnetInputField.text = "255.255.0.0"; // Class B
            }
            else if (firstOctet >= 192 && firstOctet <= 223)
            {
                subnetInputField.text = "255.255.255.0"; // Class C
            }
        }
    }

    public void ApplyChanges()
    {
        if (activeDevice != null)
        {
            activeDevice.ipAddress = ipInputField.text;
            activeDevice.subnetMask = subnetInputField.text;
            activeDevice.gateway = gatewayInputField.text;
            
            Debug.Log(activeDevice.deviceName + " configuration updated!");
            
            FloatingLabel label = activeDevice.GetComponentInChildren<FloatingLabel>();
            if (label != null) 
            {
                label.RefreshText(); // Tells the label to update itself safely
            } 
        }
        
        configPanel.SetActive(false);
    }

    public void CloseTerminal()
    {
        configPanel.SetActive(false);
    }
}