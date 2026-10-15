using UnityEngine;

namespace Qiaopi
{
    public sealed class AmbientStop
    {
        public Vector3 position, facePoint;
        public string activity;
        public float dwellSeconds;
        // null: keep carrying; empty: put down; otherwise a named physical load.
        // Apply after the dwell finishes, so collecting and handing over remain visible.
        public string carryAfterStop;
    }

    public sealed class AmbientSchedule
    {
        public string id, title, model;
        public float speed;
        // Spawn at this stop, execute it, then advance modulo stops.Length.
        // Initial cargo is the last non-null carryAfterStop before this index,
        // searching backwards cyclically. Do not interpret a waypoint as unloading.
        public int startIndex;
        public AmbientStop[] stops;
    }

    /// <summary>
    /// Purposeful, cyclic daily work on the authored metre-scale street plans.
    /// Points have y=0; movement resolves paving height and each leg with WalkPath.Find.
    /// Empty activities are transit points, never timed poses. The final leg returns
    /// to stop zero; explicit return waypoints keep that leg on the same streets.
    /// No core family/hero models, dialogue identities, missions or save flags are used.
    /// DestinationWorld's three overseas variants share this physical street layout.
    /// </summary>
    public static class AmbientSchedules
    {
        public static AmbientSchedule[] Get(string world)
        {
            switch (world)
            {
                case "quanzhou": return Quanzhou();
                case "harbor": return Harbor();
                case "ship": return Ship();
                case "port": return Port();
                case "market": return Market();
                case "quarters": return Quarters();
                case "postoffice": return PostOffice();
                default: return new AmbientSchedule[0];
            }
        }

        static AmbientSchedule[] Quanzhou()
        {
            // ExpandedSettlements: x=0 village spine, z=-22 west lane, z=8 east lane.
            // The z=5.6 lane is between Chen's rear wall z=3 and cottage fronts z>=7.
            // Never enter the family gathering at (-23,-5), writing room (-17.25,-8),
            // ancestral-court gathering (21,18), or the farewell circle (0,30).
            return new[] {
                DistrictS("quanzhou", 0, "qz_grain", "收送米筐的乡亲", "Worker", .92f, 0,
                    "在东巷米架前提起米筐", "grain", P(35.7f,3),
                    "把米筐送到东巷晒场", P(50.7f,29)),
                S("qz_mail", "递送乡信的乡亲", "Clerk", 1.02f, 3,
                    At(21,8.7f,"接过堂屋捎来的信",7,"letters",21,11),
                    Via(8,8),Via(0,8),Via(0,5.6f),Via(-20,5.6f),
                    At(-27,5.8f,"在屋门外交付乡信",10,"",-27,7.5f),
                    Via(-20,5.6f),Via(0,5.6f),Via(0,8),Via(8,8)),
                S("qz_field", "照料田地的村民", "Uncle", .74f, 0,
                    At(-12,21,"在田埂边收拾菜篮",12,"basket",-17,22),
                    Via(-10,19),Via(-5,19),Via(-5,5.6f),
                    At(-12,5.5f,"把菜篮送到屋门前",9,"",-12,7),
                    Via(-5,5.6f),Via(-5,19),Via(-10,19)),
                S("qz_yard", "照看晒场的伙计", "Guide", .88f, 3,
                    At(29,-24,"在屋门外领取空筐",7,"basket",29,-22.5f),
                    Via(21,-24),Via(5,-24),Via(5,4),
                    At(24,3.8f,"在晒场边归置米筐",11,"",25.6f,.7f),
                    Via(5,4),Via(5,-24),Via(21,-24)),
                S("qz_water", "送水的邻居", "Xusheng", .84f, 4,
                    At(9,-10.1f,"从晒场水缸舀水",10,"water",7.5f,-10.1f),
                    Via(9,-12),Via(5,-12),Via(5,-20),Via(-20,-20),
                    At(-23,-19,"把水送到陈家院门外",9,"",-23,-17),
                    Via(-20,-20),Via(5,-20),Via(5,-12),Via(9,-12))
            };
        }

