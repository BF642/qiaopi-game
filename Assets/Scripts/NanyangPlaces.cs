using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>Position is the visitor's clear standing point, never the furniture centre.</summary>
    public sealed class NanyangPlace
    {
        public readonly string world, role, title;
        public readonly Vector3 position, facePoint;

        public NanyangPlace(string world, string role, string title, float x, float z,
            float faceX, float faceY, float faceZ)
        {
            this.world = world; this.role = role; this.title = title;
            position = new Vector3(x, 0, z); facePoint = new Vector3(faceX, faceY, faceZ);
        }
    }

    /// <summary>
    /// Authored in the existing four overseas courtyards, shared by all three destinations.
    /// Art and story both use these metre coordinates. Ground height is resolved on spawning,
    /// using WorldFactory.CharacterGroundPosition; do not spawn a desk at position.
    /// </summary>
    public static class NanyangPlaces
    {
        static readonly NanyangPlace[] places =
        {
            // The communal kitchen opens at (22,-18.5). The centre aisle remains empty.
            new NanyangPlace("quarters", "meal", "灶间饭摊", 24.1f, -13.8f, 24.1f, .98f, -12.2f),
            new NanyangPlace("quarters", "vendor", "饭摊乡亲", 24.1f, -10.9f, 24.1f, 1.3f, -13.8f),
            new NanyangPlace("quarters", "rest", "自己的床位", -27.5f, 4.1f, -29.05f, .55f, 4.1f),
            new NanyangPlace("quarters", "homeletter", "床头家信夹", -27.5f, 5.6f, -28.12f, 1.0f, 5.95f),
            new NanyangPlace("quarters", "colleague", "通铺院的乡亲", -24.5f, 10.5f, -22.5f, 1.3f, 9),
            new NanyangPlace("quarters", "reply", "领取家中回批", 22, 14, 22, 1.3f, 14),
            new NanyangPlace("quarters", "employer", "住处接应人", -22, 8, -22, 1.3f, 8),

            // Chen's court: the writing room occupies the west; these use the east edge.
            new NanyangPlace("market", "accounts", "陈记核账台", -14.6f, 15.1f, -14.6f, .95f, 16.55f),
            new NanyangPlace("market", "deliverylist", "货架交货清单", -14.6f, 18.7f, -12.21f, 1.14f, 18.7f),
            new NanyangPlace("market", "colleague", "账房外的同乡", -17.5f, 14.5f, -19, 1.3f, 15),
            new NanyangPlace("market", "employer", "陈记掌柜", -20, 19, -20, 1.3f, 19),

            // Inside the existing northern tally shelter, clear of both delivery circles.
            new NanyangPlace("port", "accounts", "工班工账台", -27.4f, 27.1f, -27.4f, .98f, 28.65f),
            new NanyangPlace("port", "count", "货物点数处", -29.8f, 28.5f, -30.5f, .70f, 30.65f),
            new NanyangPlace("port", "colleague", "点收棚的工友", -23.8f, 26.7f, -26, 1.3f, 27.1f),
            new NanyangPlace("port", "employer", "码头管事", 23, 15, 23, 1.3f, 15),

            // Furnish existing work surfaces; keep x[18,26] in the east hall clear.
            new NanyangPlace("postoffice", "receive", "收寄分拣处", -26.8f, -14, -28.05f, 1.04f, -13.9f),
            new NanyangPlace("postoffice", "archive", "银信留底台", -15.9f, 29, -14.1f, 1.02f, 29),
            new NanyangPlace("postoffice", "verify", "待核批件台", 13.3f, -.6f, 11.5f, 1.04f, -.6f),
            new NanyangPlace("postoffice", "reply", "回批架", 26.9f, 1, 27.36f, 1.21f, 1),
            new NanyangPlace("postoffice", "colleague", "留底院的同乡", -18, 28, -20, 1.3f, 28),
            new NanyangPlace("postoffice", "employer", "批局柜上人", -21, 10, -21, 1.3f, 10)
        };

        public static bool TryGet(string world, string role, out NanyangPlace place)
        {
            // Evidence and witness are story roles, sharing the same physical work station.
            if (role == "evidence") role = world == "postoffice" ? "archive" : "accounts";
            if (role == "witness") role = "colleague";
            if (role == "home") role = "rest";
            foreach (var candidate in places)
                if (candidate.world == world && candidate.role == role) { place = candidate; return true; }
            place = null; return false;
        }

        public static NanyangPlace Get(string world, string role)
        {
            if (TryGet(world, role, out var place)) return place;
            throw new ArgumentException("No Nanyang place for " + world + "/" + role);
        }

        public static NanyangPlace[] ForWorld(string world)
        {
            var result = new List<NanyangPlace>();
            foreach (var place in places) if (place.world == world) result.Add(place);
            return result.ToArray();
        }
    }
}
