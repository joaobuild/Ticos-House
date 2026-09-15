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
        IEnumerator Start()
        {
            g=GetComponent<GameRuntime>();g.TestingFrozen=true;
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
            Invoke(g,"StartNight");Invoke(g,"Teleport",new Vector3(.3f,0,.6f));
            var player=FindObjectsByType<CharacterController>(FindObjectsSortMode.None).First(c=>c.name=="Gust first person");
            player.transform.rotation=Quaternion.Euler(0,210,0);g.RoomCamera.transform.localRotation=Quaternion.Euler(9,0,0);
            yield return Capture("02-bedroom");
            Invoke(g,"Teleport",new Vector3(-3.5f,0,0));for(int i=0;i<20;i++)player.Move(Vector3.left*.2f);
            Require(player.transform.position.x> -3.8f,"bedroom wall collision");
            Invoke(g,"Teleport",new Vector3(-1.5f,0,-1.05f));Invoke(g,"Interact");Require(g.Night.AtComputer,"chair proximity interaction");
            Invoke(g,"SyncCursor");Require(g.ComputerLobby&&Cursor.lockState==CursorLockMode.None&&Cursor.visible,"lobby releases cursor for queue button");
            g.Fps.NewMatch();Invoke(g,"SyncCursor");Require(!g.ComputerLobby&&(!Application.isFocused||Cursor.lockState==CursorLockMode.Locked),"match selects free-aim input mode");
            g.Fps.Intermission=0;Require(g.Fps.Bots.Count==9,"four allies and five enemies");
            Require(g.Fps.Bots.Where(b=>b.Ally).Select(b=>b.Name).Distinct().Count()==4,"unique randomized friend team");
            g.Fps.Tick(.8f,false);yield return Capture("03-computer");
            Require(g.Fps.Health==150,"Gust starts with 150 health");
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
            Require(g.Fps.TransitionRemaining>0,"viewpoint change grants reaction time");
            g.Fps.Tick(.2f,false);Require(g.Fps.Health==117&&g.Fps.TransitionRemaining>0,"no damage during viewpoint transition");
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
            g.Fps.NewMatch();g.Fps.Intermission=0;
            var oldAim=g.Fps.Aim;g.Fps.Aim=new Vector2(0,0);int oldHits=g.Fps.Stats.Hits;Invoke(g.Fps,"Shoot");Require(g.Fps.Stats.Hits==oldHits,"miss does not award hit");g.Fps.Aim=oldAim;
            g.Fps.Ammo=0;g.Fps.Reload();Require(g.Fps.ReloadRemaining>0,"empty magazine reload starts");g.Fps.Tick(1.5f,false);Require(g.Fps.Ammo==24,"reload restores magazine");
            g.Fps.NewMatch();g.Fps.Intermission=0;int frames=0;
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

