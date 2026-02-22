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
        canvasImage.style.scale = new Scale(new Vector3(-1, 1, 1));
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
        // Get UI elements in screencordinate size
        float width = canvasImage.resolvedStyle.width;
        float height = canvasImage.resolvedStyle.height;
        Debug.Log($"CanvasImage dimensions: {width}x{height}");

        // Clickposition -> UV (0-1) on RenderTexture. LocalPos is where we clicked on the UI element (pixel)
        float u = localPos.x / width;
        float v = localPos.y / height;
        
        // u = 1f - u;  not needed
        v = 1f - v;  // UI origin top-left, textur origin bottom-left
        
        
        // Runtime Copy fo sprite
        spriteRenderer = targetSprite.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer != currentPaintableSR)
        {
            CreateRuntimeSprite();
            currentPaintableSR = spriteRenderer;
        }
        
        // Calculate camera view in world units
        float camHeight = renderCamera.orthographicSize * 2f;
        float camWidth = camHeight * ((float)renderTexture.width / renderTexture.height);
        Debug.Log($"Camera view: {camWidth} x {camHeight} world units");
        Debug.Log($"Sprite bounds: {spriteRenderer.bounds.size}");

        //Calculate UV on RenderTexture -> world position via camera
        Vector3 camWorldPos = renderCamera.transform.position;
        float worldX_cam = camWorldPos.x + (u - 0.5f) * camWidth;
        float worldY_cam = camWorldPos.y + (v - 0.5f) * camHeight;
        Debug.Log($"Click world pos (via camera): ({worldX_cam}, {worldY_cam})");

        // Calculate Worldposition -> sprite localUV (0-1 within sprite bounds)
        Bounds spriteBounds = spriteRenderer.bounds;
        float spriteU = (worldX_cam - spriteBounds.min.x) / spriteBounds.size.x;
        float spriteV = (worldY_cam - spriteBounds.min.y) / spriteBounds.size.y;
        Debug.Log($"Sprite UV: ({spriteU}, {spriteV})");
        
        // Calculate Sprite UV -> texturepixel (account for sprite rect)
        Sprite spr = spriteRenderer.sprite;
        int texX = Mathf.Clamp(
            Mathf.FloorToInt(spriteU * spr.rect.width + spr.rect.x), 0, runtimeTexture != null ? runtimeTexture.width - 1 : 1928);
        int texY = Mathf.Clamp(
            Mathf.FloorToInt(spriteV * spr.rect.height + spr.rect.y), 0, runtimeTexture != null ? runtimeTexture.height - 1 : 1079);
        
        lastClickedPixel = new Vector2Int(texX, texY);
        spriteWorldPos = new Vector2(worldX_cam,worldY_cam);

        Debug.Log($"Texture dimensions: {renderTexture.width}x{renderTexture.height}");
        Debug.Log($"Texture pixel: {texX}, {texY}");
        Debug.Log($"Sprite size: {spriteRenderer.bounds.size}");
        Debug.Log($"Click UV: ({u:F3}, {v:F3}) → Sprite UV: ({spriteU:F3}, {spriteV:F3}) → Pixel: ({texX}, {texY})");


        PaintTexture();

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


    public void PaintTexture()
    {
        Debug.Log("Clicking Canvas and Filling texture...");

        if (spriteRenderer == null || runtimeTexture == null)
        {
            Debug.LogError("No runtime texture to paint on!");
            return;
        }

        Vector2Int texCoords = lastClickedPixel;

        Debug.Log($"Painting at pixel ({texCoords.x}, {texCoords.y})");

        Color paintColor = palette.PickedColor; // TODO refactor
        Color boarderColor = Color.black;

        // DEBUG RED PIXEL
        // for (int dx = -5; dx <= 5; dx++)
        // {
        //     for (int dy = -5; dy <= 5; dy++)
        //     {
        //         int px = Mathf.Clamp(texCoords.x + dx, 0, runtimeTexture.width - 1);
        //         int py = Mathf.Clamp(texCoords.y + dy, 0, runtimeTexture.height - 1);
        //         runtimeTexture.SetPixel(px, py, Color.red);
        //     }
        // }

        runtimeTexture.FloodFillBorder(texCoords.x, texCoords.y, paintColor, boarderColor, 0.05f);
        // runtimeTexture.FloodFillArea(texCoords.x, texCoords.y, paintColor, 0.05f);

        runtimeTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        
        //TODO if the canvaspointerdown event is not fired we dont need to fill anything bcs we have not hit anything

    }
}