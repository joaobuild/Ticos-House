using UnityEngine;
using System.Collections.Generic;

namespace TicosHouse
{
    // Recorded breathing, offline neural dialogue, and original layered effects.
    public sealed class ProceduralAudio : MonoBehaviour
    {
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly List<AudioSource> house=new List<AudioSource>(), fps=new List<AudioSource>();
        public Transform Listener;
        public float FpsVolume=.45f,MasterVolume=.65f;
        public bool Online=true, Quiet,WearingHeadphones;
        public bool Sleeping=true;
        AudioSource snore,dialogue,callVoice;
        bool wasQuiet;int snoreIndex,stepIndex;
        public bool SnorePlaying {get{return snore!=null&&snore.isPlaying;}}
        public bool DialoguePlaying {get{return dialogue!=null&&dialogue.isPlaying;}}
        float lastShot;
        System.Random rng=new System.Random(914);
        void Awake()
        {
            for(int i=0;i<12;i++) {
                GameObject g=new GameObject("House voice "+i); g.transform.parent=transform;
                var s=g.AddComponent<AudioSource>(); s.playOnAwake=false; s.spatialBlend=1; s.rolloffMode=AudioRolloffMode.Linear; s.minDistance=1; s.maxDistance=20; house.Add(s);
                s.dopplerLevel=0;g.AddComponent<AudioLowPassFilter>().cutoffFrequency=9000;
                GameObject f=new GameObject("FPS voice "+i); f.transform.parent=transform;
                var a=f.AddComponent<AudioSource>(); a.playOnAwake=false; fps.Add(a);
            }
            Make("shot",.16f,82,.7f,0); Make("step",.18f,95,.65f,1); Make("stair",.48f,145,.38f,2);
            Make("snore",1.8f,74,.44f,3); Make("door",.9f,160,.26f,2); Make("creak",.8f,330,.17f,2);
            Make("bark",.36f,230,.4f,4); Make("growl",1.2f,67,.3f,3); Make("crash",.6f,730,.6f,0);
            Make("ping",.35f,880,0,5); Make("reload",.6f,1700,.55f,1); Make("heart",.16f,53,.04f,1);
            Make("scream",.8f,260,.45f,4); Make("scare",1.5f,65,.7f,4); Make("offline",1.3f,440,.04f,6);
            Make("breath",1.4f,210,.75f,3); Make("explosion",.8f,48,.9f,0); Make("ability",.7f,630,.15f,6);
            for(int i=0;i<4;i++)Make("step"+i,.28f,65+i*7,.7f,1);
            snore=NewVoice("Recorded snore",true);dialogue=NewVoice("House dialogue",true);callVoice=NewVoice("Call dialogue",false);
            for(int i=0;i<4;i++){var recording=Resources.Load<AudioClip>("AudioReal/snore"+i);if(recording!=null)clips["snore"+i]=recording;}
        }
        AudioSource NewVoice(string name,bool spatial){var go=new GameObject(name);go.transform.SetParent(transform);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=spatial?1:0;s.dopplerLevel=0;s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=2;s.maxDistance=22;go.AddComponent<AudioLowPassFilter>().cutoffFrequency=spatial?7000:15000;return s;}
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
                if(name.StartsWith("step")){float heel=Mathf.Exp(-t*38),toe=t>.065f?Mathf.Exp(-(t-.065f)*48):0;sample=filtered*(heel*.9f+toe*.5f)+Mathf.Sin(t*frequency*6.283f)*heel*.24f;}
                if(name=="heart")sample=(Mathf.Sin(t*53*6.283f)*.22f)*Mathf.Exp(-t*24);
                if(name=="stair"||name=="creak"||name=="door"){
                    float rub=Mathf.Sin(6.283f*(170*t+43*t*t+2*Mathf.Sin(t*5)))*.10f;
                    sample=rub*(.45f+.55f*Mathf.Sin(t*21)*Mathf.Sin(t*21))+filtered*.3f;
                    if(name=="stair")sample+=Mathf.Sin(t*72*6.283f)*Mathf.Exp(-t*32)*.35f;
                    if(name=="door"&&t>.65f)sample+=filtered*Mathf.Exp(-(t-.65f)*30)*1.2f;
                }
                if(name=="snore"||name=="growl")sample=filtered*.65f+Mathf.Sin(t*58*6.283f)*.10f;
                data[i]=Mathf.Clamp(sample*env*.45f,-.28f,.28f);
            }
            AudioClip clip=AudioClip.Create(name,count,1,rate,false); clip.SetData(data,0); clips[name]=clip;
        }
        void Update() {
            AudioListener.volume=Mathf.Clamp01(MasterVolume);
            if(Quiet!=wasQuiet){foreach(var s in house)Pause(s,Quiet);foreach(var s in fps)Pause(s,Quiet);Pause(snore,Quiet);Pause(dialogue,Quiet);Pause(callVoice,Quiet);wasQuiet=Quiet;}
            if(!Sleeping)snore.Stop();if(!Online)callVoice.Stop();
            float duck=dialogue.isPlaying||callVoice.isPlaying?.48f:1;
            foreach(var s in fps)s.volume=Online&&!Quiet&&WearingHeadphones?FpsVolume*.42f*duck:0;
            callVoice.volume=Online&&!Quiet&&WearingHeadphones?FpsVolume*.90f:0;
        }
        static void Pause(AudioSource source,bool pause){if(source==null)return;if(pause)source.Pause();else source.UnPause();}
        AudioSource Free(List<AudioSource> pool) { foreach(var s in pool) if(!s.isPlaying)return s; return pool[0]; }
        public void SelfStep(){if(Quiet||Listener==null)return;var s=Free(house);s.spatialBlend=1;s.transform.position=Listener.position-Vector3.up*1.5f;s.volume=.38f;s.pitch=Random.Range(.95f,1.04f);s.GetComponent<AudioLowPassFilter>().cutoffFrequency=9000;s.PlayOneShot(clips["step"+(stepIndex++%4)]);}
        public void Fps(string clip,Vector3 relative,float gain=1)
        {
            if(!Online||Quiet||!WearingHeadphones)return;
            if(clip=="shot"&&Time.unscaledTime-lastShot<.10f)return;
            if(clip=="shot")lastShot=Time.unscaledTime;
            AudioSource s=Free(fps); s.transform.position=Listener!=null?Listener.position:Vector3.zero;
            s.panStereo=Mathf.Clamp(relative.x/10,-.9f,.9f); s.volume=FpsVolume*.48f;
            s.pitch=Random.Range(.97f,1.03f); s.PlayOneShot(clips[clip],gain);
        }
        public void House(HouseCue cue,float position)
        {
            if(Quiet)return;
            if(cue==HouseCue.Snore&&clips.ContainsKey("snore0")){
                if(snore.isPlaying||!Sleeping)return;snore.clip=clips["snore"+(snoreIndex++%4)];snore.transform.position=new Vector3(1.3f,-1.5f,10);snore.volume=.78f;snore.pitch=Random.Range(.97f,1.02f);snore.GetComponent<AudioLowPassFilter>().cutoffFrequency=1800;snore.Play();return;
            }
            string clip="step"+(stepIndex++%4);
            switch(cue) {
                case HouseCue.Snore:clip="snore";break; case HouseCue.Creak:clip="creak";break;
                case HouseCue.Stair:clip="stair";break; case HouseCue.Door:clip="door";break;
                case HouseCue.Knock:clip="step";break; case HouseCue.Bark:clip="bark";break;
                case HouseCue.Crash:clip="crash";break; case HouseCue.Notification:clip="ping";break;
                case HouseCue.Growl:clip="growl";break; case HouseCue.Scream:clip="breath";break;
                case HouseCue.WifiLost:clip="offline"; foreach(var a in fps)a.Stop();callVoice.Stop();snore.Stop();break;
                case HouseCue.Heart:clip="heart";break;
            }
            AudioSource s=Free(house); s.pitch=cue==HouseCue.Brother?Random.Range(1.08f,1.15f):Random.Range(.96f,1.04f);
            s.transform.position=Vector3.Lerp(new Vector3(1.3f,-1.5f,10),new Vector3(1.3f,1.3f,3.7f),position);
            s.spatialBlend=cue==HouseCue.Heart||cue==HouseCue.WifiLost||cue==HouseCue.Scream?0:1;
            s.volume=cue==HouseCue.Snore?.95f:cue==HouseCue.Heart?.12f:.85f;
            s.GetComponent<AudioLowPassFilter>().cutoffFrequency=s.spatialBlend==0?14000:Mathf.Lerp(1700,10000,Mathf.Clamp01(position));
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
            if(Quiet||(call&&(!Online||!WearingHeadphones)))return;
            AudioSource s=call?callVoice:dialogue;s.Stop();s.pitch=1;
            s.spatialBlend=call||id.StartsWith("gust")?0:1;
            s.transform.position=id=="tico"?new Vector3(1.3f,-1,8):new Vector3(2.05f,1.65f,2.95f);
            s.GetComponent<AudioLowPassFilter>().cutoffFrequency=id=="tico"?2400:call?6500:12000;
            s.volume=call?FpsVolume*.90f:.82f;s.clip=voice;s.Play();
        }
        public void Announce(string id)
        {
            if(!Online||Quiet||!WearingHeadphones)return;var clip=Resources.Load<AudioClip>("Voices/"+id);if(clip==null)return;
            callVoice.Stop();callVoice.spatialBlend=0;callVoice.pitch=1;callVoice.GetComponent<AudioLowPassFilter>().cutoffFrequency=12000;callVoice.volume=FpsVolume*.9f;callVoice.clip=clip;callVoice.Play();
        }
        public void StopAll() { foreach(var s in house)s.Stop(); foreach(var s in fps)s.Stop();snore.Stop();dialogue.Stop();callVoice.Stop(); }
        void OnDestroy() { foreach(var pair in clips)if(!(pair.Key.StartsWith("snore")&&pair.Key.Length>5))Destroy(pair.Value); }
    }
}
