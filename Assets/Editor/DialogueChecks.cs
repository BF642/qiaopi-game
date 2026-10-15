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
            Require(people.Count == 13 && people.Contains("narrator") && people.Contains("wife"), "Expected twelve characters and a narrator");
            Require(CharacterRoster.Get("wife").model == "Wife" && CharacterRoster.Get("wife").name == "妻子"
                && CharacterRoster.Get("aman").role == "文生的妹妹", "Wife and younger sister must remain separate identities");
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
            // Exercise the actual persisted prologue actions; fabricated flags must
            // not produce family recollections or make new voiced variants unreachable.
            foreach (string purchase in new[] { "rice", "tea" })
                foreach (string word in new[] { "home", "return" }) {
                    var home = StoryEngine.NewGame(1);
                    foreach (string action in new[] { "walk_market", purchase, "put_basket", "lift_grain", "store_grain", word, "share_meal" })
                        Require(HomeChapter.TryAct(home, action), "Complete home dialogue fixture " + action);
                    Require(HomeChapter.HasMemory(home, "meal"), "Home fixture has actual family memory");
                    contexts.Add(home);
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
                    bool familyLetter = node == "reply" || node == "trace_family";
                    Require(lines.Count >= 2 && lines.Count <= (familyLetter ? 5 : 4), "Dialogue has too many lines at " + node);
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
                    Require(length >= 80 && length <= (familyLetter ? 200 : 180), "Dialogue length outside brief at " + node + ": " + length);
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
            VerifyFamilyPresence();
            VerifyDepartureMemory();
            Debug.Log("QIAOPI DIALOGUE CHECKS PASSED: 30 nodes, 3 destination variants, " + catalog.Count + " unique audio keys, 13 speaker identities, "
                + scenes + " scene variants, brief family conversations, remembered farewell across actual departure choices, no gameplay mutations, letter speakers separated from world NPCs.");
        }

        static void VerifyDepartureMemory()
        {
            foreach (string purchase in new[] { "rice", "tea" })
                foreach (string word in new[] { "home", "return" })
                    foreach (string destination in new[] { "singapore", "penang", "rangoon" }) {
                        var state = StoryEngine.NewGame(2600);
                        state.journey.destination = destination;
                        foreach (string action in new[] { "walk_market", purchase, "put_basket", "lift_grain", "store_grain", word, "share_meal" })
                            Require(HomeChapter.TryAct(state, action), "Real home fixture before departure " + action);
                        foreach (string choice in new[] { "meal", "neighbors", "ledger", "guarantor" })
                            Require(StoryEngine.Choose(state, choice) && StoryEngine.Continue(state), "Actual route to farewell " + choice);
                        Require(state.nodeId == "farewell" && state.decisions == 4, "Home memories add no forced story decisions");
                        var farewell = DialogueScript.Get(state);
                        string wife = farewell.Find(line => line.speakerId == "wife").text;
                        string sister = farewell.Find(line => line.speakerId == "aman").text;
                        Require(wife.Contains(purchase == "tea" ? "一起买的茶" : "米油")
                            && !wife.Contains(purchase == "tea" ? "米油" : "一起买的茶"), "Farewell remembers the delivered purchase exclusively");
                        Require(sister.Contains(word == "return" ? "「归」" : "「家」")
                            && !sister.Contains(word == "return" ? "「家」" : "「归」"), "Farewell remembers the chosen lesson exclusively");
                        var choices = StoryEngine.GetScene(state).choices;
                        Require(choices.Count == 3 && choices[0].id == "mother" && choices[1].id == "sister" && choices[2].id == "both", "Farewell keeps the original promise decisions");
                        string oldSave = JsonUtility.ToJson(state);
                        var restored = JsonUtility.FromJson<GameState>(oldSave);
                        Require(StoryEngine.Validate(restored) && DialogueKeys(restored) == DialogueKeys(state), "Departure memory survives save restore");
                        restored.homeChapterVersion = 0;
                        Require(DialogueKeys(restored) == "farewell_mother_01|farewell_aman_01|farewell_wife_01|farewell_wensheng_01", "Legacy migration marker overrides a materialized home object");
                        Require(JsonUtility.ToJson(state) == oldSave, "Memory dialogue is read-only");
                        foreach (string choice in new[] { "mother", "share" })
                            Require(StoryEngine.Choose(state, choice) && StoryEngine.Continue(state), "Actual sea passage " + choice);
                        Require(state.nodeId == "shore" && state.decisions == 6, "Memory adds no arrival stage");
                        var arrival = DialogueScript.Get(state);
                        Require(arrival.Count == 3 && arrival[0].speakerId == "guide"
                            && arrival[1].speakerId == "wensheng" && arrival[2].speakerId == "guide", "Arrival recollection stays with present people, not a teleported family voice");
                        Require(arrival[1].text.Contains(word == "return" ? "「归」" : "「家」"), "Arrival carries the selected letter forward");
                        string guideKey = destination == "penang" ? "shore_guide_penang" : destination == "rangoon" ? "shore_guide_rangoon" : "shore_guide_01";
                        Require(arrival[0].clipKey == guideKey, "Memory does not erase destination-specific arrival");
                        state.homeChapterVersion = 0;
                        Require(DialogueKeys(state) == guideKey + "|shore_wensheng_01|shore_guide_02", "Old overseas saves retain their original dialogue");
                    }
            // A purchased basket still in the player's hands is not an afternoon
            // spent together, even if a malformed or external caller asks for farewell.
            var incomplete = StoryEngine.NewGame(2600);
            Require(HomeChapter.TryAct(incomplete, "walk_market") && HomeChapter.TryAct(incomplete, "tea"), "Undelivered basket fixture");
            incomplete.nodeId = "farewell";
            Require(!DialogueKeys(incomplete).Contains("_260_"), "No completed-afternoon memory from an unfinished action");
        }

        static string DialogueKeys(GameState state)
        {
            var keys = new List<string>();
            foreach (var line in DialogueScript.Get(state)) keys.Add(line.clipKey);
            return string.Join("|", keys.ToArray());
        }

        static void VerifyFamilyPresence()
        {
            foreach (string node in new[] { "peace", "pressure", "home", "funding", "farewell", "return_choice", "end_home_lamp", "end_home_scar", "end_school_window", "end_unsent_letter" })
            {
                string primary = node == "end_school_window" ? "aman" : CharacterRoster.ForNode(node);
                var companions = FamilyPresence.Companions(node, "quanzhou", primary);
                Require(companions.Contains("wife") && !companions.Contains(primary), "Family missing or duplicates main speaker at " + node);
                if (node != "funding" && primary != "aman") Require(companions.Contains("aman"), "Wife replaced the younger sister at " + node);
                var state = StoryEngine.NewGame(1); state.nodeId = node;
                Require(DialogueScript.Get(state).Exists(line => line.speakerId == "wife"), "Wife never speaks at " + node);
            }
            foreach (string node in Nodes)
                foreach (string world in new[] { "harbor", "ship", "port", "market", "quarters", "postoffice" })
                    Require(FamilyPresence.Companions(node, world, CharacterRoster.ForNode(node)).Count == 0, "Family teleported overseas at " + node + "/" + world);
            foreach (string node in new[] { "reply", "trace_family" })
            {
                var state = StoryEngine.NewGame(1); state.nodeId = node;
                Require(DialogueScript.Get(state).Exists(line => line.speakerId == "wife"), "Missing wife's letter voice at " + node);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QIAOPI DIALOGUE CHECK FAILED: " + message);
        }
    }
}
