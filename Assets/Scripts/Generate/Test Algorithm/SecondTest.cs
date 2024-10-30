using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PictureGenerator
{
    public class SecondTest : GenerateAlgorithm
    {
        float Dist(Color c1, Color c2) =>
            Mathf.Pow(c1.r - c2.r, 2) + Mathf.Pow(c1.g - c2.g, 2) + Mathf.Pow(c1.b - c2.b, 2);


        protected async override UniTask<Texture2D> StartGeneration(Texture2D texture2D, int countColors)
        {
            return await ProcessTexture(texture2D, countColors, 40);
        }

        public async UniTask<Texture2D> ProcessTexture(Texture2D inputTexture, int P, int N, int M = 3)
        {
            int width = inputTexture.width;
            int height = inputTexture.height;
            Color[] pixels = inputTexture.GetPixels();

            Color Mean(List<Color> colors)
            {
                Color sum = Color.black;
                foreach (var color in colors)
                {
                    sum += color;
                }
                return sum / colors.Count;
            }

            Color Colorize(Color color, List<Color> palette) =>
                palette.OrderBy(p => Dist(color, p)).First();

            List<Color> Cluster(List<Color> colors, int k, int maxN = 10000, int maxI = 10)
            {
                if (colors.Count > maxN)
                {
                    colors = new List<Color>(colors.Take(maxN));
                }
                List<Color> centroids = colors.OrderBy(c => Random.value).Take(k).ToList();
                List<Color> oldCentroids = null;
                int i = 0;

                while (i < maxI && (oldCentroids == null || !centroids.SequenceEqual(oldCentroids)))
                {
                    oldCentroids = new List<Color>(centroids);
                    var labels = colors.Select(c => Colorize(c, centroids)).ToList();

                    centroids = centroids.Select(centroid =>
                    {
                        var assignedColors = colors
                            .Where(c => Colorize(c, oldCentroids).Equals(centroid)).ToList();
                        return assignedColors.Any() ? Mean(assignedColors) : centroid;
                    }).ToList();
                    i++;
                }

                return centroids;
            }

            List<Color> allColors = pixels.ToList();
            List<Color> palette = Cluster(allColors, P);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Colorize(pixels[i], palette);
            }

            pixels = ApplyMedianFilter(pixels, width, height, M);

            HashSet<Vector2Int> allCoords = new HashSet<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    allCoords.Add(new Vector2Int(x, y));
                }
            }

            List<HashSet<Vector2Int>> segments = SegmentRegions(allCoords, pixels, width, height);

            while (segments.Count > N)
            {
                MergeSegments(segments, pixels, width, height);
            }

            foreach (var segment in segments)
            {
                Color avgColor = Colorize(Mean(segment.Select(coord => pixels[coord.y * width + coord.x]).ToList()), palette);
                foreach (var coord in segment)
                {
                    pixels[coord.y * width + coord.x] = avgColor;
                }
            }

            Texture2D outputTexture = new Texture2D(width, height);
            outputTexture.SetPixels(pixels);
            outputTexture.Apply();
            return outputTexture;
        }

        private Color[] ApplyMedianFilter(Color[] pixels, int width, int height, int size)
        {
            Color[] filteredPixels = (Color[])pixels.Clone();

            int halfSize = size / 2;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    List<Color> neighbors = new List<Color>();
                    for (int dy = -halfSize; dy <= halfSize; dy++)
                    {
                        for (int dx = -halfSize; dx <= halfSize; dx++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, width - 1);
                            int ny = Mathf.Clamp(y + dy, 0, height - 1);
                            neighbors.Add(pixels[ny * width + nx]);
                        }
                    }

                    neighbors = neighbors.OrderBy(c => Dist(c, filteredPixels[y * width + x])).ToList();
                    filteredPixels[y * width + x] = neighbors[neighbors.Count / 2];
                }
            }

            return filteredPixels;
        }

        private List<HashSet<Vector2Int>> SegmentRegions(HashSet<Vector2Int> allCoords, Color[] pixels, int width, int height)
        {
            List<HashSet<Vector2Int>> segments = new List<HashSet<Vector2Int>>();
            while (allCoords.Any())
            {
                Vector2Int startCoord = allCoords.First();
                HashSet<Vector2Int> segment = GetRegion(startCoord, pixels, width, height);
                allCoords.ExceptWith(segment);
                segments.Add(segment);
            }
            return segments;
        }

        private HashSet<Vector2Int> GetRegion(Vector2Int startCoord, Color[] pixels, int width, int height)
        {
            Color targetColor = pixels[startCoord.y * width + startCoord.x];
            HashSet<Vector2Int> region = new HashSet<Vector2Int> { startCoord };
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(startCoord);

            while (queue.Count > 0)
            {
                Vector2Int coord = queue.Dequeue();
                foreach (Vector2Int neighbor in GetNeighbors(coord, width, height))
                {
                    if (!region.Contains(neighbor) && pixels[neighbor.y * width + neighbor.x] == targetColor)
                    {
                        region.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return region;
        }

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int coord, int width, int height)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var direction in directions)
            {
                Vector2Int neighbor = coord + direction;
                if (neighbor.x >= 0 && neighbor.x < width && neighbor.y >= 0 && neighbor.y < height)
                    yield return neighbor;
            }
        }

        private void MergeSegments(List<HashSet<Vector2Int>> segments, Color[] pixels, int width, int height)
        {
            var smallestSegment = segments.OrderBy(seg => seg.Count).First();
            segments.Remove(smallestSegment);

            foreach (var neighbor in smallestSegment)
            {
                foreach (var segment in segments)
                {
                    if (segment.Any(coord => GetNeighbors(neighbor, width, height).Contains(coord)))
                    {
                        segment.UnionWith(smallestSegment);
                        return;
                    }
                }
            }
        }
    }
}
