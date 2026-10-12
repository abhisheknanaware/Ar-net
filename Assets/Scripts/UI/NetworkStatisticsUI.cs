using TMPro;
using UnityEngine;

public class NetworkStatisticsUI : MonoBehaviour
{
    public TMP_Text packetsSentText;
    public TMP_Text packetsDeliveredText;
    public TMP_Text packetsFailedText;
    public TMP_Text successRateText;
    public TMP_Text totalHopsText;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (NetworkStatistics.Instance == null)
            return;

        int sent =
            NetworkStatistics.Instance.packetsSent;

        int delivered =
            NetworkStatistics.Instance.packetsDelivered;

        int failed =
            NetworkStatistics.Instance.packetsFailed;

        int hops =
            NetworkStatistics.Instance.totalHops;

        float successRate = 0f;

        if (sent > 0)
        {
            successRate =
                ((float)delivered / sent) * 100f;
        }

        if (packetsSentText != null)
        {
            packetsSentText.text =
                "Packets Sent: " + sent;
        }

        if (packetsDeliveredText != null)
        {
            packetsDeliveredText.text =
                "Packets Delivered: " + delivered;
        }

        if (packetsFailedText != null)
        {
            packetsFailedText.text =
                "Packets Failed: " + failed;
        }

        if (successRateText != null)
        {
            successRateText.text =
                "Success Rate: " +
                successRate.ToString("F1") +
                "%";
        }

        if (totalHopsText != null)
        {
            totalHopsText.text =
                "Total Hops: " + hops;
        }
    }
}