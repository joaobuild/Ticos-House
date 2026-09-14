using System;
using TicosHouse;

static class SimulationTests
{
    static int assertions;
    static void Check(bool condition,string name){assertions++;if(!condition)throw new Exception(name);}
    static NightSimulation Night(int seed=42){return new NightSimulation(seed){Started=true};}
    static void Advance(NightSimulation n,float seconds){for(int i=0;i<(int)(seconds*20);i++)n.Tick(.05f);}
    static void Ranking()
    {
        var excellent=new MatchStats{Kills=24,Deaths=9,Headshots=12,Shots=70,Hits=34,Mvp=true,Clutches=2,Decisive=3,TeamBestKills=18};
        var good=new MatchStats{Kills=18,Deaths=12,Headshots=6,Shots=65,Hits=25,Mvp=false,TeamBestKills=21};
        var normal=new MatchStats{Kills=14,Deaths=14,Headshots=3,Shots=70,Hits=20,TeamBestKills=19};
        var poor=new MatchStats{Kills=8,Deaths=17,Headshots=1,Shots=70,Hits=14,TeamBestKills=20,EarlyDeaths=4};
        int e=PerformanceRating.Gain(excellent),g=PerformanceRating.Gain(good),n=PerformanceRating.Gain(normal),p=PerformanceRating.Gain(poor);
        Check(e>=35&&e<=40,"excellent range");Check(g>=28&&g<=34,"good range");Check(n>=22&&n<=27,"normal range");Check(p>=15&&p<=21,"poor range");
        Check(Math.Ceiling(100f/e)==3,"excellent 3 wins");Check(Math.Ceiling(100f/g)<=4,"good <=4 wins");Check(Math.Ceiling(100f/n)<=5,"normal <=5 wins");
        var night=Night();night.CompleteMatch(excellent,true);night.CompleteMatch(excellent,true);Check(night.Result==Ending.None,"not premature diamond");night.CompleteMatch(excellent,true);Check(night.Result==Ending.Diamond,"diamond threshold");
        var loss=Night();loss.CompleteMatch(poor,false);Check(loss.Pdl==0,"loss floor");
        Console.WriteLine("PDL excellent/good/normal/poor: "+e+"/"+g+"/"+n+"/"+p);
    }
    static void Hunt()
    {
        for(int seed=0;seed<200;seed++) {
            var n=Night(seed);n.BeginFinalHunt();n.InBed=true;n.MonitorOn=false;n.AtComputer=false;
            float hunt=n.HuntRemaining;n.BeginFinalHunt();Check(n.HuntRemaining==hunt,"hunt reentry must not reset timer");
            n.CompleteMatch(new MatchStats{Kills=100},true);Check(n.Pdl==0&&n.Matches==0,"no progress after cut");
            Advance(n,20);Check(n.Result==Ending.Wifi,"bed cannot escape hunt");Check(!n.Internet,"internet irreversible");
        }
        var sunrise=Night();sunrise.Elapsed=NightSimulation.NightSeconds-.2f;sunrise.BeginFinalHunt();Advance(sunrise,20);Check(sunrise.Result==Ending.Wifi,"sunrise cannot escape hunt");
        var forced=Night();forced.BeginFinalHunt();forced.End(Ending.Diamond);Check(forced.Result==Ending.Wifi,"ending priority");
        var loud=Night();loud.AtComputer=true;loud.MicOpen=true;loud.Noise=100;loud.Suspicion=90;Advance(loud,2);Check(loud.FinalHunt,"extreme sustained noise triggers hunt");
        var moderate=Night();moderate.Noise=90;moderate.Suspicion=70;Advance(moderate,2);Check(!moderate.FinalHunt,"90 noise is still recoverable");
    }
    static void Detection()
    {
        foreach(bool bed in new[]{true,false})foreach(bool monitor in new[]{true,false}) {
            var n=Night();n.Tico=TicoState.Checking;n.StateRemaining=0;n.InBed=bed;n.MonitorOn=monitor;n.Tick(.05f);
            Check((n.Result==Ending.Caught)==(!bed||monitor),"checking bed/monitor detection");
        }
        var early=Night();early.Tico=TicoState.FakeLeaving;early.InBed=true;early.LeaveBed();Check(early.Result==Ending.Caught,"fake leave catches early wake");
        var dawn=Night();dawn.Elapsed=NightSimulation.NightSeconds-.01f;dawn.Tick(.05f);Check(dawn.Result==Ending.Survivor,"sunrise survivor");
        var paused=Night();paused.Paused=true;Advance(paused,10);Check(paused.Elapsed==0,"pause time");
        var reset=Night();Check(reset.Pdl==0&&reset.Internet&&reset.Result==Ending.None&&reset.Tico==TicoState.Sleeping,"fresh restart state");
        var comm=Night();float muted=comm.Communication;comm.MicOpen=true;Check(comm.Communication>muted,"microphone changes communication");
        var dog=Night();dog.Stress=80;dog.DogPresent=true;dog.PetDog();Check(dog.Stress==60,"pet -20");dog.PetDog();Check(dog.Stress==60,"no repeated pet exploit");
    }
    static void Soak()
    {
        int visits=0;
        for(int seed=0;seed<30;seed++) {
            var n=Night(seed);n.InBed=true;n.MonitorOn=false;
            for(int i=0;i<25201;i++) {
                n.Tick(.05f);if(n.Tico==TicoState.Checking)visits++;
                Check(!float.IsNaN(n.Stress)&&n.Stress>=0&&n.Stress<=100,"stress bounded");
                Check(n.Noise>=0&&n.Noise<=100&&n.Suspicion>=0&&n.Suspicion<=100,"noise suspicion bounded");
            }
            Check(n.Result==Ending.Survivor,"quiet sleeper survives every seed");
        }
        Check(visits>100,"AI reaches bedroom during soak");
    }
    public static int Main()
    {
        try {Ranking();Hunt();Detection();Soak();Console.WriteLine("PASS: "+assertions+" assertions");return 0;}
        catch(Exception e){Console.Error.WriteLine("FAIL: "+e);return 1;}
    }
}
