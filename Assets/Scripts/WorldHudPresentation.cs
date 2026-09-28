using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        Texture2D hudWash;
        readonly Color hudText = C("F5EFD9"), hudMuted = C("D2D6CB"), hudGold = C("E4C687");

        void HudSurface(Rect r, float opacity = .48f)
        {
            if (!hudWash) {
                const int size = 64;
                hudWash = new Texture2D(size, size, TextureFormat.RGBA32, false);
                hudWash.name = "轻墨晕底";
                hudWash.wrapMode = TextureWrapMode.Clamp;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                    float edgeX = Mathf.SmoothStep(0, 1, Mathf.Min(x, size - 1 - x) / 8f);
                    float edgeY = Mathf.SmoothStep(0, 1, Mathf.Min(y, size - 1 - y) / 13f);
                    pixels[y * size + x] = new Color(1, 1, 1, edgeX * edgeY);
                }
                hudWash.SetPixels(pixels); hudWash.Apply(false, true);
            }
            var old = GUI.color;
            GUI.color = new Color(.09f, .16f, .14f, opacity);
            GUI.DrawTexture(r, hudWash);
            GUI.color = old;
        }

        void HudText(Rect r, string text, int size, Color color, bool title = false,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, size,
                new Color(.025f, .045f, .035f, .83f), title, align);
            Label(r, text, size, color, title, align);
        }

        bool HudNav(Rect r, string text, string icon, bool selected, int size)
        {
            bool hover = GUI.enabled && r.Contains(Event.current.mousePosition);
            if (hover || selected) HudSurface(r, .38f);
            Color color = selected ? hudGold : hudText;
            float side = MobileControls ? 52 : 38;
            bool hasText = !string.IsNullOrEmpty(text);
            float iconX = hasText ? r.x + 12 : r.center.x - side / 2;
            LetterIcon(new Rect(iconX, r.center.y - side / 2, side, side), icon, color);
            if (hasText)
                HudText(new Rect(r.x + side + 24, r.y, r.width - side - 36, r.height - 3),
                    text, size, color, false, TextAnchor.MiddleLeft);
            if (selected) Box(new Rect(r.x + 14, r.yMax - 4, r.width - 28, 2), hudGold);
            return GUI.Button(r, GUIContent.none, blank);
        }

        bool HudAction(Rect r, string title, bool primary, int size)
        {
            bool hover = GUI.enabled && r.Contains(Event.current.mousePosition);
            HudSurface(r, hover ? .55f : .28f);
            Color color = primary ? hudGold : hudText;
            float side = MobileControls ? 42 : 32;
            DrawIllustratedIcon(new Rect(r.x + 12, r.center.y - side / 2, side, side),
                primary ? "livelihood" : "write");
            Rect textArea = new Rect(r.x + side + 22, r.y, r.width - side - 36, r.height - 3);
            bool wrap = style.wordWrap;
            style.wordWrap = false;
            style.fontSize = size;
            float measured = style.CalcSize(new GUIContent(title)).x;
            int fitted = Mathf.Min(size, Mathf.FloorToInt(size * textArea.width / Mathf.Max(1, measured)));
            HudText(textArea, title, fitted, color, false, TextAnchor.MiddleLeft);
            style.wordWrap = wrap;
            Box(new Rect(textArea.x, r.yMax - 6, textArea.width, 1), new Color(color.r, color.g, color.b, .4f));
            return GUI.Button(r, GUIContent.none, blank);
        }

        string ShortObjective()
        {
            if(writingRequested)return WritingObjective;
            if (sideCarrying) return "送交帮工货物";
            if (LifeJourney.IsActive(state) && string.IsNullOrEmpty(state.journey.pendingJob)) return "安排下一轮生活";
            if (state.nodeId == "passage" && loadedWorld == "harbor") return "到栈桥登船";
            if (Complete) return LifeJourney.IsActive(state) ? "向" + mission.npcName + "领工钱" : "与" + mission.npcName.Replace("与", "、") + "交谈";
            if (mission.activity == "inspect") return "核对" + mission.itemName;
            if (carrying) return "送交" + mission.itemName;
            if (mission.activity == "collect" || mission.activity == "deliver") return "领取" + mission.itemName;
            return "与" + mission.npcName.Replace("与", "、") + "交谈";
        }

        string ShortProgress()
        {
            if (sideCarrying || writingRequested) return "";
            return mission.activity == "inspect" || mission.activity == "deliver"
                ? Mathf.Min(progress, mission.required) + " / " + mission.required : "";
        }

        string ContextActionName(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return "互动";
            if (prompt.Contains("落座")) return "落座";
            if (prompt.Contains("安排生活")) return "安排";
            if (prompt.Contains("领取工钱")) return "领工钱";
            if (prompt.Contains("登船")) return "登船";
            if (prompt.Contains("核对")) return "核对";
            if (prompt.Contains("拿起")) return "拾取";
            if (prompt.Contains("交付") || prompt.Contains("放下")) return "交付";
            if (prompt.Contains("歇脚")) return "歇脚";
            if (prompt.Contains("帮工")) return "帮工";
            if (prompt.Contains("见闻")) return "阅读";
            return "交谈";
        }
    }
}
