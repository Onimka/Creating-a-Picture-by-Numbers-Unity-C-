using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PictureGenerator
{
    public class ImageViewItem : MonoBehaviour, IPointerClickHandler
    {
        [field: SerializeField] public RawImage RawImage { get; private set; }


        public void SetImage(Texture2D texture2D)
        {
            RawImage.texture = texture2D;

            RawImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture2D.width / (float)texture2D.height;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ServiceLocator.Get<PictureOverlay>().ShowPicture(RawImage.texture);
        }
    }
}
