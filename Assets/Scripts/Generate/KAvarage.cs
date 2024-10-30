using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


namespace PictureGenerator
{
    public class KAvarage : GenerateAlgorithm
    {

        private List<PixelProperty> pixelProperties = new();
        private const int MaxKMeansIterations = 100; // 100
        private const float MinGrayScale = 0.0000165f;
        private const float MinGrayScale_2 = 0.00001f;
       

        private Texture2D num;


        protected async override UniTask<Texture2D> StartGeneration(Texture2D texture2D, int countColors)
        {
            

            ScanTexture(texture2D);

            List<Color> pallet = await GetDominantColors(countColors, cancellationTokenSource.Token);

            foreach (var pixel in pixelProperties)
            {
                pixel.ChangeColor(pallet[GetClosestCentroidIndex(pixel.OriginalColor, pallet)]);
            }

            var index = 0;
            for (int x = 0; x < texture2D.width; x++)
            {
                for (int y = 0; y < texture2D.height; y++)
                {
                    texture2D.SetPixel(x, y, pixelProperties[index].ChangedColor);
                    index++;
                }
            }
            texture2D.Apply();

            await ReplaceSmallParticles(texture2D, MinGrayScale, 0.1f, Color.blue, cancellationTokenSource.Token);
            await ReplaceSmallParticles(texture2D, MinGrayScale_2, 0.6f, Color.red, cancellationTokenSource.Token);
            

            texture2D.Apply();
            pixelProperties.Clear();
            return texture2D;
        }

        private void ScanTexture(Texture2D texture2D)
        {
            var index = 0;
            var pixels = texture2D.GetPixels();

            for (int x = 0; x < texture2D.width; x++)
            {
                for (int y = 0; y < texture2D.height; y++)
                {
                    pixelProperties.Add(new PixelProperty(index, texture2D.GetPixel(x, y)));
                    index++;
                }
            }
        }

        private async UniTask<List<Color>> GetDominantColors(int colorCount, CancellationToken cancellationToken)
        {
            var clusters = await KMeans(pixelProperties.Select(c => c.OriginalColor).ToList(), colorCount, cancellationToken);
            return clusters;
        }

        //private async UniTask<List<Color>> KMeans(List<Color> colors, int k, CancellationToken cancellationToken)
        //{
        //    System.Random random = new();
        //    List<Color> centroids = colors.OrderBy(x => random.Next()).Take(k).ToList();
        //    List<int> assignments = new List<int>(new int[colors.Count]);
        //    bool changed;

        //    int iterationCount = 0;

        //    await UniTask.Run(Act, cancellationToken: cancellationToken);

        //    void Act()
        //    {
        //        do
        //        {
        //            if (cancellationToken.IsCancellationRequested) break;
        //            changed = false;

        //            // Параллельное присвоение цветов к ближайшим центроидам
        //            Parallel.For(0, colors.Count, i =>
        //            {
        //                int closestCentroidIndex = GetClosestCentroidIndex(colors[i], centroids);
        //                if (assignments[i] != closestCentroidIndex)
        //                {
        //                    assignments[i] = closestCentroidIndex;
        //                    changed = true;
        //                }
        //            });

        //            // Пересчет центроидов
        //            for (int i = 0; i < k; i++)
        //            {
        //                var clusterColors = colors.Where((_, index) => assignments[index] == i).ToList();
        //                if (clusterColors.Count > 0)
        //                {
        //                    centroids[i] = ImageUtils.AverageColor(clusterColors);
        //                }
        //            }

        //            iterationCount++;
        //            var progress = (float)iterationCount / (float)MaxKMeansIterations;
        //            MainViewModel.UpdateOperation($"Find colors: {iterationCount}/{MaxKMeansIterations}", progress);

        //        } while (changed && iterationCount < MaxKMeansIterations);
        //    }
        //    return centroids;
        //}

