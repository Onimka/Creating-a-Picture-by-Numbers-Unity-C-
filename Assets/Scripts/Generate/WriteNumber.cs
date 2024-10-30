using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;
using System.Linq;


namespace PictureGenerator
{
    public class WriteNumber/* : GenerateAlgorithm*/
    {

        private const float MinGrayScale = 0.0000165f;
        private List<ChunkGroup> largeChunks = new();

        private int fontSize = 10; // 13
        private List<Color> _pallet = new();

        public WriteNumber(List<Color> pallet)
        {
            _pallet = pallet;
        }

        //protected override async UniTask<Texture2D> StartGeneration(Texture2D texture2D, int countColors)
        //{
        //    await WriteNumSmallParticles(texture2D, 0.6f, cancellationTokenSource.Token);
        //    return texture2D;
        //}

        public async UniTask<Texture2D> GenerateFinalImage(Texture2D resultImage, Texture2D lineImage)
        {
            var final = ImageUtils.CloneTexture(lineImage);
            int width = resultImage.width;
            int height = resultImage.height;
            Color[] pixels = resultImage.GetPixels();

            int minSize = (int)(MinGrayScale * (width * height));
          
            FindLargeChunks(pixels, width, height, minSize, 0.6f);

            pixels = final.GetPixels();
            WriteLargeChunks(pixels, width, height);


            final.SetPixels(pixels);
            final.Apply();
            return final;
        }

        private async UniTask WriteNumSmallParticles(Texture2D texture, float tolerance, CancellationToken cancellationToken)
        {
            int width = texture.width;
            int height = texture.height;
            Color[] pixels = texture.GetPixels();

            int minSize = (int)(MinGrayScale * (width * height));
            //WriteNumAsyncReplaceSmallParticles(pixels, width, height, minSize, tolerance);
            FindLargeChunks(pixels, width, height, minSize, tolerance);
            WriteLargeChunks(pixels, width, height);
            texture.SetPixels(pixels);
            texture.Apply();
        }

        private void FindLargeChunks(Color[] pixels, int width, int height, int maxSize, float tolerance)
        {
            largeChunks = new();


            bool[] visited = new bool[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!visited[y * width + x])
                    {
                        Color currentColor = pixels[y * width + x];
                        List<Vector2Int> group = new List<Vector2Int>();
                        Queue<Vector2Int> queue = new Queue<Vector2Int>();
                        queue.Enqueue(new Vector2Int(x, y));
                        visited[y * width + x] = true;

                        while (queue.Count > 0)
                        {
                            Vector2Int pixel = queue.Dequeue();
                            group.Add(pixel);

                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    //if (Mathf.Abs(dx) == Mathf.Abs(dy)) continue;

                                    int nx = pixel.x + dx;
                                    int ny = pixel.y + dy;

                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[ny * width + nx])
                                    {
                                        if (pixels[ny * width + nx] == currentColor)
                                        {
                                            visited[ny * width + nx] = true;
                                            queue.Enqueue(new Vector2Int(nx, ny));
                                        }
                                    }
                                }
                            }
                        }

