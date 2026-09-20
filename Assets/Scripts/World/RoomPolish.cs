using UnityEngine;

namespace TicosHouse
{
    public static class RoomPolish
    {
        // Small, deterministic surface textures; no downloads or third-party assets.
        public static void Surface(Material m,string name)
        {
            string n=name.ToLowerInvariant();
            bool wood=n.Contains("wood")||n.Contains("oak"),cloth=n.Contains("linen")||n.Contains("duvet")||n.Contains("pillow")||n.Contains("rug")||n.Contains("shirt")||n.Contains("suit");
            bool plaster=n.Contains("wall"),fur=n.Contains("fur")||n.Contains("beard");
            if(!wood&&!cloth&&!plaster&&!fur)return;
            const int size=128;var t=new Texture2D(size,size,TextureFormat.RGB24,false);t.name=name+" surface";t.wrapMode=TextureWrapMode.Repeat;t.filterMode=FilterMode.Bilinear;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float noise=Mathf.PerlinNoise(x*.27f,y*.27f);
                float value=wood?.72f+.20f*Mathf.Sin(x*.36f+Mathf.PerlinNoise(x*.035f,y*.023f)*5)+noise*.08f:
                    cloth?.78f+noise*.12f+((x%3==0||y%3==0)?-.10f:.05f):.76f+noise*.24f;
                if(n=="tico shirt")value*=x%16<3?1.35f:.8f;
                pixels[y*size+x]=new Color(value,value,value);
            }
            t.SetPixels(pixels);t.Apply(false,true);m.mainTexture=t;m.mainTextureScale=wood?new Vector2(3,2):cloth?new Vector2(4,4):new Vector2(3,3);
            m.SetFloat("_Glossiness",wood?.32f:cloth?.04f:.12f);
        }
        public static void Decorate(Transform root)
        {
            var wood=WorldBuilder.Mat("Warm wood",new Color(.25f,.16f,.10f));var trim=WorldBuilder.Mat("Trim",new Color(.3f,.33f,.34f));
            var metal=WorldBuilder.Mat("Satin steel",new Color(.20f,.24f,.27f));metal.SetFloat("_Metallic",.7f);metal.SetFloat("_Glossiness",.6f);
            var fabric=WorldBuilder.Mat("Curtain linen",new Color(.20f,.28f,.35f));var dark=WorldBuilder.Mat("Charcoal",new Color(.045f,.055f,.067f));
            // Crown moulding and complete skirting frame the room without blocking movement.
            for(int side=-1;side<=1;side+=2){
                WorldBuilder.Box("Crown moulding",new Vector3(side*3.83f,2.97f,0),new Vector3(.12f,.13f,7.8f),trim,root,false);
                WorldBuilder.Box("Crown moulding",new Vector3(0,2.97f,side*3.83f),new Vector3(7.8f,.13f,.12f),trim,root,false);
            }
            WorldBuilder.Box("Rear skirting",new Vector3(0,.10f,-3.84f),new Vector3(7.8f,.20f,.05f),trim,root,false);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<7;i++){
                var pleat=WorldBuilder.Shape("Curtain fold",PrimitiveType.Capsule,new Vector3(-3.59f+(i%2)*.035f,1.72f,.65f+side*(1.04f+i*.065f)),new Vector3(.09f,1.05f,.105f),fabric,root);
            }
            WorldBuilder.Box("Curtain rail",new Vector3(-3.58f,2.83f,.65f),new Vector3(.06f,.06f,3.04f),metal,root,false);
            // Bed stitching and draped cover.
            var seam=WorldBuilder.Mat("Duvet stitch",new Color(.18f,.34f,.36f));
            for(int i=0;i<8;i++)WorldBuilder.Box("Duvet stitched channel",new Vector3(2.13f+i*.19f,.786f,-1.40f),new Vector3(.009f,.004f,1.82f),seam,root,false);
            WorldBuilder.Box("Duvet hanging side",new Vector3(2.02f,.52f,-1.40f),new Vector3(.035f,.42f,1.90f),WorldBuilder.Mat("Duvet",Color.cyan),root,false);
            for(int i=0;i<3;i++)WorldBuilder.Box("Headboard inset",new Vector3(2.26f+i*.54f,.91f,-3.18f),new Vector3(.46f,.64f,.025f),fabric,root,false);
            for(int s=-1;s<=1;s+=2){
                WorldBuilder.Box("Chair arm rest",new Vector3(-1.5f+s*.40f,.72f,-1.64f),new Vector3(.095f,.075f,.48f),dark,root,false);
                WorldBuilder.Box("Chair arm support",new Vector3(-1.5f+s*.40f,.57f,-1.57f),new Vector3(.05f,.27f,.05f),metal,root,false);
            }
            for(int i=0;i<5;i++){
                float a=i*Mathf.PI*2/5;var spoke=WorldBuilder.Box("Chair wheel spoke",new Vector3(-1.5f+Mathf.Sin(a)*.19f,.10f,-1.65f+Mathf.Cos(a)*.19f),new Vector3(.045f,.055f,.40f),metal,root,false);spoke.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                WorldBuilder.Shape("Caster",PrimitiveType.Sphere,new Vector3(-1.5f+Mathf.Sin(a)*.38f,.06f,-1.65f+Mathf.Cos(a)*.38f),new Vector3(.11f,.10f,.11f),dark,root);
            }
            WorldBuilder.Box("Desk drawer",new Vector3(-2.48f,.64f,-2.56f),new Vector3(.66f,.22f,.72f),wood,root,false);
            WorldBuilder.Box("Drawer pull",new Vector3(-2.48f,.64f,-2.18f),new Vector3(.22f,.025f,.03f),metal,root,false);
            // Warm bedside pool contrasts with moonlight and cool monitor spill.
            WorldBuilder.Shape("Bed lamp base",PrimitiveType.Cylinder,new Vector3(1.52f,.79f,-2.80f),new Vector3(.27f,.025f,.27f),metal,root);
            WorldBuilder.Shape("Bed lamp stem",PrimitiveType.Cylinder,new Vector3(1.52f,.98f,-2.80f),new Vector3(.035f,.19f,.035f),metal,root);
            WorldBuilder.Shape("Bed lamp shade",PrimitiveType.Cylinder,new Vector3(1.52f,1.18f,-2.80f),new Vector3(.37f,.16f,.37f),WorldBuilder.Mat("Warm shade",new Color(.65f,.40f,.20f),true),root);
            WorldBuilder.Lamp("Bedside warm pool",new Vector3(1.52f,1.05f,-2.55f),new Color(1,.64f,.33f),1.1f,3.2f,root).shadows=LightShadows.None;
            WorldBuilder.Box("Under desk LED strip",new Vector3(-1.5f,.76f,-2.93f),new Vector3(2.5f,.018f,.025f),WorldBuilder.Mat("Teal LED",Color.cyan,true),root,false);
            for(int i=0;i<3;i++)WorldBuilder.Box("Rug woven border",new Vector3(.1f,.022f,-1.47f+i*.045f),new Vector3(2.5f,.003f,.016f),fabric,root,false);
            WorldBuilder.Box("Wall outlet",new Vector3(-2.8f,.28f,-3.86f),new Vector3(.15f,.22f,.035f),trim,root,false);
            var cable=new GameObject("Power cable");cable.transform.SetParent(root,false);var line=cable.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=5;line.SetPositions(new[]{new Vector3(-2.8f,.28f,-3.80f),new Vector3(-2.8f,.04f,-3.72f),new Vector3(-2.1f,.035f,-3.2f),new Vector3(-.4f,.035f,-2.8f),new Vector3(-.24f,.90f,-2.8f)});line.startWidth=line.endWidth=.012f;line.sharedMaterial=dark;
        }
    }
}
