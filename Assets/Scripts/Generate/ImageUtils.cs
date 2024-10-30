using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;
using UnityEditorInternal;

namespace PictureGenerator
{
    public static class ImageUtils
    {

        // Gauss Filter
        public static async UniTask ApplyGaussianBlur(Texture2D texture, int filterSize, CancellationToken cancellationToken)
        {
            int halfSize = filterSize / 2;
            Color[] originalPixels = texture.GetPixels();
            Color[] blurredPixels = new Color[originalPixels.Length];

            await UniTask.RunOnThreadPool(() =>
            {
                float[,] weights = CalculateGaussianWeights(filterSize);

                Parallel.For(0, texture.width, x =>
                {
                    for (int y = 0; y < texture.height; y++)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        Color weightedColor = Color.black;
                        float totalWeight = 0f;

                        for (int fx = -halfSize; fx <= halfSize; fx++)
                        {
                            for (int fy = -halfSize; fy <= halfSize; fy++)
                            {
                                int sampleX = Mathf.Clamp(x + fx, 0, texture.width - 1);
                                int sampleY = Mathf.Clamp(y + fy, 0, texture.height - 1);
                                Color sampleColor = originalPixels[sampleY * texture.width + sampleX];

                                float weight = weights[fx + halfSize, fy + halfSize];
                                weightedColor += sampleColor * weight;
                                totalWeight += weight;
                            }
                        }

                        blurredPixels[y * texture.width + x] = weightedColor / totalWeight;
                    }
                });
            }, cancellationToken: cancellationToken);
           

            texture.SetPixels(blurredPixels);
            texture.Apply();
        }

