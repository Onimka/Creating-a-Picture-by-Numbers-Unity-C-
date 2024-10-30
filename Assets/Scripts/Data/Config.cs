using System.Collections.Generic;
using UnityEngine;

namespace PictureGenerator
{
    [CreateAssetMenu(fileName = "Config", menuName = "SO/MainConfig")]
    public class Config : ScriptableObject
    {
        [field: SerializeField] public GameObject ColorPreset { get; private set; }
        [field: SerializeField] public List<FontTexturePair> Fonts { get; private set; }
    }
}
