using System;

namespace MasterBuilderHeeler.Core.UI
{
    // the status or value part of a hud row. it stays on the row when the player hides the key
    // the width samples are every text it can show, or just the widest one, so the panel can size itself up front
    public sealed class HudIndicator
    {
        private readonly Func<string> _getText;

        public HudIndicator(Func<string> getText, params Func<string>[] widthSamples)
        {
            _getText = getText;
            WidthSamples = widthSamples;
        }

        public string Text => _getText();

        public Func<string>[] WidthSamples { get; }

        public static HudIndicator Choice(Func<bool> isFirst, Func<string> first, Func<string> second) =>
            new HudIndicator(() => isFirst() ? first() : second(), first, second);

        public static HudIndicator OnOff(Func<bool> isOn) =>
            Choice(isOn, () => HudTheme.StatusOn, () => HudTheme.StatusOff);
    }
}