        static AmbientSchedule[] Harbor()
        {
            // ExpandedHarbors: west x=-23, main x=0, cross-street z=-21.
            // All activity remains on the stone shore (z<21.3); nobody enters the
            // boarding pier or the writing room centred at (14,9).
            return new[] {
                S("harbor_parcels", "搬行李的脚夫", "Worker", .93f, 0,
                    At(-26,-18,"拾起待存的行李",8,"parcel",-28,-18),
                    Via(-23,-18),Via(-23,3.7f),
                    At(-29,4,"把行李送到货栈门外",10,"",-29,5.5f),
                    Via(-23,3.7f),Via(-23,-18)),
                S("harbor_tickets", "核查船票的伙计", "Clerk", .88f, 3,
                    At(-14,1.6f,"领取验票簿",8,"ledger",-14,3.5f),
                    Via(0,1.6f),Via(0,-18),Via(12,-18),
                    At(15,-16.1f,"在问讯屋门前交接名单",12,"",15,-14.5f),
                    Via(12,-18),Via(0,-18),Via(0,1.6f)),
                S("harbor_baggage", "照应船客的乡亲", "Guide", .90f, 0,
                    At(-15,-16.2f,"从寄存屋门前接行李",9,"parcel",-15,-14.5f),
                    Via(-8,-18),Via(-2,-18),Via(-2,4.3f),
                    At(12,4.3f,"把包袱送到候船棚外",11,"",12,6),
                    Via(-2,4.3f),Via(-2,-18),Via(-8,-18)),
                S("harbor_tea", "茶寮送水人", "Uncle", .76f, 3,
                    At(-8.2f,-28.7f,"从茶棚提起水壶",10,"water",-10,-29),
                    Via(-5,-26),Via(-5,-24.5f),Via(-23,-24.5f),Via(-23,-15),
                    At(-29,-14,"把茶水送到茶寮门前",12,"",-29,-12.5f),
                    Via(-23,-15),Via(-23,-24.5f),Via(-5,-24.5f),Via(-5,-26))
            };
        }

        static AmbientSchedule[] Ship()
        {
            // ExpandedShip: central x[-3,3] spine; the x=3.2 forward detour remains
            // outside both hatch footprints and more than 3m from Xu Sheng (0,18).
            // No route approaches the deck edge, enclosed cabins or the wheel.
            return new[] {
                S("ship_watch", "巡看甲板的船工", "Foreman", .78f, 0,
                    At(3.6f,-13,"检查桅脚绳结",12,"",5.2f,-13),
                    Via(2.5f,-10),Via(2.5f,5),
                    At(4,5,"核看货舱封绳",10,"ledger",5.2f,5),
                    Via(2.5f,5),Via(2.5f,-10)),
                S("ship_cargo", "整理船货的船工", "Worker", .86f, 4,
                    At(-6.3f,-7.1f,"提起甲板上的货包",9,"parcel",-8.1f,-6.8f),
                    Via(-3.2f,-7.1f),Via(3.2f,-7.1f),Via(3.2f,10),Via(3.2f,21),
                    At(6,21,"将货包归到船首货垛",12,"",8.1f,22),
                    Via(3.2f,21),Via(3.2f,10),Via(3.2f,-7.1f),Via(-3.2f,-7.1f)),
                S("ship_water", "照看茶水的船工", "Guide", .77f, 0,
                    At(-8.5f,-18.5f,"在茶水案边装水",10,"water",-9.8f,-18.5f),
                    Via(-8.5f,-16),Via(-3.1f,-16),Via(1.6f,-16),Via(1.6f,10),Via(3.5f,15),
                    At(6,17,"向旅客甲板水桶添水",12,"",9,16.6f),
                    Via(3.5f,15),Via(1.6f,10),Via(1.6f,-16),Via(-3.1f,-16),Via(-8.5f,-16))
            };
        }

