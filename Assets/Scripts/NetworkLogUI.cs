using TMPro;
using UnityEngine;

public class NetworkLogUI : MonoBehaviour
{
    public TMP_Text logText;
    public RectTransform panelRect;
    public RectTransform textRect;

    public float padding = 40f;
    public float minHeight = 180f;
    public float maxHeight = 450f;

    public int maxPackets = 3;

    private string[] packetLogs = new string[10];
    private int packetCount = 0;

    private string currentPacket = "";

    public void ClearLog()
    {
        packetCount = 0;
        currentPacket = "";

        for (int i = 0; i < packetLogs.Length; i++)
        {
            packetLogs[i] = "";
        }

        RefreshLog();
    }

    public void StartPacket(NetworkDevice source, NetworkDevice destination)
    {
        currentPacket = source.deviceName + " → " + destination.deviceName + "\nSTATUS: TRANSMITTING...";
        RefreshLog();
    }

    public void AddHop(NetworkDevice from, NetworkDevice to)
    {
        currentPacket += "\n✓ " + from.deviceName + " → " + to.deviceName;
        RefreshLog();
    }

    public void AddHopFailure(NetworkDevice from, NetworkDevice to)
    {
        currentPacket += "\n✕ " + from.deviceName + " → " + to.deviceName;
        RefreshLog();
    }

    public void CompletePacket(NetworkDevice destination)
    {
        currentPacket += "\nSTATUS: DELIVERED ✓";
        AddPacketToHistory();
        currentPacket = "";
        RefreshLog();
    }

    public void FailedPacket(string reason)
    {
        currentPacket += "\nSTATUS: FAILED ✕\n" + reason;
        AddPacketToHistory();
        currentPacket = "";
        RefreshLog();
    }

    public void ShowFailedPacket(NetworkDevice source, NetworkDevice destination, string reason)
    {
        Debug.Log("NO ROUTE LOG: " + source.deviceName + " → " + destination.deviceName);

        currentPacket = source.deviceName + " → " + destination.deviceName + 
                        "\n✕ NO ROUTE AVAILABLE" + 
                        "\nSTATUS: FAILED" + 
                        "\nREASON: " + reason;

        AddPacketToHistory();
        currentPacket = "";
        RefreshLog();
    }

    // === THE NEW METHOD TO HANDLE DISCONNECTIONS ===
    public void LogDisconnection(NetworkDevice deviceA, NetworkDevice deviceB)
    {
        // Treat the disconnection as an event in the history log
        currentPacket = "SYSTEM ALERT\n" + 
                        "✕ CABLE DISCONNECTED\n" + 
                        deviceA.deviceName + " ⇎ " + deviceB.deviceName;

        AddPacketToHistory();
        currentPacket = "";
        RefreshLog();
    }

    void AddPacketToHistory()
    {
        if (string.IsNullOrEmpty(currentPacket))
            return;

        if (packetCount < maxPackets)
        {
            packetLogs[packetCount] = currentPacket;
            packetCount++;
        }
        else
        {
            for (int i = 0; i < maxPackets - 1; i++)
            {
                packetLogs[i] = packetLogs[i + 1];
            }

            packetLogs[maxPackets - 1] = currentPacket;
        }
    }

    void RefreshLog()
    {
        if (logText == null)
            return;

        string finalText = "NETWORK ACTIVITY\n\n";

        for (int i = packetCount - 1; i >= 0; i--)
        {
            finalText += "EVENT #" + (i + 1) + "\n" + packetLogs[i] + "\n\n";
        }

        if (!string.IsNullOrEmpty(currentPacket))
        {
            finalText += "CURRENT PACKET\n" + currentPacket;
        }

        if (packetCount == 0 && string.IsNullOrEmpty(currentPacket))
        {
            finalText += "Waiting for activity...";
        }

        logText.text = finalText;

        Canvas.ForceUpdateCanvases();

        float textHeight = logText.preferredHeight;
        float requiredHeight = textHeight + padding;
        float newHeight = Mathf.Clamp(requiredHeight, minHeight, maxHeight);

        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, newHeight);
        }

        if (textRect != null)
        {
            textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, Mathf.Min(textHeight, maxHeight - padding));
        }

        if (textHeight > maxHeight - padding)
        {
            logText.overflowMode = TextOverflowModes.Ellipsis;
        }
        else
        {
            logText.overflowMode = TextOverflowModes.Overflow;
        }
    }
}