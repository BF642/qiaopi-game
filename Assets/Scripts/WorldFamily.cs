using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // Family voices in overseas letters belong to portraits, never local actors.
    // This list only augments the existing mission NPC; it does not change a
    // target, a route, a saved position, or the conditions for advancing a node.
    public static class FamilyPresence
    {
        public static List<string> Companions(string nodeId, string world, string primaryId)
        {
            var people = new List<string>();
            if (world != "quanzhou") return people;
            switch (nodeId)
            {
                case "peace": case "pressure": case "home": case "farewell":
                case "return_choice": case "end_home_lamp": case "end_home_scar":
                case "end_school_window": case "end_unsent_letter":
                    people.AddRange(new[] { "mother", "aman", "wife" });
                    break;
                case "funding":
                    people.AddRange(new[] { "mother", "wife" });
                    break;
            }
            people.Remove(primaryId);
            return people;
        }
    }

    public partial class WorldGame
    {
        readonly Dictionary<string, Transform> familyById = new Dictionary<string, Transform>();
        Transform FamilyActor(string id)
        { return familyById.TryGetValue(id, out var actor) && actor ? actor : null; }

        readonly List<Transform> familyActors = new List<Transform>();

        float FamilyConversationDistance()
        {
            float nearest = Dist(mission.npcPosition);
            foreach (var actor in familyActors)
                if (actor) nearest = Mathf.Min(nearest, Dist(actor.position));
            return nearest;
        }

        void SpawnFamilyCompanions(string npcId)
        {
            familyActors.Clear();
            familyById.Clear();
            if (npc) familyById[npcId] = npc.transform;
            var family = FamilyPresence.Companions(state.nodeId, mission.world, npcId);
            for (int i = 0; i < family.Count; i++)
            {
                var person = CharacterRoster.Get(family[i]);
                Vector3 offset = npcId == "mother"
                    ? (person.id == "aman" ? new Vector3(1.4f, 0, .2f) : new Vector3(-1.15f, 0, .2f))
                    : new Vector3(i % 2 == 0 ? -1.25f : 1.25f, 0, .25f);
                var actor = ModelLibrary.Spawn(person.model, actors.transform, WorldFactory.CharacterGroundPosition(mission.npcPosition + offset));
                if (!actor) continue; // The build-time asset check reports a missing family model.
                actor.name = person.name;
                actor.transform.rotation = Quaternion.Euler(0, 180, 0);
                actor.AddComponent<CharacterIdle>();
                familyActors.Add(actor.transform);
                familyById[person.id] = actor.transform;
            }
            if (npcId == "mother" && family.Contains("wife"))
            {
                mission.npcName = family.Contains("aman") ? "母亲、妻子与阿满" : "母亲与妻子";
                npc.name = mission.npcName;
            }
        }
    }
}
