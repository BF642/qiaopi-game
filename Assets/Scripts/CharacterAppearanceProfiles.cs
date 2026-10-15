using System;
using UnityEngine;

namespace Qiaopi
{
    public readonly struct SupportingAppearanceProfile
    {
        public readonly string id, model, title;
        public readonly AmbientFaceProfile face;
        public SupportingAppearanceProfile(string id, string model, string title, AmbientFaceProfile face)
        { this.id = id; this.model = model; this.title = title; this.face = face; }
    }

    // A speaking role, a supporting person and a resident are separate identities.
    // Changing the main cast asset must not silently change the other two groups.
    public static class CharacterAppearanceProfiles
    {
        public static readonly SupportingAppearanceProfile[] Supporting = {
            P("home_vendor", "Uncle", "村口日用摊主", "宽颊、圆下颌、短鼻与宽嘴", .81f, .54f, -.68f, .77f, -.48f, -.54f, .78f, -.32f, 5),
            P("dock_ashun", "Clerk", "工友阿顺", "长方脸、平颧、宽下巴与较宽眼距", .36f, .86f, .66f, -.57f, .70f, -.48f, .61f, .66f, 2),
            P("rice_customer", "Guide", "买米的街坊", "窄长脸、圆颏、细鼻与较窄嘴", -.87f, -.63f, .78f, -.49f, .51f, .62f, -.71f, .48f, 6),
            P("tea_neighbour", "Guide", "茶棚街坊", "短方脸、饱满面颊、宽鼻与圆下巴", .68f, .82f, -.72f, .73f, -.58f, -.67f, .70f, -.53f, 3),
            P("recipient_afu_young", "Worker", "陈阿福（年轻收件人）", "宽额收颌、长鼻与薄颊", .63f, -.89f, .53f, -.78f, .59f, .73f, -.64f, -.62f, 1),
            P("recipient_afu_senior", "Uncle", "陈阿福（年长收件人）", "窄额方颌、长下庭、浅颧与宽嘴", -.76f, .83f, .81f, -.68f, .48f, .55f, .63f, .76f, 7)
        };

        static SupportingAppearanceProfile P(string id, string model, string title, string description,
            float width, float jaw, float length, float cheek, float eyes, float nose, float mouth, float chin, int dye)
            => new SupportingAppearanceProfile(id, model, title,
                new AmbientFaceProfile(id, description, width, jaw, length, cheek, eyes, nose, mouth, chin, dye));

        public static SupportingAppearanceProfile Get(string id)
        {
            foreach (var profile in Supporting) if (profile.id == id) return profile;
            throw new ArgumentException("Unknown supporting appearance identity: " + id, nameof(id));
        }

        public static string CourierRecipientIdentity(bool relocated, int addressIndex)
        {
            if (addressIndex < 0 || addressIndex > 1) throw new ArgumentOutOfRangeException(nameof(addressIndex));
            // A move swaps addresses, never the person's face or age.
            return (addressIndex == 0) != relocated ? "recipient_afu_young" : "recipient_afu_senior";
        }

        public static GameObject Spawn(string identityId, Transform parent, Vector3 position)
        {
            var profile = Get(identityId);
            var actor = SpawnIndependentBase(profile.model, parent, position);
            if (!actor) return null;
            actor.name = profile.title;
            AmbientAppearance.Apply(actor, identityId, profile.model, profile.face);
            return actor;
        }

        public static GameObject SpawnResident(AmbientSchedule schedule, Transform parent, Vector3 position)
        {
            if (schedule == null) throw new ArgumentNullException(nameof(schedule));
            var actor = SpawnIndependentBase(schedule.model, parent, position);
            if (!actor) return null;
            AmbientAppearance.Apply(actor, schedule);
            return actor;
        }

        public static GameObject SpawnIndependentBase(string model, Transform parent, Vector3 position)
        {
            // Meshy has a fused skinned surface and no separate eye material.
            // The old fitted-eye sculpt cannot be inferred from its texture.
            // Preserve this independent resource until a per-asset head mask,
            // rigid eye regions, bind-space landmarks and source fingerprint
            // have been reviewed. Never guess these from whole-body bounds.
            return ModelLibrary.Spawn(model, parent, position, preferNative: false);
        }
    }
}