        static AmbientSchedule[] Port()
        {
            // Main roads x=0, x=-20, x=21 and z=-21/7/24. Warehouse doors face
            // inward at x=27 and x=-24; points stop on the road side of those doors.
            // Quay-side cranes and all water beyond x=36/z=38 remain outside routes.
            return new[] {
                DistrictS("port", 0, "port_south_cargo", "西栈搬货人", "Worker", .91f, 0,
                    "从西栈货架取一包货", "parcel", P(-39.4f,-20),
                    "把货放到待点收架上", P(-51.4f,5)),
                S("port_north_cargo", "北场转运伙计", "Guide", .94f, 3,
                    At(15,27,"核好北场的一包货",8,"parcel",13,31),
                    Via(18,24),Via(0,24),Via(-20,24),Via(-20,12),
                    At(-22.5f,12,"把货交到西栈门前",11,"",-24,12),
                    Via(-20,12),Via(-20,24),Via(0,24),Via(18,24)),
                S("port_receipts", "递送货单的伙计", "Clerk", 1.06f, 0,
                    At(10,-26,"在门岗接过货单",7,"letters",10,-27.4f),
                    Via(3,-24),Via(3,4),Via(-22,4),Via(-22,20),
                    At(-28,22,"在点货棚外交单",10,"",-28,23.5f),
                    Via(-22,20),Via(-22,4),Via(3,4),Via(3,-24)),
                S("port_inspection", "巡视货栈的管事", "Foreman", .79f, 4,
                    At(25.5f,25,"在货栈门外核记数量",13,"ledger",27,25),
                    Via(25.5f,27),Via(21,27),Via(3.3f,27),Via(3.3f,-24),
                    At(8,-26,"向门岗交还点货簿",9,"",10,-29),
                    Via(3.3f,-24),Via(3.3f,27),Via(21,27),Via(25.5f,27)),
                DistrictS("port", 1, "port_tea", "西栈送茶水的乡亲", "Uncle", .75f, 2,
                    "在西栈茶水缸前装水", "water", P(-64.6f,-18),
                    "把茶水送到歇脚棚", P(-53,-38.4f)),
                S("port_meals", "给工班送饭的乡亲", "Xusheng", .84f, 0,
                    At(-22,-3.5f,"在歇脚棚外接过饭篮",10,"basket",-25,-4),
                    Via(-20,-3.5f),Via(-4,-3.5f),Via(-4,-24),
                    At(10,-24.8f,"将饭篮送到门岗前",12,"",10,-27.4f),
                    Via(-4,-24),Via(-4,-3.5f),Via(-20,-3.5f)),
                S("port_ledger", "核收条的货栈书办", "Shopkeeper", .85f, 2,
                    At(-22.5f,9,"在西栈门外接取收条",11,"ledger",-24,9),
                    Via(-20,9),Via(-16,4),Via(-3,4),Via(2,7),Via(21,7),Via(27,7),
                    At(28.5f,9.5f,"在装卸场外核对并交单",13,"",32.5f,5.5f),
                    Via(27,7),Via(21,7),Via(2,7),Via(-3,4),Via(-16,4),Via(-20,9))
            };
        }

        static AmbientSchedule[] Market()
        {
            // ExpandedMarket: use the central street and the northern z=27 lane.
            // Stall stops stand outside awning/counter footprints; the west account
            // court is skirted at x=-8, not crossed through its surrounding walls.
            return new[] {
                S("market_rice", "替店屋买米的街坊", "Uncle", .73f, 0,
                    At(-12,-14.5f,"在米摊买一篮米",12,"basket",-12,-10.9f),
                    Via(-5,-14.5f),Via(-2,-14.5f),Via(-2,24),Via(12,24),Via(12,27.5f),
                    At(12,28.5f,"把米送到店屋门前",10,"",12,30),
                    Via(12,24),Via(-2,24),Via(-2,-14.5f),Via(-5,-14.5f)),
                S("market_cloth", "送布的伙计", "Worker", .90f, 4,
                    At(-31,-5.7f,"在布摊接过布包",8,"parcel",-31,-1.9f),
                    Via(-26,-5.7f),Via(-26,7),Via(-8,7),Via(-8,27),Via(-30,27),
                    At(-30,28.4f,"在北街铺门外交布",11,"",-30,30),
                    Via(-30,27),Via(-8,27),Via(-8,7),Via(-26,7),Via(-26,-5.7f)),
                S("market_tea", "杂货铺采买人", "Shopkeeper", .77f, 0,
                    At(13,-13.5f,"在茶摊核好茶包",11,"parcel",13,-9.3f),
                    Via(6,-14),Via(2,-14),Via(2,6),Via(-20,8),
                    At(-20,9,"把茶包送到商号院门外",9,"",-20,11),
                    Via(-20,8),Via(2,6),Via(2,-14),Via(6,-14)),
                S("market_cart", "后院搬货的伙计", "Guide", .93f, 3,
                    At(30.6f,-24,"从货筐边接货",9,"parcel",32.8f,-24),
                    Via(22,-24),Via(6,-24),Via(6,-4),Via(25,-4),
                    At(25,-2,"将货送到后院门外",12,"",25,0),
                    Via(25,-4),Via(6,-4),Via(6,-24),Via(22,-24)),
                S("market_accounts", "往来抄单的伙计", "Clerk", .97f, 0,
                    At(21,28.5f,"在北街铺门前收取货单",8,"letters",21,30),
                    Via(21,27),Via(7,27),Via(7,-16),Via(28,-16),Via(28,-12.5f),
                    At(31,-12.5f,"向油铺核对并交单",12,"",31,-7.9f),
                    Via(28,-12.5f),Via(28,-16),Via(7,-16),Via(7,27),Via(21,27)),
                S("market_orders", "替行栈跑单的管事", "Foreman", .91f, 0,
                    At(-18.5f,-4,"在街口领一张采购单",9,"letters",-18.5f,-2.5f),
                    Via(-18.5f,-7.5f),Via(-6.5f,-7.5f),
                    At(-6.5f,-11,"在米摊外核数并交单",12,"",-9,-11),
                    Via(-6.5f,-7.5f),Via(-18.5f,-7.5f)),
                S("market_produce", "给街铺送菜的乡亲", "Xusheng", .87f, 2,
                    At(-26.7f,-12.8f,"在棚市边提起菜篮",10,"basket",-29,-12.8f),
                    Via(-25,-12.8f),Via(-25,-7.4f),Via(-25,-1.4f),
                    At(-17.5f,-1.5f,"将菜篮送到杂货摊前",12,"",-14,-.3f),
                    Via(-25,-1.4f),Via(-25,-7.4f),Via(-25,-12.8f))
            };
        }

