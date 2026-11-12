using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace MasterBuilderHeeler.Core.UI.Radial
{
    public sealed class BuildRadialPage : IRadialConfig
    {
        private readonly BuildRadialEntry[] _entries;

        public BuildRadialPage(string name, BuildRadialEntry[] entries)
        {
            LocalizedName = name;
            _entries = entries;
        }

        public string LocalizedName { get; }

        public Sprite Sprite => null!;

        public bool HasAvailableEntries
        {
            get
            {
                for (int index = 0; index < _entries.Length; index++)
                {
                    if (_entries[index].IsAvailable)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void InitRadialConfig(RadialBase radial)
        {
            List<RadialMenuElement> elements = new List<RadialMenuElement>(_entries.Length + 1);
            for (int index = 0; index < _entries.Length; index++)
            {
                BuildRadialEntry entry = _entries[index];
                if (entry.IsAvailable)
                {
                    elements.Add(entry.CreateElement(radial, this));
                }
                else if (radial.IsTopLevel)
                {
                    // top ring keeps an empty slot like the game does for a missing hammer. the ring refreshes in place
                    // after a toggle, so without it the entries would shift under the cursor
                    EmptyElement emptyElement = Object.Instantiate(RadialData.SO.EmptyElement);
                    emptyElement.Init();
                    elements.Add(emptyElement);
                }
            }

            radial.ConstructRadial(elements);
        }
    }
}
