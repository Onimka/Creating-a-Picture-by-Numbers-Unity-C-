using UnityEngine;
using System;
using UnityEngine.EventSystems;

namespace PictureGenerator
{
    public class Clickable : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke();
        }
        private void OnDestroy()
        {
            Clicked = null;
        }
    }
}
