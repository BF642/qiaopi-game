using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        readonly Dictionary<string, Texture2D> illustratedIcons = new Dictionary<string, Texture2D>();

        void DrawIllustratedIcon(Rect bounds, string key)
        {
            if (!illustratedIcons.TryGetValue(key, out var texture)) {
                texture = Resources.Load<Texture2D>("UI/Icons/" + key);
                illustratedIcons.Add(key, texture);
                if (!texture) Debug.LogError("Missing illustrated UI icon: " + key);
            }
            if (!texture) return;
            // These are coloured artworks; tinting them with the text colour loses
            // the paper, jade and vermilion contrasts on both HUD and paper panels.
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, previous.a);
            GUI.DrawTexture(bounds, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }
    }
}
