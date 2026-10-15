using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // An identity is a resident, never its position in the spawn list or the
    // story actor whose clothing/rig supplies the base asset. Values are fixed
    // art direction; adding/reordering a schedule cannot reshuffle other faces.
    public readonly struct AmbientFaceProfile
    {
        public readonly string id, description;
        public readonly float width, jaw, length, cheek, eyes, nose, mouth, chin;
        public readonly int dye;
        public AmbientFaceProfile(string id, string description, float width, float jaw, float length,
            float cheek, float eyes, float nose, float mouth, float chin, int dye)
        {
            this.id = id; this.description = description; this.width = width; this.jaw = jaw;
            this.length = length; this.cheek = cheek; this.eyes = eyes; this.nose = nose;
            this.mouth = mouth; this.chin = chin; this.dye = dye;
        }
    }

    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class AmbientAppearance : MonoBehaviour
    {
        public const int Revision = 2;
        [SerializeField] string residentId, baseModel, faceDescription, runtimeOwnerId;
        [SerializeField] int modifiedVertices;
        [SerializeField] float maximumDisplacement;
        [SerializeField] Vector3 eyeCenter;
        public string ResidentId { get => residentId; private set => residentId = value; }
        public string BaseModel { get => baseModel; private set => baseModel = value; }
        public string FaceDescription { get => faceDescription; private set => faceDescription = value; }
        public int ModifiedVertices { get => modifiedVertices; private set => modifiedVertices = value; }
        public float MaximumDisplacement { get => maximumDisplacement; private set => maximumDisplacement = value; }
        public Vector3 EyeCenter { get => eyeCenter; private set => eyeCenter = value; }
        public bool Ready {
            get {
                // Edit-mode Instantiate may defer ExecuteAlways callbacks until
                // its next update. The public ready contract must establish
                // ownership even when queried immediately by a review tool.
                EnsureMeshOwnership();
                if (owned.Count == 0 || owned.Count != filters.Count || owned.Count != originals.Count || ModifiedVertices <= 500) return false;
                for (int i = 0; i < owned.Count; i++) if (!owned[i] || !filters[i] || filters[i].sharedMesh != owned[i]) return false;
                return true;
            }
        }
        [SerializeField] List<Mesh> owned = new List<Mesh>();
        [SerializeField] List<MeshFilter> filters = new List<MeshFilter>();
        [SerializeField] List<Mesh> originals = new List<Mesh>();

        void Awake() { EnsureMeshOwnership(); }
        void OnEnable() { EnsureMeshOwnership(); }

        void EnsureMeshOwnership()
        {
            string instance = gameObject.GetEntityId().ToString();
            if (runtimeOwnerId == instance) return;
            runtimeOwnerId = instance;
            // A live review copy retains identity and topology but cannot own
            // its source actor's transient meshes. Unity remaps child filters,
            // not Mesh objects; give the copied resident its own geometry.
            for (int i = 0; i < owned.Count; i++) if (owned[i]) {
                Mesh source = owned[i], copy = Instantiate(source); copy.name = source.name;
                owned[i] = copy;
                if (i < filters.Count && filters[i] && filters[i].sharedMesh == source) filters[i].sharedMesh = copy;
            }
        }

        public static readonly AmbientFaceProfile[] Profiles = {
            P("qz_grain", "宽颊短下颌", .72f, -.42f, -.65f, .70f, -.35f, -.40f, .55f, -.20f, 0),
            P("qz_mail", "窄颌长脸", -.72f, -.68f, .75f, -.22f, .52f, .55f, -.55f, .58f, 1),
            P("qz_field", "方颌高颧", .25f, .84f, -.34f, -.68f, -.42f, -.58f, .65f, .72f, 2),
            P("qz_yard", "宽额窄颌", .55f, -.85f, .25f, -.35f, .65f, .48f, .18f, -.65f, 3),
            P("qz_water", "圆颊小下巴", .85f, -.25f, -.78f, .78f, -.55f, -.45f, -.65f, -.55f, 4),
            P("harbor_parcels", "窄长颌", -.58f, -.30f, .85f, -.50f, .65f, .68f, -.48f, .64f, 5),
            P("harbor_tickets", "方圆脸", .78f, .72f, -.38f, .58f, -.40f, -.50f, .62f, -.25f, 6),
            P("harbor_baggage", "修长方颌", -.70f, .58f, .72f, -.62f, -.32f, -.55f, -.48f, .68f, 7),
            P("harbor_tea", "窄颊短脸", -.62f, -.72f, -.55f, .52f, .56f, .65f, -.62f, -.48f, 0),
            P("ship_watch", "清瘦长脸", -.82f, -.66f, .82f, -.62f, .54f, .55f, -.62f, .58f, 1),
            P("ship_cargo", "方颌厚颊", .28f, .92f, -.25f, .82f, -.55f, -.65f, .82f, .58f, 2),
            P("ship_water", "圆额短脸", .65f, .42f, -.80f, .75f, .32f, -.45f, .65f, -.58f, 3),
            P("port_south_cargo", "宽额尖颌", .88f, -.82f, .32f, -.55f, .38f, .62f, -.72f, -.62f, 4),
            P("port_north_cargo", "宽颌凹颊", .72f, .78f, .12f, -.85f, -.56f, .68f, -.24f, .64f, 5),
            P("port_receipts", "短脸窄颌", -.58f, -.42f, -.82f, .72f, .64f, .52f, .42f, -.68f, 6),
            P("port_inspection", "圆颊收颌", .78f, -.74f, -.64f, .85f, -.50f, -.68f, .62f, -.58f, 7),
            P("port_tea", "长脸圆颊", .62f, -.45f, .76f, .62f, .35f, -.62f, -.48f, .78f, 0),
            P("port_meals", "宽颌长脸", .38f, .85f, .74f, -.55f, .65f, .62f, .54f, .68f, 1),
            P("port_ledger", "清瘦收颌", -.85f, -.68f, .58f, -.80f, .60f, .66f, -.56f, .72f, 2),
            P("market_rice", "圆额宽颌", .85f, .68f, -.68f, .86f, -.58f, .62f, .72f, -.38f, 3),
            P("market_cloth", "窄颊方下巴", -.82f, .62f, .42f, -.84f, -.28f, .36f, .68f, -.55f, 4),
            P("market_tea", "短阔方脸", .48f, .88f, -.85f, .65f, -.65f, -.62f, .72f, -.44f, 5),
            P("market_cart", "窄颊短下巴", -.64f, -.54f, -.40f, -.74f, .62f, -.58f, .52f, -.65f, 6),
            P("market_accounts", "长额方颌", .42f, .82f, .86f, -.70f, -.65f, .68f, -.32f, .65f, 7),
            P("market_orders", "高颧方下巴", -.42f, .74f, -.25f, -.88f, .65f, -.62f, .74f, -.62f, 0),
            P("market_produce", "窄长椭圆脸", -.86f, -.72f, .80f, -.62f, -.54f, .55f, -.58f, -.42f, 1),
            P("quarters_water", "短阔圆颌", .85f, .42f, -.82f, -.45f, .72f, -.55f, -.30f, .72f, 2),
            P("quarters_food", "长额圆颊", -.25f, -.78f, .88f, .82f, -.68f, .64f, -.62f, .72f, 3),
            P("quarters_laundry", "短额方颌", -.62f, .88f, -.62f, .55f, .62f, -.72f, .72f, -.58f, 4),
            P("quarters_mail", "宽额尖下巴", .88f, -.86f, .45f, .82f, .28f, -.68f, -.74f, -.55f, 5),
            P("quarters_firewood", "窄额长下巴", -.82f, .65f, .85f, .62f, .68f, .38f, .38f, -.68f, 6),
            P("post_letters", "窄额方下巴", -.78f, .70f, .15f, .68f, -.62f, .35f, .85f, -.68f, 7),
            P("post_paper", "瘦削长脸", -.88f, -.78f, .82f, .58f, -.72f, -.52f, .62f, -.68f, 0),
            P("post_records", "宽额长下巴", .72f, -.85f, .82f, -.58f, -.36f, .62f, -.68f, .72f, 1),
            P("post_bags", "宽额方下巴", .88f, .45f, .65f, .45f, .62f, -.64f, .78f, -.42f, 2),
            P("post_check", "窄颌圆颊", -.72f, -.82f, .25f, .88f, -.62f, .68f, -.55f, -.48f, 3),
            P("post_tea", "宽额窄颊", .80f, -.72f, .18f, -.82f, -.68f, -.45f, .62f, .72f, 4)
        };
        static readonly string[] Dyes = { "536C66", "907D60", "6F6560", "627782", "9A8970", "727855", "7D6254", "6A7770" };
        static AmbientFaceProfile P(string id, string description, float width, float jaw, float length,
            float cheek, float eyes, float nose, float mouth, float chin, int dye)
            => new AmbientFaceProfile(id, description, width, jaw, length, cheek, eyes, nose, mouth, chin, dye);

        public static bool TryProfile(string id, out AmbientFaceProfile profile)
        {
            foreach (var item in Profiles) if (item.id == id) { profile = item; return true; }
            profile = default; return false;
        }

        public static AmbientAppearance Apply(GameObject actor, AmbientSchedule plan)
        {
            if (!actor || plan == null) return null;
            if (!TryProfile(plan.id, out var profile)) throw new InvalidOperationException("AMBIENT_FACE_PROFILE_MISSING " + plan.id);
            return Apply(actor, plan.id, plan.model, profile);
        }

        public static AmbientAppearance Apply(GameObject actor, string identityId, string model, AmbientFaceProfile profile)
        {
            if (!actor) return null;
            if (string.IsNullOrEmpty(identityId) || profile.id != identityId)
                throw new ArgumentException("Appearance requires a matching, stable person identity.");
            var existing = actor.GetComponent<AmbientAppearance>();
            if (existing) {
                if (existing.ResidentId != identityId) throw new InvalidOperationException("A person's face cannot change identity in place.");
                existing.EnsureMeshOwnership();
                return existing;
            }
            // Native faces must first receive authored eye/skin masks in their
            // own bind space. Every identity factory selects the independent
            // legacy base meanwhile; never deform a fused body by guessing.
            if (actor.GetComponent<NativeCharacterRig>())
                throw new InvalidOperationException("IDENTITY_REQUIRES_REVIEWED_NATIVE_FACE_REGIONS " + identityId);
            var result = actor.AddComponent<AmbientAppearance>();
            result.ResidentId = identityId; result.BaseModel = model; result.FaceDescription = profile.description;
            result.CreateFace(profile);
            if (!result.Ready) throw new InvalidOperationException("AMBIENT_FACE_MESH_MISSING " + identityId);
            result.ApplyDye(profile);
            result.ApplyComplexionAndHair();
            return result;
        }

        void CreateFace(AmbientFaceProfile profile)
        {
            // Existing ambient assets use a static anatomical head above the
            // independent five-bone body. Operate before any pose driver binds
            // it; never rewrite a skinned body's hands, weights or bind matrices.
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true)) {
                var renderer = filter.GetComponent<MeshRenderer>(); var source = filter.sharedMesh;
                if (!renderer || !source || !source.isReadable || !filter.name.StartsWith("Head_and_tailored_body_Mesh", StringComparison.Ordinal)) continue;
                Matrix4x4 toMetric = MetricFromHead(transform, filter.transform);
                Matrix4x4 toMesh = toMetric.inverse;
                var vertices = source.vertices; var materialSlots = renderer.sharedMaterials;
                var eyes = new bool[vertices.Length]; Vector3 left = Vector3.zero, right = Vector3.zero; int leftCount = 0, rightCount = 0;
                for (int slot = 0; slot < Mathf.Min(source.subMeshCount, materialSlots.Length); slot++) {
                    var material = materialSlots[slot];
                    if (!material || !material.shader || material.shader.name != "Qiaopi/CharacterEye") continue;
                    foreach (int index in source.GetIndices(slot)) eyes[index] = true;
                }
                for (int i = 0; i < vertices.Length; i++) if (eyes[i]) {
                    var p = toMetric.MultiplyPoint3x4(vertices[i]);
                    if (p.x < 0) { left += p; leftCount++; } else { right += p; rightCount++; }
                }
                if (leftCount < 100 || rightCount < 100) throw new InvalidOperationException("AMBIENT_EYE_LANDMARKS_MISSING " + profile.id);
                left /= leftCount; right /= rightCount;
                Vector3 eyeCenter = (left + right) * .5f; EyeCenter = eyeCenter;
                Vector3 leftShift = Warp(left, eyeCenter, profile) - left, rightShift = Warp(right, eyeCenter, profile) - right;
                var mesh = Instantiate(source); mesh.name = source.name + " · resident " + profile.id;
                // The continuous field carries skin, sockets, brows, hair and
                // glasses together. Eye globes translate rigidly, retaining
                // their original roundness and iris UVs.
                for (int i = 0; i < vertices.Length; i++) {
                    Vector3 p = toMetric.MultiplyPoint3x4(vertices[i]);
                    Vector3 shift = p.x < 0 ? leftShift : rightShift;
                    Vector3 q = eyes[i] ? p + shift : Warp(p, eyeCenter, profile);
                    if (!eyes[i]) {
                        // Keep the actual globe/lid contact intact. Stretching
                        // a socket while retaining a round globe would expose
                        // dark gaps at the corners, especially on a narrow face.
                        Vector3 near = p - (p.x < 0 ? left : right);
                        float radius = Mathf.Max(Mathf.Abs(near.x) / .025f, Mathf.Abs(near.y) / .015f, Mathf.Abs(near.z) / .026f);
                        float fitted = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.9f, 1.8f, radius));
                        // The two eye anchors must never pull the same bridge
                        // vertices in opposite directions. Fade to the shared
                        // continuous field before reaching the facial midline.
                        fitted *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.008f, .021f, Mathf.Abs(p.x)));
                        q = Vector3.Lerp(q, p + shift, fitted);
                    }
                    float displacement = Vector3.Distance(p, q);
                    if (displacement > .00001f) ModifiedVertices++;
                    MaximumDisplacement = Mathf.Max(MaximumDisplacement, displacement);
                    vertices[i] = toMesh.MultiplyPoint3x4(q);
                }
                mesh.vertices = vertices; mesh.RecalculateNormals();
                if (mesh.uv.Length == mesh.vertexCount && mesh.tangents.Length > 0) mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                filters.Add(filter); originals.Add(source); owned.Add(mesh); filter.sharedMesh = mesh;
            }
        }

        static float Bell(float value, float center, float spread)
        { float t = (value - center) / spread; return Mathf.Exp(-t * t * 2f); }

        public static Matrix4x4 MetricFromHead(Transform actor, Transform part)
        {
            // Never cancel two large world matrices: street position/facing
            // must not influence authored face coordinates or their normals.
            Matrix4x4 local = Matrix4x4.identity;
            Transform cursor = part;
            while (cursor && cursor != actor) {
                local = Matrix4x4.TRS(cursor.localPosition, cursor.localRotation, cursor.localScale) * local;
                cursor = cursor.parent;
            }
            if (cursor != actor) throw new InvalidOperationException("Ambient face is not owned by its actor.");
            return Matrix4x4.Scale(Vector3.one * Mathf.Abs(actor.localScale.x)) * local;
        }

        static Vector3 Warp(Vector3 p, Vector3 eye, AmbientFaceProfile f)
        {
            // Dimensions are metres, relative to the actual fitted eyes. A
            // smooth neck fade leaves the collar attachment exactly unchanged.
            float neck = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(eye.y - .185f, eye.y - .108f, p.y));
            if (neck <= 0) return p;
            float eyeBand = Bell(p.y, eye.y, .036f);
            float jaw = Bell(p.y, eye.y - .102f, .058f);
            float cheek = Bell(p.y, eye.y - .042f, .047f);
            float front = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(eye.z - .065f, eye.z - .006f, p.z));
            float nose = Bell(p.x, 0, .035f) * Bell(p.y, eye.y - .034f, .052f) * front;
            float lips = Bell(p.y, eye.y - .077f, .028f) * Bell(p.x, 0, .052f) * front;
            float lowerChin = Bell(p.y, eye.y - .128f, .039f) * front;
            float x = p.x * (f.width * .092f + f.jaw * .17f * jaw + f.cheek * .062f * cheek);
            // Eye distance is one rigid offset on either side. The surrounding
            // socket and brow receive the same offset near the eye; it fades
            // well before the nose bridge and temples.
            float side = Mathf.Sign(p.x);
            float socket = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.006f, .021f, Mathf.Abs(p.x))) *
                (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.069f, .094f, Mathf.Abs(p.x))));
            x += side * f.eyes * .0046f * eyeBand * socket;
            x += p.x * f.nose * .13f * nose + p.x * f.mouth * .15f * lips;
            float y = (p.y - eye.y) * f.length * .075f;
            float z = f.cheek * .0038f * cheek * front + f.nose * .0058f * nose + f.chin * .0055f * lowerChin;
            return p + new Vector3(x, y, z) * neck;
        }

        void ApplyDye(AmbientFaceProfile profile)
        {
            ColorUtility.TryParseHtmlString("#" + Dyes[profile.dye % Dyes.Length], out var dye);
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++) if (materials[slot] && materials[slot].HasProperty("_CreamColor")) {
                    var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, slot);
                    block.SetColor("_CreamColor", dye); renderer.SetPropertyBlock(block, slot);
                }
            }
        }

        void ApplyComplexionAndHair()
        {
            // Modest pigment differences supplement the authored geometry;
            // they never stand in for a distinct face, alter UVs or materials,
            // or choose a person's appearance from its current street address.
            uint hash = 17;
            foreach (char letter in ResidentId) hash = unchecked(hash * 31 + letter);
            int tone = (int)(hash % 5);
            Color[] skin = { new Color(.98f, .97f, .955f), new Color(.94f, .965f, .97f),
                new Color(1f, .98f, .97f), new Color(.955f, .945f, .935f), new Color(.98f, 1f, .99f) };
            Color[] hair = { new Color(.90f, .94f, .96f), new Color(1f, .96f, .92f),
                new Color(.89f, .90f, .88f), new Color(.96f, .94f, .91f), new Color(.94f, .97f, 1f) };
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++) {
                    var material = materials[slot];
                    if (!material || !material.shader || !material.HasProperty("_Color")) continue;
                    string shader = material.shader.name;
                    if (shader != "Qiaopi/CharacterSkinTexture" && shader != "Qiaopi/CharacterHair") continue;
                    var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, slot);
                    block.SetColor("_Color", material.GetColor("_Color") * (shader == "Qiaopi/CharacterHair" ? hair[tone] : skin[tone]));
                    renderer.SetPropertyBlock(block, slot);
                }
            }
        }

        void OnDestroy()
        {
            // A clone destroyed before its deferred edit-mode callbacks (and
            // before Apply/Ready) still borrows the source actor's mesh list.
            if (runtimeOwnerId != gameObject.GetEntityId().ToString()) return;
            for (int i = 0; i < filters.Count; i++) if (filters[i] && filters[i].sharedMesh == owned[i]) filters[i].sharedMesh = originals[i];
            foreach (var mesh in owned) if (mesh) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            owned.Clear();
        }
    }
}
