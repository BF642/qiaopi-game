using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    /// <summary>Exports exact spoken text without playing or recording any system audio.</summary>
    public static class VoiceExport
    {
        [Serializable] public sealed class Entry { public string speakerId, text, clipKey; }
        [Serializable] public sealed class Manifest
        {
            public int version = 2;
            public string attribution = "配音需另行录制或取得分发授权";
            public string keyAlgorithm = "text_ + lowercase SHA256(exact UTF8 text).Substring(0,20)";
            public int randomCompletePaths, seenNodes, seenEndings, dialogueClips, textClips;
            public List<Entry> entries = new List<Entry>();
        }
        static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        static readonly HashSet<string> nodes = new HashSet<string>();
        static readonly HashSet<string> endings = new HashSet<string>();
        static readonly string[] endingRoutes = {
            "ledger guarantor mother share dock tally rest honest details copies desk reconcile settle return home",
            "ledger borrow mother berth dock extra overtime reassure medicine copies family hide hard_work return home",
            "ledger guarantor sister share shop learn overtime honest details copies desk reconcile hard_work return school",
            "ledger guarantor mother share shop learn rest honest details copies desk reconcile settle stay steady",
            "ledger guarantor mother share courier verify rest honest details copies desk reconcile settle stay steady",
            "ledger borrow mother share dock extra rest honest details copies desk reconcile settle farther migrate",
            "ledger borrow mother share dock extra rest reassure details copies family hide hard_work return confess",
            "ledger guarantor mother share dock tally rest honest details copies desk reconcile settle stay steady"
        };
        public static string TextKey(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
                var hex = new StringBuilder(64);
                foreach (byte b in hash) hex.Append(b.ToString("x2"));
                return "text_" + hex.ToString(0, 20);
            }
        }
        [MenuItem("侨批/配音/导出完整离线配音清单")]
        public static void Export()
        {
            entries.Clear(); nodes.Clear(); endings.Clear();
            foreach (var line in DialogueScript.AllLines()) Add(line.speakerId, line.text, line.clipKey);
            int dialogueCount = entries.Count;
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
            foreach (string route in endingRoutes)
            {
                var state = StoryEngine.NewGame(19060923); state.journey.destination = destination;
                foreach (string id in ("meal neighbors " + route).Split(' '))
                {
                    CollectSceneAndAlternatives(state);
                    if (!StoryEngine.Choose(state, id)) throw new InvalidOperationException("Voice coverage route failed: " + state.nodeId + "/" + id);
                    AddText(state.lastOutcome, "narrator");
                    if (!StoryEngine.Continue(state)) throw new InvalidOperationException("Voice coverage route could not continue");
                }
                CollectSceneAndAlternatives(state);
            }
            var random = new System.Random(19060923);
            int completed = 0;
            for (int run = 0; run < 4000; run++)
            {
                var state = StoryEngine.NewGame(run + 19060923);
                for (int step = 0; step < 18; step++)
                {
                    var scene = CollectSceneAndAlternatives(state);
                    if (scene.isEnding) { completed++; break; }
                    var enabled = scene.choices.Where(c => c.enabled).ToArray();
                    if (enabled.Length == 0) throw new InvalidOperationException("No available choice: " + scene.id);
                    if (!StoryEngine.Choose(state, enabled[random.Next(enabled.Length)].id)) throw new InvalidOperationException("Choice failed during voice export");
                    AddText(state.lastOutcome, "narrator");
                    if (!StoryEngine.Continue(state)) throw new InvalidOperationException("Story did not continue during voice export");
                }
            }
            // Player side jobs and rest can change money outside the narrative path. Cover every
            // possible first-remittance amount, including zero, instead of trusting random sampling.
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
            {
                for (int amount = 0; amount <= 8; amount++)
                {
                    var state = StoryEngine.NewGame(19060923); state.journey.destination = destination; state.nodeId = "first_letter"; state.money = amount;
                    CollectSceneAndAlternatives(state);
                }
                var reserved = StoryEngine.NewGame(19060923); reserved.journey.destination = destination; reserved.nodeId = "first_letter"; reserved.flags.Add("remit_reserved");
                CollectSceneAndAlternatives(reserved);
            }
            if (completed != 4000 || nodes.Count != 30 || endings.Count != 8)
                throw new InvalidOperationException("Incomplete voice coverage: paths=" + completed + " nodes=" + nodes.Count + " endings=" + endings.Count);
            var manifest = new Manifest { randomCompletePaths = completed, seenNodes = nodes.Count, seenEndings = endings.Count,
                dialogueClips = dialogueCount, textClips = entries.Count - dialogueCount, entries = entries.Values.OrderBy(e => e.clipKey, StringComparer.Ordinal).ToList() };
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../tools/voice"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "manifest.json");
            File.WriteAllText(path, JsonUtility.ToJson(manifest, true), new UTF8Encoding(false));
            Debug.Log("QIAOPI_VOICE_MANIFEST=" + path + " clips=" + entries.Count + " dialogue=" + dialogueCount + " text=" + manifest.textClips + " paths=" + completed);
        }
        static SceneData CollectSceneAndAlternatives(GameState state)
        {
            SceneData scene = StoryEngine.GetScene(state); nodes.Add(scene.id);
            if (scene.isEnding) endings.Add(scene.endingId);
            foreach (var choice in scene.choices)
            {
                AddText(choice.title, "wensheng");
                if (!choice.enabled) continue;
                var alternate = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
                if (StoryEngine.Choose(alternate, choice.id)) AddText(alternate.lastOutcome, "narrator");
            }
            return scene;
        }
        static void AddText(string text, string speaker) { Add(speaker, text, TextKey(text)); }
        static void Add(string speaker, string text, string key)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || key.Contains("/") || key.Contains("\\"))
                throw new InvalidOperationException("Invalid voice clip key: " + key);
            if (entries.TryGetValue(key, out var previous))
            {
                if (previous.text != text || previous.speakerId != speaker) throw new InvalidOperationException("Voice key collision: " + key);
                return;
            }
            entries.Add(key, new Entry { speakerId = string.IsNullOrEmpty(speaker) ? "narrator" : speaker, text = text, clipKey = key });
        }
    }
}
