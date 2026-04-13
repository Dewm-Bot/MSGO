using UnityEngine;
using UnityEngine.UI;


// Attach this to a UI Image (set Image.Type = Filled, Fill Method = Horizontal, Fill Origin = Left).
// Call SetBoost(float current, float max) whenever the boost value updates.
public class BoostBar : MonoBehaviour
{
    [Tooltip("The UI Image that will be filled horizontally from left to right.")]
    public Image boostImage;

    void Awake()
    {
        if (boostImage == null)
            boostImage = GetComponent<Image>();

        // Ensure the image is configured correctly
        boostImage.type = Image.Type.Filled;
        boostImage.fillMethod = Image.FillMethod.Horizontal;
        boostImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }


    // Update boost bar
    public void SetBoost(float current, float max)
    {
        if (max <= 0f)
        {
            boostImage.fillAmount = 0f;
            return;
        }
        float fill = Mathf.Clamp01(current / max);
        boostImage.fillAmount = fill;
    }
}