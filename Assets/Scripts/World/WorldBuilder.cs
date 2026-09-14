using UnityEngine;
using System.Collections.Generic;

namespace TicosHouse
{
    public static class WorldBuilder
    {
        static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material Mat(string name, Color color, bool glow = false)
        {
            Material m;
            if (materials.TryGetValue(name, out m) && m != null) return m;
            Material template=Resources.Load<Material>("RuntimeStandard");
            m = template!=null?new Material(template):new Material(Shader.Find("Standard")); m.name = name; m.color = color;
            m.SetFloat("_Glossiness", .15f);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 1.8f); }
            materials[name] = m; return m;
        }
        public static GameObject Box(string name, Vector3 p, Vector3 size, Material mat, Transform parent = null, bool solid = true)
        { return Shape(name, PrimitiveType.Cube, p, size, mat, parent, solid); }
        public static GameObject Shape(string name, PrimitiveType type, Vector3 p, Vector3 size, Material mat, Transform parent = null, bool solid = false)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
        public static Light Lamp(string name, Vector3 pos, Color color, float power, float range, Transform parent = null)
        {
            GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            Light l = go.AddComponent<Light>(); l.color = color; l.intensity = power; l.range = range;
            l.shadows = LightShadows.Soft; l.shadowStrength = .85f; return l;
        }
        public static TextMesh Label(string text, Vector3 pos, float scale, Color color, Transform parent = null, float yaw = 180)
        {
            GameObject go = new GameObject("Label " + text); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            TextMesh t = go.AddComponent<TextMesh>(); t.text = text; t.characterSize = scale; t.fontSize = 70;
            t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = color; return t;
        }
        public static Transform Human(string name, Vector3 pos, bool father, bool suit, Transform parent = null)
        {
            Transform root = new GameObject(name).transform; root.SetParent(parent, false); root.localPosition = pos;
            Material skin = Mat(father ? "Tico skin" : "Twins skin", father ? new Color(.56f,.35f,.25f) : new Color(.79f,.60f,.48f));
            Material cloth = Mat(suit ? "Suit" : father ? "Tico shirt" : "Guilherme shirt", suit ? new Color(.055f,.07f,.09f) : father ? new Color(.42f,.53f,.64f) : new Color(.06f,.27f,.48f));
            Material dark = Mat("Hair", new Color(.045f,.032f,.025f));
            Material white = Mat("Ivory", new Color(.82f,.83f,.78f));
            Material pants = Mat("Pants",new Color(.055f,.065f,.085f));
            Box("Torso",new Vector3(0,1.12f,0),new Vector3(.58f,.72f,.3f),cloth,root,false);
            Box("Left arm",new Vector3(-.38f,1.08f,0),new Vector3(.17f,.69f,.19f),cloth,root,false);
            Box("Right arm",new Vector3(.38f,1.08f,0),new Vector3(.17f,.69f,.19f),cloth,root,false);
            for(int s=-1;s<=1;s+=2) {
                Box("Hand",new Vector3(s*.38f,.70f,0),new Vector3(.15f,.18f,.16f),skin,root,false);
                Box("Leg",new Vector3(s*.17f,.41f,0),new Vector3(.23f,.73f,.25f),pants,root,false);
                Box("Shoe",new Vector3(s*.17f,.08f,.08f),new Vector3(.25f,.13f,.39f),dark,root,false);
            }
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.72f,0),new Vector3(.42f,.51f,.4f),skin,root);
            Shape("Nose",PrimitiveType.Sphere,new Vector3(0,1.70f,.20f),new Vector3(.095f,.11f,.09f),skin,root);
            Shape("Hair cap",PrimitiveType.Sphere,new Vector3(0,1.94f,-.035f),new Vector3(.44f,.17f,.38f),dark,root);
            if(!father && !suit) for(int i=0;i<15;i++) {
                float a=i*2.4f;
                Shape("Messy curl",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.23f,1.87f+Mathf.Sin(a*2)*.13f,Mathf.Sin(a)*.17f),Vector3.one*.21f,dark,root);
            }
            for(int s=-1;s<=1;s+=2) {
                Shape("Eye white",PrimitiveType.Sphere,new Vector3(s*.092f,1.76f,.176f),new Vector3(.094f,.046f,.04f),white,root);
                Shape("Pupil",PrimitiveType.Sphere,new Vector3(s*.092f,1.76f,.20f),new Vector3(.037f,.042f,.018f),dark,root);
                Box("Brow",new Vector3(s*.096f,1.82f,.192f),new Vector3(.13f,.025f,.025f),dark,root,false);
            }
            Box("Mouth",new Vector3(0,1.59f,.18f),new Vector3(.14f,.025f,.03f),dark,root,false);
            if(father) {
                Material beard=Mat("Salt pepper beard",new Color(.21f,.21f,.20f));
                Shape("Beard",PrimitiveType.Sphere,new Vector3(0,1.56f,.05f),new Vector3(.37f,.20f,.33f),beard,root);
                Box("Mustache",new Vector3(0,1.65f,.203f),new Vector3(.23f,.047f,.028f),beard,root,false);
                Box("Belt prop",new Vector3(.40f,.39f,.09f),new Vector3(.055f,.63f,.03f),Mat("Leather",new Color(.16f,.065f,.025f)),root,false);
                for(int i=-3;i<=3;i++) Box("Shirt stripe",new Vector3(i*.075f,1.13f,.155f),new Vector3(.012f,.69f,.005f),white,root,false);
            }
            if(suit) {
                Box("White shirt",new Vector3(0,1.20f,.156f),new Vector3(.23f,.56f,.02f),white,root,false);
                Box("Turquoise tie",new Vector3(0,1.19f,.18f),new Vector3(.095f,.44f,.025f),Mat("Tie",new Color(.025f,.65f,.64f)),root,false);
                for(int s=-1;s<=1;s+=2) {
                    Box("Glasses upper",new Vector3(s*.105f,1.80f,.213f),new Vector3(.18f,.02f,.025f),dark,root,false);
                    Box("Glasses lower",new Vector3(s*.105f,1.71f,.213f),new Vector3(.18f,.015f,.025f),dark,root,false);
                    Box("Glasses edge",new Vector3(s*.19f,1.755f,.213f),new Vector3(.018f,.10f,.025f),dark,root,false);
                }
                Box("Glasses bridge",new Vector3(0,1.77f,.217f),new Vector3(.05f,.018f,.025f),dark,root,false);
            }
            return root;
        }
        public static Transform Dog(Vector3 pos, Transform parent)
        {
            Transform root = new GameObject("Meliça").transform; root.SetParent(parent,false); root.localPosition=pos;
            Material fur=Mat("Scruffy fur",new Color(.57f,.44f,.30f)), pale=Mat("Dog pale",new Color(.79f,.72f,.59f)), dark=Mat("Dog face",new Color(.13f,.12f,.10f));
            Shape("Little shaved body",PrimitiveType.Capsule,new Vector3(0,.31f,0),new Vector3(.33f,.40f,.65f),pale,root);
            Shape("Scruffy head",PrimitiveType.Sphere,new Vector3(0,.49f,.32f),new Vector3(.42f,.37f,.32f),fur,root);
            for(int i=0;i<4;i++) Box("Leg",new Vector3(i%2==0?-.13f:.13f,.14f,i<2?-.22f:.22f),new Vector3(.065f,.29f,.07f),pale,root,false);
            for(int s=-1;s<=1;s+=2) {
                Shape("Droopy ear",PrimitiveType.Capsule,new Vector3(s*.22f,.39f,.30f),new Vector3(.14f,.22f,.12f),dark,root);
                Shape("Uneven eye",PrimitiveType.Sphere,new Vector3(s*.10f,.52f+s*.014f,.47f),new Vector3(.07f,.08f,.03f),dark,root);
            }
            Shape("Snout",PrimitiveType.Sphere,new Vector3(0,.40f,.48f),new Vector3(.15f,.12f,.14f),dark,root);
            for(int i=0;i<13;i++) Shape("Fur tuft",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*4)*.19f,.42f+Mathf.Cos(i*7)*.15f,.38f),new Vector3(.08f,.13f,.11f),fur,root);
            Box("Pink bow",new Vector3(-.24f,.58f,.29f),new Vector3(.17f,.09f,.08f),Mat("Pink bow",new Color(.91f,.12f,.47f)),root,false);
            return root;
        }
        public static void Room(GameRuntime game)
        {
            Transform root = new GameObject("Gustavo's room").transform; game.RoomRoot=root;
            var wall=Mat("Midnight wall",new Color(.15f,.19f,.23f));
            var wood=Mat("Warm wood",new Color(.25f,.16f,.10f));
            var dark=Mat("Charcoal",new Color(.045f,.055f,.067f));
            var trim=Mat("Trim",new Color(.30f,.33f,.34f));
            var teal=Mat("Teal LED",new Color(.04f,.75f,.69f),true);
            Box("Floor",new Vector3(0,-.1f,0),new Vector3(8,.2f,8),wood,root);
            for(int i=-8;i<8;i++) Box("Floor seam",new Vector3(i*.5f,0,0),new Vector3(.012f,.003f,8),dark,root,false);
            Box("Ceiling",new Vector3(0,3.15f,0),new Vector3(8,.2f,8),wall,root);
            Box("Rear wall",new Vector3(0,1.5f,-4),new Vector3(8,3.2f,.2f),wall,root);
            Box("Left wall",new Vector3(-4,1.5f,0),new Vector3(.2f,3.2f,8),wall,root);
            Box("Right wall",new Vector3(4,1.5f,0),new Vector3(.2f,3.2f,8),wall,root);
            Box("Front left",new Vector3(-1.8f,1.5f,4),new Vector3(4.4f,3.2f,.2f),wall,root);
            Box("Front right",new Vector3(3.1f,1.5f,4),new Vector3(1.8f,3.2f,.2f),wall,root);
            Box("Door header",new Vector3(1.3f,2.8f,4),new Vector3(1.8f,.6f,.2f),wall,root);
            Box("Hall floor",new Vector3(1.3f,-.1f,7),new Vector3(2.5f,.2f,6),wood,root);
            Box("Hall left",new Vector3(.05f,1.5f,7),new Vector3(.15f,3.2f,6),wall,root);
            Box("Hall right",new Vector3(2.55f,1.5f,7),new Vector3(.15f,3.2f,6),wall,root);
            for(int i=0;i<8;i++) Box("Downstairs step",new Vector3(1.3f,-.2f-i*.23f,10+i*.45f),new Vector3(2.3f,.2f,.5f),wood,root);
            Transform pivot = new GameObject("Door hinge").transform; pivot.SetParent(root); pivot.position=new Vector3(.43f,0,3.94f); game.Door=pivot;
            Box("Bedroom door",new Vector3(.8f,1.24f,0),new Vector3(1.6f,2.43f,.09f),wood,pivot,false);
            Shape("Brass handle",PrimitiveType.Sphere,new Vector3(1.38f,1.05f,-.10f),Vector3.one*.09f,Mat("Brass",new Color(.69f,.49f,.20f)),pivot);
            Box("Light below door",new Vector3(1.23f,.025f,3.8f),new Vector3(1.55f,.012f,.10f),Mat("Amber glow",new Color(.85f,.51f,.20f),true),root,false);
            Lamp("Hall lamp",new Vector3(1.3f,2.6f,6.5f),new Color(1,.63f,.30f),1.1f,8,root);
            for(int s=-1;s<=1;s+=2) Box("Baseboard",new Vector3(s*3.88f,.1f,0),new Vector3(.04f,.2f,7.8f),trim,root,false);
            Box("Desk top",new Vector3(-1.5f,.82f,-2.6f),new Vector3(2.8f,.11f,.85f),wood,root);
            for(int s=-1;s<=1;s+=2) Box("Desk legs",new Vector3(-1.5f+s*1.2f,.4f,-2.6f),new Vector3(.13f,.8f,.65f),dark,root);
            Box("Monitor casing",new Vector3(-1.5f,1.37f,-2.82f),new Vector3(1.53f,.85f,.10f),dark,root,false);
            Box("Monitor stand",new Vector3(-1.5f,.98f,-2.84f),new Vector3(.10f,.31f,.10f),dark,root,false);
            Box("Monitor base",new Vector3(-1.5f,.90f,-2.7f),new Vector3(.48f,.03f,.31f),dark,root,false);
            game.Screen=Box("Live monitor",new Vector3(-1.5f,1.39f,-2.757f),new Vector3(1.43f,.74f,.012f),Mat("Screen",Color.white,true),root,false).GetComponent<Renderer>();
            Box("Keyboard",new Vector3(-1.55f,.9f,-2.30f),new Vector3(.76f,.035f,.25f),dark,root,false);
            for(int j=0;j<4;j++) for(int i=0;i<13;i++) Box("Key",new Vector3(-1.89f+i*.054f,.928f,-2.39f+j*.055f),new Vector3(.044f,.009f,.043f),i%3==0?teal:trim,root,false);
            Box("Mouse mat",new Vector3(-.91f,.887f,-2.31f),new Vector3(.42f,.01f,.33f),dark,root,false);
            Shape("Mouse",PrimitiveType.Sphere,new Vector3(-.91f,.925f,-2.30f),new Vector3(.09f,.052f,.14f),trim,root);
            Box("PC tower",new Vector3(-.24f,1.15f,-2.6f),new Vector3(.35f,.59f,.58f),dark,root);
            for(int i=0;i<3;i++) Shape("RGB fan",PrimitiveType.Cylinder,new Vector3(-.24f,1.00f+i*.16f,-2.29f),new Vector3(.11f,.012f,.11f),teal,root).transform.localRotation=Quaternion.Euler(90,0,0);
            Box("Chair seat",new Vector3(-1.5f,.48f,-1.65f),new Vector3(.62f,.13f,.61f),dark,root,false);
            Box("Chair back",new Vector3(-1.5f,.91f,-1.38f),new Vector3(.62f,.85f,.1f),dark,root,false);
            Box("Chair stem",new Vector3(-1.5f,.24f,-1.65f),new Vector3(.09f,.45f,.09f),trim,root,false);
            Box("Bed frame",new Vector3(2.8f,.25f,-1.8f),new Vector3(1.55f,.48f,2.8f),wood,root);
            Box("Mattress",new Vector3(2.8f,.55f,-1.8f),new Vector3(1.5f,.22f,2.72f),Mat("Linen",new Color(.42f,.47f,.47f)),root);
            Box("Duvet",new Vector3(2.8f,.70f,-1.40f),new Vector3(1.53f,.16f,1.90f),Mat("Duvet",new Color(.10f,.23f,.26f)),root,false);
            Box("Pillow",new Vector3(2.8f,.72f,-2.79f),new Vector3(1.1f,.18f,.53f),Mat("Pillow",new Color(.73f,.71f,.60f)),root,false);
            Box("Headboard",new Vector3(2.8f,.75f,-3.25f),new Vector3(1.65f,1.3f,.10f),wood,root);
            Box("Night stand",new Vector3(1.52f,.38f,-2.80f),new Vector3(.58f,.75f,.54f),wood,root);
            Box("Window frame",new Vector3(-3.88f,1.9f,.65f),new Vector3(.09f,1.65f,2.05f),trim,root,false);
            Box("Window glass",new Vector3(-3.82f,1.9f,.65f),new Vector3(.03f,1.48f,1.88f),Mat("Night glass",new Color(.015f,.06f,.10f),true),root,false);
            Box("Window cross",new Vector3(-3.79f,1.9f,.65f),new Vector3(.03f,1.48f,.055f),dark,root,false);
            Box("Window cross",new Vector3(-3.79f,1.9f,.65f),new Vector3(.03f,.055f,1.88f),dark,root,false);
            for(int i=0;i<8;i++) Box("Blind slat",new Vector3(-3.73f,2.6f-i*.08f,.65f),new Vector3(.05f,.043f,2),wood,root,false);
            Box("Poster",new Vector3(-.7f,2.05f,-3.88f),new Vector3(1.22f,1.20f,.025f),dark,root,false);
            Label("SÓ MAIS\nUMA.",new Vector3(-.7f,2.11f,-3.85f),.12f,new Color(.3f,.9f,.8f),root,0);
            Box("Book shelf",new Vector3(-2.6f,2.3f,-3.63f),new Vector3(1.65f,.09f,.45f),wood,root,false);
            for(int i=0;i<7;i++) Box("Book",new Vector3(-3.2f+i*.16f,2.48f,-3.65f),new Vector3(.1f,.30f+(i%3)*.06f,.21f),Mat("Book"+i,Color.HSVToRGB(i*.12f,.42f,.46f)),root,false);
            Shape("Mug",PrimitiveType.Cylinder,new Vector3(-2.64f,1.01f,-2.6f),new Vector3(.16f,.13f,.16f),trim,root);
            Box("Rug",new Vector3(.1f,.01f,-.35f),new Vector3(2.6f,.01f,2.4f),Mat("Rug",new Color(.22f,.16f,.14f)),root,false);
            game.MonitorLight=Lamp("Monitor light",new Vector3(-1.5f,1.7f,-2.25f),new Color(.13f,.72f,1),1.6f,4.8f,root);
            Lamp("Moonlight",new Vector3(-3.5f,2.4f,.5f),new Color(.22f,.38f,.68f),1.1f,7,root);
            Lamp("LED bounce",new Vector3(-1.3f,.55f,-3),new Color(.03f,.95f,.65f),.7f,3,root);
            game.TicoModel=Human("Tico",new Vector3(1.3f,0,6),true,false,root);
            game.TicoModel.localRotation=Quaternion.Euler(0,180,0); game.TicoModel.gameObject.SetActive(false);
            game.BrotherModel=Human("Guilherme",new Vector3(2.05f,0,2.95f),false,false,root); game.BrotherModel.localRotation=Quaternion.Euler(0,205,0);
            game.DogModel=Dog(new Vector3(.5f,0,.8f),root);
            game.GustModel=Human("Gust • suit and tie",new Vector3(-1.5f,-.4f,-1.63f),false,true,root);
        }
        public static Transform Arena()
        {
            Transform root=new GameObject("NULL//SHIFT • Relay map").transform; root.position=new Vector3(100,0,0);
            var floor=Mat("Arena concrete",new Color(.18f,.23f,.27f)); var wall=Mat("Arena walls",new Color(.27f,.33f,.37f));
            var dark=Mat("Arena dark",new Color(.09f,.12f,.16f)); var amber=Mat("Arena amber",new Color(1,.39f,.13f),true);
            Box("Arena floor",new Vector3(0,-.1f,0),new Vector3(24,.2f,34),floor,root);
            Box("Left boundary",new Vector3(-12,2,0),new Vector3(.4f,4,34),wall,root);
            Box("Right boundary",new Vector3(12,2,0),new Vector3(.4f,4,34),wall,root);
            Box("Back boundary",new Vector3(0,2,-17),new Vector3(24,4,.4f),dark,root);
            Box("Front boundary",new Vector3(0,2,17),new Vector3(24,4,.4f),dark,root);
            Vector3[] crates={new Vector3(-5,0,-8),new Vector3(5,0,-8),new Vector3(-3,0,0),new Vector3(3,0,0),new Vector3(-8,0,7),new Vector3(8,0,7),new Vector3(0,0,10)};
            for(int i=0;i<crates.Length;i++) {
                Vector3 p=crates[i]; float h=i%3==0?2.4f:1.15f;
                Box("Relay cover "+i,p+Vector3.up*h*.5f,new Vector3(2.6f,h,2.2f),wall,root);
                Box("Cover trim",p+new Vector3(0,h+.015f,0),new Vector3(2.64f,.045f,2.24f),i%2==0?amber:Mat("Cyan strip",Color.cyan,true),root,false);
            }
            for(int i=-2;i<=2;i++) {
                Box("Floor lane",new Vector3(i*4,.012f,0),new Vector3(.035f,.012f,31),Mat("Lane",new Color(.33f,.46f,.48f)),root,false);
                Box("Overhead beam",new Vector3(0,4.5f,i*7),new Vector3(24,.3f,.3f),dark,root,false);
                Lamp("Arena overhead",new Vector3(i%2==0?-5:5,4,i*6),i%2==0?new Color(.43f,.72f,1):new Color(1,.60f,.34f),2,16,root);
            }
            Label("RELAY / 07",new Vector3(0,3.0f,16.74f),.26f,Color.white,root);
            Label("NULL // SHIFT",new Vector3(0,2.8f,-16.74f),.25f,new Color(.1f,.9f,.8f),root,0);
            return root;
        }
    }
}
