using UnityEngine;
using System.Collections.Generic;

namespace TicosHouse
{
    // Original synthesized sounds. All clips are generated once, then played through pooled sources.
    public sealed class ProceduralAudio : MonoBehaviour
    {
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly List<AudioSource> house=new List<AudioSource>(), fps=new List<AudioSource>();
        public Transform Listener;
        public float FpsVolume=.45f,MasterVolume=.65f;
        public bool Online=true, Quiet,WearingHeadphones;
        float lastShot;
        System.Random rng=new System.Random(914);
        void Awake()
        {
            for(int i=0;i<12;i++) {
                GameObject g=new GameObject("House voice "+i); g.transform.parent=transform;
                var s=g.AddComponent<AudioSource>(); s.playOnAwake=false; s.spatialBlend=1; s.rolloffMode=AudioRolloffMode.Linear; s.minDistance=1; s.maxDistance=20; house.Add(s);
                GameObject f=new GameObject("FPS voice "+i); f.transform.parent=transform;
                var a=f.AddComponent<AudioSource>(); a.playOnAwake=false; fps.Add(a);
            }
            Make("shot",.16f,82,.7f,0); Make("step",.18f,95,.65f,1); Make("stair",.48f,145,.38f,2);
            Make("snore",1.8f,74,.44f,3); Make("door",.9f,160,.26f,2); Make("creak",.8f,330,.17f,2);
            Make("bark",.36f,230,.4f,4); Make("growl",1.2f,67,.3f,3); Make("crash",.6f,730,.6f,0);
            Make("ping",.35f,880,0,5); Make("reload",.6f,1700,.55f,1); Make("heart",.16f,53,.04f,1);
            Make("scream",.8f,260,.45f,4); Make("scare",1.5f,65,.7f,4); Make("offline",1.3f,440,.04f,6);
            Make("breath",1.4f,210,.75f,3); Make("explosion",.8f,48,.9f,0); Make("ability",.7f,630,.15f,6);
        }
        void Make(string name,float length,float frequency,float noise,int kind)
        {
            int rate=22050, count=(int)(length*rate); float[] data=new float[count];float filtered=0;
            for(int i=0;i<count;i++) {
                float t=(float)i/rate, u=t/length;
                float env=Mathf.Pow(1-u,kind==0?5:1.5f)*Mathf.Min(1,t*120);
                if(kind==3) env=Mathf.Sin(u*Mathf.PI)*(.65f+.35f*Mathf.Sin(t*18));
                if(kind==2) frequency+=Mathf.Sin(t*11)*.018f;
                float f=kind==6?frequency*(1-u*.8f):frequency;
                float wave=Mathf.Sin(t*f*Mathf.PI*2)+.3f*Mathf.Sin(t*f*4.01f*Mathf.PI);
                if(kind==4) wave=Mathf.Clamp(wave*3,-1,1)*(.6f+.4f*Mathf.Sin(t*34));
                float white=(float)(rng.NextDouble()*2-1);
                filtered=Mathf.Lerp(filtered,white,.07f);
                float sample=wave*(1-noise)*.32f+filtered*noise;
                if(name=="shot")sample=(filtered*.85f+Mathf.Sin(t*85*6.283f)*.20f)*Mathf.Exp(-t*38);
                if(name=="step"||name=="heart")sample=(Mathf.Sin(t*65*6.283f)*.25f+filtered*.35f)*Mathf.Exp(-t*24);
                if(name=="snore"||name=="growl")sample=filtered*.65f+Mathf.Sin(t*58*6.283f)*.10f;
                data[i]=Mathf.Clamp(sample*env*.45f,-.28f,.28f);
            }
            AudioClip clip=AudioClip.Create(name,count,1,rate,false); clip.SetData(data,0); clips[name]=clip;
        }
        void Update() { AudioListener.volume=MasterVolume;foreach(var s in fps)s.volume=Online&&!Quiet&&WearingHeadphones?FpsVolume*.48f:0; }
        AudioSource Free(List<AudioSource> pool) { foreach(var s in pool) if(!s.isPlaying)return s; return pool[0]; }
        public void Fps(string clip,Vector3 relative,float gain=1)
        {
            if(!Online||Quiet||!WearingHeadphones)return;
            if(clip=="shot"&&Time.unscaledTime-lastShot<.10f)return;
            if(clip=="shot")lastShot=Time.unscaledTime;
            AudioSource s=Free(fps); s.transform.position=Listener!=null?Listener.position:Vector3.zero;
            s.panStereo=Mathf.Clamp(relative.x/10,-.9f,.9f); s.volume=FpsVolume*.48f;
            s.pitch=1; s.PlayOneShot(clips[clip],gain);
        }
        public void House(HouseCue cue,float position)
        {
            string clip="step";
            switch(cue) {
                case HouseCue.Snore:clip="snore";break; case HouseCue.Creak:clip="creak";break;
                case HouseCue.Stair:clip="stair";break; case HouseCue.Door:clip="door";break;
                case HouseCue.Knock:clip="step";break; case HouseCue.Bark:clip="bark";break;
                case HouseCue.Crash:clip="crash";break; case HouseCue.Notification:clip="ping";break;
                case HouseCue.Growl:clip="growl";break; case HouseCue.Scream:clip="breath";break;
                case HouseCue.WifiLost:clip="offline"; foreach(var a in fps)a.Stop();break;
                case HouseCue.Heart:clip="heart";break;
            }
            AudioSource s=Free(house); s.pitch=cue==HouseCue.Brother?1.28f:1;
            s.transform.position=Vector3.Lerp(new Vector3(1.3f,-1.5f,10),new Vector3(1.3f,1.3f,3.7f),position);
            s.spatialBlend=cue==HouseCue.Heart||cue==HouseCue.WifiLost||cue==HouseCue.Scream?0:1;
            s.volume=cue==HouseCue.Snore?.95f:cue==HouseCue.Heart?.12f:.85f;
            s.PlayOneShot(clips[clip]);
        }
        public void Scare()
        {
            foreach(var a in fps)a.Stop(); AudioSource s=Free(house); s.spatialBlend=0; s.volume=.50f; s.PlayOneShot(clips["scare"]);
        }
        public void Speech(string line)
        {
            string id=null;
            if(line.Contains("calma, sou eu"))id="brother_enter";
            else if(line.Contains("ELE TÁ SUBINDO"))id="brother_warning";
            else if(line.Contains("fudeu"))id="brother_hunt";
            else if(line.Contains("vou dormir"))id="brother_leave";
            else if(line.Contains("geladeira"))id="brother_false";
            else if(line.Contains("outro monitor"))id="carlos";
            else if(line.Contains("estranho"))id="munhak";
            else if(line.Contains("ao longe"))id="tico";
            else if(line.Contains("PERDEU ISSO"))id="gust_shout";
            else if(line.Contains("sozinho no mute"))id="gust_muted";
            if(id==null)return;
            AudioClip voice=Resources.Load<AudioClip>("Voices/"+id);if(voice==null)return;
            bool call=id=="carlos"||id=="munhak";
            if(call&&!Online)return;
            AudioSource s=Free(call?fps:house);s.pitch=id=="tico"?.80f:id.StartsWith("brother")?1.08f:1;
            s.spatialBlend=call||id.StartsWith("gust")?0:1;
            s.transform.position=id=="tico"?new Vector3(1.3f,-1,8):new Vector3(2.05f,1.65f,2.95f);
            s.volume=call?FpsVolume*.72f:.75f;s.PlayOneShot(voice);
        }
        public void StopAll() { foreach(var s in house)s.Stop(); foreach(var s in fps)s.Stop(); }
        void OnDestroy() { foreach(var c in clips.Values) Destroy(c); }
    }
}
