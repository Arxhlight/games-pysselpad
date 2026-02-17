using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


public class DrawingTools : MonoBehaviour
{
    [Header("Stilus Settings")] 

    [Header("Palette Settings")]
    [SerializeField] private Palette palette;

    //should be ui toolkit buttons

    [Header("UI Document Settings")]
    [SerializeField] private UIDocument uiDocument;
    private VisualElement canvasImage;
    private PanelSettings panelSettings;
    private string imageElementName = "canvas--image";

    [Header("RenderTexture Settings")] 
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Transform targetSprite;
    [SerializeField] private Camera renderCamera;

    private SpriteRenderer spriteRenderer;
    private Vector2 spriteWorldPos;

    private Vector2Int lastClickedPixel;
    private SpriteRenderer currentPaintableSR;
    private Texture2D runtimeTexture;
    
    private void Awake()
    {
        if (renderCamera == null)
        {
            Debug.LogError("Render camera not assigned!");
        }
    }

    private void Start()
    {
        panelSettings = uiDocument.panelSettings;
        canvasImage = uiDocument.rootVisualElement.Q<VisualElement>(imageElementName);
        
        if (canvasImage == null)
        {
            Debug.LogError("Canvas image not found!");
            return;
        }
        canvasImage.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);

        if (panelSettings == null)
        {
            Debug.LogError("Panel settings not found!");
        }
        else
        {
            Debug.Log($"[Start] PanelSettings referenceResolution: {panelSettings.referenceResolution}");
        }
    }

    private void OnCanvasPointerDown(PointerDownEvent evt)
    {
        Vector2 localPos = evt.localPosition;
        Debug.Log($"Pointer down at localPos: {localPos} on canvasImage element {canvasImage.name}");
        ProcessUiClick(localPos);
        //Cast rays function here if needed
    }

    private void ProcessUiClick(Vector2 localPos)
    {
        float width = canvasImage.resolvedStyle.width;
        float height = canvasImage.resolvedStyle.height;
        
        Debug.Log($"CanvasImage dimensions: {width}x{height}");
        
        float u = localPos.x / width;
        float v = localPos.y / height;
        v = 1f - v; // om texture origin är bottom-left

        int texX = Mathf.Clamp(
            Mathf.FloorToInt(u * renderTexture.width),
            0,
            renderTexture.width - 1
        );
        int texY = Mathf.Clamp(
            Mathf.FloorToInt(v * renderTexture.height),
            0,
            renderTexture.height - 1
        );
        lastClickedPixel = new Vector2Int(texX, texY);
        
        Debug.Log($"Texture dimensions: {renderTexture.width}x{renderTexture.height}");
        Debug.Log($"Texture pixel: {texX}, {texY}");

        spriteRenderer = targetSprite.GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null && spriteRenderer != currentPaintableSR)
        {
            CreateRuntimeSprite();
            currentPaintableSR = spriteRenderer;
        }

        // Use in futer if we need to raycast

        // Vector2 spriteSize = spriteRenderer.bounds.size;
        // Debug.Log($"Sprite size: {spriteSize}");
        //
        // float worldX = (u - 0.5f) * spriteSize.x;
        // float worldY = (v - 0.5f) * spriteSize.y;
        // Debug.Log($"Sprite world position: ({worldX}, {worldY})");
        //
        // Vector3 worldPoint =
        //     targetSprite.position +
        //     targetSprite.right * worldX +
        //     targetSprite.up * worldY;
        //
        // Debug.Log($"World position: {worldPoint}");
        //
        // spriteWorldPos = worldPoint;
    }
    
    /// <summary>
    /// Creates a runtime copy of the sprite's texture.
    /// Called when hovering a NEW sprite (different from previous).
    /// </summary>
    void CreateRuntimeSprite()
    {            Debug.LogError($"Runtime sprite created for: {spriteRenderer.gameObject.name}");

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError("Cannot create runtime sprite - no sprite found!");
            return;
        }
        
        Sprite originalSprite = spriteRenderer.sprite;
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
            originalSprite.pivot / originalSprite.rect.size,
            originalSprite.pixelsPerUnit
        );
        
        // Assign runtime sprite
        spriteRenderer.sprite = runtimeSprite;
        
        Debug.Log($"Runtime sprite copy created for: {spriteRenderer.gameObject.name}");
    }
    
    public void Fill(InputAction.CallbackContext context)
    {
        //TODO if the canvaspointerdown event is not fired we dont need to fill anything bcs we have not hit anything
        if (context.performed)
        {
            Debug.Log("Clicking Canvas and Filling texture...");
            PaintTexture();
        }
    }

    void PaintTexture()
    {
        if (spriteRenderer == null || runtimeTexture == null)
        {
            Debug.LogError("No runtime texture to paint on!");
            return;
        }

        Vector2Int texCoords = lastClickedPixel;

        Debug.Log($"Painting at pixel ({texCoords.x}, {texCoords.y})");

        Color paintColor = palette.PickedColor;
        Color boarderColor = Color.black;
        
        runtimeTexture.FloodFillBorder(texCoords.x, texCoords.y, paintColor, boarderColor, 0.05f);
        // runtimeTexture.FloodFillArea(texCoords.x, texCoords.y, paintColor, 0.05f);

        runtimeTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
    }
}