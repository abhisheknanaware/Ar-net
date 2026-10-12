using UnityEngine;
using TMPro;

public class DeviceInteraction : MonoBehaviour
{
    private NetworkDevice device;

    public GameObject infoPanel;
    public TextMeshProUGUI infoText;

    void Start()
    {
        device = GetComponent<NetworkDevice>();
    }

    void OnMouseDown()
    {
        if (device == null)
            return;

        if (infoPanel != null)
            infoPanel.SetActive(true);

        if (infoText != null)
        {
            infoText.text =
                "DEVICE INFORMATION\n\n" +
                "Name: " + device.deviceName + "\n" +
                "IP: " + device.ipAddress + "\n" +
                "Status: ONLINE";
        }
    }
}