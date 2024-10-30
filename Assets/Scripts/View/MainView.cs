using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictureGenerator
{
    public class MainView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Clickable _loadImageButton;
        [SerializeField] private Clickable _generateFinalImage;
        [SerializeField] private Clickable _cancelGeneration;

        [Space]

        [Header("Images")]
        [SerializeField] private ImageViewItem _loadedImage;
        [SerializeField] private ImageViewItem _finalImage;
        [SerializeField] private ImageViewItem _draftImage;
        [SerializeField] private ImageViewItem _draftCombineFinalImage;
        [SerializeField] private ImageViewItem _finalLineImage;

        [Space]

        [SerializeField] private Image _filler;
        [SerializeField] private TMP_Text _percent;
        [SerializeField] private TMP_Text _operationName;

        [Space]
        [SerializeField] private ColorBoxViewItem _colorBoxViewItem;

        [Space]
        [Header("Settings")]
        [SerializeField] private SliderSettingViewItem _countColorsSettings;
        [SerializeField] private SliderSettingViewItem _filterSettings;
      

        private MainViewModel _mainViewModel;

        private void Awake()
        {
            _filler.transform.parent.gameObject.SetActive(false);
            _colorBoxViewItem.gameObject.SetActive(false);
            _mainViewModel = new();
            Subscribe();

            
            OnChangedGenetrationState(true);
        }

        private void Subscribe()
        {
            _loadImageButton.Clicked += OnClickedLoadImage;
            _generateFinalImage.Clicked += OnClickedGenerateImage;
            _cancelGeneration.Clicked += OnClickedCancel;


            _mainViewModel.ImageLoaded += OnImageLoaded;
            _mainViewModel.ColorImageGenerated += OnColorImageGenerated;
            _mainViewModel.LineImageGenerated += OnLineImageGenerated;
            _mainViewModel.LineCombineFinalGenerated += OnCombineImageGenerated;
            _mainViewModel.FinalLineGenerated += OnFinalImageGenerated;
        }

        private void Unubscribe()
        {
            _loadImageButton.Clicked -= OnClickedLoadImage;
            _generateFinalImage.Clicked -= OnClickedGenerateImage;
            _cancelGeneration.Clicked -= OnClickedCancel;


            _mainViewModel.ImageLoaded -= OnImageLoaded;
            _mainViewModel.ColorImageGenerated -= OnColorImageGenerated;
            _mainViewModel.LineImageGenerated -= OnLineImageGenerated;
            _mainViewModel.LineCombineFinalGenerated -= OnCombineImageGenerated;
            _mainViewModel.FinalLineGenerated -= OnFinalImageGenerated;
            _mainViewModel?.Cancel();
        }


        private void OnClickedLoadImage()
        {
            _mainViewModel.Load();
        }

        private async void OnClickedGenerateImage()
        {
            if(_loadedImage.RawImage.texture == null)
            {
                return;
            }

            _percent.text = $"0%";
            _filler.fillAmount = 0;
          
            OnChangedGenetrationState(false);

            await _mainViewModel.Generate(_countColorsSettings.Value, _filterSettings.Value);
            OnChangedGenetrationState(true);
        }

        private void OnClickedCancel()
        {
            _mainViewModel.Cancel();
            OnChangedGenetrationState(true);
        }

        private void OnImageLoaded(Texture2D texture)
        {
            _loadedImage.SetImage(texture) ;             
        }


        private void OnColorImageGenerated(Texture2D texture)
        {
            _finalImage.SetImage(texture);
          
            _colorBoxViewItem.gameObject.SetActive(true);
            _colorBoxViewItem.ShowGroup(_mainViewModel.Pallet.ToList());
        }

        private void OnLineImageGenerated(Texture2D texture)
        {
            _draftImage.SetImage(texture);
        }

        private void OnCombineImageGenerated(Texture2D texture)
        {
            _draftCombineFinalImage.SetImage(texture);
        }

        private void OnFinalImageGenerated(Texture2D texture)
        {
            _finalLineImage.SetImage(texture);
        }

        private void OnChangedGenetrationState(bool isFinished)
        {
            _generateFinalImage.gameObject.SetActive(isFinished);
            _cancelGeneration.gameObject.SetActive(!isFinished);
            _filler.transform.parent.gameObject.SetActive(!isFinished);
        }

        private void Update()
        {
            if(_filler.gameObject.activeInHierarchy)
            {
                _filler.fillAmount = MainViewModel.ProgressOperation;
                _percent.text = $"{MainViewModel.ProgressOperation * 100:0}%";
                _operationName.text = $"{MainViewModel.OperationName}";
            }         
        }

        private void OnDestroy()
        {
            Unubscribe();          
        }
    }
}
