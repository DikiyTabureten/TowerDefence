using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense
{
    /// <summary>
    /// A single shop card in the HUD style: icon, name, coin + price. Reports drag events and
    /// highlights itself on hover. Knows nothing about gold or towers — the presenter tells it
    /// whether it is affordable via SetAffordable.
    /// </summary>
    public sealed class TowerCardView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Parts")]
        [SerializeField, Tooltip("Optional. Empty = the card shows no tower name.")]
        private TMP_Text _nameText;
        [SerializeField, Tooltip("Moves up on hover. The root is positioned by the layout group, so only this child moves.")]
        private RectTransform _body;
        [SerializeField, Tooltip("Image drawing the card's border.")]
        private Image _border;
        [SerializeField] private Image _coinIcon;

        [Header("Colours")]
        [SerializeField] private Color _iconColor = Color.white;
        [SerializeField] private Color _nameColor = new Color32(0xE8, 0xE6, 0xE3, 0xFF);
        [SerializeField] private Color _priceColor = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
        [SerializeField] private Color _borderColor = new Color32(0x19, 0x1E, 0x21, 0xFF);

        [Header("Not enough gold")]
        [SerializeField] private Color _unaffordableIconColor = new Color32(0x7A, 0x7A, 0x7A, 0xFF);
        [SerializeField] private Color _unaffordableNameColor = new Color32(0x9A, 0xA0, 0xA6, 0xFF);
        [SerializeField] private Color _unaffordablePriceColor = new Color32(0xE0, 0x5A, 0x4B, 0xFF);

        [Header("Hover")]
        [SerializeField] private Color _hoverBorderColor = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
        [SerializeField, Min(0f)] private float _hoverLift = 6f;
        [SerializeField, Min(0f), Tooltip("Seconds. 0 = instant. Uses unscaled time, so it works while the game is paused.")]
        private float _hoverDuration = 0.1f;

        private bool _isDragging;
        private bool _isHovered;
        private float _highlight;          // 0 = idle, 1 = fully highlighted
        private Vector2 _bodyRestPosition;

        public TowerConfig Config { get; private set; }

        public event Action<TowerCardView, Vector2> DragStarted;
        public event Action<TowerCardView, Vector2> Dragged;
        public event Action<TowerCardView, Vector2> DragEnded;

        private bool IsHighlighted => _isHovered || _isDragging;

        private void Awake()
        {
            if (_body != null) _bodyRestPosition = _body.anchoredPosition;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            ApplyHighlight(0f);
        }

        public void Setup(TowerConfig config)
        {
            Config = config;
            _icon.sprite = config.Icon;
            _icon.preserveAspect = true;
            _priceText.text = config.Cost.ToString();
            if (_nameText != null) _nameText.text = config.DisplayName;
            SetAffordable(true);
        }

        public void SetAffordable(bool isAffordable)
        {
            // Dragging stays allowed: the presenter shows "Not enough gold" itself.
            if (_icon != null) _icon.color = isAffordable ? _iconColor : _unaffordableIconColor;
            if (_nameText != null) _nameText.color = isAffordable ? _nameColor : _unaffordableNameColor;
            if (_priceText != null) _priceText.color = isAffordable ? _priceColor : _unaffordablePriceColor;
        }

        private void Update()
        {
            float target = IsHighlighted ? 1f : 0f;
            if (Mathf.Approximately(_highlight, target)) return;

            _highlight = _hoverDuration <= 0f
                ? target
                : Mathf.MoveTowards(_highlight, target, Time.unscaledDeltaTime / _hoverDuration);
            ApplyHighlight(_highlight);
        }

        private void ApplyHighlight(float amount)
        {
            if (_border != null) _border.color = Color.Lerp(_borderColor, _hoverBorderColor, amount);
            if (_body != null) _body.anchoredPosition = _bodyRestPosition + Vector2.up * (_hoverLift * amount);
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;

        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;

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
            _isHovered = false; // the pointer is usually over the map now; a new enter event re-highlights
            DragEnded?.Invoke(this, eventData.position);
        }
    }
}
