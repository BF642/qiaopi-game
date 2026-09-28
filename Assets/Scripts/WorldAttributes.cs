using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        void AttributeBars(Rect area, bool mobile)
        {
            float gap = mobile ? 20f : 18f;
            float width = (area.width - gap * 2) / 3;
            AttributeBar(new Rect(area.x, area.y, width, area.height), "身体", state.health,
                state.health <= 25 ? letterSeal : letterInk, mobile);
            AttributeBar(new Rect(area.x + width + gap, area.y, width, area.height), "亲情", state.family,
                letterSeal, mobile);
            AttributeBar(new Rect(area.x + (width + gap) * 2, area.y, width, area.height), "信用", state.trust,
                C("8B7545"), mobile);
        }

        void AttributeBar(Rect area, string title, int value, Color fill, bool mobile)
        {
            int current = Mathf.Clamp(value, 0, 100);
            int size = mobile ? 24 : 18;
            float textHeight = mobile ? 30 : 25;
            Label(new Rect(area.x, area.y, area.width * .36f, textHeight), title, size, ink);
            Label(new Rect(area.x + area.width * .36f, area.y, area.width * .64f, textHeight),
                current + " / 100", mobile ? 23 : 17, ink, false, TextAnchor.UpperRight);

            // Only bounded attributes use a percentage. Money remains an amount.
            Rect track = new Rect(area.x, area.yMax - (mobile ? 13 : 12), area.width, mobile ? 13 : 12);
            Box(track, C("D8CCB4"));
            Rect inside = new Rect(track.x + 1, track.y + 1, track.width - 2, track.height - 2);
            if (current > 0) {
                Rect filled = new Rect(inside.x, inside.y, inside.width * current / 100f, inside.height);
                Box(filled, fill);
                Box(new Rect(filled.x, filled.y, filled.width, 2), new Color(1, .96f, .83f, .16f));
            }
            for (int i = 1; i < 5; i++)
                Box(new Rect(inside.x + inside.width * i / 5f, inside.y, 1, inside.height),
                    new Color(.97f, .92f, .82f, .65f));
            Stroke(track, new Color(.31f, .33f, .25f, .3f));
        }
    }
}