        private async UniTask<List<Color>> KMeans(List<Color> colors, int k, CancellationToken cancellationToken)
        {
            System.Random random = new();
            List<Color> centroids = colors.OrderBy(_ => random.Next()).Take(k).ToList();
            int[] assignments = new int[colors.Count];
            bool changed;

            int iterationCount = 0;

            await UniTask.RunOnThreadPool(Act, cancellationToken: cancellationToken);

            void Act()
            {
                do
                {
                    if (cancellationToken.IsCancellationRequested) break;
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

                    var newCentroids = new List<Color>(centroids);

                    Parallel.For(0, k, clusterIndex =>
                    {
                        var clusterColors = colors
                            .Where((_, colorIndex) => assignments[colorIndex] == clusterIndex)
                            .ToList();

                        if (clusterColors.Count > 0)
                        {
                            Color newCentroid = ImageUtils.AverageColor(clusterColors);
                            lock (newCentroids)
                            {
                                newCentroids[clusterIndex] = newCentroid;
                            }
                        }
                    });

                    centroids = new List<Color>(newCentroids);

                    iterationCount++;
                    var progress = (float)iterationCount / MaxKMeansIterations;
                    MainViewModel.UpdateOperation($"Find colors: {iterationCount}/{MaxKMeansIterations}", progress);

                } while (changed && iterationCount < MaxKMeansIterations);
            }
            return centroids;
        }

        //private async UniTask<List<Color>> KMeans(List<Color> colors, int k, CancellationToken cancellationToken) // Bad Result
        //{
        //    System.Random random = new();
        //    List<Color> centroids = colors.OrderBy(_ => random.Next()).Take(k).ToList();
        //    int[] assignments = new int[colors.Count];
        //    bool changed;
        //    int iterationCount = 0;

        //    await UniTask.Run(() =>
        //    {
        //        // Словарь для хранения расстояний, чтобы избежать повторных вычислений
        //        Dictionary<(Color, Color), float> distanceCache = new();
        //        List<Vector3> centroidSums = new List<Vector3>(k);
        //        List<int> counts = new List<int>(k);

        //        void Act()
        //        {
        //            do
        //            {
        //                if (cancellationToken.IsCancellationRequested) break;
        //                changed = false;

        //                // Параллельное присвоение цветов к ближайшим центроидам
        //                Parallel.For(0, colors.Count, i =>
        //                {
        //                    int closestCentroidIndex = -1;
        //                    float minDistance = float.MaxValue;

        //                    for (int j = 0; j < k; j++)
        //                    {
        //                        // Кэширование расстояний для каждой пары (цвет, центроид)
        //                        if (distanceCache.TryGetValue((colors[i], centroids[j]), out float distance))
        //                        {
        //                            distance = (float)ImageUtils.ColorDistance(colors[i], centroids[j]);
        //                            distanceCache[(colors[i], centroids[j])] = distance;
        //                        }

        //                        if (distance < minDistance)
        //                        {
        //                            minDistance = distance;
        //                            closestCentroidIndex = j;
        //                        }
        //                    }

        //                    if (assignments[i] != closestCentroidIndex)
        //                    {
        //                        assignments[i] = closestCentroidIndex;
        //                        changed = true;
        //                    }
        //                });

        //                // Инициализация временных переменных для пересчёта центроидов
        //                centroidSums.Clear();
        //                counts.Clear();
        //                for (int i = 0; i < k; i++)
        //                {
        //                    centroidSums.Add(Vector3.zero);
        //                    counts.Add(0);
        //                }

        //                // Сбор данных для нового пересчета центроидов
        //                Parallel.For(0, colors.Count, i =>
        //                {
        //                    int assignedCentroid = assignments[i];
        //                    var color = colors[i];
        //                    lock (centroidSums)
        //                    {
        //                        centroidSums[assignedCentroid] += new Vector3(color.r, color.g, color.b);
        //                        counts[assignedCentroid]++;
        //                    }
        //                });

