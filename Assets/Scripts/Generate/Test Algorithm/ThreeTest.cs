using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace PictureGenerator
{
    public class ThreeTest : GenerateAlgorithm
    {
        private const int MedianFilterSize = 1;
        private const int Circles = 70;
        private List<PixelProperty> pixelProperties = new();

        float colorThreshold = 0.05f;
        int minSegmentSize = 150; 
        float mergeTolerance = 0.3f;


        protected async override UniTask<Texture2D> StartGeneration(Texture2D texture2D, int countColors)
        {
            await ScanTexture(texture2D);

            List<Color> palette = await QuantizeColors(texture2D, countColors);

            ApplyMedianFilter(texture2D, MedianFilterSize);

         
            SegmentImage(texture2D, palette, colorThreshold, minSegmentSize, mergeTolerance);

            texture2D.Apply();
            return texture2D;
        }

        private int GetClosestCentroidIndex(Color color, List<Color> centroids)
        {
            double minDistance = double.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < centroids.Count; i++)
            {
                double distance = ColorDistance(color, centroids[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private Color AverageColor(List<Color> colors)
        {
            var r = colors.Average(c => c.r);
            var g = colors.Average(c => c.g);
            var b = colors.Average(c => c.b);
            return new(r, g, b);
        }

        private async UniTask ScanTexture(Texture2D texture2D)
        {
            var index = 0;
            float maxMlue = (float)(texture2D.width * texture2D.height);

            for (int x = 0; x < texture2D.width; x++)
            {
                for (int y = 0; y < texture2D.height; y++)
                {
                    pixelProperties.Add(new PixelProperty(index, texture2D.GetPixel(x, y)));
                    index++;
                }
            }
        }

        private async UniTask<List<Color>> KMeans(List<Color> colors, int k)
        {

            Debug.Log("Generate");
            System.Random random = new();
            List<Color> centroids = colors.OrderBy(x => random.Next()).Take(k).ToList();
            List<int> assignments = new List<int>(new int[colors.Count]);
            bool changed;

            int iterationCount = 0;

            await UniTask.Run(Act);

            void Act()
            {
                do
                {               
                    changed = false;

                    Parallel.For(0, colors.Count, i =>
                    {
                        int closestCentroidIndex = GetClosestCentroidIndex(colors[i], centroids);
                        if (assignments[i] != closestCentroidIndex)
                        {
                            assignments[i] = closestCentroidIndex;
                            changed = true;
                        }
                    });

                    for (int i = 0; i < k; i++)
                    {
                        var clusterColors = colors.Where((_, index) => assignments[index] == i).ToList();
                        if (clusterColors.Count > 0)
                        {
                            centroids[i] = AverageColor(clusterColors);
                        }
                    }

                    iterationCount++;


                } while (changed && iterationCount < Circles);
            }

            Debug.Log($"KMeans completed in {iterationCount} iterations.");
            return centroids;
        }
  

        private async UniTask<List<Color>> QuantizeColors(Texture2D texture2D, int countColors)
        {
            List<Color> allColors = new List<Color>();
            for (int x = 0; x < texture2D.width; x++)
            {
                for (int y = 0; y < texture2D.height; y++)
                {
                    allColors.Add(texture2D.GetPixel(x, y));
                }
            }

            var palette = await KMeans(allColors, countColors);
            for (int x = 0; x < texture2D.width; x++)
            {
                for (int y = 0; y < texture2D.height; y++)
                {
                    var color = texture2D.GetPixel(x, y);
                    texture2D.SetPixel(x, y, GetClosestColor(color, palette, mergeTolerance));
                }
            }
            return palette;
        }

        private void ApplyMedianFilter(Texture2D texture, int size)
        {
            Texture2D newTexture = new Texture2D(texture.width, texture.height);

            int halfSize = size / 2;
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    List<Color> neighbors = new List<Color>();

                    for (int dx = -halfSize; dx <= halfSize; dx++)
                    {
                        for (int dy = -halfSize; dy <= halfSize; dy++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, texture.width - 1);
                            int ny = Mathf.Clamp(y + dy, 0, texture.height - 1);
                            neighbors.Add(texture.GetPixel(nx, ny));
                        }
                    }

                    neighbors = neighbors.OrderBy(c => c.grayscale).ToList();
                    Color medianColor = neighbors[neighbors.Count / 2];
                    newTexture.SetPixel(x, y, medianColor);
                }
            }
            texture.SetPixels(newTexture.GetPixels());
            texture.Apply();
        }

        private void SegmentImage(Texture2D texture, List<Color> palette, float colorThreshold, int minSegmentSize, float mergeTolerance)
        {
            bool[] visited = new bool[texture.width * texture.height];
            List<HashSet<Vector2Int>> segments = new List<HashSet<Vector2Int>>();

            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    if (!visited[y * texture.width + x])
                    {
                        var segment = new HashSet<Vector2Int>();
                        var color = texture.GetPixel(x, y);
                        SegmentRegion(texture, visited, x, y, color, segment, colorThreshold);

                        if (segment.Count >= minSegmentSize)
                        {
                            segments.Add(segment);
                        }
                    }
                }
            }

            MergeSegments(texture, segments, palette, mergeTolerance);
        }

        private void SegmentRegion(Texture2D texture, bool[] visited, int startX, int startY, Color baseColor, HashSet<Vector2Int> segment, float colorThreshold)
        {
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(startX, startY));

            while (stack.Count > 0)
            {
                var pixel = stack.Pop();
                int x = pixel.x, y = pixel.y;

                if (x < 0 || x >= texture.width || y < 0 || y >= texture.height) continue;
                if (visited[y * texture.width + x]) continue;

                Color color = texture.GetPixel(x, y);
                if (ColorDistance(color, baseColor) < colorThreshold)
                {
                    visited[y * texture.width + x] = true;
                    segment.Add(new Vector2Int(x, y));

                    stack.Push(new Vector2Int(x + 1, y));
                    stack.Push(new Vector2Int(x - 1, y));
                    stack.Push(new Vector2Int(x, y + 1));
                    stack.Push(new Vector2Int(x, y - 1));
                }
            }
        }

        private void MergeSegments(Texture2D texture, List<HashSet<Vector2Int>> segments, List<Color> palette, float mergeTolerance)
        {
            foreach (var segment in segments)
            {
                Color meanColor = GetAverageColor(segment, texture);
                Color closestColor = GetClosestColor(meanColor, palette, mergeTolerance);

                foreach (var pixel in segment)
                {
                    texture.SetPixel(pixel.x, pixel.y, closestColor);
                }
            }
        }

        private Color GetClosestColor(Color color, List<Color> palette, float tolerance)
        {
            return palette.Where(p => ColorDistance(color, p) <= tolerance)
                          .OrderBy(p => ColorDistance(color, p))
                          .FirstOrDefault();
        }

        private Color GetAverageColor(HashSet<Vector2Int> segment, Texture2D texture)
        {
            float r = 0, g = 0, b = 0;
            foreach (var pos in segment)
            {
                Color color = texture.GetPixel(pos.x, pos.y);
                r += color.r;
                g += color.g;
                b += color.b;
            }
            int count = segment.Count;
            return new Color(r / count, g / count, b / count);
        }

        private double ColorDistance(Color c1, Color c2)
        {
            return Math.Pow(c1.r - c2.r, 2) + Math.Pow(c1.g - c2.g, 2) + Math.Pow(c1.b - c2.b, 2);
        }
    }
}
