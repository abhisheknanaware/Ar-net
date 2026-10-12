using UnityEngine;

public class NetworkConnection : MonoBehaviour
{
    public NetworkDevice deviceA;
    public NetworkDevice deviceB;

    public bool isConnected = true;

    [Header("Cable Thickness")]
    public float normalWidth = 0.05f;  
    public float activeWidth = 0.08f;

    [Header("Pulse & Glow Settings")]
    public float pulseSpeed = 6f;
    public float pulseAmount = 0.02f;
    
    // The [ColorUsage(true, true)] tag is the secret trick! 
    // It tells Unity to let you use HDR (High Dynamic Range) colors in the Inspector so it actually glows.
    [ColorUsage(true, true)] 
    public Color normalColor = Color.black;
    [ColorUsage(true, true)] 
    public Color glowColor = Color.cyan;

    [Header("Offset")]
    public float heightOffset = 0.0f; 

    private bool isActive = false;
    private MeshRenderer cableRenderer; 
    private Material cableMaterial; // We need to store the specific material for this cable

    void Awake()
    {
        cableRenderer = GetComponentInChildren<MeshRenderer>();

        if (cableRenderer != null)
        {
            cableRenderer.enabled = isConnected;
            
            // Get the unique material for this specific cable so they don't all glow at once
            cableMaterial = cableRenderer.material; 
            
            // Force Unity to turn on the Emission property for this material
            cableMaterial.EnableKeyword("_EMISSION");
            cableMaterial.SetColor("_EmissionColor", normalColor);
        }
    }

    void Update()
    {
        if (deviceA == null || deviceB == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!isConnected)
        {
            if (cableRenderer != null) cableRenderer.enabled = false;
            return;
        }
        else
        {
            if (cableRenderer != null) cableRenderer.enabled = true;
        }

        Update3DVisuals();
    }

    void Update3DVisuals()
    {
        Vector3 startPoint = deviceA.transform.position + (Vector3.up * heightOffset);
        Vector3 endPoint = deviceB.transform.position + (Vector3.up * heightOffset);
        float distance = Vector3.Distance(startPoint, endPoint);

        transform.position = (startPoint + endPoint) / 2f;
        
        Vector3 direction = endPoint - startPoint;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        float currentWidth = normalWidth;

        if (isActive)
        {
            // 1. Math for the thickness pulse
            float rawSine = Mathf.Sin(Time.time * pulseSpeed); // Goes from -1 to 1
            float pulse = rawSine * pulseAmount;
            currentWidth = activeWidth + pulse;
            currentWidth = Mathf.Max(currentWidth, normalWidth);

            // 2. Math for the color glow
            if (cableMaterial != null)
            {
                // We convert the -1 to 1 sine wave into a 0 to 1 percentage for the color blend
                float colorLerp = (rawSine + 1f) / 2f; 
                Color currentGlow = Color.Lerp(normalColor, glowColor, colorLerp);
                cableMaterial.SetColor("_EmissionColor", currentGlow);
            }
        }
        else
        {
            // Reset color if not active
            if (cableMaterial != null)
            {
                cableMaterial.SetColor("_EmissionColor", normalColor);
            }
        }

        transform.localScale = new Vector3(currentWidth, currentWidth, distance);
    }

   public void SetActive(bool active)
    {
        if (!isConnected) return;
        isActive = active;
        
        // Add this line to see if your packet script is actually talking to the cable!
        Debug.Log("Cable Active State changed to: " + active + " on " + gameObject.name);
        
        if (!isActive && cableMaterial != null)
        {
            cableMaterial.SetColor("_EmissionColor", normalColor);
        }
    }

    public bool Connects(NetworkDevice device1, NetworkDevice device2)
    {
        if ((deviceA == device1 && deviceB == device2) ||
            (deviceA == device2 && deviceB == device1))
        {
            return true;
        }
        return false;
    }

    public void Disconnect()
    {
        isConnected = false;
        isActive = false;

        if (cableRenderer != null) cableRenderer.enabled = false;
        
        NetworkLogUI logUI = FindFirstObjectByType<NetworkLogUI>();
        if (logUI != null) logUI.LogDisconnection(deviceA, deviceB);
    }

    public void Reconnect()
    {
        isConnected = true;
        isActive = false;

        if (cableRenderer != null)
        {
            cableRenderer.enabled = true;
            cableMaterial.SetColor("_EmissionColor", normalColor); // Ensure it resets
        }
    }

    public void ResetVisual()
    {
        isActive = false;
        if (cableMaterial != null)
        {
            cableMaterial.SetColor("_EmissionColor", normalColor);
        }
    }
}