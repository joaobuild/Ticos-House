using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TicosHouse
{
    public sealed class Combatant
    {
        public string Name; public bool Ally; public float Skill,Health=100,Cooldown,SpawnAt,Height=118,Phase;
        public int Kills,Deaths,Assists,Agent;public bool Exceptional,DamagedByPlayer;
        public Transform Model;public Vector2 ScreenPos;public float ShotFlash;public bool AimedAtPlayer;
        public float Offset,MoveSpeed,DecisionRemaining,Suppressed;public int Direction;
        public bool Alive {get{return Health>0;}}
    }
    // Fixed-camera pixel aim shooter: mouse aim and AD strafing, no map navigation.
    public sealed class FPSMinigame:MonoBehaviour
    {
        public GameRuntime Game;public Camera Cam;public RenderTexture Texture;
        public MatchStats Stats=new MatchStats();public readonly List<Combatant>Bots=new List<Combatant>();
        public const int MaxHealth=150;
        public int Blue,Red,Health=MaxHealth,Ammo=24,RoundKills;
        public int Scenario {get;private set;}
        public int ScenarioCount {get{return backgrounds.Length;}}
        public string LocationName {get{return new[]{"MERCADO B","BOMB A / HEAVEN","QUADRADO / VARANDA","BOMB B / BOATHOUSE","ÁRVORE / JARDIM"}[Scenario];}}
        public float TransitionRemaining;
        public float EnemyHeadshotFlash;
        public int EnemyHeadshots;
        public float Coordination {get{return Mathf.InverseLerp(.55f,1.2f,Game.Night.Communication);}}
        public float TeamCover {get{return Bots.Where(b=>b.Ally&&b.Alive).Sum(b=>b.Skill)/4f*Coordination;}}
        Material backgroundMaterial;Texture2D[] backgrounds;
        public float RoundTime,Intermission=3,ReloadRemaining,HitFlash,DamageFlash,FlashRemaining,LagRemaining,MuzzleFlash;
        public bool Active,MatchOver,LastWin,PlayerDead;
        public string Banner="ASCENT",KillFeed="",RatingDetails="";
        public float BannerTime,FeedTime;
        public Vector2 Aim=new Vector2(480,270);
        Transform root;float fireTime,abilityCooldown;int losses;Material agentMaterial;
        public const float ViewWidth=960,ViewHeight=540;
        public void Initialize(GameRuntime game)
        {
            Game=game;root=new GameObject("Ascent - fixed pixel scene").transform;root.position=new Vector3(100,0,0);
            var cameraObject=new GameObject("Ascent camera");cameraObject.transform.SetParent(root,false);cameraObject.transform.localPosition=new Vector3(0,0,-10);
            Cam=cameraObject.AddComponent<Camera>();Cam.orthographic=true;Cam.orthographicSize=270;Cam.nearClipPlane=.1f;Cam.farClipPlane=30;
            Cam.clearFlags=CameraClearFlags.SolidColor;Cam.backgroundColor=Color.black;Cam.cullingMask=1<<8;
            Texture=new RenderTexture(640,360,16);Texture.filterMode=FilterMode.Point;Texture.name="Pixel Ascent monitor";Cam.targetTexture=Texture;
            var background=GameObject.CreatePrimitive(PrimitiveType.Quad);background.name="Ascent artwork";background.layer=8;background.transform.SetParent(root,false);
            background.transform.localPosition=new Vector3(0,0,5);background.transform.localScale=new Vector3(960,540,1);Destroy(background.GetComponent<Collider>());
            backgrounds=new[]{Resources.Load<Texture2D>("Art/Ascent"),Resources.Load<Texture2D>("Art/AscentBombA"),Resources.Load<Texture2D>("Art/AscentMid"),Resources.Load<Texture2D>("Art/AscentBombB"),Resources.Load<Texture2D>("Art/AscentTree")};
            backgroundMaterial=new Material(Resources.Load<Material>("PixelBackground"));backgroundMaterial.mainTexture=backgrounds[0];background.GetComponent<Renderer>().sharedMaterial=backgroundMaterial;
            agentMaterial=Resources.Load<Material>("PixelAgent");
        }
        public void NewMatch()
        {
            if(Game.Night.FinalHunt||Game.Night.Result!=Ending.None)return;
            foreach(var b in Bots)if(b.Model!=null)Destroy(b.Model.gameObject);Bots.Clear();
            Stats=new MatchStats();Blue=Red=0;MatchOver=false;Active=true;losses=0;Scenario=0;backgroundMaterial.mainTexture=backgrounds[0];
            string[] friends={"Joaobuild","Trolezi","Carlos","Loogins","Tavinho","Munhak"};friends=friends.OrderBy(x=>Random.value).ToArray();
            for(int i=0;i<4;i++)Spawn(friends[i],true,(friends[i]=="Carlos"||friends[i]=="Munhak")?.22f:.65f+Random.value*.18f,i);
            for(int i=0;i<5;i++)Spawn(new[]{"Jett","Phoenix","Sage","Reyna","Omen"}[i],false,.67f+Game.Night.Difficulty*.12f,i);
            NewRound();Intermission=3;Banner="PARTIDA ENCONTRADA - ASCENT";
        }
        void Spawn(string name,bool ally,float skill,int index)
        {
            var b=new Combatant{Name=name,Ally=ally,Skill=skill,Agent=index,Exceptional=Random.value<.025f};if(b.Exceptional)b.Skill=.97f;
            if(!ally){
                GameObject q=GameObject.CreatePrimitive(PrimitiveType.Quad);q.name=name+" pixel enemy";q.layer=8;q.transform.SetParent(root,false);Destroy(q.GetComponent<Collider>());
                Material m=new Material(agentMaterial);m.mainTexture=Resources.Load<Texture2D>("Art/Agents");m.mainTextureScale=new Vector2(.2f,1);m.mainTextureOffset=new Vector2(index*.2f,0);
                q.GetComponent<Renderer>().material=m;b.Model=q.transform;
            }
            Bots.Add(b);
        }
        void NewRound()
        {
            Health=MaxHealth;Ammo=24;RoundTime=0;RoundKills=0;PlayerDead=false;ReloadRemaining=0;FlashRemaining=0;abilityCooldown=0;Aim=new Vector2(480,270);TransitionRemaining=0;fireTime=0;DamageFlash=HitFlash=MuzzleFlash=EnemyHeadshotFlash=0;EnemyHeadshots=0;
            foreach(var b in Bots){b.Health=100;b.DamagedByPlayer=false;b.Cooldown=Random.Range(.45f,.7f);b.Phase=Random.Range(0,6.28f);
                b.SpawnAt=b.Ally?0:.35f;b.Height=b.Agent%2==0?122:108;b.Suppressed=0;
                b.Offset=Random.Range(-38f,38f);b.DecisionRemaining=0;b.Direction=Random.value<.5f?-1:1;
                b.ShotFlash=0;if(!b.Ally){PlaceEnemy(b);b.Model.gameObject.SetActive(false);}
            }
            Banner="ROUND "+(Blue+Red+1);BannerTime=2;
        }
        public Rect SpriteRect(Combatant b){return new Rect(b.ScreenPos.x-b.Height*.25f,b.ScreenPos.y-b.Height,b.Height*.5f,b.Height);}
        public Rect HeadRect(Combatant b){Rect r=SpriteRect(b);return new Rect(b.ScreenPos.x-b.Height*.10f,r.y+b.Height*.10f,b.Height*.20f,b.Height*.19f);}
        public Rect BodyRect(Combatant b){Rect r=SpriteRect(b);return new Rect(b.ScreenPos.x-b.Height*.18f,r.y+b.Height*.28f,b.Height*.36f,b.Height*.66f);}
        public bool Visible(Combatant b){return !b.Ally&&b.Alive&&RoundTime>=b.SpawnAt&&Intermission<=0&&b==Bots.FirstOrDefault(x=>!x.Ally&&x.Alive);}
        void PlaceEnemy(Combatant b)
        {
            // Feet positions match the floor/balcony in each painting (960 x 540 coordinates).
            float x=Scenario==2?340:Scenario==3?405:Scenario==4?330:450;float y=Scenario==0?346:Scenario==1?357:Scenario==2?235:375;
            b.Height=Scenario==2?88:Scenario==1?105:118;
            b.ScreenPos=new Vector2(x+(b.Agent%2)*53+b.Offset,y);
            b.Model.localPosition=new Vector3(b.ScreenPos.x-480,270-b.ScreenPos.y+b.Height*.5f,1-b.Agent*.05f);
            b.Model.localScale=new Vector3(b.Height*.5f,b.Height,1);
        }
        void MoveEnemy(Combatant b,float dt)
        {
            b.DecisionRemaining-=dt;
            if(b.DecisionRemaining<=0){
                b.DecisionRemaining=Random.Range(.18f,.55f);b.Direction=Random.value<.5f?-1:1;
                b.MoveSpeed=Random.value<.18f?0:Random.Range(110f,210f)*(1+Game.Night.Difficulty*.15f);
            }
            float limit=Scenario==0||Scenario==1?43:85;
            b.Offset+=b.Direction*b.MoveSpeed*dt;
            if(Mathf.Abs(b.Offset)>limit){b.Offset=Mathf.Clamp(b.Offset,-limit,limit);b.Direction=b.Offset>0?-1:1;}
            PlaceEnemy(b);
        }
        void AdvanceScenario()
        {
            Scenario=(Scenario+1)%backgrounds.Length;backgroundMaterial.mainTexture=backgrounds[Scenario];TransitionRemaining=.15f;
            // Changing artwork must never reset enemy attacks or grant chained invulnerability.
            foreach(var b in Bots){b.ShotFlash=0;b.Offset=Random.Range(-38f,38f);b.DecisionRemaining=0;if(!b.Ally)PlaceEnemy(b);}
            Banner="ASCENT • "+LocationName;BannerTime=1.2f;
        }
        public void Tick(float dt,bool controls)
        {
            if(!Active||MatchOver||!Game.Night.Internet||Game.Night.Result!=Ending.None)return;
            HitFlash=Mathf.Max(0,HitFlash-dt);DamageFlash=Mathf.Max(0,DamageFlash-dt);MuzzleFlash=Mathf.Max(0,MuzzleFlash-dt);FeedTime-=dt;BannerTime-=dt;
            EnemyHeadshotFlash=Mathf.Max(0,EnemyHeadshotFlash-dt);
            foreach(var b in Bots){b.ShotFlash=Mathf.Max(0,b.ShotFlash-dt);b.Suppressed=Mathf.Max(0,b.Suppressed-dt);}
            if(Intermission>0){Intermission-=dt;if(Intermission<=0)NewRound();return;}
            TransitionRemaining=Mathf.Max(0,TransitionRemaining-dt);
            RoundTime+=dt;fireTime-=dt;abilityCooldown-=dt;FlashRemaining-=dt;LagRemaining=Mathf.Max(0,LagRemaining-dt);
            if(ReloadRemaining>0){ReloadRemaining-=dt;if(ReloadRemaining<=0)Ammo=24;}
            if(controls&&!PlayerDead)Control();
            foreach(var b in Bots){
                if(!b.Ally){
                    bool visible=Visible(b);b.Model.gameObject.SetActive(visible);
                    if(visible)MoveEnemy(b,dt);
                }
                if(b.Alive)UpdateBot(b,dt);
            }
            int allies=Bots.Count(b=>b.Ally&&b.Alive)+(PlayerDead?0:1),enemies=Bots.Count(b=>!b.Ally&&b.Alive);
            if(enemies==0)EndRound(true);else if(allies==0)EndRound(false);
            else if(RoundTime>18)EndRound(false); // Gust's team attacks: surviving defenders deny the objective.
        }
        void Control()
        {
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            Aim+=new Vector2(Input.GetAxisRaw("Mouse X"),-Input.GetAxisRaw("Mouse Y"))*Game.Sensitivity*3.2f;
            Aim=new Vector2(Mathf.Clamp(Aim.x,3,957),Mathf.Clamp(Aim.y,3,537));
            if(Input.GetKeyDown(KeyCode.R))Reload();
            if(Input.GetKeyDown(KeyCode.Q)&&abilityCooldown<=0){abilityCooldown=16;FlashRemaining=3.2f;Game.Audio.Fps("ability",Vector3.zero,.35f);Banner="PULSO - precisão inimiga reduzida";BannerTime=2;}
            if(Input.GetMouseButton(0)&&fireTime<=0&&ReloadRemaining<=0&&LagRemaining<=0)Shoot();
        }
        public void Reload(){if(Ammo<24&&ReloadRemaining<=0){ReloadRemaining=1.4f;Game.Audio.Fps("reload",Vector3.zero,.3f);}}
        void Shoot()
        {
            if(Ammo<=0){Reload();return;}
            Ammo--;Stats.Shots++;fireTime=.17f;MuzzleFlash=.075f;Game.Night.AddNoise(.4f);Game.Audio.Fps("shot",Vector3.zero,.65f);
            float tremor=Mathf.Max(0,Game.Night.Stress-65)*.08f;
            Vector2 shot=Aim+Random.insideUnitCircle*tremor;
            foreach(var b in Bots.Where(Visible).OrderByDescending(b=>b.Agent)){
                bool head=HeadRect(b).Contains(shot);if(!head&&!BodyRect(b).Contains(shot))continue;
                Stats.Hits++;HitFlash=.16f;b.DamagedByPlayer=true;b.Health-=head?110:36;
                if(b.Health<=0){Stats.Kills++;RoundKills++;if(head)Stats.Headshots++;Kill(b,null);KillFeed="Gust > "+b.Name+(head?" [HEADSHOT]":"");FeedTime=3;Game.Night.Stress=Mathf.Max(0,Game.Night.Stress-2);AdvanceScenario();}
                break;
            }
        }
        void UpdateBot(Combatant b,float dt)
        {
            if(!b.Ally&&!Visible(b))return;
            b.Cooldown-=dt;if(b.Cooldown>0)return;b.Cooldown=b.Ally?Random.Range(1.05f,1.5f)*Mathf.Lerp(2f,1f,Coordination):Random.Range(.22f,.38f);
            if(b.Ally){
                var enemies=Bots.Where(Visible).ToArray();if(enemies.Length==0)return;
                var target=enemies[Random.Range(0,enemies.Length)];
                if(Random.value<b.Skill*Mathf.Lerp(.20f,.85f,Coordination)){
                    target.Health-=48;target.Suppressed=.65f;
                    if(target.Health<=0)Kill(target,b);
                }
            }else{
                Game.Audio.Fps("shot",new Vector3((b.ScreenPos.x-480)/18,0,0),.2f);
                var friends=Bots.Where(x=>x.Ally&&x.Alive).ToArray();bool aimPlayer=!PlayerDead&&(friends.Length==0||Random.value<.94f-TeamCover*.52f);
                b.ShotFlash=.18f;b.AimedAtPlayer=aimPlayer;
                float chance=b.Skill*(FlashRemaining>0?.12f:1)*(b.Suppressed>0?.55f:1);
                if(Random.value<chance){
                    bool headshot=Random.value<.30f;int damage=headshot?Random.Range(100,126):Random.Range(30,45);
                    if(aimPlayer){if(headshot){EnemyHeadshots++;EnemyHeadshotFlash=.65f;}Health-=damage;DamageFlash=.23f;if(Health<=0){Health=0;PlayerDead=true;Stats.Deaths++;if(RoundTime<7)Stats.EarlyDeaths++;Game.Night.Stress=Mathf.Min(100,Game.Night.Stress+6);b.Kills++;Banner=headshot?"VOCÊ CAIU — HEADSHOT":"VOCÊ CAIU - o time continua";BannerTime=3;}}
                    else if(friends.Length>0){var target=friends[Random.Range(0,friends.Length)];target.Health-=damage;if(target.Health<=0)Kill(target,b);}
                }
                if(Random.value<.3f)Game.Audio.Fps("step",new Vector3((b.ScreenPos.x-480)/18,0,0),.35f);
            }
        }
        void Kill(Combatant target,Combatant killer)
        {
            target.Health=0;target.Deaths++;if(target.Model!=null)target.Model.gameObject.SetActive(false);
            if(!target.Ally){var next=Bots.FirstOrDefault(b=>!b.Ally&&b.Alive);if(next!=null)next.SpawnAt=Mathf.Max(next.SpawnAt,RoundTime+.25f);}
            if(killer!=null){killer.Kills++;if(target.DamagedByPlayer&&!target.Ally)Stats.Assists++;}
        }
        void EndRound(bool won)
        {
            if(Intermission>0||MatchOver)return;
            if(won){Blue++;losses=0;Game.Night.Stress=Mathf.Max(0,Game.Night.Stress-1.5f);if(RoundKills>=2)Stats.MultiKills++;
                if(!PlayerDead&&Bots.All(b=>!b.Ally||!b.Alive)&&RoundKills>0)Stats.Clutches++;if((Blue>=11||Red>=11)&&RoundKills>0)Stats.Decisive++;
            }else{Red++;losses++;Game.Night.Stress=Mathf.Min(100,Game.Night.Stress+2+losses);}
            Banner=won?"ROUND VENCIDO":"ROUND PERDIDO";BannerTime=3;Game.Audio.Fps("ping",Vector3.zero,.2f);
            foreach(var b in Bots)if(b.Model!=null)b.Model.gameObject.SetActive(false);
            if(Blue>=13||Red>=13){
                MatchOver=true;LastWin=won;Stats.TeamBestKills=Bots.Where(b=>b.Ally).Max(b=>b.Kills);Stats.Mvp=Stats.Kills>=Stats.TeamBestKills;
                RatingDetails="K/D/A "+Stats.Kills+"/"+Stats.Deaths+"/"+Stats.Assists+" - HS "+Stats.Headshots+" - Precisão "+(Stats.Accuracy*100).ToString("0")+"%";
                var carlos=Bots.Find(b=>b.Name=="Carlos");if(carlos!=null&&carlos.Kills==0&&carlos.Deaths>=25){Game.Night.End(Ending.Carlos);return;}
                Game.Night.CompleteMatch(Stats,won);return;
            }
            Intermission=2;
        }
        public void Dispose(){if(root!=null)Destroy(root.gameObject);if(Texture!=null){Texture.Release();Destroy(Texture);}}
    }
}
