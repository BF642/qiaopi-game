using UnityEngine;
namespace Qiaopi
{
    // A model belongs to one destination and one use; missing entries keep their authored building.
    public sealed class RegionalBuilding : MonoBehaviour
    {
        public string destination, function, assetId;
        public static bool Spawn(string destination, string function, Transform parent, Vector3 frontCentre, float yaw = 0)
        {
            string asset = null;
            if (destination == "singapore" && function == "general_store") asset = "SingaporeGeneralStore";
            if (destination == "penang" && function == "remittance_office") asset = "PenangRemittanceOffice";
            if (destination == "semarang" && function == "warehouse") asset = "SemarangWarehouse";
            if (asset == null) return false;
            var prefab = Resources.Load<GameObject>("EnvironmentArt/" + asset + "/Model");
            if (!prefab) return false;
            var instance = Instantiate(prefab, parent, false);
            instance.name = destination + " · " + function;
            instance.transform.localPosition = frontCentre;
            instance.transform.localRotation = Quaternion.Euler(0,yaw,0);
            return true;
        }
    }
}
