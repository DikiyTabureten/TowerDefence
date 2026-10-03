using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    /// <summary>
    /// Shop presentation: builds cards, shows the drag preview and messages,
    /// and forwards user actions as events. Never creates towers or touches gold.
    /// </summary>
    public sealed class TowerShopView : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private TowerCardView _cardPrefab;
        [SerializeField] private Transform _cardsContainer;
        [SerializeField] private RectTransform _shopPanel;

        [Header("Drag preview")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _dragPreview;
        [SerializeField] private Color _validColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.3f, 0.85f);

        [Header("Messages")]
        [SerializeField] private TMP_Text _messageText;
        [SerializeField, Min(0.1f)] private float _messageDuration = 1.5f;

        private readonly List<TowerCardView> _cards = new List<TowerCardView>();
        private Coroutine _messageRoutine;

        public event Action<TowerConfig, Vector2> DragStarted;
        public event Action<TowerConfig, Vector2> Dragged;
        public event Action<TowerConfig, Vector2> DragEnded;

        private Camera UiCamera =>
            _canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        private void Awake()
        {
            if (_dragPreview != null)
            {
                _dragPreview.raycastTarget = false; // must not block the drop
                _dragPreview.preserveAspect = true;
                _dragPreview.gameObject.SetActive(false);
            }

            if (_messageText != null)
                _messageText.gameObject.SetActive(false);
        }

        public void Show(IReadOnlyList<TowerConfig> towers)
        {
            ClearCards();

            foreach (TowerConfig config in towers)
            {
                TowerCardView card = Instantiate(_cardPrefab, _cardsContainer);
                card.Setup(config);
                card.DragStarted += OnCardDragStarted;
                card.Dragged += OnCardDragged;
                card.DragEnded += OnCardDragEnded;
                _cards.Add(card);
            }
        }

        public void SetCardAffordable(int index, bool isAffordable)
        {
            if (index >= 0 && index < _cards.Count)
                _cards[index].SetAffordable(isAffordable);
        }

        public bool IsPointerOverShop(Vector2 screenPosition)
        {
            return _shopPanel != null
                && RectTransformUtility.RectangleContainsScreenPoint(_shopPanel, screenPosition, UiCamera);
        }

        public void ShowDragPreview(Sprite icon)
        {
            if (_dragPreview == null) return;

            _dragPreview.sprite = icon;
            _dragPreview.gameObject.SetActive(true);
        }

        public void UpdateDragPreview(Vector2 screenPosition, bool isValid)
        {
            if (_dragPreview == null) return;

            RectTransform parent = (RectTransform)_dragPreview.rectTransform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, UiCamera, out Vector2 local))
                _dragPreview.rectTransform.localPosition = local;

            _dragPreview.color = isValid ? _validColor : _invalidColor;
        }

        public void HideDragPreview()
        {
            if (_dragPreview != null)
                _dragPreview.gameObject.SetActive(false);
        }

        public void ShowMessage(string message)
        {
            if (_messageText == null) return;

            if (_messageRoutine != null)
                StopCoroutine(_messageRoutine);

            _messageRoutine = StartCoroutine(MessageRoutine(message));
        }

        private IEnumerator MessageRoutine(string message)
        {
            _messageText.text = message;
            _messageText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(_messageDuration);
            _messageText.gameObject.SetActive(false);
            _messageRoutine = null;
        }

        private void OnCardDragStarted(TowerCardView card, Vector2 position) => DragStarted?.Invoke(card.Config, position);
        private void OnCardDragged(TowerCardView card, Vector2 position) => Dragged?.Invoke(card.Config, position);
        private void OnCardDragEnded(TowerCardView card, Vector2 position) => DragEnded?.Invoke(card.Config, position);

        private void ClearCards()
        {
            foreach (TowerCardView card in _cards)
            {
                if (card == null) continue;

                card.DragStarted -= OnCardDragStarted;
                card.Dragged -= OnCardDragged;
                card.DragEnded -= OnCardDragEnded;
                Destroy(card.gameObject);
            }

            _cards.Clear();
        }

        private void OnDestroy() => ClearCards();
    }
}
