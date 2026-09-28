using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        Material landscapeSky;

        void ConfigureLandscape(string place)
        {
            if (!landscapeSky) landscapeSky=new Material(Resources.Load<Shader>("LandscapeSky"));
            bool sea=place=="ship", coast=sea||place=="harbor"||place=="port";
            Color horizon=C(coast?"BDCFD0":"C5D0BE");
            landscapeSky.SetColor("_Zenith",C(coast?"729FB4":"88ACB6"));
            landscapeSky.SetColor("_Horizon",horizon);
            RenderSettings.skybox=landscapeSky;
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=horizon;
            RenderSettings.fogStartDistance=sea?280:105;
            RenderSettings.fogEndDistance=sea?850:310;
            cam.clearFlags=CameraClearFlags.Skybox;
            cam.backgroundColor=horizon;cam.farClipPlane=1200;
            var water=world?world.GetComponentInChildren<SeaEnvironment>():null;
            if(water)water.SetHorizon(horizon,sea?280:105,sea?850:310);
        }

    }
}
