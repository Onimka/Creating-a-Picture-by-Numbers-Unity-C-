using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictureGenerator
{
    public class ColorPresetViewItem : MonoBehaviour
    {
        [SerializeField] private Image _img;
        [SerializeField] private TMP_Text _code;
    
        public Color Data { get; private set; }

        public void Link(Color color, int num)
        {
            Data = color;
            _img.color = color;
            _code.text = $"{num} - #{ColorUtility.ToHtmlStringRGBA(color)}";
        }
    }
}
