using UnityEngine;
using UnityEngine.UI;

namespace PictureGenerator
{
    public class PictureOverlay : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        [SerializeField] private Clickable _close;

        public void Init()
        {
            _close.Clicked += OnClose;
            ServiceLocator.Register(this);
            gameObject.SetActive(false);
        }

        public void ShowPicture(Texture texture)
        {
            if (texture == null) return;
            _image.texture = texture;
            _image.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / (float)texture.height;
            gameObject.SetActive(true);
        }

        private void OnClose()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _close.Clicked -= OnClose;
        }
    }
}
