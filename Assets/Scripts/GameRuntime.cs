using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace TicosHouse
{
    public sealed class GameRuntime:MonoBehaviour
    {
        public NightSimulation Night;
        public FPSMinigame Fps;
        public ProceduralAudio Audio;
        public Transform RoomRoot,Door,TicoModel,BrotherModel,DogModel,GustModel;
        public Renderer Screen;
        public Light MonitorLight;
        public float Sensitivity=2.0f;
        public bool TestingFrozen;
        public Camera RoomCamera;
        CharacterController player;
        float yaw=195,pitch=9, subtitleTime, scareTime, footTimer, notificationTime, flicker;
        float savedVolume=.45f, flickerRemaining;
        string subtitle="",notification="";
        bool showHelp,endingSaved;
        Material screenMat;
        GUIStyle text,small,title,heading,center,button;
        Texture2D pixel;
        Texture2D cover;
        Color teal=new Color(.22f,.88f,.77f), amber=new Color(1,.68f,.32f), pale=new Color(.86f,.90f,.90f), red=new Color(1,.32f,.27f);
        Vector3 seat=new Vector3(-1.5f,0,-1.68f),bed=new Vector3(2.8f,0,-2.30f);
        public bool ComputerView { get {return Night.AtComputer&&Night.MonitorOn&&Night.Started&&Night.Result==Ending.None;} }
        public bool ComputerLobby { get {return ComputerView&&(!Fps.Active||Fps.MatchOver);} }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if(FindFirstObjectByType<GameRuntime>()==null)new GameObject("Tico's House").AddComponent<GameRuntime>(); }
        void Awake()
        {
            Application.targetFrameRate=90; QualitySettings.vSyncCount=1;
            QualitySettings.antiAliasing=4;QualitySettings.pixelLightCount=8;QualitySettings.shadowResolution=ShadowResolution.High;
            cover=Resources.Load<Texture2D>("Art/CoverGust");
            Sensitivity=PlayerPrefs.GetFloat("Sensitivity",2);
            RenderSettings.ambientLight=new Color(.10f,.13f,.18f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.017f,.025f,.04f);RenderSettings.fogDensity=.018f;RenderSettings.fogMode=FogMode.Exponential;
            Night=new NightSimulation(System.Environment.TickCount);
            var p=new GameObject("Gust first person");p.transform.position=new Vector3(.3f,0,.6f);
            player=p.AddComponent<CharacterController>();player.height=1.75f;player.center=Vector3.up*.875f;player.radius=.24f;player.stepOffset=.22f;
            var c=new GameObject("House ears and eyes");c.transform.SetParent(p.transform,false);c.transform.localPosition=Vector3.up*1.65f;
            RoomCamera=c.AddComponent<Camera>();RoomCamera.nearClipPlane=.06f;RoomCamera.farClipPlane=70;RoomCamera.fieldOfView=72;
            RoomCamera.allowHDR=true;RoomCamera.allowMSAA=true;
            RoomCamera.cullingMask=~(1<<8);
            RoomCamera.clearFlags=CameraClearFlags.SolidColor;RoomCamera.backgroundColor=new Color(.01f,.018f,.03f);c.AddComponent<AudioListener>();
            Audio=gameObject.AddComponent<ProceduralAudio>();Audio.Listener=c.transform;Audio.MasterVolume=PlayerPrefs.GetFloat("MasterVolume",.65f);
            Night.Sound+=Audio.House;Night.Dialogue+=Speak;Night.Finished+=OnFinish;
            Night.Effect+=effect=>{if(effect=="lag"&&Fps!=null)Fps.LagRemaining=2.3f;else flickerRemaining=1.2f;};
            WorldBuilder.Room(this);
            Fps=gameObject.AddComponent<FPSMinigame>();Fps.Initialize(this);
            screenMat=Screen.material;screenMat.mainTexture=Fps.Texture;screenMat.SetTexture("_EmissionMap",Fps.Texture);screenMat.SetColor("_EmissionColor",Color.white*.65f);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            StartCoroutine(Automation());
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--qa-test")>=0)gameObject.AddComponent<RuntimeValidation>();
        }
        void Speak(string s){subtitle=s;subtitleTime=6;if(Audio!=null)Audio.Speech(s);}
        void OnFinish(Ending ending)
        {
            if(ending==Ending.Caught||ending==Ending.Wifi) {scareTime=2.5f;Audio.Scare();Night.AtComputer=false;}
            else Audio.Fps("ping",Vector3.zero);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(!endingSaved) { PlayerPrefs.SetInt("Ending"+(int)ending,1);PlayerPrefs.Save();endingSaved=true; }
        }
        void StartNight()
        {
            Night.Started=true;showHelp=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            Speak("21h. O combinado era dormir. A fila chamou.");
        }
        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.05f);
            if(Input.GetKeyDown(KeyCode.Escape)&&Night.Started&&Night.Result==Ending.None) {
                Night.Paused=!Night.Paused; Cursor.lockState=Night.Paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=Night.Paused;
            }
            SyncCursor();Audio.Quiet=Night.Paused;
            if(Night.Paused)return;
            subtitleTime-=dt;notificationTime-=dt;
            if(!TestingFrozen&&Night.Started&&Night.Result==Ending.None) {
                InputNight(dt); Night.Tick(dt);Fps.Tick(dt,ComputerView);
            }
            Audio.FpsVolume=Night.FpsVolume;Audio.Online=Night.Internet;Audio.WearingHeadphones=Night.AtComputer&&Night.MonitorOn;
            Audio.Sleeping=Night.Tico==TicoState.Sleeping;
            flickerRemaining-=dt;MonitorLight.enabled=Night.MonitorOn&&(flickerRemaining<=0||Mathf.Sin(Time.time*70)>0);
            screenMat.SetColor("_EmissionColor",Night.MonitorOn?Color.white*.65f:Color.black);
            screenMat.color=Night.MonitorOn?Color.white:Color.black;
            GustModel.gameObject.SetActive(Night.AtComputer);
            BrotherModel.gameObject.SetActive(Night.BrotherPresent);
            DogModel.gameObject.SetActive(Night.DogPresent);
            if(Night.DogPresent)DogModel.LookAt(new Vector3(Night.Tico>=TicoState.Stairs?1.3f:player.transform.position.x,0,Night.Tico>=TicoState.Stairs?4:player.transform.position.z));
            UpdateHouse(dt);
            if(scareTime>0) {
                scareTime-=dt;TicoModel.gameObject.SetActive(true);
                TicoModel.position=RoomCamera.transform.position+RoomCamera.transform.forward*Mathf.Lerp(.55f,1.8f,Mathf.Clamp01(scareTime-1.5f))-Vector3.up*1.66f;
                TicoModel.LookAt(new Vector3(RoomCamera.transform.position.x,TicoModel.position.y,RoomCamera.transform.position.z));
                RoomCamera.transform.localRotation*=Quaternion.Euler(Mathf.Sin(Time.time*84)*1.8f,Mathf.Cos(Time.time*61)*1.6f,0);
                MonitorLight.enabled=true;MonitorLight.color=red;MonitorLight.intensity=4;
            }
            SyncCursor();
        }
        void SyncCursor()
        {
            bool free=!Night.Started||Night.Paused||Night.Result!=Ending.None||ComputerLobby;
            CursorLockMode mode=free?CursorLockMode.None:CursorLockMode.Locked;
            if(Cursor.lockState!=mode)Cursor.lockState=mode;
            Cursor.visible=free;
        }
        void OnApplicationFocus(bool focused)
        {
            if(!focused&&Night!=null&&Night.Started&&Night.Result==Ending.None&&!TestingFrozen){Night.Paused=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
        void InputNight(float dt)
        {
            if(Input.GetKeyDown(KeyCode.M)) {Night.MicOpen=!Night.MicOpen;Notify(Night.MicOpen?"MICROFONE ABERTO":"MICROFONE MUTADO");}
            if(Input.GetKeyDown(KeyCode.V)) {if(Night.FpsVolume>.01f){savedVolume=Night.FpsVolume;Night.FpsVolume=0;}else Night.FpsVolume=savedVolume;Notify("ÁUDIO FPS  "+(int)(Night.FpsVolume*100)+"%");}
            float scroll=Input.GetAxis("Mouse ScrollWheel");
            if(Input.GetKey(KeyCode.LeftAlt)&&scroll!=0)Night.FpsVolume=Mathf.Clamp01(Night.FpsVolume+Mathf.Sign(scroll)*.1f);
            if(Input.GetKeyDown(KeyCode.Minus))Night.FpsVolume=Mathf.Max(0,Night.FpsVolume-.1f);
            if(Input.GetKeyDown(KeyCode.Equals))Night.FpsVolume=Mathf.Min(1,Night.FpsVolume+.1f);
            if(Input.GetKeyDown(KeyCode.F)&&Vector3.Distance(player.transform.position,seat)<2.5f) {Night.MonitorOn=!Night.MonitorOn;Audio.House(HouseCue.Notification,1);}
            if(Input.GetKeyDown(KeyCode.E))Interact();
            if(Input.GetKeyDown(KeyCode.Return)&&Night.AtComputer&&Night.Internet&&(!Fps.Active||Fps.MatchOver))Fps.NewMatch();
            if(!Night.AtComputer&&!Night.InBed&&Cursor.lockState==CursorLockMode.Locked) {
                yaw+=Input.GetAxisRaw("Mouse X")*Sensitivity;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*Sensitivity,-78,78);
                player.transform.rotation=Quaternion.Euler(0,yaw,0);RoomCamera.transform.localRotation=Quaternion.Euler(pitch,0,0);
                Vector3 direction=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));direction=Vector3.ClampMagnitude(direction,1);
                bool run=Input.GetKey(KeyCode.LeftShift);float speed=run?3.9f:2.5f;
                Vector3 next=player.transform.position+player.transform.TransformDirection(direction)*speed*dt;
                // The bedroom is the playable area; the corridor is audio and AI space.
                if(next.z>3.65f)direction=Vector3.zero;
                player.Move((player.transform.TransformDirection(direction)*speed+Vector3.down*9)*dt);
                footTimer-=dt;if(direction.sqrMagnitude>.1f&&footTimer<=0){Night.AddNoise(run?4:1.5f);Audio.SelfStep();footTimer=run?.29f:.5f;}
                RoomCamera.transform.localPosition=new Vector3(0,1.65f+Mathf.Sin(Time.time*speed*3)*.015f*direction.magnitude,0);
            }
            if(Night.AtComputer) {
                RoomCamera.transform.localPosition=Vector3.Lerp(RoomCamera.transform.localPosition,new Vector3(0,1.40f,.32f),dt*6);
                player.transform.rotation=Quaternion.Euler(0,180,0);RoomCamera.transform.localRotation=Quaternion.identity;
            }
            if(Night.InBed) {
                yaw+=Input.GetAxisRaw("Mouse X")*Sensitivity*.35f;yaw=Mathf.Clamp(yaw,-40,40);
                RoomCamera.transform.localRotation=Quaternion.Euler(-5,yaw,5);
            }
        }
        void Interact()
        {
            if(Night.AtComputer) {
                Night.AtComputer=false;Teleport(new Vector3(-1.5f,0,-1.03f));yaw=0;pitch=0;
                RoomCamera.transform.localPosition=Vector3.up*1.65f;Night.AddNoise(5);Audio.House(HouseCue.Creak,1);return;
            }
            if(Night.InBed) {
                Night.LeaveBed();Teleport(new Vector3(1.7f,0,-1.8f));RoomCamera.transform.localPosition=Vector3.up*1.65f;yaw=0;pitch=0;return;
            }
            Vector3 pos=player.transform.position;
            if(Vector3.Distance(pos,seat)<1.7f) {
                Night.AtComputer=true;Teleport(seat);Audio.House(HouseCue.Creak,1);Night.AddNoise(5);
                if(!Night.MonitorOn)Notify("F • ligar monitor");return;
            }
            if(Vector3.Distance(pos,new Vector3(2.1f,0,-1.8f))<1.45f) {
                Night.InBed=true;Teleport(bed);RoomCamera.transform.localPosition=new Vector3(0,.96f,0);player.transform.rotation=Quaternion.identity;yaw=0;return;
            }
            if(Night.DogPresent&&Vector3.Distance(pos,DogModel.position)<1.65f)Night.PetDog();
        }
        void Teleport(Vector3 position){player.enabled=false;player.transform.position=position;player.enabled=true;}
        void Notify(string s){notification=s;notificationTime=2;}
        void UpdateHouse(float dt)
        {
            bool visible=Night.Tico==TicoState.Checking||Night.Tico==TicoState.FakeLeaving||Night.Tico==TicoState.Leaving;
            if(scareTime<=0)TicoModel.gameObject.SetActive(visible);
            float angle=visible?-73:0;
            Door.localRotation=Quaternion.Slerp(Door.localRotation,Quaternion.Euler(0,angle,0),dt*2.8f);
            if(visible&&scareTime<=0) {
                Vector3 target=Night.Tico==TicoState.Leaving?new Vector3(1.3f,0,7):new Vector3(1.3f,0,3.45f);
                TicoModel.localPosition=Vector3.MoveTowards(TicoModel.localPosition,target,dt*1.1f);
                TicoModel.localRotation=Quaternion.Euler(0,Night.Tico==TicoState.Leaving?0:180,Mathf.Sin(Time.time*5)*.6f);
            }
            flicker+=dt;MonitorLight.intensity=1.55f+Mathf.Sin(flicker*13)*.045f;
        }
        public void FlashImpact(Vector3 point)
        {
            var s=WorldBuilder.Shape("Impact",PrimitiveType.Sphere,point,Vector3.one*.055f,WorldBuilder.Mat("Impact flash",new Color(1,.76f,.4f),true));Destroy(s,.07f);
        }
        string Hint()
        {
            if(Night.AtComputer)return Night.MonitorOn?"E  LEVANTAR     F  DESLIGAR MONITOR":"F  LIGAR MONITOR     E  LEVANTAR";
            if(Night.InBed)return "FINGINDO DORMIR     •     E  LEVANTAR";
            if(Vector3.Distance(player.transform.position,seat)<1.7f)return "E  SENTAR NO COMPUTADOR";
            if(Vector3.Distance(player.transform.position,new Vector3(2.1f,0,-1.8f))<1.45f)return "E  DEITAR E FINGIR DORMIR";
            if(Night.DogPresent&&Vector3.Distance(player.transform.position,DogModel.position)<1.65f)return "E  CARINHO NA MELIÇA  /  −20 ESTRESSE";
            return "WASD  ANDAR     •     E  INTERAGIR";
        }
        void Styles()
        {
            if(pixel!=null)return;
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();
            text=new GUIStyle(GUI.skin.label){fontSize=18};text.normal.textColor=pale;
            small=new GUIStyle(text){fontSize=13};
            heading=new GUIStyle(text){fontSize=30,fontStyle=FontStyle.Bold};
            title=new GUIStyle(text){fontSize=86,fontStyle=FontStyle.Bold};
            center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(GUI.skin.button){fontSize=18,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(22,12,5,5)};
            button.normal.background=pixel;button.hover.background=pixel;button.active.background=pixel;
            button.normal.textColor=new Color(.03f,.09f,.10f);button.hover.textColor=Color.black;
        }
        void Panel(float x,float y,float w,float h,Color c) {GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),pixel);GUI.color=Color.white;}
        void Txt(float x,float y,float w,float h,string s,GUIStyle style=null,Color? c=null)
        {GUI.color=c??Color.white;GUI.Label(new Rect(x,y,w,h),s,style??text);GUI.color=Color.white;}
        bool Button(float x,float y,float w,string s){GUI.backgroundColor=teal;bool v=GUI.Button(new Rect(x,y,w,48),s,button);GUI.backgroundColor=Color.white;return v;}
        void Meter(float x,float y,string name,float value,Color color)
        {
            Txt(x,y,210,20,name,small);Txt(x+170,y,40,20,((int)value).ToString("00"),small,color);
            Panel(x,y+26,205,4,new Color(.2f,.26f,.28f));Panel(x,y+26,205*value/100,4,color);
        }
        void OnGUI()
        {
            Styles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(ScreenWidth/1280f,ScreenHeight/720f,1));
            if(!Night.Started){Menu();return;}
            if(scareTime>0){Panel(0,0,1280,720,new Color(.8f,.05f,.02f,.12f+Mathf.Abs(Mathf.Sin(Time.time*53))*.15f));return;}
            if(Night.Result!=Ending.None){EndScreen();return;}
            if(ComputerView)Computer();
            Panel(0,0,1280,68,new Color(.018f,.031f,.046f,.94f));
            Txt(30,18,220,35,"TICO'S HOUSE",text,teal);Txt(265,18,140,35,Night.Clock,heading);
            Txt(422,18,230,30,"PLATINA 3  /  "+Night.Pdl+" PDL",text);
            Meter(760,10,"ESTRESSE",Night.Stress,amber);Meter(1020,10,"BARULHO",Night.Noise,Night.Noise>75?red:teal);
            Panel(0,670,1280,50,new Color(.018f,.031f,.046f,.94f));
            Txt(25,684,550,24,Hint(),small);
            Txt(610,684,220,24,"M  MIC "+(Night.MicOpen?"ABERTO":"MUTADO"),small,Night.MicOpen?amber:teal);
            Txt(835,684,295,24,"V  FPS "+Mathf.RoundToInt(Night.FpsVolume*100)+"%    − / +  VOLUME",small);
            Txt(1160,684,110,24,"ESC  PAUSA",small);
            if(!Night.AtComputer&&!Night.InBed) {Panel(637,358,6,2,pale);Panel(639,356,2,6,pale);}
            if(subtitleTime>0){Panel(225,612,830,39,new Color(.01f,.02f,.03f,.86f));Txt(240,619,800,30,subtitle,center);}
            if(notificationTime>0)Txt(350,82,580,32,notification,center,teal);
            if(Night.FinalHunt&&Night.AtComputer&&Night.MonitorOn) {
                Panel(90,110,1100,480,new Color(.015f,.02f,.025f,.98f));Txt(300,260,700,65,"CONEXÃO PERDIDA",heading,red);Txt(300,330,700,40,"SEM INTERNET  •  CHAMADA DESCONECTADA",text);
            }
            if(Night.Paused){Panel(0,0,1280,720,new Color(.015f,.023f,.035f,.97f));Options();}
        }
        float ScreenWidth {get{return UnityEngine.Screen.width;}}float ScreenHeight {get{return UnityEngine.Screen.height;}}
        void Menu()
        {
            if(cover!=null)GUI.DrawTexture(new Rect(0,0,1280,720),cover,ScaleMode.ScaleAndCrop);
            Panel(0,0,670,720,new Color(.017f,.029f,.04f,.55f));
            Panel(50,56,45,3,teal);Txt(110,44,520,30,"UMA NOITE. UM RANK. NENHUMA PERMISSÃO.",small,teal);
            Txt(45,123,650,210,"TICO'S\nHOUSE",title);Txt(52,345,590,36,"Às 21h era pra desligar.",heading);
            Txt(54,401,558,80,"Gust prometeu dormir. Os amigos prometeram jogar bem.\nUma dessas mentiras vai acordar o Tico.",text);
            if(Button(54,505,470,"COMEÇAR A MADRUGADA   →"))StartNight();
            if(Button(54,567,224,"COMO JOGAR"))showHelp=!showHelp;
            if(Button(300,567,224,"SAIR"))Application.Quit();
            Txt(54,675,600,28,"v"+Application.version+"  •  USE FONES DE OUVIDO  •  TERROR SEM GORE",small);
            Txt(820,657,430,35,"GUST • A UM DIAMANTE DO CASTIGO",small,teal);
            if(showHelp){Panel(715,295,545,350,new Color(.02f,.035f,.045f,.98f));Help(746,316);}
        }
        void Help(float x,float y)
        {
            Txt(x,y,495,36,"HABILIDADE → TEMPO → RISCO",text,teal);
            Txt(x,y+46,495,255,"WASD / mouse   Andar e olhar no quarto\nE   Sentar, levantar, deitar ou fazer carinho\nF   Ligar / desligar monitor perto da mesa\nEnter   Buscar partida no computador\nMouse   Mover a mira na Ascent (câmera fixa)\nMouse 1   Atirar     R   Recarregar     Q   Pulso\nTAB   Placar     M   Microfone fictício\nV   Mutar FPS     − / + ou Alt + roda   Volume\n\nOuviu passos? Desligue, levante e vá à cama.\nEspere o ronco voltar. O silêncio não garante nada.\nWi-Fi cortado é derrota, mesmo na cama.",small);
        }
        void Options()
        {
            Txt(70,62,500,60,"PAUSA",heading);Help(70,145);
            Txt(690,140,430,30,"Sensibilidade do mouse",text);Sensitivity=GUI.HorizontalSlider(new Rect(690,185,400,20),Sensitivity,.4f,5);
            Txt(690,222,400,30,"Volume do FPS: "+Mathf.RoundToInt(Night.FpsVolume*100)+"%",text);Night.FpsVolume=GUI.HorizontalSlider(new Rect(690,267,400,20),Night.FpsVolume,0,1);
            Txt(690,285,400,28,"Volume geral: "+Mathf.RoundToInt(Audio.MasterVolume*100)+"%",small);Audio.MasterVolume=GUI.HorizontalSlider(new Rect(690,318,400,16),Audio.MasterVolume,0,1);
            if(Button(690,340,400,"VOLTAR À NOITE")){Night.Paused=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;PlayerPrefs.SetFloat("Sensitivity",Sensitivity);PlayerPrefs.SetFloat("MasterVolume",Audio.MasterVolume);PlayerPrefs.Save();}
            if(Button(690,405,400,"RECOMEÇAR"))Restart();
            if(Button(690,470,400,"SAIR"))Application.Quit();
        }
        void Computer()
        {
            Panel(32,87,1216,565,new Color(.027f,.043f,.063f,.99f));
            GUI.DrawTexture(new Rect(49,132,963,516),Fps.Texture,ScaleMode.StretchToFill);
            Panel(49,99,963,33,new Color(.035f,.058f,.077f));Txt(64,104,400,26,"ASCENT / "+Fps.LocationName,small,teal);
            Txt(470,99,260,35,Fps.Blue+"   :   "+Fps.Red,heading);Txt(800,107,205,25,"PRIMEIRO A 13",small);
            Panel(1027,99,203,549,new Color(.026f,.035f,.049f));Txt(1043,110,174,32,"CHAMADA",text,teal);
            Txt(1043,151,178,40,Night.Internet?"●  conectado":"○  sem internet",small,Night.Internet?teal:red);
            int i=0;foreach(var b in Fps.Bots)if(b.Ally){Txt(1043,202+i*40,180,32,b.Name,small,b.Alive?pale:new Color(.42f,.46f,.49f));i++;}
            Txt(1043,394,180,55,Night.MicOpen?"Gust  •  AO VIVO":"Gust  •  MUTADO",small,Night.MicOpen?amber:teal);
            Txt(1043,352,180,40,"COBERTURA "+(Fps.TeamCover*100).ToString("0")+"%",small,teal);
            Txt(1043,453,180,95,"PLATINA 3\n"+Night.Pdl+" / 100 PDL\n"+Night.Wins+" vitórias",text);
            Txt(1043,580,180,60,"M  MIC\nV  ÁUDIO FPS",small);
            if(!Fps.Active||Fps.MatchOver) {
                Panel(49,132,963,516,new Color(.018f,.027f,.04f,.88f));
                Txt(200,223,740,65,Fps.MatchOver?(Fps.LastWin?"VITÓRIA":"DERROTA"):"A FILA CHAMOU.",heading,teal);
                Txt(200,303,740,45,Fps.MatchOver?Fps.RatingDetails:"Gust + quatro amigos. Elimine o time adversário.",text);
                Txt(200,358,700,80,"Só a mira se move. Os inimigos fazem AD–AD.\nElimine os cinco em 18s. Tempo esgotado = derrota.",text);
                if(Button(200,486,530,"BUSCAR PARTIDA   →")){Fps.NewMatch();SyncCursor();}
                Txt(200,545,700,32,"Clique no botão ou pressione ENTER",small,amber);
            }
            else {
                Txt(64,590,280,45,"HP  "+Fps.Health+" / 150",heading,Fps.Health<40?red:pale);
                Txt(790,590,215,45,Fps.ReloadRemaining>0?"RECARGA":Fps.Ammo+" / 24",heading);
                Txt(64,554,480,30,"Q  PULSO • RAJADAS CURTAS = MAIS PRECISÃO",small,teal);
                if(!Fps.PlayerDead&&Fps.Intermission<=0){float cx=49+Fps.Aim.x/960*963,cy=132+Fps.Aim.y/540*516,gap=5+Fps.Spread;Color cc=Fps.HitFlash>0?red:teal;Panel(cx-gap-5,cy,5,2,cc);Panel(cx+gap,cy,5,2,cc);Panel(cx,cy-gap-5,2,5,cc);Panel(cx,cy+gap,2,5,cc);}
                if(Fps.MuzzleFlash>0){Panel(651,450,12,17,new Color(1,.77f,.24f,.7f));Panel(644,457,25,4,new Color(1,.92f,.5f,.8f));}
                foreach(var enemy in Fps.Bots)if(Fps.Visible(enemy)&&enemy.ShotFlash>0){
                    float sx=49+(enemy.ScreenPos.x+enemy.Height*.12f)/960*963,sy=132+(enemy.ScreenPos.y-enemy.Height*.6f)/540*516;
                    Panel(sx-7,sy-2,14,4,amber);Panel(sx-2,sy-7,4,14,Color.white);
                    Vector2 from=new Vector2(sx,sy),to=enemy.AimedAtPlayer?new Vector2(530,620):new Vector2(sx+90,sy+60);
                    Matrix4x4 saved=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg,from);
                    Panel(from.x,from.y,Vector2.Distance(from,to),2,new Color(1,.62f,.22f,.65f));GUI.matrix=saved;
                }
                if(Fps.FeedTime>0)Txt(660,153,340,33,Fps.KillFeed,small,teal);
                if(Fps.BannerTime>0||Fps.Intermission>0)Txt(250,194,560,50,Fps.Banner,center,amber);
                if(Fps.PlayerDead)Txt(245,373,570,55,"VOCÊ CAIU  •  O TIME CONTINUA",center,red);
                if(Fps.LagRemaining>0)Txt(400,450,480,30,"INSTABILIDADE NA CONEXÃO",small,amber);
                if(Fps.DamageFlash>0)Panel(49,132,963,516,new Color(1,.07f,.02f,Fps.DamageFlash*.7f));
                if(Fps.EnemyHeadshotFlash>0)Txt(370,510,380,40,"HEADSHOT RECEBIDO",center,red);
                if(Input.GetKey(KeyCode.Tab))Scoreboard();
            }
        }
        void Scoreboard()
        {
            Panel(179,193,710,365,new Color(.02f,.03f,.046f,.97f));Txt(205,207,650,33,"EQUIPE                           K       D       A",text,teal);
            Txt(205,253,640,32,string.Format("{0,-24} {1,3}     {2,3}     {3,3}","Gust",Fps.Stats.Kills,Fps.Stats.Deaths,Fps.Stats.Assists),text,amber);
            int i=0;foreach(var b in Fps.Bots)if(b.Ally){Txt(205,296+i*43,640,35,string.Format("{0,-24} {1,3}     {2,3}     {3,3}",b.Name,b.Kills,b.Deaths,b.Assists),text);i++;}
            Txt(205,498,640,30,"HS "+Fps.Stats.Headshots+"   •   PRECISÃO "+(Fps.Stats.Accuracy*100).ToString("0")+"%   •   CLUTCHES "+Fps.Stats.Clutches,small,teal);
        }
        void EndScreen()
        {
            Panel(0,0,1280,720,new Color(.012f,.021f,.031f,.97f));Panel(70,76,70,4,teal);
            string headline="",description="",tag="";
            switch(Night.Result) {
                case Ending.Diamond:headline="DIAMANTE.";tag="01 / DIAMANTE ENDING";description="Gust subiu de rank. O Tico continua dormindo.\nAmanhã, a olheira é problema de amanhã.";break;
                case Ending.Survivor:headline="BOM DIA, GUST.";tag="02 / SURVIVOR ENDING";description="06:00. Você sobreviveu à madrugada.\nO Diamante ficou para a próxima desculpa.";break;
                case Ending.Caught:headline="EU AVISEI, GUSTAVO.";tag="03 / TICO ENDING";description="Era para desligar às nove.\nO monitor não sabe fingir que está dormindo.";break;
                case Ending.Wifi:headline="ELE OUVIU.";tag="04 / WI-FI FINAL HUNT";description="Sem internet. Sem chamada. Sem mais uma.\nO castigo começou antes de você chegar à cama.";break;
                case Ending.Carlos:headline="ZERO. VINTE E CINCO.";tag="05 / O LEGADO DO CARLOS";description="Carlos: pelo menos eu passei informação.\nGust fechou o jogo. Pela primeira vez, Tico concordou.";break;
            }
            Txt(70,111,1050,35,tag,small,teal);Txt(66,185,1160,110,headline,new GUIStyle(title){fontSize=58});
            Txt(72,322,1040,100,description,new GUIStyle(text){fontSize=23});
            Txt(72,444,1100,46,Night.Clock+"     •     "+Night.Pdl+" PDL     •     "+Night.Wins+" VITÓRIAS     •     "+Night.Matches+" PARTIDAS",text,amber);
            if(Button(72,550,380,"TENTAR SÓ MAIS UMA"))Restart();
            if(Button(478,550,245,"SAIR"))Application.Quit();
            int found=0;for(int i=1;i<=5;i++)found+=PlayerPrefs.GetInt("Ending"+i,0);
            Txt(72,662,1040,32,"FINAIS DESCOBERTOS   "+found+" / 5",small);
        }
        void Restart(){Audio.StopAll();Fps.Dispose();Destroy(player.gameObject);Destroy(RoomRoot.gameObject);SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
        IEnumerator Automation()
        {
            string[] args=System.Environment.GetCommandLineArgs();
            if(System.Array.IndexOf(args,"--smoke-test")<0)yield break;
            yield return null;StartNight();Night.AtComputer=true;Teleport(seat);Fps.NewMatch();
            yield return new WaitForSeconds(4);
            if(Fps.Bots.Count!=9||Night.Pdl!=0){Debug.LogError("TICOS_SMOKE_FAIL: initialization");Application.Quit(1);yield break;}
            Night.BeginFinalHunt();Night.InBed=true;Night.MonitorOn=false;
            for(int i=0;i<2200;i++)Night.Tick(.01f);
            if(Night.Result!=Ending.Wifi){Debug.LogError("TICOS_SMOKE_FAIL: hunt escaped");Application.Quit(1);yield break;}
            Debug.Log("TICOS_SMOKE_PASS");Application.Quit(0);
        }
        void OnDestroy(){if(screenMat!=null)Destroy(screenMat);if(pixel!=null)Destroy(pixel);}
    }
}
