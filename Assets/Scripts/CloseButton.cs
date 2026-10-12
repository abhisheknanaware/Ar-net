using UnityEngine;

public class CloseButton : MonoBehaviour
{
    public DeviceInfoPanel infoPanel;

    public void Close()
    {
        if (infoPanel != null)
        {
            infoPanel.gameObject.SetActive(false);
        }
    }
}