using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TicosHouse
{
    // Opt-in integration runner. Uses the shipping player, physics and actual gameplay methods.
    public sealed class RuntimeValidation:MonoBehaviour
    {
        GameRuntime g;
        string output;
        static int restartPhase;
        int checks;
        bool failed;
        void Require(bool success,string name)
        {
            checks++;Debug.Log((success?"QA PASS ":"QA FAIL ")+name);
            if(!success) {failed=true;Debug.LogError("TICOS_QA_FAIL "+name);}
        }
        object Invoke(object target,string method,params object[] args)
        {return target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.2f);
        }
        void RenderRoom(string name,Vector3 position,Vector3 target)
        {
            var camera=g.RoomCamera;Vector3 oldPosition=camera.transform.position;Quaternion oldRotation=camera.transform.rotation;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1280,720,24);rt.antiAliasing=4;rt.Create();camera.targetTexture=rt;camera.transform.position=position;camera.transform.LookAt(target);camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            camera.targetTexture=oldTarget;camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(texture);
        }
        IEnumerator Start()
        {
            g=GetComponent<GameRuntime>();g.TestingFrozen=true;g.Audio.MasterVolume=0;
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--qa-dir");
            output=index>=0&&index+1<args.Length?args[index+1]:Application.persistentDataPath;
            Directory.CreateDirectory(output);
            yield return new WaitForSeconds(restartPhase==0?20:1);
            if(restartPhase==1) {
                Require(g.Night.Internet&&g.Night.Pdl==0&&g.Night.Result==Ending.None&&!g.Night.Started,"scene restart resets all state");
                Require(FindObjectsByType<GameRuntime>(FindObjectsSortMode.None).Length==1,"one runtime after restart");
                Require(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"one audio listener after restart");
                yield return Capture("07-restarted");Debug.Log(failed?"TICOS_QA_FAIL":"TICOS_QA_PASS");Application.Quit(failed?1:0);yield break;
            }
            yield return Capture("01-menu");
            Require(g.Screen.sharedMaterial.shader!=null&&g.Screen.sharedMaterial.shader.isSupported,"shader present and supported");
            Require(g.Fps.Texture.IsCreated(),"monitor render texture allocated");
            Require(Resources.Load<AudioClip>("Voices/brother_warning")!=null,"Portuguese warning audio loaded");
            bool audioValid=true;
            foreach(string id in new[]{"AudioReal/snore0","AudioReal/snore1","AudioReal/snore2","AudioReal/snore3","Voices/brother_warning","Voices/tico"}){
                var clip=Resources.Load<AudioClip>(id);audioValid&=clip!=null&&clip.length>1;
                if(clip!=null){var samples=new float[clip.samples*clip.channels];clip.GetData(samples,0);float peak=0;foreach(float sample in samples){audioValid&=!float.IsNaN(sample);peak=Mathf.Max(peak,Mathf.Abs(sample));}audioValid&=peak>.01f&&peak<=.65f;}
            }
            Require(audioValid,"recorded snore and neural dialogue are non-silent and peak controlled");
            g.Audio.Sleeping=true;g.Audio.House(HouseCue.Snore,0);Require(g.Audio.SnorePlaying,"recorded snore plays on dedicated source");
            g.Audio.Sleeping=false;Invoke(g.Audio,"Update");Require(!g.Audio.SnorePlaying,"snore stops when Tico wakes");
            g.Audio.Quiet=false;g.Audio.Speech("Guilherme: ELE TÁ SUBINDO");Require(g.Audio.DialoguePlaying,"warning uses dedicated dialogue source");
            g.Audio.StopAll();Require(!g.Audio.DialoguePlaying&&!g.Audio.SnorePlaying,"restart stops speech and snore");g.Audio.Sleeping=true;
            Invoke(g,"StartNight");Invoke(g,"Teleport",new Vector3(.3f,0,.6f));
            var player=FindObjectsByType<CharacterController>(FindObjectsSortMode.None).First(c=>c.name=="Gust first person");
            player.transform.rotation=Quaternion.Euler(0,210,0);g.RoomCamera.transform.localRotation=Quaternion.Euler(9,0,0);
            yield return Capture("02-bedroom");
            Require(Resources.Load<Texture2D>("Art/CoverGust")!=null,"Gust cover included in player");
            Require(WorldBuilder.Mat("Warm wood",Color.white).mainTexture!=null&&QualitySettings.antiAliasing==4,"room surface textures and antialiasing enabled");
            RenderRoom("room-polished-desk",new Vector3(.6f,1.8f,1.8f),new Vector3(-1.4f,1.1f,-2.6f));
            RenderRoom("room-polished-bed",new Vector3(-2f,1.7f,.5f),new Vector3(2.3f,.9f,-2));
            var models=new[]{g.GustModel,g.BrotherModel,g.TicoModel};
            for(int person=0;person<models.Length;person++){
                var model=models[person];var position=model.position;var rotation=model.rotation;bool active=model.gameObject.activeSelf;
                model.gameObject.SetActive(true);model.position=new Vector3(0,0,-1);model.rotation=Quaternion.identity;
                RenderRoom("character-"+person,new Vector3(0,1.6f,1.2f),new Vector3(0,1.2f,-1));
                model.position=position;model.rotation=rotation;model.gameObject.SetActive(active);
            }
            Invoke(g,"Teleport",new Vector3(-3.5f,0,0));for(int i=0;i<20;i++)player.Move(Vector3.left*.2f);
            Require(player.transform.position.x> -3.8f,"bedroom wall collision");
            Invoke(g,"Teleport",new Vector3(-1.5f,0,-1.05f));Invoke(g,"Interact");Require(g.Night.AtComputer,"chair proximity interaction");
            Invoke(g,"SyncCursor");Require(g.ComputerLobby&&Cursor.lockState==CursorLockMode.None&&Cursor.visible,"lobby releases cursor for queue button");
            g.Fps.NewMatch();Invoke(g,"SyncCursor");Require(!g.ComputerLobby&&(!Application.isFocused||Cursor.lockState==CursorLockMode.Locked),"match selects free-aim input mode");
            g.Fps.Intermission=0;Require(g.Fps.Bots.Count==9,"four allies and five enemies");
            Require(g.Fps.Bots.Where(b=>b.Ally).Select(b=>b.Name).Distinct().Count()==4,"unique randomized friend team");
            Require(g.Fps.Health==150,"Gust starts with 150 health");
            g.Fps.Tick(.8f,false);yield return Capture("03-computer");
            Require(Resources.Load<Texture2D>("Art/AscentBombA")!=null&&Resources.Load<Texture2D>("Art/AscentMid")!=null,"additional Ascent backgrounds included");
            var enemy=g.Fps.Bots.First(b=>!b.Ally);
            Require(g.Fps.ScenarioCount==5&&Resources.Load<Texture2D>("Art/AscentBombB")!=null&&Resources.Load<Texture2D>("Art/AscentTree")!=null,"five Ascent viewpoints loaded");
            var randomState=UnityEngine.Random.state;UnityEngine.Random.InitState(1202);
            float minX=float.MaxValue,maxX=float.MinValue;int reversals=0,previousDirection=enemy.Direction;bool bounded=true;
            for(int motion=0;motion<600;motion++){
                Invoke(g.Fps,"MoveEnemy",enemy,.016f);minX=Mathf.Min(minX,enemy.ScreenPos.x);maxX=Mathf.Max(maxX,enemy.ScreenPos.x);
                if(enemy.Direction!=previousDirection)reversals++;previousDirection=enemy.Direction;
                bounded&=Mathf.Abs(enemy.Offset)<=43.01f&&g.Fps.HeadRect(enemy).xMin>0&&g.Fps.HeadRect(enemy).xMax<960;
            }
            UnityEngine.Random.state=randomState;
            Require(maxX-minX>70&&reversals>8&&bounded,"agile movement changes direction and stays inside combat lane");
            foreach(var b in g.Fps.Bots){b.Health=0;if(b.Model!=null)b.Model.gameObject.SetActive(false);}
            enemy.Health=100;enemy.SpawnAt=0;enemy.Model.gameObject.SetActive(true);
            g.Fps.Aim=g.Fps.HeadRect(enemy).center;
            g.Fps.Health=117;int previousScenario=g.Fps.Scenario;
            Invoke(g.Fps,"Shoot");Require(g.Fps.Stats.Headshots==1&&g.Fps.Stats.Kills==1,"pixel head hitbox kill");
            Require(g.Fps.Scenario!=previousScenario&&g.Fps.Health==117&&g.Fps.RoundKills==1,"kill changes viewpoint without healing or resetting round stats");
            var attacker=g.Fps.Bots.First(b=>!b.Ally&&b!=enemy);attacker.Health=100;attacker.Skill=1;attacker.SpawnAt=0;attacker.Cooldown=.01f;
            Invoke(g.Fps,"AdvanceScenario");Require(attacker.Cooldown==.01f,"kill transition preserves enemy attack cooldown");
            float beforeTransitionTime=g.Fps.RoundTime;g.Fps.Tick(.02f,false);
            Require(g.Fps.Health<117&&g.Fps.RoundTime>beforeTransitionTime,"enemy attacks and clock continue during viewpoint changes");
            for(int view=0;view<g.Fps.ScenarioCount;view++){
                Invoke(g.Fps,"AdvanceScenario");g.Fps.Cam.Render();RenderTexture previous=RenderTexture.active;RenderTexture.active=g.Fps.Texture;
                var capture=new Texture2D(g.Fps.Texture.width,g.Fps.Texture.height,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,capture.width,capture.height),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(output,"ascent-view-"+g.Fps.Scenario+".png"),capture.EncodeToPNG());Destroy(capture);RenderTexture.active=previous;
            }
            Require(g.Fps.Ammo==23&&g.Fps.Stats.Shots==1&&g.Fps.Stats.Hits==1,"ammo and hit stats consistent");
            g.Fps.NewMatch();g.Fps.Intermission=0;enemy=g.Fps.Bots.First(b=>!b.Ally);
            foreach(var b in g.Fps.Bots){b.Health=0;if(b.Model!=null)b.Model.gameObject.SetActive(false);}
            enemy.Health=100;enemy.Skill=1;enemy.SpawnAt=0;enemy.Model.gameObject.SetActive(true);
            g.Fps.Aim=g.Fps.BodyRect(enemy).center;Invoke(g.Fps,"Shoot");
            Require(enemy.Health==64&&g.Fps.Stats.Headshots==0,"body shot deals 36 without headshot");
            Require(g.Fps.Health==150,"new match restores 150 health");
            for(int i=0;i<180&&g.Fps.Health==150;i++){Physics.SyncTransforms();g.Fps.Tick(.1f,false);}
            Require(g.Fps.Health<150&&enemy.ShotFlash>0&&enemy.AimedAtPlayer,"enemy attacks visibly damage player");
            g.Fps.Health=1;enemy.Cooldown=0;g.Fps.Tick(.01f,false);
            Require(g.Fps.Health==0&&g.Fps.PlayerDead&&g.Fps.Stats.Deaths==1,"enemy fire kills and counts one death");
            randomState=UnityEngine.Random.state;UnityEngine.Random.InitState(1303);
            int headshotsBefore=g.Fps.EnemyHeadshots;bool rapidFire=true;
            for(int burst=0;burst<100;burst++){
                g.Fps.Health=150;g.Fps.PlayerDead=false;enemy.Cooldown=0;
                Invoke(g.Fps,"UpdateBot",enemy,.01f); // restore live round after prior death
                if(g.Fps.Intermission>0){g.Fps.Intermission=0;burst--;continue;}
                rapidFire&=enemy.Cooldown>=.25f&&enemy.Cooldown<=.42f;
            }
            Require(g.Fps.EnemyHeadshots>headshotsBefore&&g.Fps.EnemyHeadshots<headshotsBefore+100,"enemy fire includes headshots and body shots");
            Require(rapidFire,"enemy cadence is 0.25 to 0.42 seconds");
            UnityEngine.Random.state=randomState;
            g.Fps.NewMatch();g.Fps.Intermission=0;
            var ally=g.Fps.Bots.First(b=>b.Ally);enemy=g.Fps.Bots.First(b=>!b.Ally);
            foreach(var b in g.Fps.Bots)b.Health=0;ally.Health=100;ally.Skill=.8f;enemy.Health=100;enemy.SpawnAt=0;
            float savedStress=g.Night.Stress;bool savedMic=g.Night.MicOpen;g.Night.Stress=0;
            float mutedDamage=0,openDamage=0,mutedCover=0,openCover=0;
            randomState=UnityEngine.Random.state;
            for(int mode=0;mode<2;mode++){
                g.Night.MicOpen=mode==1;UnityEngine.Random.InitState(731);
                if(mode==0)mutedCover=g.Fps.TeamCover;else openCover=g.Fps.TeamCover;
                for(int duel=0;duel<300;duel++){
                    enemy.Health=100;ally.Cooldown=0;Invoke(g.Fps,"UpdateBot",ally,.01f);
                    if(mode==0)mutedDamage+=100-enemy.Health;else openDamage+=100-enemy.Health;
                }
            }
            Require(openDamage>mutedDamage*1.15f&&mutedDamage>0,"communication improves ally duels while muted allies remain useful");
            Require(openCover>mutedCover*2,"communication improves living ally cover");
            ally.Health=0;Require(g.Fps.TeamCover==0,"dead teammates provide no cover");
            UnityEngine.Random.state=randomState;g.Night.Stress=savedStress;g.Night.MicOpen=savedMic;
            g.Fps.NewMatch();randomState=UnityEngine.Random.state;
            var teammates=g.Fps.Bots.Where(b=>b.Ally).ToArray();
            string[] benchmarkNames={"Joaobuild","Trolezi","Carlos","Munhak"};
            for(int i=0;i<4;i++){teammates[i].Name=benchmarkNames[i];teammates[i].Skill=i<2?.75f:.22f;teammates[i].Exceptional=false;}
            for(int mode=0;mode<2;mode++){
                int goodKills=0,weakKills=0;bool capped=true;g.Night.MicOpen=mode==1;
                for(int trial=0;trial<60;trial++){
                    UnityEngine.Random.InitState(9000+trial);g.Night.Stress=20;g.Fps.Blue=g.Fps.Red=0;g.Fps.Intermission=0;Invoke(g.Fps,"NewRound");
                    int goodBefore=teammates[0].Kills+teammates[1].Kills,weakBefore=teammates[2].Kills+teammates[3].Kills;
                    float nextShot=.9f;
                    for(int step=0;step<400&&g.Fps.Intermission<=0;step++){
                        g.Fps.Tick(.05f,false);
                        if(!g.Fps.PlayerDead&&g.Fps.Intermission<=0&&g.Fps.RoundTime>=nextShot){
                            var target=g.Fps.Bots.FirstOrDefault(b=>g.Fps.Visible(b));
                            if(target!=null){g.Fps.Aim=g.Fps.BodyRect(target).center;Invoke(g.Fps,"Shoot");}nextShot=g.Fps.RoundTime+.75f;
                        }
                    }
                    goodKills+=teammates[0].Kills+teammates[1].Kills-goodBefore;weakKills+=teammates[2].Kills+teammates[3].Kills-weakBefore;
                    capped&=g.Fps.AllyRoundKills<=2&&g.Fps.AllyRoundKills<=g.Fps.AllyKillBudget;
                    if(trial%10==0)yield return null;
                }
                Debug.Log("TEAM_BALANCE mic="+(mode==1)+" goodKillsPerRound="+(goodKills/60f)+" weakKillsPerRound="+(weakKills/60f));
                Require(goodKills>=48&&goodKills>weakKills*3,"good teammates contribute consistently and outperform Carlos/Munhak mode "+mode);
                Require(capped&&(goodKills+weakKills)/60f<=1.7f,"team stays under two kills each round and near target average mode "+mode);
            }
            UnityEngine.Random.state=randomState;g.Night.Stress=savedStress;g.Night.MicOpen=savedMic;
            g.Fps.NewMatch();g.Fps.Intermission=0;g.Fps.RoundTime=.36f;
            Require(g.Fps.Bots.Count(b=>g.Fps.Visible(b))==1,"only one enemy enters at a time");
            var queued=g.Fps.Bots.Last(b=>!b.Ally);queued.Skill=1;queued.Cooldown=0;int healthBeforeQueue=g.Fps.Health;
            Invoke(g.Fps,"UpdateBot",queued,1f);
            Require(g.Fps.Health==healthBeforeQueue&&queued.ShotFlash==0,"queued enemy cannot fire offscreen");
            bool sequenceCorrect=true;
            for(int duel=0;duel<5;duel++){
                var current=g.Fps.Bots.First(b=>!b.Ally&&b.Alive);Invoke(g.Fps,"Kill",current,null);
                sequenceCorrect&=g.Fps.Bots.Count(b=>g.Fps.Visible(b))==0;
                g.Fps.RoundTime+=.3f;
                sequenceCorrect&=g.Fps.Bots.Count(b=>g.Fps.Visible(b))==(duel==4?0:1);
            }
            Require(sequenceCorrect&&g.Fps.Health==150,"five sequential duels with no overlap or healing");
            g.Fps.NewMatch();g.Fps.Intermission=0;
            foreach(var b in g.Fps.Bots)b.Cooldown=999;
            g.Fps.RoundTime=18;g.Fps.Tick(.01f,false);
            Require(g.Fps.Red==1&&g.Fps.Blue==0,"waiting with numerical advantage cannot win the objective");
            g.Fps.NewMatch();g.Fps.Intermission=0;
            var oldAim=g.Fps.Aim;g.Fps.Aim=new Vector2(0,0);int oldHits=g.Fps.Stats.Hits;Invoke(g.Fps,"Shoot");Require(g.Fps.Stats.Hits==oldHits,"miss does not award hit");g.Fps.Aim=oldAim;
            g.Fps.Ammo=0;g.Fps.Reload();Require(g.Fps.ReloadRemaining>0,"empty magazine reload starts");g.Fps.Tick(1.5f,false);Require(g.Fps.Ammo==24,"reload restores magazine");
            g.Fps.NewMatch();g.Fps.Intermission=0;int frames=0;
            foreach(var b in g.Fps.Bots)b.Cooldown=999;
            g.Fps.Aim=new Vector2(20,450);float originalAimY=g.Fps.Aim.y;
            Require(g.Fps.Spread==0,"first shot has no weapon spread");
            for(int spray=0;spray<8;spray++)Invoke(g.Fps,"Shoot");
            Require(g.Fps.Spread>=20&&g.Fps.Aim.y<originalAimY-30&&g.Fps.Ammo==16,"held spray builds spread and actual upward recoil");
            float heatBeforeKill=g.Fps.SprayHeat;Invoke(g.Fps,"AdvanceScenario");
            Require(g.Fps.SprayHeat==heatBeforeKill,"kill transition does not reset weapon accuracy");
            g.Fps.Tick(1f,false);Require(g.Fps.Spread==0,"cease fire recovers weapon accuracy");
            g.Fps.NewMatch();g.Fps.Intermission=0;
            while(!g.Fps.MatchOver&&frames<20000) {
                Physics.SyncTransforms();g.Fps.Tick(.1f,false);frames++;
                if(frames%100==0)yield return null;
            }
            Require(g.Fps.MatchOver&&(g.Fps.Blue==13||g.Fps.Red==13),"unattended full match reaches exactly 13");
            Require(g.Fps.Blue+g.Fps.Red<=25,"no invalid round totals");
            int matches=g.Night.Matches;g.Fps.Tick(1,false);Require(g.Night.Matches==matches,"completed match cannot award twice");
            g.Night.Result=Ending.None;g.Night.MonitorOn=false;Invoke(g,"Interact");Require(!g.Night.AtComputer,"stand up interaction");
            Invoke(g,"Teleport",new Vector3(1.7f,0,-1.8f));Invoke(g,"Interact");Require(g.Night.InBed,"bed proximity interaction");
            g.Night.Tico=TicoState.Checking;g.Night.StateRemaining=0;g.Night.Noise=0;g.Night.Tick(.05f);
            Require(g.Night.Result==Ending.None,"dark monitor and bed survive inspection");
            yield return new WaitForSeconds(1);yield return Capture("04-hiding");
            g.Night.Tico=TicoState.Sleeping;g.Night.StateRemaining=999;Invoke(g,"Interact");Require(!g.Night.InBed,"safe leave bed");
            g.Night.DogPresent=true;g.Night.Stress=60;Invoke(g,"Teleport",new Vector3(.5f,0,.8f));Invoke(g,"Interact");Require(g.Night.Stress==40,"pet interaction reduces stress");
            g.Night.BrotherPresent=true;g.Night.DogPresent=true;
            g.Night.BeginFinalHunt();g.Night.InBed=true;g.Night.MonitorOn=false;g.Night.Elapsed=NightSimulation.NightSeconds-.01f;
            for(int i=0;i<220;i++)g.Night.Tick(.1f);
            Require(g.Night.Result==Ending.Wifi&&!g.Night.Internet,"final hunt overrides bed and sunrise");
            yield return new WaitForSeconds(.4f);yield return Capture("05-jumpscare");
            yield return new WaitForSeconds(2.5f);yield return Capture("06-ending");
            Debug.Log("TICOS_QA_FIRST_PHASE "+checks+" checks; failed="+failed);
            if(failed){Application.Quit(1);yield break;}
            restartPhase=1;Invoke(g,"Restart");
        }
    }
}

