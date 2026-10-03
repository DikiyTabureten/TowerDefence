using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense
{
    /// <summary>A single shop card. Displays icon + price and reports drag events. Knows nothing about gold or towers.</summary>
    public sealed class TowerCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField, Range(0f, 1f)] private float _unaffordableAlpha = 0.45f;

        private bool _isDragging;

        public TowerConfig Config { get; private set; }

        public event Action<TowerCardView, Vector2> DragStarted;
        public event Action<TowerCardView, Vector2> Dragged;
        public event Action<TowerCardView, Vector2> DragEnded;

        public void Setup(TowerConfig config)
        {
            Config = config;
            _icon.sprite = config.Icon;
            _icon.preserveAspect = true;
            _priceText.text = config.Cost.ToString();
        }

        public void SetAffordable(bool isAffordable)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = isAffordable ? 1f : _unaffordableAlpha;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _isDragging = true;
            DragStarted?.Invoke(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            Dragged?.Invoke(this, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            DragEnded?.Invoke(this, eventData.position);
        }
    }
}
