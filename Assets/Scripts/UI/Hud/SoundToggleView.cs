using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    /// <summary>View of the sound switch: a button whose label shows the current state. No logic.</summary>
    public sealed class SoundToggleView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private string _onText = "Sound: On";
        [SerializeField] private string _offText = "Sound: Off";

        public event Action Clicked;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void ShowMuted(bool isMuted)
        {
            if (_label != null) _label.text = isMuted ? _offText : _onText;
        }
    }
}