                        if (group.Count > maxSize)
                        {
                            ChunkGroup chank = new(currentColor);
                            chank.Group.AddRange(group);
                            largeChunks.Add(chank);
                        }
                    }
                }
            }
        }

        private void WriteLargeChunks(Color[] pixels, int width, int height)
        {
            foreach (var chunk in largeChunks)
            {
                Vector2Int center = FindChunkCenter(chunk.Group);

                DrawNumberOnTexture(pixels, width, center, chunk.Color);
            }
        }

        private void WriteNumAsyncReplaceSmallParticles(Color[] pixels, int width, int height, int maxSize, float tolerance)
        {
            largeChunks = new();


            bool[] visited = new bool[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!visited[y * width + x])
                    {
                        Color currentColor = pixels[y * width + x];
                        List<Vector2Int> group = new List<Vector2Int>();
                        Queue<Vector2Int> queue = new Queue<Vector2Int>();
                        queue.Enqueue(new Vector2Int(x, y));
                        visited[y * width + x] = true;

                        while (queue.Count > 0)
                        {
                            Vector2Int pixel = queue.Dequeue();
                            group.Add(pixel);

                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    //if (Mathf.Abs(dx) == Mathf.Abs(dy)) continue;

                                    int nx = pixel.x + dx;
                                    int ny = pixel.y + dy;

                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[ny * width + nx])
                                    {
                                        if (pixels[ny * width + nx] == currentColor)
                                        {
                                            visited[ny * width + nx] = true;
                                            queue.Enqueue(new Vector2Int(nx, ny));
                                        }
                                    }
                                }
                            }
                        }

                        if (group.Count > maxSize)
                        {
                            ChunkGroup chank = new(currentColor);
                            chank.Group.AddRange(group);
                            largeChunks.Add(chank);
                        }
                    }
                }
            }

            foreach (var chunk in largeChunks)
            {
                Vector2Int center = FindChunkCenter(chunk.Group);

                DrawNumberOnTexture(pixels, width, center, chunk.Color);
            }
        }


        private Vector2Int FindChunkCenter(List<Vector2Int> group)
        {
            if (group == null || group.Count == 0)
                throw new ArgumentException("Group cannot be null or empty.");

            int centerX = 0;
            int centerY = 0;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var pixel in group)
            {
                if (pixel.x < minX) minX = pixel.x;
                if (pixel.x > maxX) maxX = pixel.x;
                if (pixel.y < minY) minY = pixel.y;
                if (pixel.y > maxY) maxY = pixel.y;
            }

            for (int attempt = 0; attempt < 10; attempt++)
            {
                System.Random random = new();

                centerX = (minX + maxX) / 2;
                centerY = (minY + maxY) / 2;

                Vector2Int center = new Vector2Int(centerX, centerY);

                if (group.Contains(center))
                {
                    return center;
                }

                centerX += random.Next(-1, 2);
                centerY += random.Next(-1, 2);

                if (centerX < minX) centerX = minX;
                if (centerX > maxX) centerX = maxX;
                if (centerY < minY) centerY = minY;
                if (centerY > maxY) centerY = maxY;

                Vector2Int newCenter = new Vector2Int(centerX, centerY);

                if (group.Contains(newCenter))
                {
                    return newCenter;
                }
            }

            int sumX = 0;
            int sumY = 0;

            foreach (var pixel in group)
            {
                sumX += pixel.x;
                sumY += pixel.y;
            }

            centerX = sumX / group.Count;
            centerY = sumY / group.Count;

            return new Vector2Int(centerX, centerY);
        }

        private void DrawNumberOnTexture(Color[] pixels, int width, Vector2Int position, Color color)
        {

            int numberWidth = fontSize;
            int numberHeight = fontSize;
           
            var clr = _pallet.FirstOrDefault(c=>ImageUtils.AreColorsSimilar(color, c, 0.05f));
            if(clr == null)
            {
                return;
            }
            var numTexture = GenerateCombinedTexture(_pallet.IndexOf(clr) + 1, -180);

            var fage = ImageUtils.UpscaleTexture(numTexture, new(fontSize, fontSize));
            fage.alphaIsTransparency = true;
       
            for (int x = 0; x < numberWidth; x++)
            {
                for (int y = 0; y < numberHeight; y++)
                {
                    int pixelX = position.x + x - fontSize / 2;
                    int pixelY = position.y + y - fontSize / 2;

                    if (pixelX >= 0 && pixelX < width && pixelY >= 0 && pixelY < pixels.Length / width)
                    {
                        var pixel = fage.GetPixel(x, y);
                        if(pixel.a > 0.1) pixels[pixelY * width + pixelX] = pixel;
                    }
                }
            }
        }

        private Texture2D GenerateCombinedTexture(int index, int spacing)
        {
            List<Texture2D> textures = new();
            var config = ServiceLocator.Get<Config>().Fonts;

            foreach (var num in index.ToString())
            {
                textures.Add(ImageUtils.CloneTexture(config.First(c => c.Number == num).Texture));
            }

            return CombineTexturesHorizontally(textures, spacing);
        }

        private Texture2D CombineTexturesHorizontally(List<Texture2D> textures, int spacing)
        {
            if (textures == null || textures.Count == 0) return null;

            int totalWidth = textures.Sum(texture => texture.width) + spacing * (textures.Count - 1);
            int maxHeight = textures.Max(texture => texture.height);

            Texture2D combinedTexture = new Texture2D(totalWidth, maxHeight);

            Color[] emptyPixels = new Color[totalWidth * maxHeight];
            for (int i = 0; i < emptyPixels.Length; i++)
            {
                emptyPixels[i] = Color.clear;
            }
            combinedTexture.SetPixels(emptyPixels);

            int offsetX = 0;

            foreach (var texture in textures)
            {
                Color[] pixels = texture.GetPixels();

                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        Color pixelColor = pixels[y * texture.width + x];

                        if (pixelColor.a > 0)
                        {
                            combinedTexture.SetPixel(offsetX + x, y, pixelColor);
                        }
                    }
                }

                offsetX += texture.width + spacing;
            }

            combinedTexture.Apply();
            return combinedTexture;
        }
    }
}
