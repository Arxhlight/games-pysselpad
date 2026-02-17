using UnityEngine;
using UnityEngine.InputSystem;

public class Palette : MonoBehaviour
{
    [Header("Palette Settings")]
    [SerializeField] private LayerMask layerMask;
    public Color PickedColor { get; private set; }
    
    [Header("Debug Settings")]
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private float minDepth = 0f;
    [SerializeField] private float maxDepth = 100f;
    [SerializeField] private Color debugRayColor = Color.cyan;
    
    private SpriteRenderer sr;
    private Vector2 worldPos;
    private ContactFilter2D filter;
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        filter = new ContactFilter2D();
        filter.SetLayerMask(layerMask);
        filter.useLayerMask = true;
        filter.SetDepth(minDepth, maxDepth);
        filter.useDepth = true;
    }



    /// <summary>
    /// Finds a color sprite under the mouse and picks the color at that pixel.
    /// Uses Physics2D.Raycast with ContactFilter2D for layer filtering.
    /// </summary>
    public void PickColorAtMouse()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 rayDirection = Vector2.down;

        Debug.DrawRay(mouseWorldPos, rayDirection * rayDistance, debugRayColor, 0.5f);

        RaycastHit2D[] results = new RaycastHit2D[1];
        int hitCount = Physics2D.Raycast(mouseWorldPos, rayDirection, filter, results, rayDistance);

        if (hitCount == 0)
        {
            Debug.Log("No color sprite found under mouse!");
            return;
        }

        sr = results[0].collider.GetComponent<SpriteRenderer>();
        worldPos = mouseWorldPos;

        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("Hit object has no SpriteRenderer or Sprite!");
            return;
        }

        SetColor();
    }

    /// <summary>
    /// Converts world position to texture pixel coordinates and Sets the Color.
    /// </summary>
    private void SetColor()
    {
        Vector2Int texCoords = WorldToTextureCoords();
        Texture2D texture = sr.sprite.texture;
        
        PickedColor = texture.GetPixel(texCoords.x, texCoords.y);
        Debug.Log($"Picked color at pixel ({texCoords.x}, {texCoords.y}): {PickedColor}");
    }

    /// <summary>
    /// Converts stored world position to texture pixel coordinates.
    /// </summary>
    Vector2Int WorldToTextureCoords()
    {
        Sprite sprite = sr.sprite;
        Texture2D texture = sprite.texture;
        
        Vector2 localPos = sr.transform.InverseTransformPoint(worldPos);
        Vector2 pivotNormalized = sprite.pivot / new Vector2(sprite.rect.width, sprite.rect.height);
        
        float u = (localPos.x / sprite.bounds.size.x) + pivotNormalized.x;
        float v = (localPos.y / sprite.bounds.size.y) + pivotNormalized.y;
        
        int texX = Mathf.RoundToInt(sprite.rect.x + u * sprite.rect.width);
        int texY = Mathf.RoundToInt(sprite.rect.y + v * sprite.rect.height);
        
        texX = Mathf.Clamp(texX, 0, texture.width - 1);
        texY = Mathf.Clamp(texY, 0, texture.height - 1);
        
        return new Vector2Int(texX, texY);
    }
}
