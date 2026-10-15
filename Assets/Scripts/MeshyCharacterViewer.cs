using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    // Isolated body-rig/art inspection. No WorldGame, save store, PlayerPrefs,
    // post-processing, facial deformation or audio is used by this viewer.
    public sealed class MeshyCharacterViewer : MonoBehaviour
    {
        public const string ModelResource = "Models/WenshengMeshy/WenshengMeshy";
        static readonly string[] Characters = { "Wensheng", "Wife", "Mother", "Aman", "Xusheng", "MasterHe", "Clerk", "Uncle", "Guide", "Foreman", "Shopkeeper", "Worker" };
        static readonly string[] CharacterLabels = { "文生", "妻子", "母亲", "阿满", "许生", "何师傅", "信局伙计", "族亲", "接应同乡", "码头工头", "陈掌柜", "帮工乡亲" };
        public string captureDirectory;
        const float StepSeconds = 1f / 60f;
        enum Motion { Idle, Walk, Offer, Sign, Seated, Run }
        Camera viewCamera;
        Transform subject;
        NativeCharacterRig rig;
        CharacterPerformance performance;
        Renderer floor;
        GameObject stool,writingTable;
        Transform writingPen;
        Font font;
        GUIStyle heading, hint, button;
        readonly List<Material> studioMaterials = new List<Material>();
        readonly Dictionary<SkinnedMeshRenderer, Mesh> sourceMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();
        readonly List<CaptureState> captures = new List<CaptureState>();
        readonly bool[] available = new bool[Characters.Length];
        Motion motion;
        float yaw, pitch, distance, targetHeight, actionTime;
        bool captureMode, ready, dragging, fullBody = true;
        string outputDirectory, error;
        string character = "Wensheng";
        string CurrentResource { get { return ModelLibrary.MeshyResource(character); } }
        string CharacterLabel {
            get {
                int index = Array.IndexOf(Characters, character);
                return index >= 0 ? CharacterLabels[index] : character;
            }
        }

        [Serializable] sealed class CaptureState
        {
            public string file, pose, gesture;
            public int frame, steppedFrames;
            public float steppedSeconds, gestureProgress, cameraYaw;
            public bool finiteBounds, nativeRig, seated, sourceMeshesUnchanged;
            public Vector3 boundsCenter, boundsSize, leftHand, rightHand, head;
            public Quaternion leftArmRotation, rightArmRotation, leftKneeRotation;
        }
        [Serializable] sealed class CaptureReport
        {
            public string character, resource, unityVersion, status, error;
            public string validationScope, walkingClip, runningClip, idleClip, seatedClip;
            public bool nativeRig, upperRig, elbowRig, seatedRig, faceMotion, muted;
            public bool importedMotion, authoredIdle, authoredSeat;
            public int skinCount, vertexCount, boneCount, missingMaterials;
            public float fixedStep;
            public CaptureState[] captures;
        }

        void Awake()
        {
            Silence();
            captureMode = HasArgument("-qiaopi-meshy-capture");
            string requested = ArgumentValue("-meshy-character");
            if (HasArgument("-meshy-character")) {
                character = requested ?? "";
                foreach (string candidate in Characters)
                    if (string.Equals(candidate, requested, StringComparison.OrdinalIgnoreCase)) character = candidate;
            }
            outputDirectory = ArgumentValue("-meshy-output") ??
                (character == "Wensheng" || string.IsNullOrEmpty(captureDirectory) ? captureDirectory : Path.Combine(captureDirectory, character));
            Application.targetFrameRate = 60;
            ReleasePointer();
        }

        void Start()
        {
            try
            {
                CreateStudio();
                font = Resources.Load<Font>("Fonts/Body");
                for (int i = 0; i < Characters.Length; i++)
                    available[i] = !string.IsNullOrEmpty(ModelLibrary.MeshyResource(Characters[i])) && Resources.Load<GameObject>(ModelLibrary.MeshyResource(Characters[i]));
                SelectCharacter(character);
                if (captureMode) StartCoroutine(CaptureAll());
            }
            catch (Exception exception) { Fail(exception); }
        }

        static void Silence() { AudioListener.pause = true; AudioListener.volume = 0f; }

        void SelectCharacter(string selected)
        {
            ReleasePointer(); ready = false;
            UnloadSubject();
            character = selected; error = null;
            sourceMeshes.Clear(); captures.Clear();
            LoadSubject();
            SetMotion(Motion.Idle);
            Frame(true); ready = true;
            Debug.Log("QIAOPI_MESHY_STUDY_READY character=" + character + " resource=" + CurrentResource);
        }

        void UnloadSubject()
        {
            if (subject) {
                subject.gameObject.SetActive(false);
                Destroy(subject.gameObject);
            }
            subject = null; rig = null; performance = null;
            if (stool) stool.SetActive(false);
        }

        void LoadSubject()
        {
            // Require the native resource explicitly; never silently inspect the
            // old cast if ModelLibrary's optional native asset is missing.
            if (string.IsNullOrEmpty(CurrentResource))
                throw new ArgumentException("不支持的人物：" + character + "。请指定已导入的侨批角色名称。");
            var prefab = Resources.Load<GameObject>(CurrentResource);
            if (!prefab) throw new FileNotFoundException("尚未导入" + CharacterLabel + "新模型：" + CurrentResource + "。预览不会替用旧模型。");
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("预览模型不能附带游戏脚本。");
            var actor = ModelLibrary.Spawn(character, transform, Vector3.zero);
            if (!actor) throw new InvalidOperationException("无法加载" + CharacterLabel + "模型。");
            subject = actor.transform;
            rig = actor.GetComponent<NativeCharacterRig>();
            if (!rig) throw new InvalidOperationException("新人物模型未启用原生骨架：" + character);
            rig.ValidateJoints();
            foreach (var source in actor.GetComponentsInChildren<AudioSource>(true))
            { source.Stop(); source.playOnAwake = false; source.enabled = false; }
            foreach (var listener in actor.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            if (!rig.HasImportedMotion)
                foreach (var animator in actor.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!skin.sharedMesh) throw new InvalidOperationException("人物蒙皮缺少网格。");
                sourceMeshes.Add(skin, skin.sharedMesh);
                skin.updateWhenOffscreen = true;
            }
            if (sourceMeshes.Count == 0) throw new InvalidOperationException("人物没有可检查的蒙皮网格。");
            Bounds bounds = ModelLibrary.WorldBounds(subject);
            RequireBounds(bounds);
            subject.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            performance = CharacterPerformance.For(actor);
            performance.Automatic = false;
            performance.TurnBodyToLook = false;
            if (!performance.UsesNativeRig || !performance.HasUpperRig || !performance.HasElbowRig || !performance.HasSeatedRig)
                throw new InvalidOperationException("原生人物身体骨架不完整。");
            if (performance.HasFaceMotion)
                throw new InvalidOperationException("本预览不应启用旧模型的面部变形。");
        }

        void CreateStudio()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.39f, .40f, .41f);
            RenderSettings.ambientEquatorColor = new Color(.30f, .31f, .32f);
            RenderSettings.ambientGroundColor = new Color(.23f, .22f, .21f);
            RenderSettings.reflectionIntensity = .1f;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 15f;
            QualitySettings.pixelLightCount = 4;
            viewCamera = new GameObject("Meshy study camera").AddComponent<Camera>();
            viewCamera.tag = "MainCamera";
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = new Color(.29f, .31f, .32f);
            viewCamera.fieldOfView = 30f;
            viewCamera.nearClipPlane = .025f;
            viewCamera.farClipPlane = 30f;
            viewCamera.allowHDR = false;
            viewCamera.allowMSAA = true;
            // No AudioListener, exposure adjustment, smoothing or face material overrides.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Neutral inspection floor";
            ground.transform.position = new Vector3(0, -.006f, 0);
            ground.transform.localScale = Vector3.one * 1.5f;
            ground.GetComponent<Collider>().enabled = false;
            floor = ground.GetComponent<Renderer>();
            floor.sharedMaterial = MaterialFor("Inspection floor", new Color(.28f, .29f, .29f));
            stool = new GameObject("Seat surface at 43 cm");
            var stoolMaterial = MaterialFor("Plain wooden stool", new Color(.25f, .21f, .17f));
            Block("Seat", stool.transform, new Vector3(0, .40f, 0), new Vector3(.40f, .06f, .35f), stoolMaterial);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Block("Leg", stool.transform, new Vector3(x * .15f, .185f, z * .12f), new Vector3(.035f, .37f, .035f), stoolMaterial);
            stool.SetActive(false);
            writingTable=new GameObject("Writing QA table, surface 78 cm");
            Block("Desk board",writingTable.transform,new Vector3(0,.755f,.59f),new Vector3(1.05f,.05f,.67f),stoolMaterial);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Block("Desk leg",writingTable.transform,new Vector3(x*.46f,.365f,.59f+z*.27f),new Vector3(.045f,.73f,.045f),stoolMaterial);
            Block("Real size letter paper",writingTable.transform,new Vector3(0,.783f,.47f),new Vector3(.29f,.002f,.32f),MaterialFor("Letter paper",new Color(.83f,.77f,.61f)));
            var pen=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pen.name="Writing brush, tip on paper";
            pen.transform.SetParent(writingTable.transform,false);pen.GetComponent<Collider>().enabled=false;
            pen.GetComponent<Renderer>().sharedMaterial=MaterialFor("Writing brush",new Color(.15f,.09f,.04f));writingPen=pen.transform;
            writingTable.SetActive(false);
            AddLight("Soft key", new Vector3(-3, 4, 5), new Color(1f, .97f, .93f), .62f, true);
            AddLight("Soft fill", new Vector3(3, 2, 4), new Color(.94f, .97f, 1f), .24f, false);
            AddLight("Shoulder separation", new Vector3(1, 3, -4), Color.white, .20f, false);
        }

        Material MaterialFor(string label, Color color)
        {
            var shader = Shader.Find("Standard");
            if (!shader) throw new InvalidOperationException("预览场景缺少 Standard shader。");
            var result = new Material(shader) { name = label, color = color };
            result.SetFloat("_Glossiness", .12f);
            result.SetFloat("_Metallic", 0);
            studioMaterials.Add(result);
            return result;
        }

        static void Block(string label, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = label; block.transform.SetParent(parent, false);
            block.transform.localPosition = position; block.transform.localScale = size;
            block.GetComponent<Collider>().enabled = false;
            block.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void AddLight(string label, Vector3 position, Color color, float intensity, bool shadows)
        {
            var source = new GameObject(label).AddComponent<Light>();
            source.transform.position = position;
            source.transform.LookAt(new Vector3(0, 1.1f, 0));
            source.type = LightType.Directional;
            source.color = color; source.intensity = intensity;
            source.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            source.shadowStrength = .4f; source.shadowBias = .015f; source.shadowNormalBias = .025f;
        }

        void Update()
        {
            Silence();
            if (!ready || captureMode) return;
            if (Input.GetKeyDown(KeyCode.F)) Frame(true);
            if (Input.GetKeyDown(KeyCode.H)) Frame(false);
            if (Input.GetKeyDown(KeyCode.Escape)) ReleasePointer();
            if (Input.GetMouseButtonDown(1))
            { dragging = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (dragging && Input.GetMouseButton(1))
            {
                yaw += Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -30, 30) * 2.5f;
                pitch = Mathf.Clamp(pitch - Mathf.Clamp(Input.GetAxisRaw("Mouse Y"), -30, 30) * 2f, -20, 45);
            }
            if (dragging && !Input.GetMouseButton(1)) ReleasePointer();
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > .001f)
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(-wheel * .12f), .62f, 5f);
                targetHeight = Mathf.Lerp(HeadHeight(), .9f, Mathf.InverseLerp(.7f, 3.8f, distance));
            }
        }

        void LateUpdate()
        {
            if (!ready || captureMode) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            actionTime += dt;
            if ((motion == Motion.Offer || motion == Motion.Sign) && actionTime > 3.2f)
            { actionTime = 0; performance.Play(motion == Motion.Offer ? CharacterGesture.Offer : CharacterGesture.Sign, 2.6f); }
            performance.Step(dt);
            UpdateWritingProp();
            if (!fullBody) targetHeight = HeadHeight();
            PositionCamera();
        }

        void SetMotion(Motion value)
        {
            motion = value; actionTime = 0;
            if (fullBody) targetHeight = value == Motion.Seated ? .65f : .91f;
            performance.ResetPose();
            performance.ClearLook();
            performance.SetLocomotion(value == Motion.Run ? 1.5f : value == Motion.Walk ? .8f : 0);
            // Match the actual family stool for the child instead of judging
            // her foot/seat contact on the adult 43 cm preview fixture.
            float seatHeight = character == "Aman" ? .36f : .43f;
            stool.transform.localScale = new Vector3(1, seatHeight / .43f, 1);
            performance.SetSeated(value == Motion.Seated || (value==Motion.Sign && rig.HasImportedMotion), seatHeight);
            if (value == Motion.Offer) performance.Play(CharacterGesture.Offer, 2.6f);
            if (value == Motion.Sign) performance.Play(CharacterGesture.Sign, 2.6f);
            stool.SetActive(value == Motion.Seated || (value==Motion.Sign && rig.HasImportedMotion));
            // A child handwriting fixture follows her reach, as do task targets.
            writingTable.transform.localScale=Vector3.one*(character=="Aman" ? 1.26f/1.74f : 1f);
            writingTable.SetActive(value==Motion.Sign && rig.HasImportedMotion);
        }

        void UpdateWritingProp()
        {
            if(!writingTable || !writingTable.activeSelf || !writingPen || !rig) return;
            var index=rig.Joint("mixamorig:RightHandIndex2");var thumb=rig.Joint("mixamorig:RightHandThumb2");
            Vector3 grip=index && thumb ? (index.position+thumb.position)*.5f : performance.RightHandPoint;
            Vector3 localGrip=writingTable.transform.InverseTransformPoint(grip);
            Vector3 tip=writingTable.transform.TransformPoint(new Vector3(Mathf.Clamp(localGrip.x,-.12f,.12f),.786f,Mathf.Clamp(localGrip.z+.015f,.34f,.60f)));
            Vector3 direction=(grip-tip).normalized;Vector3 end=grip+direction*.035f;
            writingPen.position=(tip+end)*.5f;writingPen.rotation=Quaternion.FromToRotation(Vector3.up,end-tip);
            writingPen.localScale=new Vector3(.004f,Vector3.Distance(tip,end)*.5f/writingTable.transform.lossyScale.y,.004f);
        }

        float HeadHeight()
        {
            // The imported head joint is at the neck, so use the actual bind
            // height offset. This also tracks the seated body without zooming it.
            return performance && performance.HeadJoint ? performance.HeadJoint.position.y + .10f : 1.57f;
        }

        void Frame(bool entireBody)
        {
            ReleasePointer(); fullBody = entireBody;
            yaw = 0; pitch = entireBody ? 2 : 0;
            distance = entireBody ? 3.85f : .84f;
            targetHeight = entireBody ? (motion == Motion.Seated ? .65f : .91f) : HeadHeight();
            PositionCamera();
        }

        void PositionCamera()
        {
            if (!viewCamera) return;
            if (floor) floor.enabled = distance > 2.2f;
            Vector3 focus = new Vector3(0, targetHeight, 0);
            Vector3 offset = Quaternion.Euler(-pitch, yaw, 0) * Vector3.forward * distance;
            viewCamera.transform.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset, Vector3.up));
        }

        void ReleasePointer() { dragging = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnApplicationFocus(bool focused) { if (!focused) ReleasePointer(); }
        void OnDisable() { ReleasePointer(); }
        void OnDestroy() { foreach (var material in studioMaterials) if (material) Destroy(material); }

        void OnGUI()
        {
            if (captureMode) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { font = font, fontSize = 23, normal = { textColor = new Color(.94f, .92f, .86f) } };
                hint = new GUIStyle(heading) { fontSize = 14, wordWrap = true, normal = { textColor = new Color(.77f, .80f, .80f) } };
                button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 17 };
            }
            float scale = Mathf.Clamp(Screen.width / 1300f, .60f, 1.5f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            GUILayout.BeginArea(new Rect(26, 20, width - 52, 190));
            GUILayout.Label(CharacterLabel + " · 动作预览", heading);
            GUILayout.Label("右键旋转 · 滚轮缩放 · 静音", hint);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Characters.Length; i++) {
                if (i > 0 && i % 6 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
                bool oldEnabled = GUI.enabled;
                Color oldColor = GUI.backgroundColor;
                GUI.enabled = oldEnabled && available[i];
                GUI.backgroundColor = character == Characters[i] ? new Color(.63f, .72f, .63f) : new Color(.75f, .76f, .74f);
                string label = CharacterLabels[i] + (available[i] ? "" : "（待导入）");
                if (GUILayout.Button(label, button, GUILayout.Width(Mathf.Min(160, (width - 70) / 6)), GUILayout.Height(34))) {
                    try { SelectCharacter(Characters[i]); }
                    catch (Exception exception) { Fail(exception); }
                }
                GUI.enabled = oldEnabled; GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (!string.IsNullOrEmpty(error)) GUI.Label(new Rect(30, height * .4f, width - 60, 160), error, hint);
            if (ready)
            {
                GUILayout.BeginArea(new Rect((width - 840) * .5f, height - 74, 840, 54));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("全身", button, GUILayout.Height(42))) Frame(true);
                if (GUILayout.Button("近景", button, GUILayout.Height(42))) Frame(false);
                GUILayout.Space(14);
                string[] labels = { "待机", "行走", "递信", "写信", "坐下", "跑步" };
                for (int i = 0; i < labels.Length; i++)
                {
                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = motion == (Motion)i ? new Color(.63f, .72f, .63f) : new Color(.75f, .76f, .74f);
                    if (GUILayout.Button(labels[i], button, GUILayout.Height(42))) SetMotion((Motion)i);
                    GUI.backgroundColor = old;
                }
                GUILayout.EndHorizontal(); GUILayout.EndArea();
            }
            GUI.matrix = previous;
        }

        IEnumerator CaptureAll()
        {
            Exception failure = null;
            try
            {
                if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("请通过 -meshy-output 指定截图目录。");
                outputDirectory = Path.GetFullPath(outputDirectory);
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception exception) { failure = exception; }
            if (failure != null) { Fail(failure); yield break; }
            string[] names = { "front_full", "face_front", "face_three_quarter", "side_full", "idle", "walk", "offer", "sign", "seated", "run", "walk_after_cycles", "stop_after_walk" };
            Motion[] motions = { Motion.Idle, Motion.Idle, Motion.Idle, Motion.Idle, Motion.Idle, Motion.Walk, Motion.Offer, Motion.Sign, Motion.Seated, Motion.Run, Motion.Walk, Motion.Idle };
            int[] steps = { 0, 0, 0, 0, 60, 24, 54, 70, 65, 24, 180, 90 };
            var captureArguments=Environment.GetCommandLineArgs();
            int captureOnlyIndex=Array.IndexOf(captureArguments,"-meshy-capture-only");
            string captureOnly=captureOnlyIndex>=0 && captureOnlyIndex+1<captureArguments.Length ? ","+captureArguments[captureOnlyIndex+1]+"," : null;
            for (int i = 0; i < names.Length; i++)
            {
                if(captureOnly!=null && !captureOnly.Contains(","+names[i]+",")) continue;
                try
                {
                    if (names[i] == "stop_after_walk") {
                        // Keep the previous walking state to inspect the actual
                        // stop transition, rather than resetting into an idle pose.
                        motion = Motion.Idle;
                        performance.SetLocomotion(0);
                    } else SetMotion(motions[i]);
                    if (i < 4) performance.ResetPose(); // First views preserve the authored A-pose.
                }
                catch (Exception exception) { failure = exception; }
                if (failure != null) { Fail(failure); yield break; }
                for (int frame = 0; frame < steps[i]; frame++)
                {
                    try { performance.Step(StepSeconds); UpdateWritingProp(); }
                    catch (Exception exception) { failure = exception; }
                    if (failure != null) { Fail(failure); yield break; }
                    yield return null; // Let Unity update transforms/skinning for every body step.
                }
                Frame(i != 1 && i != 2);
                yaw = i == 2 ? 35 : i == 3 ? 90 : i >= 5 ? 28 : 0;
                PositionCamera();
                // Explicit camera rendering follows two real player frames, not
                // a screenshot of a Blender render or a stale GPU skin pose.
                yield return null;
                yield return null;
                try
                {
                    var state = Inspect(names[i], steps[i]);
                    CaptureCamera(Path.Combine(outputDirectory, names[i] + ".png"), 1100, 1400);
                    captures.Add(state);
                    WriteReport("capturing", null);
                    Debug.Log("QIAOPI_MESHY_CAPTURE=" + state.file);
                    if (HasArgument("-meshy-shading-diagnostics")) {
                        // Isolate rendering faults without modifying imported art or
                        // treating diagnostic views as production acceptance.
                        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                        var shadows = new LightShadows[lights.Length];
                        for (int l = 0; l < lights.Length; l++) {
                            shadows[l] = lights[l].shadows; lights[l].shadows = LightShadows.None;
                        }
                        CaptureCamera(Path.Combine(outputDirectory, names[i] + "_no_shadows.png"), 1100, 1400);
                        var materials = new List<Material>();
                        var bumps = new List<float>();
                        var normalTextures = new List<Texture>();
                        foreach (var renderer in subject.GetComponentsInChildren<Renderer>())
                            foreach (var material in renderer.materials)
                                if (material.HasProperty("_BumpScale")) {
                                    materials.Add(material); bumps.Add(material.GetFloat("_BumpScale"));
                                    normalTextures.Add(material.GetTexture("_BumpMap"));
                                    material.SetFloat("_BumpScale", 0);
                                }
                        CaptureCamera(Path.Combine(outputDirectory, names[i] + "_no_shadows_no_bump.png"), 1100, 1400);
                        var shaders = new List<Shader>();
                        var unlit = Resources.Load<Shader>("MeshyDiagnosticUnlit");
                        if (!unlit) throw new InvalidOperationException("Missing diagnostic unlit shader");
                        foreach (var material in materials) { shaders.Add(material.shader); material.shader = unlit; }
                        CaptureCamera(Path.Combine(outputDirectory, names[i] + "_unlit.png"), 1100, 1400);
                        foreach (var material in materials) {
                            material.shader = Shader.Find("Standard");
                            material.SetFloat("_Metallic", 0); material.SetFloat("_Glossiness", .1f);
                            material.SetTexture("_BumpMap", null); material.DisableKeyword("_NORMALMAP");
                        }
                        CaptureCamera(Path.Combine(outputDirectory, names[i] + "_standard.png"), 1100, 1400);
                        for (int m = 0; m < materials.Count; m++) materials[m].shader = shaders[m];
                        for (int m = 0; m < materials.Count; m++) {
                            materials[m].SetFloat("_BumpScale", bumps[m]);
                            materials[m].SetTexture("_BumpMap", normalTextures[m]);
                        }
                        for (int l = 0; l < lights.Length; l++) lights[l].shadows = shadows[l];
                        Debug.Log("QIAOPI_SHADING_DIAGNOSTIC=" + names[i]);
                    }
                    if (i >= 4 && HasArgument("-meshy-review-angles")) {
                        // Review the same sampled pose from the side and rear:
                        // a long skirt can look intact in front while splitting behind.
                        foreach (float angle in new[] { 90f, 180f }) {
                            yaw = angle; PositionCamera();
                            string view = names[i] + (angle == 90f ? "_side" : "_rear");
                            CaptureCamera(Path.Combine(outputDirectory, view + ".png"), 1100, 1400);
                            captures.Add(Inspect(view, steps[i]));
                        }
                        WriteReport("capturing", null);
                    }
                }
                catch (Exception exception) { failure = exception; }
                if (failure != null) { Fail(failure); yield break; }
            }
            try { WriteReport("passed", null); }
            catch (Exception exception) { Fail(exception); yield break; }
            Debug.Log("QIAOPI_MESHY_CAPTURE_COMPLETE=" + outputDirectory);
            Application.Quit(0);
        }

        CaptureState Inspect(string label, int steppedFrames)
        {
            rig.ValidateJoints();
            Bounds bounds = ModelLibrary.WorldBounds(subject);
            RequireBounds(bounds);
            foreach (var pair in sourceMeshes)
                if (!pair.Key || pair.Key.sharedMesh != pair.Value)
                    throw new InvalidOperationException("预览动作改变了原生模型网格引用。");
            foreach (var joint in subject.GetComponentsInChildren<Transform>())
                if (!Finite(joint.position) || !Finite(joint.lossyScale))
                    throw new InvalidOperationException("人物关节出现非有限坐标：" + joint.name);
            return new CaptureState {
                file = label + ".png", pose = label, frame = Time.frameCount,
                steppedFrames = steppedFrames, steppedSeconds = steppedFrames * StepSeconds,
                gesture = performance.Gesture.ToString(), gestureProgress = performance.Progress,
                cameraYaw = yaw, finiteBounds = true, nativeRig = performance.UsesNativeRig,
                seated = performance.IsSeated, sourceMeshesUnchanged = true,
                boundsCenter = bounds.center, boundsSize = bounds.size,
                leftHand = performance.LeftHandPoint, rightHand = performance.RightHandPoint,
                head = performance.HeadJoint.position,
                leftArmRotation = rig.Joint("LeftArm").localRotation,
                rightArmRotation = rig.Joint("RightArm").localRotation,
                leftKneeRotation = rig.Joint("LeftKnee").localRotation
            };
        }

        void WriteReport(string status, string failure)
        {
            var report = new CaptureReport {
                character = character, resource = CurrentResource, unityVersion = Application.unityVersion, status = status, error = failure,
                nativeRig = performance && performance.UsesNativeRig,
                validationScope = "Finite transforms, source mesh references and materials; screenshots still require visual review.",
                importedMotion = rig && rig.HasImportedMotion,
                authoredIdle = rig && rig.Motion && rig.Motion.HasAuthoredIdle,
                authoredSeat = rig && rig.Motion && rig.Motion.HasAuthoredSeat,
                walkingClip = rig && rig.Motion ? rig.Motion.WalkingClip : null,
                runningClip = rig && rig.Motion ? rig.Motion.RunningClip : null,
                idleClip = rig && rig.Motion ? rig.Motion.IdleClip : null,
                seatedClip = rig && rig.Motion ? rig.Motion.SeatedClip : null,
                upperRig = performance && performance.HasUpperRig, elbowRig = performance && performance.HasElbowRig,
                seatedRig = performance && performance.HasSeatedRig, faceMotion = performance && performance.HasFaceMotion,
                muted = AudioListener.pause && AudioListener.volume == 0, fixedStep = StepSeconds,
                skinCount = sourceMeshes.Count, captures = captures.ToArray()
            };
            foreach (var pair in sourceMeshes)
            {
                if (pair.Value) report.vertexCount += pair.Value.vertexCount;
                if (!pair.Key) continue;
                report.boneCount += pair.Key.bones.Length;
                foreach (var material in pair.Key.sharedMaterials)
                    if (!material || !material.shader || !material.shader.isSupported) report.missingMaterials++;
            }
            if (status == "passed" && report.missingMaterials != 0)
                throw new InvalidOperationException("人物存在缺失或不可用的材质。");
            File.WriteAllText(Path.Combine(outputDirectory, "capture-state.json"), JsonUtility.ToJson(report, true));
        }

        void Fail(Exception exception)
        {
            error = exception.Message; ready = false;
            Debug.LogException(exception);
            if (!captureMode) { UnloadSubject(); sourceMeshes.Clear(); return; }
            try
            {
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    outputDirectory = Path.GetFullPath(outputDirectory);
                    Directory.CreateDirectory(outputDirectory);
                    WriteReport("failed", exception.ToString());
                }
            }
            catch (Exception reportFailure) { Debug.LogException(reportFailure); }
            Application.Quit(1);
        }

        static bool Finite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.z));
        }
        static void RequireBounds(Bounds value)
        {
            if (!Finite(value.center) || !Finite(value.size) || value.size.y < .3f || value.size.y > 3f || value.size.x > 4 || value.size.z > 4)
                throw new InvalidOperationException("人物尺寸异常：" + value);
        }

        void CaptureCamera(string path, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            RenderTexture oldActive = RenderTexture.active, oldTarget = viewCamera.targetTexture;
            float oldAspect = viewCamera.aspect;
            Texture2D pixels = null;
            try
            {
                target.Create(); viewCamera.targetTexture = target; viewCamera.aspect = (float)width / height;
                viewCamera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply(false);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                viewCamera.targetTexture = oldTarget; viewCamera.aspect = oldAspect; RenderTexture.active = oldActive;
                if (pixels) Destroy(pixels);
                target.Release(); Destroy(target);
            }
        }

        static bool HasArgument(string flag)
        { foreach (string value in Environment.GetCommandLineArgs()) if (value == flag) return true; return false; }
        static string ArgumentValue(string flag)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++) if (arguments[i] == flag) return arguments[i + 1];
            return null;
        }
    }
}
