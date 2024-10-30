using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PictureGenerator
{
    public class ColorBoxViewItem : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        private List<ColorPresetViewItem> ViewItems = new();

        private GameObject Prefab;

        private void Awake()
        {
            Prefab = ServiceLocator.Get<Config>().ColorPreset;
        }

        public void ShowGroup(List<Color> colors)
        {
            ViewItems.ForEach(c => c.gameObject.SetActive(false));

            //foreach (var color in colors)
            //{
            //    GetObj().Link(color);
            //}
            for (var i = 0; i < colors.Count; i++)
            {
                GetObj().Link(colors[i], i+1);
            }
        }

        private ColorPresetViewItem GetObj()
        {
            var obj = ViewItems.FirstOrDefault(c=> !c.gameObject.activeInHierarchy);
            if(obj != null)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }

            var inst = Instantiate(Prefab, _content);

            var preset = inst.GetComponent<ColorPresetViewItem>();
            ViewItems.Add(preset);
            return preset;
        }
    }
}
