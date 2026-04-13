using UnityEngine;
using UnityEngine.UI;

// Attach this to a UI Image (set Image.Type = Filled, Fill Method = Horizontal, Fill Origin = Left).
// Call SetHealth(float current, float max) whenever the health value updates.
public class HealthBar : MonoBehaviour
{
    [Tooltip("The UI Image that will be filled horizontally from left to right.")]
    public Image healthImage;

    void Awake()
    {
        if (healthImage == null)
            healthImage = GetComponent<Image>();

        // Ensure the image is configured correctly
        healthImage.type = Image.Type.Filled;
        healthImage.fillMethod = Image.FillMethod.Horizontal;
        healthImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }


    public void SetHealth(float current, float max)
    {
        if (max <= 0f)
        {
            healthImage.fillAmount = 0f;
            return;
        }
        float fill = Mathf.Clamp01(current / max);
        healthImage.fillAmount = fill;
    }
}