        static AmbientSchedule[] Quarters()
        {
            // ExpandedQuarters: dormitory entrance z=0, kitchen entrance z=-18.5.
            // Laundry yard's west opening is z[10.35,17.65]. The basin approach
            // turns south at x=24 to avoid the writing room (28.1,13.4).
            return new[] {
                DistrictS("quarters", 0, "quarters_water", "替后巷公灶送柴的乡亲", "Worker", .81f, 0,
                    "从柴棚提起一篮柴", "firewood", P(-1.4f,39),
                    "把柴篮放到后巷公灶边", P(8.4f,49)),
                S("quarters_food", "分送饭食的乡亲", "Guide", .80f, 3,
                    At(19,-20,"在灶间门前接过饭篮",9,"basket",19,-18.5f),
                    Via(8,-20),Via(2,-20),Via(2,-2),
                    At(-23,-2,"将饭篮送到通铺院门前",12,"",-23,0),
                    Via(2,-2),Via(2,-20),Via(8,-20)),
                S("quarters_laundry", "收送换洗衣物的乡亲", "Xusheng", .83f, 0,
                    At(-20.5f,-2.2f,"在通铺院门前收衣篮",10,"basket",-20.5f,0),
                    Via(-2,-2.2f),Via(4,-2.2f),Via(4,16.7f),Via(16,16.7f),Via(24,16.7f),
                    At(29,17,"在晾衣绳前整理衣物",15,"",29,18.7f),
                    Via(24,16.7f),Via(16,16.7f),Via(4,16.7f),Via(4,-2.2f),Via(-2,-2.2f)),
                S("quarters_mail", "挨户送回批的人", "Clerk", 1.00f, 4,
                    At(-27,18.5f,"在长屋门外接取回批",8,"letters",-27,20),
                    Via(-19.2f,18.5f),Via(-19.2f,21.3f),Via(-5,21.3f),Via(-5,-2),
                    At(-25.5f,-2,"按门牌把回批送到院门外",12,"",-25.5f,0),
                    Via(-5,-2),Via(-5,21.3f),Via(-19.2f,21.3f),Via(-19.2f,18.5f)),
                DistrictS("quarters", 1, "quarters_firewood", "给后巷送水的乡亲", "Uncle", .72f, 2,
                    "在后巷水缸前装水", "water", P(50.7f,-20),
                    "把水送到同乡茶桌旁", P(39.4f,25))
            };
        }

