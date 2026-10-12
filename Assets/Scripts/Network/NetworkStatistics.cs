using UnityEngine;

public class NetworkStatistics : MonoBehaviour
{
    public static NetworkStatistics Instance;

    public int packetsSent;
    public int packetsDelivered;
    public int packetsFailed;
    public int totalHops;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PacketSent()
    {
        packetsSent++;
    }

    // --- NEW: Counts a single hop instantly ---
    public void AddHop()
    {
        totalHops++;
    }

    // --- UPDATED: Now only counts the delivery, since hops are counted mid-flight ---
    public void PacketDelivered()
    {
        packetsDelivered++;
    }

    public void PacketFailed()
    {
        packetsFailed++;
    }

    public float GetSuccessRate()
    {
        if (packetsSent == 0)
            return 0f;

        return (float)packetsDelivered / packetsSent * 100f;
    }

    public float GetAverageHops()
    {
        if (packetsDelivered == 0)
            return 0f;

        return (float)totalHops / packetsDelivered;
    }

    public void ResetStatistics()
    {
        packetsSent = 0;
        packetsDelivered = 0;
        packetsFailed = 0;
        totalHops = 0;
    }
}