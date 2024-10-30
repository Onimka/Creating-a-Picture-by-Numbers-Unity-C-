using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PictureGenerator
{
    public class FirstTest : GenerateAlgorithm
    {
        protected async override UniTask<Texture2D> StartGeneration(Texture2D texture2D, int countColors)
        {
            return await ProcessImage(texture2D, countColors, 40);
        }

        public async UniTask<Texture2D> ProcessImage(Texture2D inputTexture, int countColors, int ChunkNum, int M = 1)
        {
            Color[] pixels = inputTexture.GetPixels();
            int width = inputTexture.width;
            int height = inputTexture.height;

            float Dist(Color c1, Color c2)
            {
                return Mathf.Pow(c1.r - c2.r, 2) + Mathf.Pow(c1.g - c2.g, 2) + Mathf.Pow(c1.b - c2.b, 2);
            }

            Color Mean(List<Color> colors)
            {
                Color sum = Color.black;
                foreach (var color in colors)
                {
                    sum += color;
                }
                return sum / colors.Count;
            }

            Color Colorize(Color color, List<Color> palette)
            {
                Color closest = palette[0];
                float minDist = Dist(color, closest);
                foreach (var col in palette)
                {
                    float distance = Dist(color, col);
                    if (distance < minDist)
                    {
                        minDist = distance;
                        closest = col;
                    }
                }
                return closest;
            }

            List<Color> Cluster(List<Color> colors, int k, int maxN = 10000, int maxI = 10)
            {

                if (colors.Count > maxN) colors = new List<Color>(colors.GetRange(0, maxN));

                List<Color> centroids = new List<Color>();
                for (int i = 0; i < k; i++)
                {
                    centroids.Add(colors[Random.Range(0, colors.Count)]);
                }

                int iIterations = 0;
                List<Color> oldCentroids = null;

                while (iIterations < maxI && (oldCentroids == null || !centroids.SequenceEqual(oldCentroids)))
                {
                    oldCentroids = new List<Color>(centroids);
                    var labels = new List<Color>();

                    foreach (var color in colors)
                    {
                        labels.Add(Colorize(color, centroids));
                    }
                    for (int j = 0; j < centroids.Count; j++)
                    {
                        var assignedColors = colors.FindAll(c => Colorize(c, centroids).Equals(centroids[j]));
                        if (assignedColors.Count > 0)
                        {
                            centroids[j] = Mean(assignedColors);
                        }
                    }               
                    iIterations++;
                }

                return centroids;
            }

            List<Color> allColors = new List<Color>(pixels);
            List<Color> palette = Cluster(allColors, countColors);
            Debug.Log($"palette:{palette.Count}");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = Colorize(pixels[y * width + x], palette);
                }
            }

            inputTexture.SetPixels(pixels);
            inputTexture.Apply();

            return inputTexture;
        }
    }

}