        //                // Пересчёт центроидов
        //                for (int i = 0; i < k; i++)
        //                {
        //                    if (counts[i] > 0)
        //                    {
        //                        Vector3 sum = centroidSums[i] / counts[i];
        //                        centroids[i] = new Color(sum.x, sum.y, sum.z);
        //                    }
        //                }

        //                iterationCount++;
        //                var progress = (float)iterationCount / MaxKMeansIterations;
        //                MainViewModel.UpdateOperation($"Find colors: {iterationCount}/{MaxKMeansIterations}", progress);

        //            } while (changed && iterationCount < MaxKMeansIterations);
        //        }

        //        Act();
        //    }, cancellationToken: cancellationToken);

        //    return centroids;
        //}

        private int GetClosestCentroidIndex(Color color, List<Color> centroids)
        {
            double minDistance = double.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < centroids.Count; i++)
            {
                double distance = ImageUtils.ColorDistance(color, centroids[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private async UniTask ReplaceSmallParticles(Texture2D texture, float scale, float tolerance, Color indication, CancellationToken cancellationToken)
        {
            int width = texture.width;
            int height = texture.height;
            Color[] pixels = texture.GetPixels();
         
            int minSize = (int)(scale * (width * height));

            await UniTask.RunOnThreadPool(() => AsyncReplaceSmallParticles(cancellationToken, pixels, width, height, minSize, tolerance, indication), cancellationToken: cancellationToken);
            texture.SetPixels(pixels);
          
            texture.Apply();
        }

        private void AsyncReplaceSmallParticles(CancellationToken cancellationToken, Color[] pixels, int width, int height, int maxSize, float tolerance, Color indication)
        {

            List<ChunkGroup> largeChunks = new();
            List<ChunkGroup> smallChunks = new();

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

                        if (group.Count < maxSize)
                        {
                            ChunkGroup chank = new(currentColor);
                            chank.Group.AddRange(group);

                            smallChunks.Add(chank);
                        }
                        else
                        {
                            ChunkGroup chank = new(currentColor);
                            chank.Group.AddRange(group);
                            largeChunks.Add(chank);
                        }
                    }
                }
            }

            int index = 0;

            foreach (var group in smallChunks)
            {
                Color nearestColor = FindNearestLargeChunkColor(group.Color, group.Group, largeChunks, tolerance);

                foreach (var particle in group.Group)
                {
                    pixels[particle.y * width + particle.x] = nearestColor;
                }
                var progress = (float)index / smallChunks.Count;
                MainViewModel.UpdateOperation($"Remove small Trash:{index}/{smallChunks.Count}", progress);
                index++;

                if (cancellationToken.IsCancellationRequested) return;
            }
        }

        private Color FindNearestLargeChunkColor(Color smallColor, List<Vector2Int> smallGroup, List<ChunkGroup> largeChunks, float tolerance )
        {         
            foreach (var group in largeChunks)
            {
                var randomPoints = GetRandom(group);
                var dist = randomPoints.Min(c=> (c- smallGroup[0]).magnitude);
                group.Distance = dist;
            }
            List<ChunkGroup> sorted = largeChunks.OrderBy(c => c.Distance).ThenByDescending(c=>c.Group.Count).ToList();          
            for (int x = 0; x < sorted.Count; x++)
            {
                Color color = sorted[x].Color;
                //return color;
                if (ImageUtils.AreColorsSimilar(smallColor, color, tolerance))
                {
                    return color;
                }
                if (x > (float)sorted.Count * 0.1f)
                {
                    break;
                }
            }      
            return smallColor;

           
            List<Vector2Int> GetRandom(ChunkGroup chunkColor)
            {
                System.Random random = new System.Random();
                List<Vector2Int> rand = new();
                for (int x = 0; x < 20; x++)
                {
                    int index = random.Next(0, chunkColor.Group.Count);
                    rand.Add(chunkColor.Group[index]);
                }
                return rand;
            }
        }

    }
}
