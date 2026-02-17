using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    
    [SerializeField] DrawingTools drawingTools;
    [SerializeField] Palette palette;
    public void AreaFill(InputAction.CallbackContext context)
    {
        //TODO if the canvaspointerdown event is not fired we dont need to fill anything bcs we have not hit anything
        if (context.performed)
        {
            Debug.Log("Clicking Canvas and Filling texture...");
            drawingTools.PaintTexture();
        }
    }
    
    public void ColorPick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Clicking Canvas and Picking color...");
            palette.PickColorAtMouse();
        }
    }
}
