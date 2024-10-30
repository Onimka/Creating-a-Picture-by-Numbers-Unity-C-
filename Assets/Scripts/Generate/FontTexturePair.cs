using UnityEngine;

namespace PictureGenerator
{
    [System.Serializable]
    public class FontTexturePair
    {
        [field: SerializeField] public char Number { get; private set; }
        [field: SerializeField] public Texture2D Texture { get; private set; }

    }
}
