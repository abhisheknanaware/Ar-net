using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // --- NEW: Required for the Lists in our naming logic ---

public class DevicePlacementManager : MonoBehaviour
{
    public GameObject pcPrefab;
    public GameObject switchPrefab;
    public GameObject routerPrefab;
    public GameObject serverPrefab;

    public Camera mainCamera;

    [Header("Device Size")]
    public float targetDeviceSize = 1.2f;

    [Header("Spacing")]
    public float minimumSpacing = 0.5f;

    [Header("Ghost Preview")]
    public Material validGhostMaterial;
    public Material invalidGhostMaterial;

    private GameObject selectedPrefab;
    private GameObject ghostObject;

    // --- REMOVED hardcoded int counters ---

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (selectedPrefab == null)
            return;

        UpdateGhostPosition();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceDevice();
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }

    public void SelectPC()
    {
        StartPlacement(pcPrefab);
        Debug.Log("PC placement selected");
    }

    public void SelectSwitch()
    {
        StartPlacement(switchPrefab);
        Debug.Log("Switch placement selected");
    }

    public void SelectRouter()
    {
        StartPlacement(routerPrefab);
        Debug.Log("Router placement selected");
    }

    public void SelectServer()
    {
        StartPlacement(serverPrefab);
        Debug.Log("Server placement selected");
    }

    void StartPlacement(GameObject prefab)
    {
        CancelPlacement();

        selectedPrefab = prefab;

        if (selectedPrefab == null)
        {
            Debug.LogError("Selected prefab is missing.");
            return;
        }

        ghostObject = Instantiate(selectedPrefab);

        ghostObject.name = "PlacementPreview";

        NormalizeSize(ghostObject);

        DisablePhysics(ghostObject);

        SetGhostMaterial(validGhostMaterial);
    }

    void UpdateGhostPosition()
    {
        if (ghostObject == null)
            return;

        if (mainCamera == null)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        Plane placementPlane = new Plane(Vector3.up, Vector3.zero);

        if (!placementPlane.Raycast(ray, out float distance))
            return;

        Vector3 position = ray.GetPoint(distance);

        position.y = 0f;

        ghostObject.transform.position = position;

        bool valid = IsValidPlacement(ghostObject);

        if (valid)
            SetGhostMaterial(validGhostMaterial);
        else
            SetGhostMaterial(invalidGhostMaterial);
    }

    void PlaceDevice()
    {
        if (ghostObject == null)
            return;

        if (!IsValidPlacement(ghostObject))
        {
            Debug.LogWarning("Cannot place device here. Too close to another device.");
            return;
        }

        Vector3 position = ghostObject.transform.position;

        GameObject device = Instantiate(
            selectedPrefab,
            position,
            Quaternion.identity
        );

        NormalizeSize(device);

        AssignDeviceName(device);

        Debug.Log("Created device: " + device.name);

        Destroy(ghostObject);

        ghostObject = null;
        selectedPrefab = null;
    }

    public void CancelPlacement()
    {
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }

        selectedPrefab = null;

        Debug.Log("Device placement cancelled");
    }

    void NormalizeSize(GameObject device)
    {
        Renderer[] renderers = device.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float largestDimension = Mathf.Max(
            bounds.size.x,
            bounds.size.y,
            bounds.size.z
        );

        if (largestDimension <= 0.001f)
            return;

        float scaleMultiplier = targetDeviceSize / largestDimension;

        device.transform.localScale *= scaleMultiplier;
    }

    bool IsValidPlacement(GameObject device)
    {
        Renderer[] renderers = device.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return true;

        Bounds newBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            newBounds.Encapsulate(renderers[i].bounds);
        }

        newBounds.Expand(minimumSpacing);

        GameObject[] devices;

        devices = GameObject.FindGameObjectsWithTag("PC");
        if (IsOverlapping(newBounds, devices, device)) return false;

        devices = GameObject.FindGameObjectsWithTag("Switch");
        if (IsOverlapping(newBounds, devices, device)) return false;

        devices = GameObject.FindGameObjectsWithTag("Router");
        if (IsOverlapping(newBounds, devices, device)) return false;

        devices = GameObject.FindGameObjectsWithTag("Server");
        if (IsOverlapping(newBounds, devices, device)) return false;

        return true;
    }

    bool IsOverlapping(Bounds newBounds, GameObject[] existingDevices, GameObject currentDevice)
    {
        foreach (GameObject device in existingDevices)
        {
            if (device == null)
                continue;

            if (device == currentDevice)
                continue;

            NetworkDevice netDeviceScript = device.GetComponent<NetworkDevice>();
            if (netDeviceScript != null && netDeviceScript.isPreviewModel)
            {
                continue;
            }

            Renderer[] renderers = device.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                continue;

            Bounds existingBounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                existingBounds.Encapsulate(renderers[i].bounds);
            }

            if (newBounds.Intersects(existingBounds))
                return true;
        }

        return false;
    }

    void DisablePhysics(GameObject ghost)
    {
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        Rigidbody[] rigidbodies = ghost.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }
    }

    void SetGhostMaterial(Material material)
    {
        if (material == null)
            return;

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.materials = materials;
        }
    }

    // --- NEW: Smart Naming Logic ---
    private string GetNextAvailableName(string prefix)
    {
        NetworkDevice[] allDevices = FindObjectsByType<NetworkDevice>(FindObjectsSortMode.None);
        List<int> usedNumbers = new List<int>();

        foreach (NetworkDevice device in allDevices)
        {
            // Make sure the device actually has a name and starts with our prefix (e.g. "PC")
            if (device != null && !string.IsNullOrEmpty(device.deviceName) && device.deviceName.StartsWith(prefix) && !device.isPreviewModel)
            {
                // Strip out the prefix to just get the number (e.g. "PC1" -> "1")
                string numberPart = device.deviceName.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int num))
                {
                    usedNumbers.Add(num);
                }
            }
        }

        int nextNumber = 1;
        // Keep counting up until we find a number that isn't being used
        while (usedNumbers.Contains(nextNumber))
        {
            nextNumber++;
        }

        return prefix + nextNumber;
    }

    void AssignDeviceName(GameObject device)
    {
        string prefix = "Device";

        if (device.CompareTag("PC")) prefix = "PC";
        else if (device.CompareTag("Switch")) prefix = "Switch";
        else if (device.CompareTag("Router")) prefix = "Router";
        else if (device.CompareTag("Server")) prefix = "Server";
        else Debug.LogWarning("Created device has no correct tag: " + device.name);

        string finalName = GetNextAvailableName(prefix);
        device.name = finalName;

        // Apply the hierarchy name to the internal NetworkDevice script
        NetworkDevice netDeviceScript = device.GetComponent<NetworkDevice>();
        if (netDeviceScript != null)
        {
            netDeviceScript.deviceName = finalName;
        }
    }
}