using System;
using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MasterBuilderHeeler.Core.UI
{
    public sealed class HudPanelView
    {
        private const int RoundedSpriteSize = 64;
        private const int RoundedSpriteCornerRadius = 24;

        private static Sprite? _cachedRoundedSprite;

        private readonly PanelLayoutSettings _layoutSettings;
        private readonly Action? _onFontSizeChanged;
        private readonly Action? _onDeactivated;
        private readonly GameObject _panelRoot;
        private readonly RectTransform _rootRect;
        private readonly Image _backgroundImage;
        private readonly HorizontalLayoutGroup _horizontalLayout;
        private readonly RectTransform _leftColumnRect;
        private readonly RectTransform _rightColumnRect;
        private readonly VerticalLayoutGroup _leftLayout;
        private readonly VerticalLayoutGroup _rightLayout;
        private readonly LayoutElement _leftLayoutElement;
        private readonly LayoutElement _rightLayoutElement;
        private readonly List<HudLabelRow> _rows = new List<HudLabelRow>();

        private TextMeshProUGUI? _floatingHeader;
        private float _maxObservedLeftWidth;
        private float _maxObservedRightWidth;
        private int _currentFontSize;
        private bool _isDestroyed;

        public HudPanelView(
            Transform parentTransform,
            string panelName,
            PanelLayoutSettings layoutSettings,
            Action? onFontSizeChanged = null,
            Action? onDeactivated = null)
        {
            _layoutSettings = layoutSettings;
            _onFontSizeChanged = onFontSizeChanged;
            _onDeactivated = onDeactivated;
            _currentFontSize = layoutSettings.FontSize.Value;

            Color initialBackground = layoutSettings.BackgroundColor.Value;

            _panelRoot = new GameObject(panelName, typeof(RectTransform));
            _panelRoot.transform.SetParent(parentTransform, false);

            _rootRect = (RectTransform)_panelRoot.transform;
            _rootRect.sizeDelta = Vector2.zero;

            _backgroundImage = _panelRoot.AddComponent<Image>();
            _backgroundImage.sprite = GetOrCreateRoundedSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = initialBackground;
            _backgroundImage.raycastTarget = false;
            _backgroundImage.enabled = ModConfig.ShowInfoPanelBackground.Value && initialBackground.a > 0.001f;

            _horizontalLayout = _panelRoot.AddComponent<HorizontalLayoutGroup>();
            _horizontalLayout.spacing = 8f;
            _horizontalLayout.childAlignment = TextAnchor.UpperLeft;
            _horizontalLayout.childForceExpandWidth = false;
            _horizontalLayout.childForceExpandHeight = true;
            _horizontalLayout.childControlWidth = true;
            _horizontalLayout.childControlHeight = true;
            _horizontalLayout.padding = new RectOffset(10, 10, 8, 8);

            ContentSizeFitter rootFitter = _panelRoot.AddComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _leftColumnRect = CreateColumn("LeftColumn", out _leftLayout, out _leftLayoutElement);
            _rightColumnRect = CreateColumn("RightColumn", out _rightLayout, out _rightLayoutElement);

            SetPosition(_layoutSettings.Offset);
            SubscribeLayoutEvents();
        }

        public static void Cleanup()
        {
            if (_cachedRoundedSprite)
            {
                if (_cachedRoundedSprite.texture)
                {
                    UnityEngine.Object.Destroy(_cachedRoundedSprite.texture);
                }

                UnityEngine.Object.Destroy(_cachedRoundedSprite);
                _cachedRoundedSprite = null;
            }
        }

        public void Destroy()
        {
            if (_isDestroyed)
            {
                return;
            }

            _isDestroyed = true;
            UnsubscribeLayoutEvents();

            if (_panelRoot)
            {
                UnityEngine.Object.Destroy(_panelRoot);
            }
        }

        public HudLabelRow AddRow(string labelText = "", bool isSecondaryValue = false)
        {
            TextMeshProUGUI labelTextComponent = CreateLabel(_leftColumnRect, "Label");
            labelTextComponent.alignment = TextAlignmentOptions.Right;
            labelTextComponent.color = HudTheme.LabelColor;
            labelTextComponent.text = labelText;

            TextMeshProUGUI valueTextComponent = CreateLabel(_rightColumnRect, "Value");
            valueTextComponent.alignment = TextAlignmentOptions.Left;
            valueTextComponent.color = isSecondaryValue
                ? HudTheme.SecondaryValueColor
                : HudTheme.PrimaryValueColor;
            valueTextComponent.text = string.Empty;

            float rowHeight = _currentFontSize * 1.35f;

            LayoutElement leftLayoutElement = labelTextComponent.gameObject.AddComponent<LayoutElement>();
            ApplyRowHeight(leftLayoutElement, rowHeight);

            LayoutElement rightLayoutElement = valueTextComponent.gameObject.AddComponent<LayoutElement>();
            ApplyRowHeight(rightLayoutElement, rowHeight);

            HudLabelRow newRow = new HudLabelRow(labelTextComponent, valueTextComponent, leftLayoutElement, rightLayoutElement, isSecondaryValue);
            _rows.Add(newRow);
            return newRow;
        }

        public void SetAnchors(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            _rootRect.anchorMin = anchorMin;
            _rootRect.anchorMax = anchorMax;
            _rootRect.pivot = pivot;

            _leftColumnRect.pivot = new Vector2(0f, pivot.y);
            _rightColumnRect.pivot = new Vector2(0f, pivot.y);

            TextAnchor alignment = pivot.y switch
            {
                <= 0.25f => TextAnchor.LowerLeft,
                >= 0.75f => TextAnchor.UpperLeft,
                _ => TextAnchor.MiddleLeft
            };

            _horizontalLayout.childAlignment = alignment;
            _leftLayout.childAlignment = alignment;
            _rightLayout.childAlignment = alignment;
        }

        public void SetFloatingHeader(string title)
        {
            if (!_floatingHeader)
            {
                _floatingHeader = CreateLabel(_rootRect, "FloatingHeader");
                _floatingHeader.alignment = TextAlignmentOptions.Center;
                _floatingHeader.fontSize = _currentFontSize + 2;
                _floatingHeader.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                RectTransform rectTransform = _floatingHeader.rectTransform;
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                rectTransform.anchoredPosition = new Vector2(0f, 16f);
            }

            _floatingHeader.color = HudTheme.HeaderColor;
            _floatingHeader.text = $"<b><u>{title}</u></b>";
            _floatingHeader.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        public void SetBackground(bool visible, Color backgroundColor)
        {
            if (_panelRoot)
            {
                _backgroundImage.color = backgroundColor;
                _backgroundImage.enabled = visible && backgroundColor.a > 0.001f;
            }
        }

        public void SetActive(bool active)
        {
            if (_panelRoot && _panelRoot.activeSelf != active)
            {
                _panelRoot.SetActive(active);
            }
        }

        public void ObserveAllRowLabels()
        {
            float maxLeftWidth = 0f;
            for (int index = 0; index < _rows.Count; index++)
            {
                maxLeftWidth = Mathf.Max(maxLeftWidth, _rows[index].Label.GetPreferredValues(_rows[index].Label.text).x);
            }

            ApplyObservedWidths(maxLeftWidth, 0f);
        }

        public void ObserveSampleWidths(string sampleLabel, string sampleValue)
        {
            if (_rows.Count == 0)
            {
                return;
            }

            float leftWidth = string.IsNullOrEmpty(sampleLabel) ? 0f : _rows[0].Label.GetPreferredValues(sampleLabel).x;
            float rightWidth = string.IsNullOrEmpty(sampleValue) ? 0f : _rows[0].Value.GetPreferredValues(sampleValue).x;
            ApplyObservedWidths(leftWidth, rightWidth);
        }

        // i hate panel jitter. you will still get some, sometimes, but this helps prevent it from scaling horizontally.
        public void UpdateClampedWidth()
        {
            float maxLeftWidth = 0f;
            float maxRightWidth = 0f;

            for (int index = 0; index < _rows.Count; index++)
            {
                HudLabelRow row = _rows[index];
                if (!row.Label.gameObject.activeSelf)
                {
                    continue;
                }

                maxLeftWidth = Mathf.Max(maxLeftWidth, row.Label.GetPreferredValues(row.Label.text).x);
                maxRightWidth = Mathf.Max(maxRightWidth, row.Value.GetPreferredValues(row.Value.text).x);
            }

            ApplyObservedWidths(maxLeftWidth, maxRightWidth);
        }

        public void ResetClampedWidth()
        {
            _maxObservedLeftWidth = 0f;
            _maxObservedRightWidth = 0f;
            if (_panelRoot)
            {
                _leftLayoutElement.minWidth = 0f;
                _rightLayoutElement.minWidth = 0f;
            }
        }

        private static void ApplyRowHeight(LayoutElement layoutElement, float rowHeight)
        {
            layoutElement.minHeight = rowHeight;
            layoutElement.preferredHeight = rowHeight;
            layoutElement.flexibleHeight = 0f;
        }

        private static Sprite GetOrCreateRoundedSprite()
        {
            if (_cachedRoundedSprite)
            {
                return _cachedRoundedSprite;
            }

            Texture2D texture = new Texture2D(RoundedSpriteSize, RoundedSpriteSize, TextureFormat.RGBA32, false)
            {
                name = "MBH_RoundedPanelBackground",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[RoundedSpriteSize * RoundedSpriteSize];
            int maxCornerBound = RoundedSpriteSize - RoundedSpriteCornerRadius - 1;
            for (int pixelY = 0; pixelY < RoundedSpriteSize; pixelY++)
            {
                for (int pixelX = 0; pixelX < RoundedSpriteSize; pixelX++)
                {
                    float cornerX = Mathf.Clamp(pixelX, RoundedSpriteCornerRadius, maxCornerBound);
                    float cornerY = Mathf.Clamp(pixelY, RoundedSpriteCornerRadius, maxCornerBound);
                    float deltaX = pixelX - cornerX;
                    float deltaY = pixelY - cornerY;
                    float distance = Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

                    float alpha = Mathf.Clamp01(RoundedSpriteCornerRadius - distance + 0.5f);
                    pixels[(pixelY * RoundedSpriteSize) + pixelX] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            float border = RoundedSpriteCornerRadius;
            _cachedRoundedSprite = Sprite.Create(
                texture,
                new Rect(0, 0, RoundedSpriteSize, RoundedSpriteSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            _cachedRoundedSprite.name = "MBH_RoundedPanelSprite";
            _cachedRoundedSprite.hideFlags = HideFlags.HideAndDontSave;

            return _cachedRoundedSprite;
        }

        private RectTransform CreateColumn(string columnName, out VerticalLayoutGroup layoutGroup, out LayoutElement layoutElement)
        {
            GameObject columnObject = new GameObject(columnName, typeof(RectTransform));
            columnObject.transform.SetParent(_rootRect, false);

            RectTransform columnRect = (RectTransform)columnObject.transform;
            columnRect.sizeDelta = Vector2.zero;

            layoutGroup = columnObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 6f;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            layoutElement = columnObject.AddComponent<LayoutElement>();
            return columnRect;
        }

        private TextMeshProUGUI CreateLabel(Transform parentTransform, string objectName)
        {
            // stay inactive while adding TextMeshProUGUI or Awake tries to load default tmp resources that got stripped
            // thanks iron gate. UnifiedPopup font material gets copied for fallback symbols TMP_AveriaSansLibre doesnt have
            // btw, there are some pretty badass glyphs in the fonts they chose. check em out.
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
            labelObject.SetActive(false);
            labelObject.transform.SetParent(parentTransform, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();

            if (UnifiedPopup.instance && UnifiedPopup.instance.bodyText)
            {
                label.font = UnifiedPopup.instance.bodyText.font;
                label.fontSharedMaterial = UnifiedPopup.instance.bodyText.fontSharedMaterial;
            }
            else
            {
                label.font = Jotunn.Managers.GUIManager.Instance.TMP_AveriaSansLibre;
            }

            label.rectTransform.sizeDelta = Vector2.zero;
            label.margin = Vector4.zero;
            label.fontSize = _currentFontSize;
            label.richText = true;
            label.raycastTarget = false;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            labelObject.SetActive(true);
            return label;
        }

        private void ApplyObservedWidths(float leftWidth, float rightWidth)
        {
            if (leftWidth > _maxObservedLeftWidth)
            {
                _maxObservedLeftWidth = leftWidth;
                _leftLayoutElement.minWidth = _maxObservedLeftWidth;
            }

            if (rightWidth > _maxObservedRightWidth)
            {
                _maxObservedRightWidth = rightWidth;
                _rightLayoutElement.minWidth = _maxObservedRightWidth;
            }
        }

        private void SubscribeLayoutEvents()
        {
            _layoutSettings.OffsetX.SettingChanged += OnOffsetSettingChanged;
            _layoutSettings.OffsetY.SettingChanged += OnOffsetSettingChanged;
            _layoutSettings.FontSize.SettingChanged += OnFontSizeSettingChanged;
            _layoutSettings.BackgroundColor.SettingChanged += OnBackgroundSettingChanged;
            _layoutSettings.Show.SettingChanged += OnVisibilitySettingChanged;
            ModConfig.ShowInfoPanelBackground.SettingChanged += OnBackgroundSettingChanged;
            ModConfig.EnableMod.SettingChanged += OnVisibilitySettingChanged;
            HudTheme.ThemeChanged += ApplyThemeColors;
        }

        private void UnsubscribeLayoutEvents()
        {
            _layoutSettings.OffsetX.SettingChanged -= OnOffsetSettingChanged;
            _layoutSettings.OffsetY.SettingChanged -= OnOffsetSettingChanged;
            _layoutSettings.FontSize.SettingChanged -= OnFontSizeSettingChanged;
            _layoutSettings.BackgroundColor.SettingChanged -= OnBackgroundSettingChanged;
            _layoutSettings.Show.SettingChanged -= OnVisibilitySettingChanged;
            ModConfig.ShowInfoPanelBackground.SettingChanged -= OnBackgroundSettingChanged;
            ModConfig.EnableMod.SettingChanged -= OnVisibilitySettingChanged;
            HudTheme.ThemeChanged -= ApplyThemeColors;
        }

        private void ApplyThemeColors()
        {
            if (!_panelRoot)
            {
                return;
            }

            if (_floatingHeader)
            {
                _floatingHeader.color = HudTheme.HeaderColor;
            }

            Color labelColor = HudTheme.LabelColor;
            Color primaryValueColor = HudTheme.PrimaryValueColor;
            Color secondaryValueColor = HudTheme.SecondaryValueColor;

            for (int index = 0; index < _rows.Count; index++)
            {
                HudLabelRow row = _rows[index];
                row.Label.color = labelColor;
                row.Value.color = row.IsSecondaryValue ? secondaryValueColor : primaryValueColor;
            }
        }

        private void SetFontSize(int fontSize)
        {
            _currentFontSize = fontSize;
            ResetClampedWidth();
            float rowHeight = fontSize * 1.35f;

            for (int index = 0; index < _rows.Count; index++)
            {
                HudLabelRow row = _rows[index];
                row.Label.fontSize = fontSize;
                row.Value.fontSize = fontSize;
                ApplyRowHeight(row.LeftLayoutElement, rowHeight);
                ApplyRowHeight(row.RightLayoutElement, rowHeight);
            }

            if (_floatingHeader)
            {
                _floatingHeader.fontSize = fontSize + 2;
            }
        }

        private void SetPosition(Vector2 position)
        {
            if (_panelRoot)
            {
                _rootRect.anchoredPosition = position;
            }
        }

        private void OnOffsetSettingChanged(object sender, EventArgs eventArgs) =>
            SetPosition(_layoutSettings.Offset);

        private void OnFontSizeSettingChanged(object sender, EventArgs eventArgs)
        {
            if (!_panelRoot)
            {
                return;
            }

            SetFontSize(_layoutSettings.FontSize.Value);
            if (_onFontSizeChanged != null)
            {
                _onFontSizeChanged();
            }
        }

        private void OnBackgroundSettingChanged(object sender, EventArgs eventArgs) =>
            SetBackground(ModConfig.ShowInfoPanelBackground.Value, _layoutSettings.BackgroundColor.Value);

        private void OnVisibilitySettingChanged(object sender, EventArgs eventArgs)
        {
            if (!_panelRoot)
            {
                return;
            }

            if (!ModConfig.EnableMod.Value || !_layoutSettings.Show.Value)
            {
                if (_onDeactivated != null)
                {
                    _onDeactivated();
                }
                else
                {
                    SetActive(false);
                }
            }
        }
    }
}