        private static float[,] CalculateGaussianWeights(int filterSize)
        {
            float[,] weights = new float[filterSize, filterSize];
            float sigma = 1.0f;
            float mean = filterSize / 2;
            float totalWeight = 0;

            for (int x = 0; x < filterSize; x++)
            {
                for (int y = 0; y < filterSize; y++)
                {
                    float weight = Mathf.Exp(-((Mathf.Pow(x - mean, 2) + Mathf.Pow(y - mean, 2)) / (2 * sigma * sigma)));
                    weights[x, y] = weight;
                    totalWeight += weight;
                }
            }

            for (int x = 0; x < filterSize; x++)
            {
                for (int y = 0; y < filterSize; y++)
                {
                    weights[x, y] /= totalWeight;
                }
            }

            return weights;
        }
        //
        public static async UniTask ApplyMedianFilterAsync(Texture2D texture, int size, CancellationToken cancellationToken)
        {
            int halfSize = size / 2;
            Color[] originalPixels = texture.GetPixels();
            Color[] newPixels = new Color[originalPixels.Length];
            int width = texture.width;
            int height = texture.height;
            int max = width * height;

            Color FindMedianColor(Color[] colors)
            {
                int midIndex = colors.Length / 2;
                Array.Sort(colors, (a, b) => a.grayscale.CompareTo(b.grayscale));
                return colors[midIndex];
            }

            await UniTask.RunOnThreadPool(() =>
            {
                Parallel.For(0, width, x =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {                            
                            break;
                        }

                            int neighborCount = (2 * halfSize + 1) * (2 * halfSize + 1);
                        Color[] neighbors = new Color[neighborCount];
                        int neighborIndex = 0;

                        for (int dx = -halfSize; dx <= halfSize; dx++)
                        {
                            for (int dy = -halfSize; dy <= halfSize; dy++)
                            {
                                int nx = Mathf.Clamp(x + dx, 0, width - 1);
                                int ny = Mathf.Clamp(y + dy, 0, height - 1);
                                neighbors[neighborIndex++] = originalPixels[ny * width + nx];
                            }
                        }

                        Color medianColor = FindMedianColor(neighbors);
                        newPixels[y * width + x] = medianColor;

                        int index = y * width + x;
                        float progress = (float)index / max;
                        MainViewModel.UpdateOperation($"Apply Filters: {index}/{max}", progress);
                    }
                });
            }, cancellationToken: cancellationToken);

            texture.SetPixels(newPixels);
            texture.Apply();
        }
        //public static async UniTask ApplyMedianFilterAsync(Texture2D texture, int size)
        //{
        //    int halfSize = size / 2;
        //    Color[] originalPixels = texture.GetPixels();
        //    Color[] newPixels = new Color[originalPixels.Length];

        //    int max = texture.width * texture.height;
        //    int index = 0;
        //    await UniTask.Run(() =>
        //    {
        //        for (int x = 0; x < texture.width; x++)
        //        {
        //            for (int y = 0; y < texture.height; y++)
        //            {
        //                List<Color> neighbors = new List<Color>();

        //                for (int dx = -halfSize; dx <= halfSize; dx++)
        //                {
        //                    for (int dy = -halfSize; dy <= halfSize; dy++)
        //                    {
        //                        int nx = Mathf.Clamp(x + dx, 0, texture.width - 1);
        //                        int ny = Mathf.Clamp(y + dy, 0, texture.height - 1);
        //                        neighbors.Add(originalPixels[ny * texture.width + nx]);
        //                    }
        //                }

        //                neighbors.Sort((a, b) => a.grayscale.CompareTo(b.grayscale));
        //                Color medianColor = neighbors[neighbors.Count / 2];
        //                newPixels[y * texture.width + x] = medianColor;

        //                var progress = (float)index / (max);
        //                index++;

        //                MainViewModel.UpdateOperation($"Apply Filters: {index}/{max}", progress);
        //            }
        //        }
        //    });       
        //    texture.SetPixels(newPixels);
        //    texture.Apply(); 
        //}

        public static Texture2D CloneTexture(Texture2D original)
        {
            var newTexture = new Texture2D(original.width, original.height, original.format, false);
            newTexture.filterMode = FilterMode.Bilinear;
            newTexture.SetPixels(original.GetPixels());
            newTexture.Apply();
            return newTexture;
        }

        public static Texture2D UpscaleTexture(Texture2D original, Vector2Int targetResolution)
        {        
            float aspectRatio = (float)original.width / (float)original.height;
            int newWidth, newHeight;

            if (aspectRatio > 1) // Width dominate
            {
                newWidth = targetResolution.x;
                newHeight = Mathf.RoundToInt((float)targetResolution.x / aspectRatio);
            }
            else // Height dominate
            {
                newHeight = targetResolution.y;
                newWidth = Mathf.RoundToInt((float)targetResolution.y * aspectRatio);
            }

            Texture2D upscaledTexture = new Texture2D(newWidth, newHeight);
            upscaledTexture.filterMode = FilterMode.Bilinear;
            upscaledTexture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {               
                    float u = (float)x / (float)newWidth;
                    float v = (float)y / (float)newHeight;

                    Color pixelColor = original.GetPixelBilinear(u, v);
                    upscaledTexture.SetPixel(x, y, pixelColor);
                }
            }

            upscaledTexture.Apply();
            return upscaledTexture;
        }

        public static double ColorDistance(Color c1, Color c2)
        {
            return Math.Pow(c1.r - c2.r, 2) + Math.Pow(c1.g - c2.g, 2) + Math.Pow(c1.b - c2.b, 2);
        }

        public static Color AverageColor(List<Color> colors)
        {
            var r = colors.Average(c => c.r);
            var g = colors.Average(c => c.g);
            var b = colors.Average(c => c.b);
            return new(r, g, b);
        }

        public static bool AreColorsSimilar(Color color, Color b, float tolerance)
        {
            var dominate = Mathf.Max(color.g, color.b, color.r);
            if (color.r == dominate)
            {
                return Mathf.Abs(color.r - b.r) < tolerance;
            }
            else if (color.g == dominate)
            {
                return Mathf.Abs(color.g - b.g) < tolerance;
            }
            else if (color.b == dominate)
            {
                return Mathf.Abs(color.b - b.b) < tolerance;
            }
            return false;
        }

    }
}
