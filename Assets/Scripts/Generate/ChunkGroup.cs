using UnityEngine;
using System.Collections.Generic;

namespace PictureGenerator
{
    public class ChunkGroup
    {
        public Color Color { get; private set; }
        public List<Vector2Int> Group = new();
        public float Distance;

        public ChunkGroup(Color color)
        {
            Color = color;
        }
    }
}
