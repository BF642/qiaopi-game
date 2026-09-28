using UnityEngine;
namespace Qiaopi
{
    public sealed class RegionZone
    {
        public string name;public Rect bounds;
        public RegionZone(string name,float x,float z,float w,float d){this.name=name;bounds=new Rect(x,z,w,d);}
    }
    public sealed class RegionNote
    {
        public string id,title,body;public Vector3 position;
        public RegionNote(string id,string title,string body,float x,float z){this.id=id;this.title=title;this.body=body;position=new Vector3(x,0,z);}
    }
    public sealed class RegionInfo
    {
        public string id,title,description;public Rect bounds;
        public Vector3 spawn,rest,sidePickup,sideDrop;public RegionZone[] zones;public RegionNote[] notes;
        public bool IsGround(Vector3 p){return bounds.Contains(new Vector2(p.x,p.z))&&(id!="harbor"||p.z<=21.3f||Mathf.Abs(p.x)<=5.65f);}
        public Vector3 Clamp(Vector3 p){p.x=Mathf.Clamp(p.x,bounds.xMin,bounds.xMax-.05f);p.z=Mathf.Clamp(p.z,bounds.yMin,bounds.yMax-.05f);if(id=="harbor"&&p.z>21.3f&&Mathf.Abs(p.x)>5.65f)p.z=21.3f;return p;}
    }
    public static class WorldRegions
    {
        public static readonly string[] All={"quanzhou","harbor","ship","port","market","quarters","postoffice"};
        public static RegionInfo Get(string id)
        {
            var r=new RegionInfo{id=id,bounds=new Rect(-35,-31,70,68),spawn=P(0,-27),rest=P(-6,-22),sidePickup=P(-24,-22),sideDrop=P(23,14)};
            switch(id){
                case "quanzhou":
                    r.title="泉州 · 晋江榕溪村";r.description="陈家院落、晒埕、族亲堂屋与村口田埂";
                    r.zones=new[]{Z("陈家院",-33,-18,20,23),Z("晒埕村巷",-12,-18,25,30),Z("族亲堂屋",13,8,21,22),Z("村口田埂",-12,25,25,12)};
                    r.notes=N(id,"晒埕上的行囊","衣衫、账本与一封介绍信，是文生带出村子的家当。行李不多，每一样都要自己作主。","石板巷口","从这里走到族亲堂屋，还要穿过半个村子。盘缠借来以后，怎样归还，也会跟着人一同过海。","村口的路","母亲没有催你走。离乡前最后一段路，常常最慢。记住这个路口，往后的批都要寄回这里。");break;
                case "harbor":
                    r.title="厦门 · 出洋码头";r.description="出洋街、验票棚、行李院与候船栈桥";
                    r.zones=new[]{Z("出洋街",-33,-27,25,27),Z("候船院",9,-26,25,28),Z("石驳岸",-32,5,64,16),Z("登船栈桥",-6,21,12,16)};
                    r.notes=N(id,"行李上的乡名","有人把村名写在包袱上，也有人紧攥着亲友的地址。到了海外，要凭这些字寻找接应的人。","候船的灯","船期和工钱一样，未必照着人的打算来。先留出吃饭钱，再决定还要带走多少。","登船木牌","走到栈桥尽头按 E 登船。登船后还要穿过甲板，找到同乡许生，才开始海上的交谈。");break;
                case "ship":
                    r.title="海路 · 南下客船";r.description="船尾舵台、货舱口、旅客甲板与船首";r.bounds=new Rect(-10,-29,20,58);r.spawn=P(0,-25);r.rest=P(-6,-19);r.sidePickup=P(-7,-12);r.sideDrop=P(7,12);
                    r.zones=new[]{Z("船尾",-9,-28,18,10),Z("货舱与舱口",-9,-15,18,12),Z("旅客甲板",-9,1,18,17),Z("船首",-8,21,16,7)};
                    r.notes=N(id,"舱口的包袱","包袱挨着包袱，乡音里夹着陌生的地名。船上同乡的一句话，可能影响你上岸后的第一份差事。","风里的家书","文生还没有领到工钱。第一封要寄给家里的信，已经在心里改了好几遍。","海的另一边","海面看不见门牌。上岸后，介绍信、乡亲的信用和你愿不愿意伸手帮忙，会成为新的路标。");break;
                case "port":
                    r.title="新加坡 · 货运港埠";r.description="登岸路、货堆、苦力歇脚棚与沿岸货栈";
                    r.zones=new[]{Z("登岸路",-33,-26,23,22),Z("货堆",-32,0,22,27),Z("货运通道",-7,-25,15,56),Z("沿岸货栈",12,4,22,28)};
                    r.notes=N(id,"招工棚","一份差事先看体力，也看谁肯担保。文生要在这里决定怎么开始谋生。","货栈的收条","货包送到还不算完，交给谁、什么时候交，都要有人记下。日后查批时，这些经手记录也会派上用场。","下一程的码头","当年是带着行囊上岸，后来可能带着积蓄回乡，也可能去另一个陌生地方。去留的分量已经不同。");break;
                case "market":
                    r.title="新加坡 · 商铺街";r.description="连排店屋、棚市、陈记柜台与进货后院";
                    r.zones=new[]{Z("棚市",-32,-26,23,22),Z("街铺",13,-23,21,24),Z("陈记柜台",-33,5,23,25),Z("进货后院",8,7,25,25)};
                    r.notes=N(id,"进货木牌","一袋货和一本账，都连着店里几个人的饭钱。核对时读清两边的名字和数目，再落笔。","柜台外的街","谋生不只是一笔工钱。愿意帮谁、怎样处理差错，都会慢慢变成别人对你的印象。","旧账后院","生意有起落，家用却不能停。几年过去，账本里多了款项，也多了不容易说出口的话。");break;
                case "quarters":
                    r.title="新加坡 · 客工住处";r.description="通铺院、窄巷、晾衣后院与公用灶间";
                    r.zones=new[]{Z("公用灶间",-32,-26,22,21),Z("通铺院",-33,0,23,24),Z("晾衣后院",11,2,23,27),Z("住处巷口",-8,23,16,13)};
                    r.notes=N(id,"灶间的一碗饭","把第一笔工钱寄走多少、给自己留下多少，要在这样的一碗饭前认真盘算。","床边的批匣","家书折得整整齐齐。里面既有报平安的话，也有欠着没还的钱、没敢说出的病痛。","送信人的脚步","门外的脚步声让人抬头。回批抵达后，这间海外住处终于与泉州的家有了回应。");break;
                default:
                    r.id="postoffice";r.title="新加坡 · 侨批局";r.description="纸铺、收寄柜台、写批台、账房与封袋后院";
                    r.zones=new[]{Z("纸铺",12,-26,22,22),Z("收寄与写批",-33,-2,25,25),Z("柜台留底",12,2,22,23),Z("封袋后院",-13,24,26,13)};
                    r.notes=N(r.id,"纸铺的批纸","先把收信人的住处写清，再写要交代的家事。银钱和话语放在一起，才是这趟路上最重的牵挂。","收寄柜台","经手人要核对收信人、款额和来处。一份底单留在这里，让寄出的人日后还有据可查。","回批与留底","收到故乡回信，文生才知道银钱和心意走到了哪里。若家里仍在等款，还要沿经手记录继续追问。");break;
            }
            return r;
        }
        static RegionNote[] N(string id,string a,string ab,string b,string bb,string c,string cb)=>new[]{new RegionNote(id+"_0",a,ab,3.2f,-13),new RegionNote(id+"_1",b,bb,3.2f,3),new RegionNote(id+"_2",c,cb,3.2f,22)};
        static RegionZone Z(string n,float x,float z,float w,float d)=>new RegionZone(n,x,z,w,d);
        public static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    }
}
