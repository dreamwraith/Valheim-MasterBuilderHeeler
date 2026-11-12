using System;
using MasterBuilderHeeler.Core.Input;
using TMPro;
using UnityEngine;
using Valheim.UI;

namespace MasterBuilderHeeler.Core.UI.Radial
{
    public sealed class BuildRadialEntry
    {
        private const float LabelSize = 160f;

        private static readonly Func<bool> CloseRadial = () => true;
        private static readonly Func<bool> KeepRadialOpen = () => false;

        private readonly string _name;
        private readonly BuildAction? _action;
        private readonly Func<string>? _getStatus;
        private readonly Func<bool> _closeOnInteract;
        private readonly BuildRadialPage? _page;

        private BuildRadialEntry(string name, BuildAction? action, Func<string>? getStatus, Func<bool> closeOnInteract, BuildRadialPage? page)
        {
            _name = name;
            _action = action;
            _getStatus = getStatus;
            _closeOnInteract = closeOnInteract;
            _page = page;
        }

        public bool IsAvailable =>
            _page != null ? _page.HasAvailableEntries : _action!.IsAvailable();

        // stays open and refreshes so the new state shows up on the entry
        public static BuildRadialEntry Toggle(string name, BuildAction action, Func<string> getStatus) =>
            new BuildRadialEntry(name, action, getStatus, KeepRadialOpen, page: null);

        public static BuildRadialEntry OneShot(string name, BuildAction action, Func<string>? getStatus = null) =>
            new BuildRadialEntry(name, action, getStatus, CloseRadial, page: null);

        public static BuildRadialEntry Group(BuildRadialPage page) =>
            new BuildRadialEntry(page.LocalizedName, action: null, getStatus: null, KeepRadialOpen, page);

        // everything is cloned from the GroupElement prefab on purpose. the radial remembers any other element type as
        // LastUsed and then shows it in the last slot of the base game main ring
        public RadialMenuElement CreateElement(RadialBase radial, BuildRadialPage parentPage)
        {
            GroupElement element = UnityEngine.Object.Instantiate(RadialData.SO.GroupElement);
            if (_page != null)
            {
                element.Init(_page, parentPage, radial);
                AddLabel(element, radial, _name);
                return element;
            }

            BuildAction action = _action!;
            string? status = _getStatus != null ? _getStatus() : null;
            element.Name = _name;
            element.SubTitle = status;
            element.CloseOnInteract = _closeOnInteract;
            element.Interact = () =>
            {
                BuildInputRouter.TryPress(action, radial.m_localPlayerRef);
                return true;
            };
            element.m_icon.gameObject.SetActive(false);
            AddLabel(element, radial, status != null ? $"{_name}\n<size=80%>{status}</size>" : _name);
            return element;
        }

        // no icon art for these so the name goes where the prefab keeps its icon. thats the center of the element root
        private static void AddLabel(RadialMenuElement element, RadialBase radial, string text)
        {
            // inactive while adding TextMeshProUGUI for the same reason as HudPanelView.CreateLabel
            GameObject labelObject = new GameObject("MBH_Label", typeof(RectTransform));
            labelObject.SetActive(false);
            labelObject.layer = element.gameObject.layer;

            RectTransform labelTransform = (RectTransform)labelObject.transform;
            labelTransform.SetParent(element.transform, false);
            labelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            labelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelTransform.pivot = new Vector2(0.5f, 0.5f);
            labelTransform.anchoredPosition = Vector2.zero;
            labelTransform.sizeDelta = new Vector2(LabelSize, LabelSize);

            TextMeshProUGUI titleText = radial.m_elementInfo.m_title;
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = titleText.font;
            label.fontSharedMaterial = titleText.fontSharedMaterial;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 26f;
            label.richText = true;
            label.raycastTarget = false;
            label.text = text;
            labelObject.AddComponent<BuildRadialLabelTint>().Initialize(element.m_icon, label);

            labelObject.SetActive(true);
        }
    }
}