        static AmbientSchedule[] PostOffice()
        {
            // ExpandedPostOffice: work at the outside of hall/yard doors. Neither
            // the west writing desks nor mission pickup (-22,-16) is a stopping point.
            // The north records run crosses at z=32 to skirt the quest desk (0,28).
            return new[] {
                S("post_letters", "接送批件的伙计", "Clerk", 1.03f, 0,
                    At(-22.5f,-22,"在收批处门外接批包",9,"letters",-22.5f,-20),
                    Via(-15,-22),Via(2,-22),Via(2,-7),
                    At(22,-7,"向交付厅门外交接批件",12,"",22,-5),
                    Via(2,-7),Via(2,-22),Via(-15,-22)),
                DistrictS("postoffice", 0, "post_paper", "搬送封袋的伙计", "Worker", .87f, 2,
                    "从空封袋架领取封袋", "parcel", P(-1.4f,39),
                    "把封袋送到来批架前", P(8.4f,49)),
                S("post_records", "送留底册的账房伙计", "Shopkeeper", .79f, 0,
                    At(-19,-11.9f,"在收寄厅门外接留底册",11,"ledger",-21,-10),
                    Via(-8.7f,-11.9f),Via(-4.5f,-8),Via(-2,-8),Via(-2,23),
                    At(-21,23,"将留底册交到存批院门前",13,"",-21,25),
                    Via(-2,23),Via(-2,-8),Via(-4.5f,-8),Via(-8.7f,-11.9f)),
                S("post_bags", "递送封袋的伙计", "Guide", .90f, 3,
                    At(-25,-22.2f,"在收批门外领取封袋",8,"parcel",-25,-20),
                    Via(-2,-22.2f),Via(2,-22.2f),Via(2,23),
                    At(24,23,"在封袋院门前交袋核数",12,"",24,25),
                    Via(2,23),Via(2,-22.2f),Via(-2,-22.2f)),
                S("post_check", "巡查门牌与件数的管事", "Foreman", .76f, 0,
                    At(6,4,"在交付厅侧门核记件数",12,"ledger",8,4),
                    Via(2,4),Via(2,9),
                    At(-5,9,"在代写厅门外核对来件",11,"ledger",-7,9),
                    Via(3.3f,9),Via(3.3f,32),Via(-6,32),
                    At(-6,29.5f,"在账房侧门交回记录",13,"",-8,29.5f),
                    Via(-6,32),Via(3.3f,32),Via(3.3f,9),Via(2,4)),
                DistrictS("postoffice", 1, "post_tea", "给批局后巷送水的乡亲", "Uncle", .72f, 0,
                    "在后巷水缸前装水", "water", P(50.7f,-20),
                    "把茶水送到伙计茶桌", P(39.4f,25))
            };
        }

        static AmbientSchedule S(string id, string title, string model, float speed,
            int startIndex, params AmbientStop[] stops)
        {
            return new AmbientSchedule { id=id, title=title, model=model, speed=speed,
                startIndex=startIndex, stops=stops };
        }

        // Reuse residents within the existing population budget. Each expanded
        // district has two physical workstations (0 and 2), with transit points
        // completing a loop; cargo changes only at the matching real fixture.
        // All selected residents remain among the first six mobile schedules.
        static AmbientSchedule DistrictS(string world, int loop, string id, string title, string model,
            float speed, int startIndex, string collect, string cargo, Vector3 collectFace, string deliver, Vector3 deliverFace)
        {
            Vector3[] path = InquiryPlaces.PedestrianRoute(world, loop);
            var stops = new AmbientStop[path.Length];
            for (int index = 0; index < path.Length; index++) {
                bool pickup = index == 0, dropoff = index == 2;
                stops[index] = new AmbientStop {
                    position = path[index],
                    facePoint = pickup ? collectFace : dropoff ? deliverFace : path[index],
                    activity = pickup ? collect : dropoff ? deliver : "",
                    dwellSeconds = pickup ? 9 : dropoff ? 11 : 0,
                    carryAfterStop = pickup ? cargo : dropoff ? "" : null
                };
            }
            return S(id, title, model, speed, startIndex, stops);
        }

        static AmbientStop Via(float x, float z)
        {
            return new AmbientStop { position=P(x,z), facePoint=P(x,z), activity="",
                dwellSeconds=0, carryAfterStop=null };
        }

        static AmbientStop At(float x, float z, string activity, float dwell,
            string carry, float faceX, float faceZ)
        {
            return new AmbientStop { position=P(x,z), facePoint=P(faceX,faceZ), activity=activity,
                dwellSeconds=dwell, carryAfterStop=carry };
        }

        static Vector3 P(float x, float z) { return new Vector3(x,0,z); }
    }
}
