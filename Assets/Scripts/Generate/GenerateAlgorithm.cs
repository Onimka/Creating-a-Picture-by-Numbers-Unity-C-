using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

namespace PictureGenerator
{
    public abstract class GenerateAlgorithm
    {
        
        protected CancellationTokenSource cancellationTokenSource;
        private float _startTime;

        public async UniTask<Texture2D> Start(Texture2D texture2D, int countColors)
        {
            cancellationTokenSource?.Cancel();

            cancellationTokenSource = new();
            _startTime = Time.time;
            var result = await StartGeneration(texture2D, countColors);
            Debug.Log($"Operation Time: {Time.time - _startTime}");
            return result;
        }


        protected abstract UniTask<Texture2D> StartGeneration(Texture2D texture2D, int countColors);


        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }

    }
}
