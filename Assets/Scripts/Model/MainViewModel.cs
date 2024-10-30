using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace PictureGenerator
{
    public class MainViewModel
    {
        public event Action<Texture2D> ImageLoaded;
        public event Action<Texture2D> ColorImageGenerated;
        public event Action<Texture2D> LineImageGenerated;
        public event Action<Texture2D> LineCombineFinalGenerated;
        public event Action<Texture2D> FinalLineGenerated;

        public bool ActiveProcess { get; private set; }

        //private int MedianFilterSize = 17; // 9 - 1920x1080 / 22 - 3840x2160

        private Texture2D _originalImage;
        private Texture2D _colorTexture;
        private Texture2D _lineDraftTexture;
        public IReadOnlyList<Color> Pallet { get; private set; }

        private GenerateAlgorithm _generateAlgorithm;
        private List<Texture2D> _sliced = new();
        private CancellationTokenSource _cancellationTokenSource;

        private Vector2Int _defaultResolution = new(3500, 3500);



        public MainViewModel()
        {
        }

        public void Cancel()
        {
            _generateAlgorithm?.Cancel();
            _cancellationTokenSource?.Cancel();
        }

        // Load
        public async void Load()
        {
            string filePath = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg,bmp");

            Texture2D texture = null;

            if (filePath != "")
            {
                texture = await LoadTextureAsync(filePath);
                if (texture != null)
                {
                    _originalImage = texture;
                    ImageLoaded?.Invoke(texture);
                }
                else
                {
                    // Log
                }
            }
        }

        private async UniTask<Texture2D> LoadTextureAsync(string filePath)
        {
            byte[] imageData = await LoadImageDataAsync(filePath);
            return CreateTexture(imageData);
        }

        private async UniTask<byte[]> LoadImageDataAsync(string filePath)
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllBytes(filePath);
                }
                else
                {
                    Debug.LogError("File not found: " + filePath);
                    return null;
                }
            });
        }

        private Texture2D CreateTexture(byte[] imageData)
        {
            if (imageData == null)
                return null;

            _generateAlgorithm?.Cancel();
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            return texture;
        }
        //

        // Generate
        public async UniTask Generate(int countColors, int medianFilterSize)
        {
            if (_originalImage == null)
            {
                return;
            }
            ActiveProcess = true;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();

            Texture2D cloneTexture = ImageUtils.CloneTexture(_originalImage);

            cloneTexture = ImageUtils.UpscaleTexture(cloneTexture, _defaultResolution);
        
            await ImageUtils.ApplyMedianFilterAsync(cloneTexture, medianFilterSize, _cancellationTokenSource.Token);
            await ImageUtils.ApplyGaussianBlur(cloneTexture, 3, _cancellationTokenSource.Token);

            // Main Result
            _generateAlgorithm = new KAvarage();
            await _generateAlgorithm.Start(cloneTexture, countColors);

            _colorTexture = cloneTexture;
            Pallet = AnalysePallet(_colorTexture);

            //var writeAlg = new WriteNumber(Pallet.ToList());
            //_finalTexture = await writeAlg.Start(cloneTexture, countColors);

            ColorImageGenerated?.Invoke(_colorTexture);

            // Lines
            await CreateLinesAsync(_colorTexture, 2, _cancellationTokenSource.Token);
            _lineDraftTexture = CombineTextures(_sliced);
            LineImageGenerated?.Invoke(_lineDraftTexture);

            // Combine
            var combine = await DraftCombineResult();
            LineCombineFinalGenerated?.Invoke(combine);

            // Final
            var writeAlg = new WriteNumber(Pallet.ToList());
            var test = await writeAlg.GenerateFinalImage(_colorTexture, _lineDraftTexture);
            FinalLineGenerated?.Invoke(test);


            ActiveProcess = false;
        }

        private List<Color> AnalysePallet(Texture2D texture2D)
        {
            List<Color> colors = new();
            for (int x = 0; x < texture2D.width; x++)
            {
                for (int y = 0; y < texture2D.height; y++)
                {
                    var pixel = texture2D.GetPixel(x, y);
                    if (!colors.Contains(pixel))
                    {
                        colors.Add(pixel);
                    }
                }
            }
            return colors;
        }

        public async UniTask CreateLinesAsync(Texture2D texture2D, int outlineThickness, CancellationToken cancellationToken)
        {
            var colorTextures = new Dictionary<Color, Texture2D>();
            var pixels = texture2D.GetPixels();

            for (int x = 0; x < texture2D.width; x++)
            {
                for (int y = 0; y < texture2D.height; y++)
                {
                    var pixel = pixels[y * texture2D.width + x];
                    if (!colorTextures.ContainsKey(pixel) && pixel.a > 0)
                    {
                        var outlinedTexture = await CreateOutlinedTextureAsync(texture2D, pixel, outlineThickness, cancellationToken);
                        colorTextures[pixel] = outlinedTexture;
                    }
                }
            }

            _sliced = colorTextures.Values.ToList();
        }

        private async UniTask<Texture2D> CreateOutlinedTextureAsync(Texture2D originalTexture, Color targetColor, int outlineThickness, CancellationToken cancellationToken)
        {
            int width = originalTexture.width;
            int height = originalTexture.height;
            var outlinedTexture = new Texture2D(width, height);
            var originalPixels = originalTexture.GetPixels();
            var outlinePixels = new Color[originalPixels.Length];

            int halfThickness = outlineThickness / 2;

            await UniTask.RunOnThreadPool(() =>
            {
                Parallel.For(0, width, x =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        int index = y * width + x;
                        var pixelColor = originalPixels[index];

                        if (pixelColor == targetColor)
                        {
                            outlinePixels[index] = Color.white;

                            for (int dx = -halfThickness; dx <= halfThickness; dx++)
                            {
                                for (int dy = -halfThickness; dy <= halfThickness; dy++)
                                {
                                    if (dx == 0 && dy == 0) continue;

                                    int nx = x + dx;
                                    int ny = y + dy;
                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                    {
                                        int neighborIndex = ny * width + nx;
                                        if (originalPixels[neighborIndex] != targetColor && originalPixels[neighborIndex].a > 0)
                                        {
                                            outlinePixels[neighborIndex] = Color.grey;
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }, cancellationToken: cancellationToken);

            outlinedTexture.SetPixels(outlinePixels);
            outlinedTexture.Apply();

            return outlinedTexture;
        }

        private Texture2D CombineTextures(List<Texture2D> textures)
        {
            if (textures.Count == 0) return null;

            int width = textures[0].width;
            int height = textures[0].height;
            var combinedPixels = new Color[width * height];

            foreach (var texture in textures)
            {
                var pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a > 0)
                    {
                        combinedPixels[i] = pixels[i];
                    }
                }
            }

            Texture2D combinedTexture = new Texture2D(width, height);
            combinedTexture.SetPixels(combinedPixels);
            combinedTexture.Apply();
            return combinedTexture;
        }

        private async UniTask<Texture2D> DraftCombineResult()
        {
            var texture = ImageUtils.CloneTexture(_lineDraftTexture);
            var pixels = texture.GetPixels();
            await UniTask.RunOnThreadPool(() =>
            {
                Parallel.For(0, pixels.Length, x =>
                {
                    if (pixels[x] == Color.white)
                    {
                        pixels[x] = Color.clear;
                    }
                });            
            });

            texture.SetPixels(pixels);
            texture.Apply();

            return CombineTextures(new List<Texture2D> { _colorTexture, texture });
        }



        public static string OperationName { get; private set; }

        public static float ProgressOperation { get; private set; }

        public static void UpdateOperation(string operationName, float progress)
        {
            OperationName = operationName;
            ProgressOperation = progress;
        }
    }
    
}
