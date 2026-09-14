using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TicosHouse
{
    public sealed class Combatant
    {
        public string Name; public bool Ally; public float Skill, Health=100, Cooldown, MoveClock;
        public int Kills, Deaths, Assists; public bool Exceptional, DamagedByPlayer;
        public Transform Model; public Vector3 Destination;
        public bool Alive { get {return Health>0;} }
    }
    public sealed class HitTarget:MonoBehaviour { public Combatant Bot; public bool Head; }
    public sealed class FPSMinigame:MonoBehaviour
    {
        public GameRuntime Game;
        public Camera Cam;
        public RenderTexture Texture;
        public MatchStats Stats=new MatchStats();
        public readonly List<Combatant> Bots=new List<Combatant>();
        public int Blue, Red, Health=100, Ammo=24, RoundKills;
        public float RoundTime, Intermission=3, ReloadRemaining, HitFlash, DamageFlash, FlashRemaining, LagRemaining;
        public bool Active, MatchOver, LastWin, PlayerDead;
        public string Banner="NULL // SHIFT", KillFeed="", RatingDetails="";
        public float BannerTime, FeedTime;
        CharacterController controller;
        Transform root, weapon;
        float yaw,pitch,fireTime,shotKick,abilityCooldown, footTimer;
        Vector3 weaponRest;
        int losses;
        public void Initialize(GameRuntime game)
        {
            Game=game; root=WorldBuilder.Arena();
            GameObject actor=new GameObject("FPS player"); actor.transform.position=new Vector3(100,0,-13);
            controller=actor.AddComponent<CharacterController>(); controller.height=1.8f; controller.center=Vector3.up*.9f; controller.radius=.32f;
            var c=new GameObject("FPS camera"); c.transform.SetParent(actor.transform,false); c.transform.localPosition=Vector3.up*1.65f;
            Cam=c.AddComponent<Camera>(); Cam.fieldOfView=78; Cam.nearClipPlane=.06f; Cam.farClipPlane=65;
            Cam.clearFlags=CameraClearFlags.SolidColor; Cam.backgroundColor=new Color(.025f,.05f,.085f);
            Texture=new RenderTexture(1280,720,24); Texture.name="Live FPS monitor"; Cam.targetTexture=Texture;
            weapon=WorldBuilder.Box("Vektor rifle",new Vector3(.27f,-.24f,.49f),new Vector3(.13f,.13f,.48f),WorldBuilder.Mat("Rifle",new Color(.065f,.075f,.09f)),Cam.transform,false).transform;
            WorldBuilder.Box("Rifle slide",new Vector3(0,.55f,.05f),new Vector3(.65f,.20f,.85f),WorldBuilder.Mat("Rifle accent",new Color(.07f,.65f,.59f),true),weapon,false);
            WorldBuilder.Box("Rifle grip",new Vector3(0,-.9f,-.12f),new Vector3(.65f,1.2f,.24f),WorldBuilder.Mat("Grip",new Color(.1f,.12f,.13f)),weapon,false);
            weaponRest=weapon.localPosition;
        }
        public void NewMatch()
        {
            if(Game.Night.FinalHunt||Game.Night.Result!=Ending.None)return;
            foreach(var b in Bots)if(b.Model!=null)Destroy(b.Model.gameObject); Bots.Clear();
            Stats=new MatchStats(); Blue=Red=0; MatchOver=false; Active=true; losses=0;
            string[] friends={"Joaobuild","Trolezi","Carlos","Loogins","Tavinho","Munhak"};
            friends=friends.OrderBy(x=>Random.value).ToArray();
            for(int i=0;i<4;i++) Spawn(friends[i],true,(friends[i]=="Carlos"||friends[i]=="Munhak")?.25f:.65f+Random.value*.2f);
            for(int i=0;i<5;i++) Spawn(new[]{"Vector","Mantis","Prisma","Eco","Zeta"}[i],false,.58f+Game.Night.Difficulty*.10f);
            NewRound(); Intermission=3; Banner="PARTIDA ENCONTRADA";
        }
        void Spawn(string name,bool ally,float skill)
        {
            var b=new Combatant{Name=name,Ally=ally,Skill=skill,Exceptional=Random.value<.025f};
            if(b.Exceptional) b.Skill=.98f;
            b.Model=new GameObject(name).transform; b.Model.SetParent(root,false);
            Material mat=WorldBuilder.Mat(ally?"Ally armor":"Enemy armor",ally?new Color(.06f,.56f,.61f):new Color(.85f,.25f,.13f));
            GameObject body=WorldBuilder.Shape("Body",PrimitiveType.Capsule,new Vector3(0,.91f,0),new Vector3(.58f,.70f,.58f),mat,b.Model,true);
            body.AddComponent<HitTarget>().Bot=b;
            GameObject head=WorldBuilder.Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.67f,0),Vector3.one*.37f,WorldBuilder.Mat("Visor",new Color(.15f,.18f,.21f)),b.Model,true);
            var target=head.AddComponent<HitTarget>(); target.Bot=b; target.Head=true;
            WorldBuilder.Box("Visor stripe",new Vector3(0,1.69f,-.18f),new Vector3(.29f,.052f,.024f),WorldBuilder.Mat(ally?"Ally visor":"Enemy visor",ally?Color.cyan:new Color(1,.55f,.18f),true),b.Model,false);
            WorldBuilder.Box("Bot rifle",new Vector3(.30f,1.08f,-.21f),new Vector3(.12f,.14f,.62f),WorldBuilder.Mat("Rifle",Color.gray),b.Model,false);
            Bots.Add(b);
        }
        void NewRound()
        {
            Health=100; Ammo=24; RoundTime=0; RoundKills=0; PlayerDead=false; ReloadRemaining=0; FlashRemaining=0; abilityCooldown=0;
            controller.enabled=false; controller.transform.position=new Vector3(100,0,-13); controller.enabled=true;
            yaw=0; pitch=0;
            int a=0,e=0;
            foreach(var b in Bots) {
                int i=b.Ally?a++:e++; b.Health=100; b.DamagedByPlayer=false; b.Cooldown=Random.Range(1.0f,3.0f); b.MoveClock=0;
                b.Model.gameObject.SetActive(true); b.Model.localPosition=new Vector3((i-2)*3.3f,0,b.Ally?-11:12);
                b.Destination=b.Model.position+new Vector3(Random.Range(-2f,2f),0,b.Ally?12:-14);
            }
            Banner="ROUND "+(Blue+Red+1); BannerTime=2;
        }
        public void Tick(float dt,bool controls)
        {
            if(!Active||MatchOver||!Game.Night.Internet||Game.Night.Result!=Ending.None)return;
            HitFlash=Mathf.Max(0,HitFlash-dt); DamageFlash=Mathf.Max(0,DamageFlash-dt); FeedTime-=dt; BannerTime-=dt;
            if(Intermission>0){Intermission-=dt; if(Intermission<=0)NewRound(); return;}
            RoundTime+=dt; fireTime-=dt; abilityCooldown-=dt; FlashRemaining-=dt; LagRemaining=Mathf.Max(0,LagRemaining-dt);
            if(ReloadRemaining>0) { ReloadRemaining-=dt; if(ReloadRemaining<=0)Ammo=24; }
            if(controls&&!PlayerDead) Control(dt);
            // All tactical actors continue running when the player leaves the computer.
            foreach(var b in Bots) if(b.Alive) UpdateBot(b,dt);
            int allies=Bots.Count(b=>b.Ally&&b.Alive)+(PlayerDead?0:1), enemies=Bots.Count(b=>!b.Ally&&b.Alive);
            if(enemies==0)EndRound(true); else if(allies==0)EndRound(false);
            else if(RoundTime>27) {
                float blue=allies+(Health/100f)*.3f, red=enemies;
                EndRound(blue>red||(Mathf.Approximately(blue,red)&&Random.value<.5f));
            }
            shotKick=Mathf.Lerp(shotKick,0,dt*15); weapon.localPosition=weaponRest+new Vector3(0,0,-shotKick);
            weapon.localRotation=Quaternion.Euler(ReloadRemaining>0?25:0,ReloadRemaining>0?-22:0,0);
        }
        void Control(float dt)
        {
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            float sensitivity=Game.Sensitivity;
            yaw+=Input.GetAxisRaw("Mouse X")*sensitivity; pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*sensitivity,-78,78);
            controller.transform.rotation=Quaternion.Euler(0,yaw,0);
            float tremor=Mathf.Max(0,Game.Night.Stress-55)*.009f;
            Cam.transform.localRotation=Quaternion.Euler(pitch+Mathf.Sin(Time.time*29)*tremor,yaw*0,Mathf.Sin(Time.time*23)*tremor);
            Vector3 direction=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")); direction=Vector3.ClampMagnitude(direction,1);
            float speed=Input.GetKey(KeyCode.LeftShift)?2.1f:4.1f;
            controller.Move((controller.transform.TransformDirection(direction)*speed+Vector3.down*9)*dt);
            footTimer-=dt; if(direction.sqrMagnitude>.1f&&footTimer<=0){ Game.Audio.Fps("step",Vector3.zero,.25f);footTimer=.43f;}
            if(Input.GetKeyDown(KeyCode.R)&&Ammo<24&&ReloadRemaining<=0) { ReloadRemaining=1.6f;Game.Audio.Fps("reload",Vector3.zero); }
            if(Input.GetKeyDown(KeyCode.Q)&&abilityCooldown<=0) { abilityCooldown=16;FlashRemaining=3.2f;Game.Audio.Fps("ability",Vector3.zero);Banner="PULSO: inimigos desorientados";BannerTime=2; }
            if(Input.GetMouseButton(0)&&fireTime<=0&&ReloadRemaining<=0&&LagRemaining<=0) Shoot();
        }
        void Shoot()
        {
            if(Ammo<=0){ReloadRemaining=1.6f;Game.Audio.Fps("reload",Vector3.zero);return;}
            Ammo--;Stats.Shots++;fireTime=.14f;shotKick=.045f;Game.Night.AddNoise(.45f);
            Game.Audio.Fps("shot",Vector3.zero,.8f);
            float spread=Input.GetKey(KeyCode.LeftShift)?.0015f:.005f;
            spread+=Mathf.Max(0,Game.Night.Stress-55)*.00015f;
            Vector3 dir=Cam.transform.forward+Cam.transform.right*Random.Range(-spread,spread)+Cam.transform.up*Random.Range(-spread,spread);
            RaycastHit hit;
            if(Physics.Raycast(Cam.transform.position,dir,out hit,60)) {
                var target=hit.collider.GetComponent<HitTarget>();
                if(target!=null&&target.Bot.Alive&&!target.Bot.Ally) {
                    Stats.Hits++;HitFlash=.15f;target.Bot.DamagedByPlayer=true;
                    float damage=target.Head?110:36;target.Bot.Health-=damage;
                    if(target.Bot.Health<=0) {
                        Stats.Kills++;RoundKills++;if(target.Head)Stats.Headshots++;
                        Kill(target.Bot,null);KillFeed="Gust  >  "+target.Bot.Name+(target.Head?"  [HEADSHOT]":"");
                        FeedTime=3;Game.Night.Stress=Mathf.Max(0,Game.Night.Stress-2);
                    }
                }
                Game.FlashImpact(hit.point);
            }
        }
        bool ClearLine(Vector3 a,Vector3 b,Combatant target=null)
        {
            RaycastHit hit;
            if(!Physics.Linecast(a,b,out hit))return true;
            if(target==null && hit.collider==controller)return true;
            var h=hit.collider.GetComponent<HitTarget>(); return h!=null&&h.Bot==target;
        }
        void UpdateBot(Combatant b,float dt)
        {
            b.Cooldown-=dt;b.MoveClock+=dt;
            if(b.MoveClock>2.5f) {
                b.MoveClock=0;
                b.Destination=root.position+new Vector3(Random.Range(-10f,10f),0,Random.Range(-8f,8f));
            }
            Vector3 eye=b.Model.position+Vector3.up*1.5f;
            Combatant target=null; float nearest=999;
            foreach(var other in Bots)if(other.Alive&&other.Ally!=b.Ally) {
                float d=Vector3.Distance(other.Model.position,b.Model.position);
                if(d<nearest&&ClearLine(eye,other.Model.position+Vector3.up*1.3f,other)){nearest=d;target=other;}
            }
            bool aimPlayer=!b.Ally&&!PlayerDead&&ClearLine(eye,Cam.transform.position)&&Vector3.Distance(b.Model.position,controller.transform.position)<nearest;
            Vector3 goal=aimPlayer?controller.transform.position:target!=null?target.Model.position:b.Destination;
            Vector3 delta=goal-b.Model.position;delta.y=0;
            if(delta.sqrMagnitude>.01f)b.Model.rotation=Quaternion.LookRotation(delta);
            bool hasTarget=aimPlayer||target!=null;
            if(!hasTarget||delta.magnitude>10) {
                Vector3 motion=delta.normalized*(b.Ally?2.8f:2.5f)*dt;
                RaycastHit obstruction;
                if(!Physics.SphereCast(b.Model.position+Vector3.up*.75f,.32f,motion.normalized,out obstruction,.7f))b.Model.position+=motion;
                else {b.MoveClock=3;b.Model.position+=b.Model.right*Mathf.Sin(Time.time*2+b.Name.Length)*dt;}
            }
            if(b.Cooldown<=0&&hasTarget) {
                b.Cooldown=Random.Range(.65f,1.25f); Game.Audio.Fps("shot",Cam.transform.InverseTransformPoint(eye),.3f);
                float accuracy=b.Skill*(b.Ally?Game.Night.Communication:1)*(aimPlayer?.38f:.62f);
                if(!b.Ally&&FlashRemaining>0)accuracy*=.15f;
                if(Random.value<accuracy) {
                    if(aimPlayer) { Health-=Random.Range(15,28);DamageFlash=.24f;
                        if(Health<=0) {Health=0;PlayerDead=true;Stats.Deaths++;if(RoundTime<7)Stats.EarlyDeaths++;Game.Night.Stress=Mathf.Min(100,Game.Night.Stress+6);b.Kills++;Banner="VOCÊ CAIU • o time continua";BannerTime=3;}
                    }
                    else {target.Health-=Random.Range(25,45);if(target.Health<=0)Kill(target,b);}
                }
            }
            if(b.MoveClock<dt&&Random.value<.5f)Game.Audio.Fps("step",Cam.transform.InverseTransformPoint(eye),.4f);
        }
        void Kill(Combatant target,Combatant killer)
        {
            target.Health=0;target.Deaths++;target.Model.gameObject.SetActive(false);
            if(killer!=null) {
                killer.Kills++;
                if(target.DamagedByPlayer&&!target.Ally)Stats.Assists++;
            }
        }
        void EndRound(bool won)
        {
            if(Intermission>0||MatchOver)return;
            if(won) {
                Blue++;losses=0;Game.Night.Stress=Mathf.Max(0,Game.Night.Stress-1.5f);
                if(RoundKills>=2)Stats.MultiKills++;
                if(!PlayerDead&&Bots.All(b=>!b.Ally||!b.Alive)&&RoundKills>0)Stats.Clutches++;
                if((Blue>=11||Red>=11)&&RoundKills>0)Stats.Decisive++;
            }
            else {Red++;losses++;Game.Night.Stress=Mathf.Min(100,Game.Night.Stress+2+losses);}
            Banner=won?"ROUND VENCIDO":"ROUND PERDIDO";BannerTime=3;Game.Audio.Fps("ping",Vector3.zero,.4f);
            if(Blue>=13||Red>=13) {
                MatchOver=true;LastWin=won;Stats.TeamBestKills=Bots.Where(b=>b.Ally).Max(b=>b.Kills);Stats.Mvp=Stats.Kills>=Stats.TeamBestKills;
                RatingDetails="K/D/A "+Stats.Kills+"/"+Stats.Deaths+"/"+Stats.Assists+" • HS "+Stats.Headshots+" • Precisão "+(Stats.Accuracy*100).ToString("0")+"%";
                Combatant carlos=Bots.Find(b=>b.Name=="Carlos");
                if(carlos!=null&&carlos.Kills==0&&carlos.Deaths>=25) {Game.Night.End(Ending.Carlos);return;}
                Game.Night.CompleteMatch(Stats,won);return;
            }
            Intermission=2;
        }
        public void Dispose()
        {
            if(root!=null)Destroy(root.gameObject);if(controller!=null)Destroy(controller.gameObject);
            if(Texture!=null){Texture.Release();Destroy(Texture);}
        }
    }
}
