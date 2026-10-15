using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    public partial class WorldGame
    {
        Transform workHandTools, workHandSpoon, workHandBrush, workHandWeight, workHandCoins, workSpoonRice;
        Transform workHandLedger, workHandReceipt;
        FirstPersonHandGrip workHandGrip;
        readonly Transform[] workHandSleeves = new Transform[2];
        readonly Vector3[] workSleevePositions = new Vector3[2], workSleeveScales = new Vector3[2];
        readonly Quaternion[] workSleeveRotations = new Quaternion[2];
        readonly GameObject[] workHeldParcels = new GameObject[3];
        readonly GameObject[] workPalmCoins = new GameObject[6];
        readonly Transform[] workRiceDrops = new Transform[12];
        Vector3 workLetterRestPosition;
        Quaternion workLetterRestRotation;
        bool workHandClipWasActive;
        static readonly Vector3 WorkHandRest = new Vector3(0, -.37f, .66f);
        static readonly Vector3 WorkPalm = new Vector3(.174f, .025f, .033f);

        void SetupWorkHandTools()
        {
            if (!heldRig || workHandTools) return;
            if (heldLetter) { workLetterRestPosition = heldLetter.transform.localPosition; workLetterRestRotation = heldLetter.transform.localRotation; }
            workHandTools = TradeScenery.Group(heldRig.transform, "手中职业用具", Vector3.zero);
            BuildWorkSpoon(); BuildWorkBrush(); BuildWorkMoney(); BuildWorkHeldPaper();
            if (heldHands) {
                workHandGrip = heldHands.AddComponent<FirstPersonHandGrip>();
                int sleeve = 0;
                foreach (Transform part in heldHands.GetComponentsInChildren<Transform>(true)) {
                    if (part.name != "布袖" || sleeve >= workHandSleeves.Length) continue;
                    workHandSleeves[sleeve] = part; workSleevePositions[sleeve] = part.localPosition;
                    workSleeveRotations[sleeve] = part.localRotation; workSleeveScales[sleeve] = part.localScale; sleeve++;
                }
            }
            for (int kind = 0; kind < workHeldParcels.Length; kind++) {
                GameObject parcel = TradeScenery.Parcel(heldRig.transform, Vector3.zero, kind, false);
                parcel.name = kind == 0 ? "手托麻布米袋" : kind == 1 ? "手托捆扎布匹" : "手托防潮纸货";
                parcel.transform.localScale = Vector3.one * (kind == 0 ? .46f : .45f);
                parcel.transform.localPosition = new Vector3(0, -.105f, .04f);
                SetWorkHeldLayer(parcel.transform); parcel.SetActive(false); workHeldParcels[kind] = parcel;
            }
            SetWorkHeldLayer(workHandTools); workHandTools.gameObject.SetActive(false);
        }

        void BuildWorkSpoon()
        {
            workHandSpoon = TradeScenery.Group(workHandTools, "木米勺", WorkPalm);
            workHandSpoon.localRotation = Quaternion.Euler(-48, -36, 0);
            var g = new TradeScenery.Geometry(workHandSpoon);
            // A shaped wooden handle passes through the existing palm; the
            // shallow turned bowl lies beyond the fingers, never beside them.
            g.Beam(new Vector3(0, 0, -.105f), new Vector3(0, 0, .157f), .017f, TradeScenery.Wood);
            g.Lathe(new Vector3(0, -.008f, .215f), new[] {
                new Vector2(0,-.014f), new Vector2(.037f,-.013f), new Vector2(.059f,.001f),
                new Vector2(.067f,.017f), new Vector2(.062f,.023f), new Vector2(.052f,.009f),
                new Vector2(.028f,.001f), new Vector2(0,.001f) }, TradeScenery.Wood);
            g.Flush();
            workSpoonRice = TradeScenery.Group(workHandSpoon, "勺中的米", new Vector3(0, .005f, .215f));
            var rice = new TradeScenery.Geometry(workSpoonRice);
            rice.Lathe(Vector3.zero, new[] { new Vector2(0,0),new Vector2(.045f,0),new Vector2(.037f,.015f),new Vector2(.019f,.025f),new Vector2(0,.028f) }, TradeScenery.Rice);
            for (int i = 0; i < 19; i++) {
                float angle = i * 2.4f, radius = .008f + (i % 4) * .009f;
                RiceGrain(rice, new Vector3(Mathf.Cos(angle) * radius, .026f - radius * .25f, Mathf.Sin(angle) * radius), Quaternion.Euler(0, i * 29, 72));
            }
            rice.Flush();
            for (int i = 0; i < workRiceDrops.Length; i++) {
                Transform grain = TradeScenery.Group(workHandTools, "落入秤盘的米粒 " + i, Vector3.zero);
                var falling = new TradeScenery.Geometry(grain); RiceGrain(falling, Vector3.zero, Quaternion.Euler(0, i * 33, 68)); falling.Flush();
                workRiceDrops[i] = grain; grain.gameObject.SetActive(false);
            }
        }

        static void RiceGrain(TradeScenery.Geometry g, Vector3 position, Quaternion rotation)
        {
            g.Lathe(position, new[] { new Vector2(0,-.0045f), new Vector2(.002f,-.003f),
                new Vector2(.0024f,0), new Vector2(.002f,.003f), new Vector2(0,.0045f) }, TradeScenery.Rice, rotation);
        }

        void BuildWorkBrush()
        {
            workHandBrush = TradeScenery.Group(workHandTools, "掌中竹杆毛笔", WorkPalm);
            var g = new TradeScenery.Geometry(workHandBrush);
            Vector3 tip = new Vector3(0, -.079f, .12f), ferrule = new Vector3(0, -.052f, .098f);
            Vector3 end = new Vector3(0, .142f, -.064f);
            g.Beam(ferrule, end, .009f, TradeScenery.Wood);
            g.Beam(ferrule, Vector3.Lerp(ferrule, end, .065f), .012f, TradeScenery.Dark);
            Quaternion q = Quaternion.FromToRotation(Vector3.up, (ferrule - tip).normalized);
            g.Lathe(tip, new[] { new Vector2(0,0), new Vector2(.002f,.004f), new Vector2(.005f,.015f),new Vector2(.0045f,.035f) }, TradeScenery.Ink, q);
            g.Flush();
        }

        void BuildWorkMoney()
        {
            workHandWeight = TradeScenery.Group(workHandTools, "手持小砝码", WorkPalm + new Vector3(.025f,.055f,.025f));
            var weight = new TradeScenery.Geometry(workHandWeight);
            weight.Cylinder(Vector3.zero, .025f, .034f, TradeScenery.Dark);
            weight.Cylinder(new Vector3(0,.024f,0), .012f, .017f, TradeScenery.Brass); weight.Flush();
            workHandCoins = TradeScenery.Group(workHandTools, "掌中的找零钱", WorkPalm + new Vector3(0,.032f,.025f));
            for (int i = 0; i < workPalmCoins.Length; i++) {
                Transform coin = TradeScenery.Group(workHandCoins, "实物零钱 " + (i + 1), new Vector3((i % 3 - 1) * .024f, (i / 3) * .005f, (i % 2) * .007f));
                var money = new TradeScenery.Geometry(coin);
                money.Cylinder(Vector3.zero, .017f, .0035f, TradeScenery.Brass);
                money.Box(new Vector3(0,.002f,0), new Vector3(.005f,.001f,.005f), TradeScenery.Dark); money.Flush();
                workPalmCoins[i] = coin.gameObject;
            }
        }

        void BuildWorkHeldPaper()
        {
            // The left palm supports a book brought close enough to read. The
            // right palm remains on its authored grip point while the nib moves
            // across the page; neither arm reaches through a distant counter.
            workHandLedger = TradeScenery.Group(workHandTools, "手托陈记账本", Vector3.zero);
            var book = new TradeScenery.Geometry(workHandLedger);
            Vector3 cover = new Vector3(-.025f, .024f, .148f);
            book.Box(cover, new Vector3(.40f, .022f, .32f), TradeScenery.Dark);
            book.Box(cover + Vector3.up * .017f, new Vector3(.376f, .012f, .297f), TradeScenery.Paper);
            book.Box(cover + new Vector3(-.172f,.025f,0), new Vector3(.009f,.003f,.293f), TradeScenery.Linen);
            for (int row = 0; row < 6; row++)
                book.Box(new Vector3(-.015f,.049f,.04f + row * .044f), new Vector3(.30f,.0015f,.0018f), TradeScenery.Wood);
            for (int row = 0; row < 3; row++) {
                book.Box(new Vector3(-.108f,.051f,.062f + row * .044f), new Vector3(.047f,.0015f,.003f), TradeScenery.Ink);
                book.Box(new Vector3(-.053f,.051f,.064f + row * .044f), new Vector3(.025f,.0015f,.002f), TradeScenery.Ink);
            }
            book.Box(new Vector3(.109f,.051f,.242f), new Vector3(.034f,.002f,.031f), TradeScenery.Red);
            book.Flush();

            // The returning receipt is one thin slip, visibly unlike the tied
            // silver-and-letter bundle that was delivered to the recipient.
            workHandReceipt = TradeScenery.Group(workHandTools, "手中收讫回执", new Vector3(0,.028f,.084f));
            var receipt = new TradeScenery.Geometry(workHandReceipt);
            receipt.Box(Vector3.zero, new Vector3(.29f,.004f,.21f), TradeScenery.Paper);
            for (int row = 0; row < 4; row++)
                receipt.Box(new Vector3(-.055f + row*.036f,.0028f,.014f), new Vector3(.002f,.001f,.107f), TradeScenery.Ink);
            receipt.Box(new Vector3(.092f,.003f,-.062f), new Vector3(.031f,.0012f,.032f), TradeScenery.Red);
            receipt.Flush();
        }

        void RestoreWorkSleeves()
        {
            for (int i = 0; i < workHandSleeves.Length; i++) if (workHandSleeves[i]) {
                workHandSleeves[i].localPosition = workSleevePositions[i];
                workHandSleeves[i].localRotation = workSleeveRotations[i];
                workHandSleeves[i].localScale = workSleeveScales[i];
            }
        }

        void AnchorWorkSleeves()
        {
            if (!cam || !heldRig) return;
            // Existing fallback cuffs have a rounded upper-arm end. Keep that
            // end below the frame as the hand reaches, instead of translating
            // a detached pair of forearms into the middle of the picture.
            for (int i = 0; i < workHandSleeves.Length; i++) {
                Transform sleeve = workHandSleeves[i]; if (!sleeve) continue;
                float side = Mathf.Sign(workSleevePositions[i].x);
                Vector3 wrist = heldRig.transform.TransformPoint(new Vector3(side * .185f, -.034f, -.019f));
                Vector3 lower = cam.transform.TransformPoint(new Vector3(side * .36f, -.90f, .16f));
                Vector3 direction = wrist - lower;
                // Capsule caps end at the wrist; the end towards the body stays
                // beyond the lower screen edge, even during counter close-ups.
                sleeve.position = (lower + wrist) * .5f;
                sleeve.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                sleeve.localScale = new Vector3(.12f, direction.magnitude * .5f, .13f);
            }
        }

        Vector3 WorkSpoonReach(Vector3 angles)
        {
            if (!tradeRicePan || !cam) return WorkHandRest + new Vector3(-.14f,.24f,.08f);
            // Match the bowl's projection to the actual moving rice pan. The
            // forearm stays near the camera; it does not grow to span the aisle.
            return WorkToolReach(tradeRicePan.position + Vector3.up * .105f, workHandSpoon.localPosition + workHandSpoon.localRotation * new Vector3(0,-.008f,.215f), angles);
        }

        Vector3 WorkToolReach(Vector3 worldPoint, Vector3 toolContact, Vector3 angles)
        {
            if (!cam) return WorkHandRest;
            Vector3 target = cam.transform.InverseTransformPoint(worldPoint);
            if (target.z <= .05f) return WorkHandRest;
            Vector3 projected = target * (.98f / target.z);
            return projected - Quaternion.Euler(angles) * toolContact;
        }

        static void SetWorkHeldLayer(Transform root)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = HeldItemLayer;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) {
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
        }

        // The job's cargo identity remains visible between actions. Old story
        // crates retain their original presentation and regression behaviour.
        void UpdateWorkHeldParcel(bool hasBox)
        {
            int kind = InTrade && TradeHeldBox ? TradeWork.DockDestination(state, state.tradeShift.held) : -1;
            bool tradeCargo = hasBox && kind >= 0 && kind < workHeldParcels.Length;
            if (heldBox) heldBox.SetActive(hasBox && !tradeCargo);
            for (int i = 0; i < workHeldParcels.Length; i++) if (workHeldParcels[i]) workHeldParcels[i].SetActive(tradeCargo && i == kind);
        }

        bool ApplyWorkHandPresentation()
        {
            if (!heldRig || !workHandTools) return false;
            if (!WorkPerformanceBusy) {
                workHandTools.gameObject.SetActive(false);
                if (workHandGrip && !HomeCarrying) workHandGrip.SetPose(0, 0);
                if (workHandClipWasActive) {
                    heldRig.transform.localPosition = WorkHandRest; heldRig.transform.localRotation = Quaternion.identity;
                    if (heldHands) { heldHands.SetActive(true); heldHands.transform.localPosition = Vector3.zero; heldHands.transform.localRotation = Quaternion.identity; }
                    if (heldLetter) { heldLetter.transform.localPosition = workLetterRestPosition; heldLetter.transform.localRotation = workLetterRestRotation; }
                    RestoreWorkSleeves();
                    workHandClipWasActive = false;
                }
                return false;
            }
            workHandClipWasActive = true;
            string kind = WorkPerformanceKind;
            float p = Mathf.Clamp01(WorkPerformanceProgress);
            float reach = Mathf.SmoothStep(0, 1, p / .30f) * (1f - Mathf.SmoothStep(0, 1, (p - .70f) / .30f));
            heldRig.SetActive(true); if (heldHands) heldHands.SetActive(kind != "courier-sign" || p >= .78f); if (heldCamera) heldCamera.enabled = true;
            if (heldBox) heldBox.SetActive(false); if (heldLetter) heldLetter.SetActive(false);
            for (int i = 0; i < workHeldParcels.Length; i++) if (workHeldParcels[i]) workHeldParcels[i].SetActive(false);
            workHandTools.gameObject.SetActive(true);
            workHandSpoon.gameObject.SetActive(kind == "shop-scoop");
            workHandBrush.gameObject.SetActive(kind == "shop-ledger");
            workHandBrush.localRotation = Quaternion.identity; workHandBrush.localPosition = WorkPalm;
            workHandLedger.gameObject.SetActive(kind == "shop-ledger");
            workHandReceipt.gameObject.SetActive(kind == "courier-sign" && p >= .82f);
            workHandWeight.gameObject.SetActive(kind == "shop-weigh" && p < .68f);
            int coinAmount = kind == "shop-pay" ? TradeWork.ShopChangeTarget(state) : Mathf.Abs(workPerformanceAmount);
            bool coinsVisible = kind == "shop-coin" && workPerformanceAmount < 0 ? p > .34f : p < .72f;
            workHandCoins.gameObject.SetActive((kind == "shop-coin" || kind == "shop-pay") && coinAmount > 0 && coinsVisible);
            for (int i = 0; i < workRiceDrops.Length; i++) workRiceDrops[i].gameObject.SetActive(false);
            Vector3 position = WorkHandRest, angles = Vector3.zero;
            float curl = (kind == "shop-ledger" ? 42 : kind == "shop-scoop" ? 32 : kind == "shop-weigh" ? 24 : 0) * reach;
            float wristRise = kind == "shop-ledger" ? .075f * reach : 0;
            if (workHandGrip) workHandGrip.SetPose(curl, wristRise);

            switch (kind) {
                case "dock-pick": {
                    float lift = Mathf.SmoothStep(0, 1, (p - .12f) / .84f);
                    position += new Vector3(0, -.24f * (1-lift), .17f * (1-lift)); angles.x = 19 * (1-lift);
                    ShowWorkClipParcel(p > .34f); break;
                }
                case "dock-drop":
                    position += new Vector3(0, -.13f * reach, .22f * reach); angles.x = 15 * reach;
                    ShowWorkClipParcel(p < .69f); break;
                case "shop-scoop": {
                    bool removing = workPerformanceAmount < 0;
                    float scoop = Mathf.Sin(p * Mathf.PI * 2) * reach;
                    position += new Vector3(-.035f * reach, .065f * reach, .13f * reach);
                    angles = new Vector3(removing ? -6 * Mathf.Sin(p * Mathf.PI) * reach : 12 * scoop, 0, -7 * reach);
                    // Wrist and spoon rotate together; its handle never leaves
                    // the authored palm. Removing excess rice goes out empty
                    // and returns full, without a pouring motion or falling rice.
                    float pour = removing ? 0 : Mathf.SmoothStep(0,1,(p-.45f)/.15f) * (1-Mathf.SmoothStep(0,1,(p-.76f)/.17f));
                    angles.x += 27 * pour; workSpoonRice.gameObject.SetActive(removing ? p >= .52f : p < .61f);
                    for (int i = 0; i < workRiceDrops.Length; i++) {
                        bool falling = !removing && p > .49f && p < .76f;
                        workRiceDrops[i].gameObject.SetActive(falling);
                        float fall = Mathf.Repeat((p - .49f) * 5 + i * .083f, 1);
                        workRiceDrops[i].localPosition = workHandSpoon.localPosition + workHandSpoon.localRotation * new Vector3((i % 3 - 1) * .008f, -.01f, .22f + (i % 2) * .007f) - Vector3.up * (fall * .16f);
                    }
                    position = Vector3.Lerp(WorkHandRest, WorkSpoonReach(angles), reach);
                    break;
                }
                case "shop-weigh":
                    angles.z = -8 * reach; angles.x = 5 * reach;
                    if (tradeWeightPan) position = Vector3.Lerp(WorkHandRest,
                        WorkToolReach(tradeWeightPan.position + Vector3.up * .115f, workHandWeight.localPosition, angles), reach);
                    break;
                case "shop-coin":
                case "shop-pay":
                    angles = new Vector3(11 * reach, 0, -8 * reach);
                    position = Vector3.Lerp(WorkHandRest,
                        WorkToolReach(workPerformanceTarget + Vector3.up * .025f, workHandCoins.localPosition, angles), reach);
                    int count = Mathf.Clamp(coinAmount, 0, workPalmCoins.Length);
                    for (int i = 0; i < workPalmCoins.Length; i++) workPalmCoins[i].SetActive(i < count);
                    break;
                case "shop-ledger": {
                    position += new Vector3(-.012f * reach, .065f * reach, .02f * reach);
                    angles = new Vector3(-4 * reach, 0, 0);
                    Vector3 originalNib = new Vector3(0,-.079f,.12f);
                    float writing = p > .24f && p < .83f ? 1 : 0;
                    float x = -.080f + Mathf.Sin(p * 35) * .010f * writing;
                    // Raise the writing wrist above the sheet. The left hand keeps
                    // supporting the book, while the right fingers close gently.
                    workHandBrush.localPosition = WorkPalm + Vector3.up * wristRise;
                    float y = .027f - wristRise;
                    float z = Mathf.Sqrt(Mathf.Max(.001f, originalNib.sqrMagnitude - x*x - y*y));
                    // The nib stays on the held page at y=.052m as the grip
                    // rises. The shaft now leans upwards from paper to fingers.
                    workHandBrush.localRotation = Quaternion.FromToRotation(originalNib, new Vector3(x,y,z));
                    break;
                }
                case "courier-take":
                    position += new Vector3(0, .065f * reach, .21f * reach); angles.x = 8 * reach;
                    if (heldLetter) heldLetter.SetActive(p > .34f); break;
                case "courier-sign":
                    // The recipient signs; the courier reaches for the return
                    // receipt. Never depict the player signing for somebody.
                    float takeReceipt = Mathf.SmoothStep(0,1,(p-.76f)/.15f);
                    position += new Vector3(0, -.12f + .16f * takeReceipt, .06f + .06f * takeReceipt);
                    angles.x = 3 * takeReceipt;
                    break;
                case "courier-offer":
                case "courier-return":
                    position += new Vector3(0, .085f * reach, .26f * reach); angles.x = -8 * reach;
                    if (heldLetter) heldLetter.SetActive(p < .74f); break;
                default:
                    position += new Vector3(0, .055f * reach, .10f * reach); angles.x = 5 * reach; break;
            }
            heldRig.transform.localPosition = position; heldRig.transform.localRotation = Quaternion.Euler(angles);
            RestoreWorkSleeves();
            if (kind.StartsWith("shop-") || kind == "courier-sign") AnchorWorkSleeves();
            return true;
        }

        void ShowWorkClipParcel(bool visible)
        {
            int kind = Mathf.Clamp(workPerformanceParcelKind, 0, workHeldParcels.Length - 1);
            if (workHeldParcels[kind]) workHeldParcels[kind].SetActive(visible);
        }
    }

    // Instance-only hand posing. The authored UVs and five complete fingers stay
    // intact; neither the imported FBX nor another character's mesh is changed.
    // A smooth wrist lift reaches back along the sleeve, so no wrist gap opens.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class FirstPersonHandGrip : MonoBehaviour
    {
        sealed class Part
        {
            public MeshFilter filter;
            public Mesh original, mesh;
            public Vector3[] vertices, posed;
            public Vector3[] normals;
            public Matrix4x4 toHand, fromHand;
        }
        readonly System.Collections.Generic.List<Part> parts = new System.Collections.Generic.List<Part>();
        float previousCurl = float.NaN, previousRise = float.NaN;
        bool initialized, previousBoth;

        void Awake() { Initialize(); }
        void Initialize()
        {
            if (initialized) return; initialized = true;
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true)) {
                Mesh source = filter.sharedMesh;
                if (!source || !source.isReadable || !filter.name.EndsWith("_Mesh", System.StringComparison.Ordinal)) continue;
                Vector3[] vertices = source.vertices;
                Matrix4x4 toHand = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                bool hasHand = false;
                for (int i = 0; i < vertices.Length; i++) if (Mathf.Abs(toHand.MultiplyPoint3x4(vertices[i]).x) > .08f) { hasHand = true; break; }
                if (!hasHand) continue;
                Mesh copy = Instantiate(source); copy.name = source.name + " · hand grip"; copy.MarkDynamic();
                parts.Add(new Part { filter = filter, original = source, mesh = copy, vertices = vertices,
                    posed = new Vector3[vertices.Length], normals = source.normals, toHand = toHand, fromHand = toHand.inverse });
                filter.sharedMesh = copy;
            }
        }

        public void SetPose(float curlDegrees, float wristRise) { ApplyPose(curlDegrees, wristRise, false); }
        public void SetHouseholdPose(float curlDegrees) { ApplyPose(curlDegrees, 0, true); }

        void ApplyPose(float curlDegrees, float wristRise, bool bothHands)
        {
            Initialize(); curlDegrees = Mathf.Clamp(curlDegrees, 0, 50); wristRise = Mathf.Clamp(wristRise, 0, .09f);
            if (bothHands == previousBoth && Mathf.Abs(curlDegrees - previousCurl) < .025f && Mathf.Abs(wristRise - previousRise) < .00005f) return;
            previousCurl = curlDegrees; previousRise = wristRise; previousBoth = bothHands;
            foreach (var part in parts) {
                if (!part.mesh) continue;
                if (curlDegrees == 0 && wristRise == 0) { part.mesh.vertices = part.vertices; part.mesh.normals = part.normals; part.mesh.RecalculateBounds(); continue; }
                for (int i = 0; i < part.vertices.Length; i++) {
                    Vector3 point = part.toHand.MultiplyPoint3x4(part.vertices[i]);
                    if (point.x > .065f || (bothHands && point.x < -.065f)) {
                        float sleeveLift = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.38f, -.018f, point.z));
                        // Curl the four forward fingers in towards the palm;
                        // the upper thumb and wrist remain stable. A broad
                        // smooth transition avoids creasing through knuckles.
                        float finger = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.045f, .13f, point.z));
                        float thumb = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.060f, .082f, point.y));
                        Vector3 pivot = new Vector3(point.x, point.y, .047f);
                        if (bothHands) {
                            float strength = curlDegrees / 50f;
                            float forward = Mathf.Max(0, point.z - .047f);
                            float curl = Mathf.Clamp01(forward / .105f);
                            float angle = curl * 165f * Mathf.Deg2Rad;
                            // Bring the four fingers together, then fold their
                            // successive phalanges around the thin bamboo ear.
                            // The palm/wrist keep their authored metric size.
                            Vector3 closed = point;
                            closed.y = Mathf.Lerp(point.y, .025f, .44f * finger * thumb);
                            closed.x -= Mathf.Sign(point.x) * .022f * (1f - Mathf.Cos(angle)) * thumb;
                            closed.z = Mathf.Lerp(point.z, .047f + .022f * Mathf.Sin(angle), finger * thumb);
                            float thumbPart = (1f - thumb) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.005f, .02f, point.z)) * (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.10f, .14f, point.z)));
                            if (thumbPart > 0) {
                                Vector3 thumbBase = new Vector3(Mathf.Sign(point.x) * .163f, .040f, .005f);
                                Vector3 foldedThumb = thumbBase + Quaternion.Euler(40, 0, 0) * (point - thumbBase);
                                closed = Vector3.Lerp(closed, foldedThumb, thumbPart);
                            }
                            point = Vector3.Lerp(point, closed, strength);
                        } else point = pivot + Quaternion.Euler(0, -curlDegrees * finger * thumb, 0) * (point - pivot);
                        point.y += wristRise * sleeveLift;
                    }
                    part.posed[i] = part.fromHand.MultiplyPoint3x4(point);
                }
                part.mesh.vertices = part.posed; part.mesh.RecalculateNormals(); part.mesh.RecalculateBounds();
            }
        }

        void OnDestroy()
        {
            foreach (var part in parts) {
                if (part.filter) part.filter.sharedMesh = part.original;
                if (part.mesh) { if (Application.isPlaying) Destroy(part.mesh); else DestroyImmediate(part.mesh); }
            }
        }
    }
}
