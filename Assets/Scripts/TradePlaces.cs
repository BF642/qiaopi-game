using UnityEngine;

namespace Qiaopi
{
    /// <summary>Clear standing points, in metres. Face points belong to the object/person,
    /// not the visitor. The compact dock run never crosses the main cart road.</summary>
    public static class TradePlaces
    {
        public static Vector3 DockPickup(int i) => new Vector3(-17, 0, 10 + 4 * Mathf.Clamp(i, 0, 2));
        public static Vector3 DockDrop(int zone) => new Vector3(-9, 0, 10 + 4 * Mathf.Clamp(zone, 0, 2));
        public static Vector3 DockPickupFace(int i) => DockPickup(i) + new Vector3(-1.35f, .73f, 0);
        public static Vector3 DockDropFace(int zone) => DockDrop(zone) + new Vector3(1.45f, .8f, 0);
        public static string DockZone(int zone) => zone == 0 ? "米粮栈" : zone == 1 ? "布匹车" : "纸货雨棚";
        public static string DockZone(GameState state,int zone) => LifeJourney.DestinationId(state)=="semarang"
            ? zone==0?"糖货暂存区":zone==1?"布匹交接区":"糖货避雨区" : DockZone(zone);
        public static Vector3 ShopCounter => new Vector3(-14.6f, 0, 15.1f);
        public static Vector3 ShopFace => new Vector3(-14.6f, 1.2f, 13.5f);
        public static Vector3 CourierRead => new Vector3(-26.8f, 0, -14);
        public static Vector3 CourierReadFace => new Vector3(-28.05f, 1.04f, -13.9f);
        public static Vector3 CourierNeighbour => new Vector3(-10, 0, -23.6f);
        public static Vector3 CourierRecipient(int i) => i == 0 ? new Vector3(-30.5f, 0, -26.2f) : new Vector3(23.5f, 0, 27.3f);
        public static Vector3 CourierFace(int i) => CourierRecipient(i) + Vector3.up * 1.45f;
        public static string CourierAddress(int i) => i == 0 ? "南街三号" : "北巷七号";
        public static Vector3[] ForWorld(string world)
        {
            if (world == "port") return new[] { DockPickup(0), DockPickup(1), DockPickup(2), DockDrop(0), DockDrop(1), DockDrop(2) };
            if (world == "market") return new[] { ShopCounter };
            if (world == "postoffice") return new[] { CourierRead, CourierNeighbour, CourierRecipient(0), CourierRecipient(1) };
            return new Vector3[0];
        }
    }
}
