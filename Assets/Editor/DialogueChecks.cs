using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class DialogueChecks
    {
        private static readonly string[] Nodes = {
            "peace", "pressure", "home", "funding", "farewell", "passage", "shore", "dock", "shop", "courier",
            "first_pay", "first_letter", "reply", "records", "missing", "trace_desk", "trace_harbor",
            "trace_family", "storms", "horizon", "return_choice", "stay_choice", "end_home_lamp",
            "end_home_scar", "end_school_window", "end_shop_bridge", "end_trusted_route",
            "end_new_harbor", "end_unsent_letter", "end_silver_thread"
        };
        private static readonly string[] VariantFlags = {
            "quilt", "reassure", "sister_school", "twin_promise", "ask_school", "school_supported",
            "ledger", "copies", "verified", "shop_record", "ship_friend", "crew_help", "marked_cargo",
            "resolved", "debt", "debt_paid", "route_shop", "route_courier", "concealed"
        };

        public static void Run()
        {
            var people = new HashSet<string>();
            foreach (CharacterInfo person in CharacterRoster.All())
            {
                Require(people.Add(person.id), "Duplicate character " + person.id);
                Require(!string.IsNullOrWhiteSpace(person.name) && !string.IsNullOrWhiteSpace(person.role)
                    && !string.IsNullOrWhiteSpace(person.model), "Incomplete character " + person.id);
            }
            Require(people.Count == 12 && people.Contains("narrator"), "Expected eleven characters and a narrator");
            var catalog = new Dictionary<string, DialogueLine>();
            foreach (DialogueLine line in DialogueScript.AllLines())
            {
                Require(line != null && !string.IsNullOrWhiteSpace(line.text) && !string.IsNullOrWhiteSpace(line.clipKey), "Incomplete line");
                Require(people.Contains(line.speakerId), "Unknown speaker " + line.speakerId);
                Require(!catalog.ContainsKey(line.clipKey), "Repeated audio key " + line.clipKey);
                foreach (char c in line.text) Require(c > 127, "Use Chinese text and punctuation in spoken line " + line.clipKey);
                catalog.Add(line.clipKey, line);
            }
            var contexts = new List<GameState>();
            foreach (string destination in new[] { "singapore", "penang", "rangoon" })
            {
                var ordinary = StoryEngine.NewGame(1); ordinary.journey.destination = destination; contexts.Add(ordinary);
                var tired = StoryEngine.NewGame(1); tired.journey.destination = destination; tired.health = 20; contexts.Add(tired);
                var rich = StoryEngine.NewGame(1); rich.journey.destination = destination; rich.flags.AddRange(VariantFlags); contexts.Add(rich);
                foreach (string flag in VariantFlags)
                {
                    var one = StoryEngine.NewGame(1); one.journey.destination = destination; one.flags.Add(flag); contexts.Add(one);
                }
            }
            var reached = new HashSet<string>();
            int scenes = 0;
            foreach (string node in Nodes)
            {
                string worldSpeaker = CharacterRoster.ForNode(node);
                Require(people.Contains(worldSpeaker) && worldSpeaker != "narrator", "Invalid world speaker for " + node);
                foreach (GameState state in contexts)
                {
                    state.nodeId = node;
                    string flagsBefore = string.Join("|", state.flags.ToArray());
                    int healthBefore = state.health, moneyBefore = state.money, decisionsBefore = state.decisions;
                    string saveBefore = JsonUtility.ToJson(state);
                    var lines = DialogueScript.Get(state);
                    Require(lines.Count >= 2 && lines.Count <= 4, "Expected two to four lines at " + node);
                    int length = 0;
                    var speakers = new HashSet<string>();
                    foreach (DialogueLine line in lines)
                    {
                        DialogueLine canonical;
                        Require(catalog.TryGetValue(line.clipKey, out canonical), "Missing export line " + line.clipKey);
                        Require(canonical.text == line.text && canonical.speakerId == line.speakerId, "Audio key changed meaning: " + line.clipKey);
                        Require(LifeJourney.DestinationId(state) == "singapore" || !line.text.Contains("新加坡"), "Dialogue mentions the wrong destination at " + node);
                        length += line.text.Length; speakers.Add(line.speakerId); reached.Add(line.clipKey);
                    }
                    Require(length >= 80 && length <= 180, "Dialogue length outside brief at " + node + ": " + length);
                    Require(speakers.Count >= 2, "Node should not be one person speaking every line: " + node);
                    Require(state.money == moneyBefore && state.health == healthBefore && state.decisions == decisionsBefore
                        && flagsBefore == string.Join("|", state.flags.ToArray()), "Dialogue changed gameplay state");
                    Require(saveBefore == JsonUtility.ToJson(state), "Dialogue changed saved journey or letter fields");
                    scenes++;
                }
            }
            Require(reached.Count == catalog.Count, "Some exported variants were never selected: " + reached.Count + "/" + catalog.Count);
            Require(CharacterRoster.ForNode("reply") == "clerk" && CharacterRoster.ForNode("trace_family") == "clerk"
                && CharacterRoster.ForNode("end_school_window") == "clerk", "Letter voices must not replace the world courier");
            Debug.Log("QIAOPI DIALOGUE CHECKS PASSED: 30 nodes, 3 destination variants, " + catalog.Count + " unique audio keys, 12 speaker identities, "
                + scenes + " scene variants, two to four lines per scene, no gameplay mutations, letter speakers separated from world NPCs.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QIAOPI DIALOGUE CHECK FAILED: " + message);
        }
    }
}
