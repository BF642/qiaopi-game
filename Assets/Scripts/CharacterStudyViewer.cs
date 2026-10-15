using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    // A standalone art viewer. This component has no dependency on WorldGame,
    // story state, the save store, or any preference storage.
    public sealed class CharacterStudyViewer : MonoBehaviour
    {
        public string captureDirectory;
        public float modelYaw;
        const string ModelResource = "Models/WenshengStudy";
        const float CharacterHeight = 1.72f;
        Camera studyCamera;
        Renderer studioFloor;
        Transform subject;
        Font font;
        float yaw, pitch = 5f, distance = 1.22f, targetHeight = 1.46f;
        bool captureMode, dragging, ready;
        string error;
        GUIStyle titleStyle, hintStyle, buttonStyle, errorStyle;

        void Awake()
        {
            Silence();
            captureMode = HasArgument("-qiaopi-study-capture");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            try
            {
                CreateStudio();
                LoadSubject();
                font = Resources.Load<Font>("Fonts/Body");
                Frame(false);
                ready = true;
                Debug.Log("QIAOPI_STUDY_READY resource=" + ModelResource + " height=" + CharacterHeight);
                if (captureMode) StartCoroutine(CaptureViews());
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogException(exception);
                if (captureMode) Application.Quit(1);
            }
        }

        static void Silence()
        {
            AudioListener.pause = true;
            AudioListener.volume = 0f;
        }

        void LoadSubject()
        {
            var prefab = Resources.Load<GameObject>(ModelResource);
            if (!prefab) throw new InvalidOperationException("未找到人物样稿，请先导入 WenshengStudy 模型。");
            // Imported FBX data should never carry gameplay behaviours. Check
            // before instantiation, so a future prefab cannot run save/audio code.
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("人物样稿只能包含模型，不能附带游戏脚本。");
            subject = new GameObject("Character study subject").transform;
            Instantiate(prefab, subject, false);
            subject.rotation = Quaternion.Euler(0f, modelYaw, 0f);
            foreach (var source in subject.GetComponentsInChildren<AudioSource>(true))
            {
                source.Stop(); source.playOnAwake = false; source.enabled = false;
            }
            foreach (var listener in subject.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            foreach (var animator in subject.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var collider in subject.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Bounds bounds = SubjectBounds();
            if (bounds.size.y < .01f || float.IsNaN(bounds.size.y) || float.IsInfinity(bounds.size.y))
                throw new InvalidOperationException("人物样稿没有有效的模型尺寸。");
            subject.localScale = Vector3.one * (CharacterHeight / bounds.size.y);
            bounds = SubjectBounds();
            subject.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = SubjectBounds();
            if (Mathf.Abs(bounds.size.y - CharacterHeight) > .005f)
                throw new InvalidOperationException("人物样稿未能归一到 1.72 米。");
            Debug.Log("QIAOPI_STUDY_BOUNDS=" + bounds);
        }

        Bounds SubjectBounds()
        {
            var local = ModelLibrary.LocalBounds(subject);
            Bounds combined = new Bounds(subject.TransformPoint(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var point = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                combined.Encapsulate(subject.TransformPoint(point));
            }
            return combined;
        }

        void CreateStudio()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.43f, .43f, .42f);
            RenderSettings.ambientEquatorColor = new Color(.37f, .37f, .36f);
            RenderSettings.ambientGroundColor = new Color(.28f, .27f, .25f);
            RenderSettings.reflectionIntensity = .12f;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 15f;
            QualitySettings.pixelLightCount = 4;
            var cameraObject = new GameObject("Character study camera");
            studyCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            studyCamera.clearFlags = CameraClearFlags.SolidColor;
            studyCamera.backgroundColor = new Color(.43f, .415f, .38f);
            studyCamera.fieldOfView = 30f;
            studyCamera.nearClipPlane = .025f;
            studyCamera.farClipPlane = 30f;
            studyCamera.allowHDR = true;
            studyCamera.allowMSAA = true;
            // Deliberately no AudioListener or audio clips in this scene.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Quiet studio floor";
            floor.transform.position = new Vector3(0f, -.055f, 0f);
            floor.transform.localScale = Vector3.one * 1.5f;
            studioFloor = floor.GetComponent<Renderer>();
            studioFloor.sharedMaterial = StudioMaterial("Studio floor", new Color(.32f, .305f, .275f));
            floor.GetComponent<Collider>().enabled = false;
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Character study plinth";
            plinth.transform.position = new Vector3(0f, -.025f, 0f);
            plinth.transform.localScale = new Vector3(1.08f, .025f, 1.08f);
            plinth.GetComponent<Renderer>().sharedMaterial = StudioMaterial("Studio plinth", new Color(.27f, .29f, .28f));
            plinth.GetComponent<Collider>().enabled = false;
            StudioLight("Broad key", new Vector3(-2.5f, 3f, 5f), new Color(1f, .96f, .90f), .54f, true);
            StudioLight("Soft fill", new Vector3(3f, 2.1f, 5f), new Color(.93f, .96f, 1f), .36f, false);
            StudioLight("Hair rim", new Vector3(1f, 3f, -3f), new Color(1f, .96f, .90f), .28f, false);
        }

        static Material StudioMaterial(string name, Color colour)
        {
            var shader = Shader.Find("Standard");
            if (!shader) throw new InvalidOperationException("人物样稿缺少 Standard 材质着色器。");
            var material = new Material(shader) { name = name, color = colour };
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", .12f);
            return material;
        }

        static void StudioLight(string name, Vector3 position, Color colour, float intensity, bool shadows)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.position = position;
            lightObject.transform.LookAt(new Vector3(0f, 1.2f, 0f));
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = colour;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = .32f;
            light.shadowBias = .025f;
            light.shadowNormalBias = .05f;
        }

        void Update()
        {
            Silence();
            if (!ready || captureMode) return;
            if (Input.GetKeyDown(KeyCode.F)) Frame(true);
            if (Input.GetKeyDown(KeyCode.H)) Frame(false);
            if (Input.GetKeyDown(KeyCode.Escape)) ReleasePointer();
            if (Input.GetMouseButtonDown(1))
            {
                dragging = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (dragging && Input.GetMouseButton(1))
            {
                yaw += Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -30f, 30f) * 2.5f;
                pitch = Mathf.Clamp(pitch - Mathf.Clamp(Input.GetAxisRaw("Mouse Y"), -30f, 30f) * 2f, -15f, 40f);
            }
            if (dragging && !Input.GetMouseButton(1)) ReleasePointer();
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > .001f)
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(-scroll * .12f), .62f, 4.8f);
                targetHeight = Mathf.Lerp(1.56f, .90f, Mathf.InverseLerp(.62f, 3.75f, distance));
            }
            PositionCamera();
        }

        void Frame(bool fullBody)
        {
            ReleasePointer();
            yaw = 0f; pitch = fullBody ? 2f : 5f;
            distance = fullBody ? 3.75f : 1.22f;
            targetHeight = fullBody ? .90f : 1.46f;
            PositionCamera();
        }

        void PositionCamera()
        {
            if (!studyCamera) return;
            // Portraits have a continuous neutral backdrop; the ground remains
            // visible when viewing the body and its real foot contact.
            if (studioFloor) studioFloor.enabled = distance > 2.2f;
            Vector3 focus = new Vector3(0f, targetHeight, 0f);
            Vector3 offset = Quaternion.Euler(-pitch, yaw, 0f) * new Vector3(0f, 0f, distance);
            studyCamera.transform.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset, Vector3.up));
        }

        void ReleasePointer()
        {
            dragging = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnApplicationFocus(bool focused) { if (!focused) ReleasePointer(); }
        void OnDisable() { ReleasePointer(); }

        void OnGUI()
        {
            if (captureMode) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 24, normal = { textColor = new Color(.92f, .90f, .85f) } };
                hintStyle = new GUIStyle(titleStyle) { fontSize = 15, normal = { textColor = new Color(.75f, .79f, .79f) } };
                buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = 19 };
                errorStyle = new GUIStyle(titleStyle) { fontSize = 20, wordWrap = true };
            }
            float scale = Mathf.Clamp(Screen.width / 1440f, .7f, 1.5f);
            Matrix4x4 before = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.Label(new Rect(30f, 22f, 340f, 40f), "人物样稿", titleStyle);
            GUI.Label(new Rect(30f, 64f, 520f, 32f), "鼠标右键旋转 · 滚轮缩放 · 静音", hintStyle);
            if (!string.IsNullOrEmpty(error)) GUI.Label(new Rect(40f, height * .4f, width - 80f, 160f), error, errorStyle);
            if (ready)
            {
                Color oldBackground = GUI.backgroundColor;
                GUI.backgroundColor = new Color(.30f, .36f, .37f, .88f);
                if (GUI.Button(new Rect(width * .5f - 154f, height - 76f, 144f, 46f), "全身 F", buttonStyle)) Frame(true);
                if (GUI.Button(new Rect(width * .5f + 10f, height - 76f, 144f, 46f), "近景 H", buttonStyle)) Frame(false);
                GUI.backgroundColor = oldBackground;
            }
            GUI.matrix = before;
        }

        IEnumerator CaptureViews()
        {
            string output = ArgumentValue("-study-output") ?? captureDirectory;
            Exception failure = null;
            try
            {
                if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("请通过 -study-output 指定样稿截图目录。");
                output = Path.GetFullPath(output);
                Directory.CreateDirectory(output);
            }
            catch (Exception exception) { failure = exception; }
            if (failure != null) { Debug.LogException(failure); Application.Quit(1); yield break; }
            yield return null;
            string[] names = { "face", "front", "profile", "body" };
            float[] distances = { .70f, 1.22f, .80f, 3.75f };
            float[] heights = { 1.56f, 1.46f, 1.56f, .90f };
            for (int i = 0; i < names.Length; i++)
            {
                yaw = names[i] == "profile" ? 90f : 0f;
                // Keep portrait cameras at eye level: a camera below the eyes
                // turns a neutral gaze into an upward stare and exposes nostrils.
                pitch = names[i] == "body" ? 2f : names[i] == "front" ? 5f : 0f;
                distance = distances[i]; targetHeight = heights[i]; PositionCamera();
                yield return null;
                try
                {
                    string file = Path.Combine(output, names[i] + ".png");
                    CaptureCamera(file, 1200, 1500);
                    Debug.Log("QIAOPI_STUDY_CAPTURE=" + file);
                }
                catch (Exception exception) { failure = exception; }
                if (failure != null) { Debug.LogException(failure); Application.Quit(1); yield break; }
            }
            Debug.Log("QIAOPI_STUDY_CAPTURE_COMPLETE=" + output);
            Application.Quit(0);
        }

        void CaptureCamera(string path, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = studyCamera.targetTexture;
            float previousAspect = studyCamera.aspect;
            Texture2D image = null;
            try
            {
                target.Create();
                studyCamera.targetTexture = target;
                studyCamera.aspect = (float)width / height;
                studyCamera.Render();
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply(false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                studyCamera.targetTexture = previousTarget;
                studyCamera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                if (image) Destroy(image);
                target.Release(); Destroy(target);
            }
        }

        static bool HasArgument(string name)
        {
            foreach (string value in Environment.GetCommandLineArgs()) if (value == name) return true;
            return false;
        }

        static string ArgumentValue(string name)
        {
            string[] values = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < values.Length; i++) if (values[i] == name) return values[i + 1];
            return null;
        }
    }
}
