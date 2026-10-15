using UnityEngine;
using UnityEngine.Rendering;

namespace Qiaopi
{
    public partial class WorldGame
    {
        GameObject firstDayCompanion;
        FirstDayLightState firstDayLightState;

        // These are standing points in existing working aisles, not new props or
        // destinations. Leave the delivery marker and the employer clear.
        Vector3 FirstDayContact
        {
            get
            {
                string route = state != null && state.firstDay != null ? state.firstDay.route : "";
                if (route == "dock" || loadedWorld == "port") return new Vector3(18.4f, 0, 21.2f);
                if (route == "shop" || loadedWorld == "market") return NanyangPlaces.Get("market","colleague").position;
                if (route == "courier" || loadedWorld == "postoffice") return new Vector3(18, 0, 13);
                return new Vector3(-22, 0, 8);
            }
        }

        Vector3 FirstDayBedPoint => new Vector3(-27.5f, 0, 4.1f);

        string FirstDayTimeLabel
        {
            get
            {
                if (!FirstDayChapter.IsActive(state)) return "";
                switch (state.firstDay.stage)
                {
                    case "evening": return "1906年秋 · 首日傍晚";
                    case "letter": case "bed": return "1906年秋 · 首日入夜";
                    case "morning": return "1906年秋 · 翌晨";
                    case "reply": case "complete": return "1906年秋 · 数周后";
                    default: return "1906年秋 · 首日午后";
                }
            }
        }

        void SetupFirstDayArt()
        {
            if (firstDayCompanion)
            {
                firstDayCompanion.SetActive(false);
                Destroy(firstDayCompanion);
            }
            firstDayCompanion = null;
            if (!actors || !FirstDayChapter.IsActive(state)) return;
            string stage = state.firstDay.stage;
            if (stage != "work" && stage != "encounter") return;
            string route = state.firstDay.route;
            if (!(route == "dock" && loadedWorld == "port") &&
                !(route == "shop" && loadedWorld == "market") &&
                !(route == "courier" && loadedWorld == "postoffice")) return;

            Vector3 ground = WorldFactory.CharacterGroundPosition(FirstDayContact);
            firstDayCompanion = ModelLibrary.Spawn("Xusheng", actors.transform, ground);
            if (!firstDayCompanion) return;
            firstDayCompanion.name = "许生";
            Vector3 facing = mission.destination - ground;
            facing.y = 0;
            if (facing.sqrMagnitude < .01f) facing = Vector3.forward;
            firstDayCompanion.transform.rotation = Quaternion.LookRotation(facing);
            firstDayCompanion.AddComponent<CharacterIdle>();
        }

        void FirstDayContactTag()
        {
            if (firstDayCompanion && FirstDayChapter.IsActive(state) && state.firstDay.stage == "encounter")
                Tag(firstDayCompanion.transform.position + Vector3.up * 2.14f, "许生", true);
        }

