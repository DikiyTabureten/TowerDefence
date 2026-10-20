using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    /// <summary>One level in the main menu list: name, description and a click event. No logic.</summary>
    public sealed class LevelButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;

        public event Action<LevelButtonView> Clicked;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Setup(string title, string description)
        {
            if (_title != null) _title.text = title;
            if (_description != null) _description.text = description;
        }
    }
}
