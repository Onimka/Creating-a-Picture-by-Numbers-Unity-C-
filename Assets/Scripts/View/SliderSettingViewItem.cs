using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictureGenerator
{
    public class SliderSettingViewItem : MonoBehaviour
    {
        [SerializeField] private Slider _countSlider;
        [SerializeField] private TMP_Text _valueTXT;
        public int Value { get; private set; }

        private void Awake()
        {         
            OnValueChanged(_countSlider.value);
            _countSlider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(float value) 
        {
            Value = (int)value;
            _valueTXT.text = Value.ToString();
        }

        private void OnDestroy()
        {

            _countSlider.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}