        // Restore before every change, so revisiting a phase never compounds a
        // tint. ConfigureLandscape may already have prepared a different world;
        // in that case its new sky and fog must not be replaced by the old ones.
        void UpdateFirstDayLighting()
        {
            RestoreFirstDayLighting();
            if (!world || (!FirstDayChapter.IsActive(state)&&!NanyangIncident)) return;
            string stage = FirstDayChapter.IsActive(state)?state.firstDay.stage:NanyangStage=="waiting"?"letter":NanyangStage;
            bool evening = stage == "evening";
            bool night = stage == "letter" || stage == "bed";
            bool morning = stage == "morning";
            if (!evening && !night && !morning) return;

            var sunObject = GameObject.Find("Afternoon Sun");
            Light sun = sunObject ? sunObject.GetComponent<Light>() : null;
            Light lamp = null;
            if (writingPlace != null)
                foreach (var candidate in world.GetComponentsInChildren<Light>(true))
                    if (candidate.name == "Warm oil-lamp light" &&
                        (candidate.transform.position - writingPlace.lookAt).sqrMagnitude < 64)
                    { lamp = candidate; break; }
            firstDayLightState = new FirstDayLightState(world, cam, sun, lamp, landscapeSky);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = C(night ? "647686" : evening ? "798B98" : "94A1A7");
            RenderSettings.ambientEquatorColor = C(night ? "625D56" : evening ? "786E61" : "817F70");
            RenderSettings.ambientGroundColor = C(night ? "42443E" : evening ? "4E4D43" : "585749");
            if (sun)
            {
                sun.color = C(night ? "ADBAC6" : evening ? "F6C99A" : "FFF0D8");
                sun.intensity = night ? .28f : evening ? .70f : .88f;
                sun.transform.rotation = Quaternion.Euler(night ? 8 : evening ? 18 : 22, morning ? -112 : -64, 0);
            }
            Color horizon = C(night ? "9D9588" : evening ? "C2A98D" : "D6CEC0");
            if (landscapeSky)
            {
                landscapeSky.SetColor("_Zenith", C(night ? "556676" : evening ? "798E9C" : "8AAAB6"));
                landscapeSky.SetColor("_Horizon", horizon);
            }
            RenderSettings.fogColor = horizon;
            if (cam) cam.backgroundColor = horizon;
            var water = world.GetComponentInChildren<SeaEnvironment>();
            if (water) water.SetHorizon(horizon, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance);
            if (lamp && (evening || night))
            {
                lamp.enabled = true;
                lamp.intensity = night ? 1.05f : .88f;
                lamp.range = 4.2f;
                lamp.color = new Color(1, .83f, .61f);
                lamp.renderMode = LightRenderMode.ForcePixel;
                lamp.cullingMask &= ~(1 << PortraitLayer);
            }
        }

        void RestoreFirstDayLighting()
        {
            if (firstDayLightState == null) return;
            var saved = firstDayLightState;
            saved.Restore(world, cam);
            firstDayLightState = null;
        }

        sealed class FirstDayLightState
        {
            readonly GameObject world;
            readonly AmbientMode ambientMode;
            readonly Color sky, equator, ground, fog, cameraBackground, zenith, horizon;
            readonly Material skyMaterial;
            readonly Light sun, lamp;
            readonly Color sunColor, lampColor;
            readonly float sunIntensity, lampIntensity, lampRange;
            readonly Quaternion sunRotation;
            readonly bool lampEnabled;
            readonly LightRenderMode lampRenderMode;
            readonly int lampMask;

            public FirstDayLightState(GameObject world, Camera camera, Light sun, Light lamp, Material skyMaterial)
            {
                this.world = world; this.sun = sun; this.lamp = lamp; this.skyMaterial = skyMaterial;
                ambientMode = RenderSettings.ambientMode;
                sky = RenderSettings.ambientSkyColor; equator = RenderSettings.ambientEquatorColor;
                ground = RenderSettings.ambientGroundColor; fog = RenderSettings.fogColor;
                cameraBackground = camera ? camera.backgroundColor : fog;
                if (skyMaterial) { zenith = skyMaterial.GetColor("_Zenith"); horizon = skyMaterial.GetColor("_Horizon"); }
                if (sun) { sunColor = sun.color; sunIntensity = sun.intensity; sunRotation = sun.transform.rotation; }
                if (lamp)
                {
                    lampEnabled = lamp.enabled; lampColor = lamp.color; lampIntensity = lamp.intensity;
                    lampRange = lamp.range; lampRenderMode = lamp.renderMode; lampMask = lamp.cullingMask;
                }
            }

            public void Restore(GameObject currentWorld, Camera camera)
            {
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
                if (sun) { sun.color = sunColor; sun.intensity = sunIntensity; sun.transform.rotation = sunRotation; }
                if (lamp)
                {
                    lamp.enabled = lampEnabled; lamp.color = lampColor; lamp.intensity = lampIntensity;
                    lamp.range = lampRange; lamp.renderMode = lampRenderMode; lamp.cullingMask = lampMask;
                }
                if (world != currentWorld) return;
                if (skyMaterial) { skyMaterial.SetColor("_Zenith", zenith); skyMaterial.SetColor("_Horizon", horizon); }
                RenderSettings.fogColor = fog;
                if (camera) camera.backgroundColor = cameraBackground;
                var water = currentWorld ? currentWorld.GetComponentInChildren<SeaEnvironment>() : null;
                if (water) water.SetHorizon(fog, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance);
            }
        }
    }
}
