using UnityEngine;
using UnityEngine.UI;

namespace ARChess.Scripts
{
    public class RawImageOpacityControl : MonoBehaviour
    {
        
        [Header("Controls")]
        [Tooltip("The color of the opacity control.")]
        [InspectorName("Opacity Image")]
        [Range(0f, 1f)]
        public float opacity;

        void Update()
        {
            if (TryGetComponent(out RawImage targetRawImage))
            {
                AssignColor(targetRawImage);
            }
            else if(TryGetComponent(out Image targetImage))
            {
                AssignColor(targetImage);
            }
            
        }

        private void AssignColor(RawImage targetRawImage)
        {
            Color color = targetRawImage.color;
            color.a = opacity;
            targetRawImage.color = color;
        }

        private void AssignColor(Image targetImage)
        {
            Color color = targetImage.color;
            color.a = opacity;
            targetImage.color = color;
        }
    }
}
