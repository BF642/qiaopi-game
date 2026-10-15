using UnityEngine;

namespace Qiaopi
{
    /// <summary>Physical addresses shared by the investigation, scenery and route checks.</summary>
    public static class InquiryPlaces
    {
        public static readonly string[] Routes = { "trace_desk", "trace_harbor", "trace_family" };
        public static string World(string route) => route == "trace_harbor" ? "port" : route == "trace_family" ? "quarters" : "postoffice";
        public static Vector3 Evidence(string route, int index)
        {
            index = Mathf.Clamp(index, 0, 2);
            if (route == "trace_harbor") return index == 0 ? P(-47, 15) : index == 1 ? P(-59, -8) : P(-44, -35);
            if (route == "trace_family") return index == 0 ? P(17, 44) : index == 1 ? P(44, 43) : P(44, 8);
            return index == 0 ? P(16, 44) : index == 1 ? P(43, 44) : P(44, 10);
        }
        public static string Title(string route, int index)
        {
            index = Mathf.Clamp(index, 0, 2);
            if (route == "trace_harbor") return new[] { "点收间", "经手间", "船期簿房" }[index];
            if (route == "trace_family") return new[] { "读信间", "同乡小院", "旧址登记间" }[index];
            return new[] { "留底账房", "转交簿房", "封袋复核间" }[index];
        }
        public static Vector3 Return(string route) => route == "trace_harbor" ? P(23, 15) : route == "trace_family" ? P(22, 14) : P(-21, 10);
        public static Vector3 DocumentPoint(string route, int index) => Evidence(route, index) + new Vector3(0, .80f, 1.25f);
        public static Vector3[] PedestrianRoute(string world, int index)
        {
            if (world == "port") return index == 0 ? new[] { P(-38,-20), P(-50,-20), P(-50,5), P(-38,5) } : new[] { P(-66,-18), P(-66,-39.8f), P(-53,-39.8f), P(-53,-18) };
            if (world == "quanzhou") return index == 0 ? new[] { P(37,3), P(52,3), P(52,29), P(40,29) } : new[] { P(-25.5f,47), P(-25.5f,42), P(-17,47), P(-17,42) };
            return index == 0 ? new[] { P(0,39), P(7,39), P(7,49), P(0,49) } : new[] { P(52,-20), P(52,25), P(38,25), P(38,20), P(52,20) };
        }
        static Vector3 P(float x, float z) => new Vector3(x, 0, z);
    }
}
