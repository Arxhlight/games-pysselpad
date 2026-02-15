using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Stilus : MonoBehaviour
{
    [Header("Stilus Settings")]
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Palette palette;
    
    [Header("Debug Settings")]
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private float minDepth = 0f;
    [SerializeField] private float maxDepth = 100f;
    [SerializeField] private Color debugRayColor = Color.red;
    
    private SpriteRenderer sr;
    private Vector2 worldPos;
    private ContactFilter2D filter;
    private Camera mainCamera;
    
    // Track WHICH sprite we have a runtime copy for
    private SpriteRenderer currentPaintableSR;
    private Texture2D runtimeTexture;
    private Sprite originalSprite;
    
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

    public void Fill(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Clicking Canvas and Filling texture...");
            PaintTexture();
        }
    }

    private void FixedUpdate()
    {
        FindSpriteAtMouse();
    }

    /// <summary>
    /// Finds the SpriteRenderer under the mouse using Physics2D.Raycast with ContactFilter2D.
    /// Storing SpriteRenderer and world position for later use.
    /// Requires a Collider2D on the sprite.
    /// </summary>
    void FindSpriteAtMouse()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 rayDirection = Vector2.down;
        
       
        Debug.DrawRay(mouseWorldPos, rayDirection * rayDistance, debugRayColor, 0.1f);
        
        RaycastHit2D[] results = new RaycastHit2D[1];
        int hitCount = Physics2D.Raycast(mouseWorldPos, rayDirection, filter, results, rayDistance);
        
        if (hitCount == 0)
        {
            sr = null;
            worldPos = Vector2.zero;
            return;
        }

        worldPos = mouseWorldPos;
        sr = results[0].collider.GetComponent<SpriteRenderer>();
        
        // Create runtime copy ONLY if this is a DIFFERENT sprite than before
        if (sr != null && sr != currentPaintableSR)
        {
            CreateRuntimeSprite();
            currentPaintableSR = sr; // Remember which sprite we made a copy for
        }
    }

    /// <summary>
    /// Creates a runtime copy of the sprite's texture.
    /// Called when hovering a NEW sprite (different from previous).
    /// </summary>
    void CreateRuntimeSprite()
    {
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("Cannot create runtime sprite - no sprite found!");
            return;
        }
        
        originalSprite = sr.sprite;
        Texture2D originalTexture = originalSprite.texture;
        
        // Create texture copy
        runtimeTexture = new Texture2D(
            originalTexture.width, 
            originalTexture.height, 
            originalTexture.format, 
            false
        );
        runtimeTexture.SetPixels(originalTexture.GetPixels());
        runtimeTexture.Apply(false, false);
        
        // Create new sprite using the copied texture
        Sprite runtimeSprite = Sprite.Create(
            runtimeTexture,
            originalSprite.rect,
            originalSprite.pivot / originalSprite.rect.size, // Normalized pivot
            originalSprite.pixelsPerUnit
        );
        
        // Assign runtime sprite
        sr.sprite = runtimeSprite;
        
        Debug.Log($"Runtime sprite copy created for: {sr.gameObject.name}");
    }

    /// <summary>
    /// Converts stored world position to texture pixel coordinates.
    /// Uses the sr and worldPos fields set by FindSpriteAtMouse().
    /// </summary>
    Vector2Int WorldToTextureCoords()
    {
        Sprite sprite = sr.sprite;
        
        Vector2 localPos = sr.transform.InverseTransformPoint(worldPos);
        Vector2 pivotNormalized = sprite.pivot / new Vector2(sprite.rect.width, sprite.rect.height);
        
        float u = (localPos.x / sprite.bounds.size.x) + pivotNormalized.x;
        float v = (localPos.y / sprite.bounds.size.y) + pivotNormalized.y;
        
        int texX = Mathf.RoundToInt(sprite.rect.x + u * sprite.rect.width);
        int texY = Mathf.RoundToInt(sprite.rect.y + v * sprite.rect.height);
        
        texX = Mathf.Clamp(texX, 0, runtimeTexture.width - 1);
        texY = Mathf.Clamp(texY, 0, runtimeTexture.height - 1);
        
        return new Vector2Int(texX, texY);
    }

    void PaintTexture()
    {
        if (sr == null || runtimeTexture == null)
        {
            Debug.LogError("No runtime texture to paint on!");
            return;
        }

        Vector2Int texCoords = WorldToTextureCoords();
        Debug.Log($"Painting at pixel ({texCoords.x}, {texCoords.y})");

        Color paintColor = palette.PickedColor;
        Color boarderColor = Color.black;
        
        runtimeTexture.FloodFillBorder(texCoords.x, texCoords.y, paintColor, boarderColor, 0.05f);
        // runtimeTexture.FloodFillArea(texCoords.x, texCoords.y, paintColor, 0.05f);

        runtimeTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
    }
}