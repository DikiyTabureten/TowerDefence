using UnityEngine;

namespace TowerDefense
{
    /// <summary>Draws a circle with a LineRenderer. Created at runtime by Tower, no manual setup needed.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class RangeIndicator : MonoBehaviour
    {
        private const int Segments = 64;
        private static Material _defaultMaterial;

        private LineRenderer _line;

        public static RangeIndicator Create(Transform owner, float radius, Color color, float width,
            Material material, int sortingOrder)
        {
            var go = new GameObject("RangeIndicator");
            go.transform.SetParent(owner, false);

            RangeIndicator indicator = go.AddComponent<RangeIndicator>(); // LineRenderer is added by RequireComponent
            indicator.Setup(color, width, material, sortingOrder);
            indicator.SetRadius(radius);
            return indicator;
        }

        public void SetRadius(float radius)
        {
            // Points are in local space, so compensate for the tower's scale.
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f);
            float localRadius = radius / scale;

            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * localRadius, Mathf.Sin(angle) * localRadius, 0f));
            }
        }

        public void SetVisible(bool isVisible) => _line.enabled = isVisible;

        private void Setup(Color color, float width, Material material, int sortingOrder)
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = Segments;
            _line.widthMultiplier = width;
            _line.startColor = color;
            _line.endColor = color;
            _line.sortingOrder = sortingOrder;
            _line.sharedMaterial = material != null ? material : GetDefaultMaterial();
        }

        private static Material GetDefaultMaterial()
        {
            if (_defaultMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) _defaultMaterial = new Material(shader);
            }

            return _defaultMaterial;
        }
    }
}
