using System;
using System.Collections.Generic;

namespace TicosHouse
{
    public enum Ending { None, Diamond, Survivor, Caught, Wifi, Carlos }
    public enum TicoState { Sleeping, Waking, Awake, Suspicious, Downstairs, Stairs, Hall, Door, Checking, Leaving, FakeLeaving, FinalHunt }
    public enum HouseCue { Snore, Creak, Footstep, Stair, Door, Knock, Bark, Crash, Notification, Brother, Growl, Scream, WifiLost, Heart }

    // Pure simulation: no scene references, so final-hunt, ranking and AI can be tested outside Unity.
    public sealed class NightSimulation
    {
        public const float NightSeconds = 1260f;
        public float Elapsed, Stress = 12, Noise, Suspicion;
        public float FpsVolume = .65f;
        public bool MicOpen, MonitorOn = true, AtComputer, InBed, Internet = true;
        public bool BrotherPresent, DogPresent, Started, Paused;
        public int Pdl, Matches, Wins;
        public Ending Result;
        public TicoState Tico = TicoState.Sleeping;
        public float StateRemaining = 28, HuntRemaining, EventRemaining = 22;
        public float BrotherRemaining, DogRemaining;
        public bool FinalHunt { get { return !Internet; } }
        public float Communication { get { return Clamp((MicOpen ? 1.2f : .8f) - Stress / 500f, .55f, 1.2f); } }
        public float Difficulty { get { return Clamp(Elapsed / NightSeconds, 0, 1); } }
        public string Clock { get { int m = (21 * 60 + (int)(540 * Difficulty)) % 1440; return (m / 60).ToString("00") + ":" + (m % 60).ToString("00"); } }
        public event Action<HouseCue, float> Sound;
        public event Action<string> Dialogue;
        public event Action<Ending> Finished;
        readonly Random random;
        float snoreTimer, stepTimer, shoutCooldown = 20, extremeExposure, heartbeat, mutedTime;
        bool checkedRoom;
        public NightSimulation(int seed) { random = new Random(seed); }
        public static float Clamp(float v, float min, float max) { return Math.Max(min, Math.Min(max, v)); }
        float Range(float a, float b) { return a + (float)random.NextDouble() * (b - a); }
        bool Chance(float p) { return random.NextDouble() < p; }
        void Cue(HouseCue cue, float position) { if (Sound != null) Sound(cue, position); }
        public void Say(string line) { if (Dialogue != null) Dialogue(line); }
        public void AddNoise(float value)
        {
            if (Result != Ending.None) return;
            Noise = Clamp(Noise + value, 0, 100);
            Suspicion = Clamp(Suspicion + value * .15f, 0, 100);
        }
        public void PetDog()
        {
            if (!DogPresent || FinalHunt || Result != Ending.None) return;
            Stress = Math.Max(0, Stress - 20); DogPresent = false;
            Say("Meliça: um carinho, vinte pontos a menos de ódio.");
        }
        public void LeaveBed()
        {
            InBed = false;
            if (Tico == TicoState.Checking || Tico == TicoState.FakeLeaving) End(Ending.Caught);
        }
        public void CompleteMatch(MatchStats stats, bool won)
        {
            if (!Internet || Result != Ending.None) return;
            Matches++;
            if (won) { Wins++; int gain = PerformanceRating.Gain(stats); Pdl += gain; Say("Vitória. +" + gain + " PDL • " + Pdl + "/100"); Stress = Math.Max(0, Stress - 12); }
            else { Pdl = Math.Max(0, Pdl - 12); Stress = Clamp(Stress + 14, 0, 100); Say("Derrota. −12 PDL. Respira, Gust."); }
            if (Pdl >= 100) End(Ending.Diamond);
        }
        public void BeginFinalHunt()
        {
            if (FinalHunt || Result != Ending.None) return;
            Internet = false; Tico = TicoState.FinalHunt;
            HuntRemaining = Range(10, 18); stepTimer = Range(1, 4);
            Cue(HouseCue.WifiLost, 0);
            Say(BrotherPresent ? "Guilherme: Gust... fudeu. ELE TÁ VINDO." : "CONEXÃO PERDIDA");
        }
        public void End(Ending ending)
        {
            if (Result != Ending.None) return;
            // No win, sunrise or hiding can override the irreversible Wi-Fi ending.
            Result = FinalHunt ? Ending.Wifi : ending;
            if (Finished != null) Finished(Result);
        }
        public void Tick(float dt)
        {
            if (!Started || Paused || Result != Ending.None || dt <= 0) return;
            Elapsed += dt;
            Noise = Math.Max(0, Noise - dt * (AtComputer ? 3.4f : 7));
            Suspicion = Clamp(Suspicion - dt * .12f, 0, 100);
            shoutCooldown -= dt;
            if (FinalHunt)
            {
                HuntRemaining -= dt; stepTimer -= dt;
                if (stepTimer <= 0 && HuntRemaining < 10) { Cue(HouseCue.Footstep, Clamp(1 - HuntRemaining / 10, 0, 1)); stepTimer = .29f; }
                if (HuntRemaining <= 0) End(Ending.Wifi);
                return;
            }
            if (Elapsed >= NightSeconds) { End(Ending.Survivor); return; }
            if (AtComputer)
            {
                Noise = Clamp(Noise + dt * (MicOpen ? 4.4f : 2.1f), 0, 100);
                Stress = Clamp(Stress + dt * (MicOpen ? -.27f : .20f) + dt * Difficulty * .09f, 0, 100);
                mutedTime += MicOpen ? -Math.Min(mutedTime, dt * 2) : dt;
                if (mutedTime > 45) { Say("Gust: tô falando sozinho no mute de novo..."); mutedTime = 0; Stress = Clamp(Stress + 5, 0, 100); }
            }
            else Stress = Math.Max(0, Stress - dt * (InBed ? .65f : .30f));
            if (Noise > 55) Suspicion = Clamp(Suspicion + dt * (Noise - 55) * .12f, 0, 100);
            if (Stress > 93 && AtComputer && shoutCooldown <= 0)
            {
                Cue(HouseCue.Scream, .98f); Say("Gust: CARLOS, COMO VOCÊ PERDEU ISSO?!");
                AddNoise(48); shoutCooldown = 14; Stress = Math.Max(0, Stress - 5);
            }
            extremeExposure = Noise >= 94 ? extremeExposure + dt : Math.Max(0, extremeExposure - dt * .5f);
            if (extremeExposure > 1.4f && Suspicion > 52) { BeginFinalHunt(); return; }
            heartbeat -= dt;
            if (heartbeat <= 0 && Stress > 45) { Cue(HouseCue.Heart, 1); heartbeat = 1.1f - Stress * .006f; }
            UpdateTico(dt);
            if (Result != Ending.None) return;
            UpdateVisitors(dt);
            EventRemaining -= dt;
            if (EventRemaining <= 0) { RandomEvent(); EventRemaining = Range(16, 33) * (1 - .25f * Difficulty); }
        }
        void Transition(TicoState state, float seconds)
        {
            Tico = state; StateRemaining = seconds; stepTimer = 0;
            if (state == TicoState.Stairs && BrotherPresent && Chance(.75f)) Say("Guilherme: Gust, desliga isso. ELE TÁ SUBINDO!");
            if (state == TicoState.Hall && DogPresent) Cue(HouseCue.Growl, .9f);
        }
        void UpdateTico(float dt)
        {
            StateRemaining -= dt; stepTimer -= dt;
            if (Tico == TicoState.Sleeping)
            {
                snoreTimer -= dt;
                if (snoreTimer <= 0) { Cue(HouseCue.Snore, 0); snoreTimer = 3.6f; }
                if (Noise > 73) StateRemaining -= dt * 5;
            }
            else if (Tico == TicoState.Downstairs || Tico == TicoState.Stairs || Tico == TicoState.Hall || Tico == TicoState.Leaving)
            {
                if (stepTimer <= 0) { Cue(Tico == TicoState.Stairs ? HouseCue.Stair : HouseCue.Footstep, Tico == TicoState.Downstairs ? .2f : Tico == TicoState.Stairs ? .45f : .8f); stepTimer = .82f; }
            }
            if (StateRemaining > 0) return;
            switch (Tico)
            {
                case TicoState.Sleeping: Transition(TicoState.Waking, Range(3, 7)); break;
                case TicoState.Waking:
                    if (Chance(.32f * (1 - Suspicion / 110))) Transition(TicoState.Sleeping, Range(22, 45));
                    else { Cue(HouseCue.Creak, .1f); Transition(TicoState.Awake, 3); } break;
                case TicoState.Awake: Transition(TicoState.Suspicious, Range(2, 5)); break;
                case TicoState.Suspicious: Transition(TicoState.Downstairs, Range(4, 7)); break;
                case TicoState.Downstairs:
                    if (Chance(.18f) && Noise < 40) Transition(TicoState.Sleeping, Range(20, 38));
                    else Transition(TicoState.Stairs, Range(5, 8) - Difficulty * 2); break;
                case TicoState.Stairs: Transition(TicoState.Hall, Range(3.5f, 5.5f)); break;
                case TicoState.Hall: Cue(HouseCue.Knock, 1); Transition(TicoState.Door, Range(2, 4)); break;
                case TicoState.Door:
                    Cue(HouseCue.Door, 1); checkedRoom = false; Transition(TicoState.Checking, 1.3f); break;
                case TicoState.Checking:
                    if (!InBed || MonitorOn || Noise > 45) { End(Ending.Caught); break; }
                    if (!checkedRoom) { checkedRoom = true; StateRemaining = Range(4, 8); Stress = Clamp(Stress + 9, 0, 100); }
                    else if (Chance(.15f + Difficulty * .1f)) { Cue(HouseCue.Footstep, .8f); Transition(TicoState.FakeLeaving, Range(4, 8)); }
                    else Transition(TicoState.Leaving, 5); break;
                case TicoState.FakeLeaving:
                    if (!InBed || MonitorOn) End(Ending.Caught);
                    else { Cue(HouseCue.Door, 1); Transition(TicoState.Leaving, 5); } break;
                case TicoState.Leaving: Transition(TicoState.Sleeping, Range(25, 52) * (1 - Difficulty * .35f) * (1 - Suspicion / 220)); break;
            }
        }
        void UpdateVisitors(float dt)
        {
            if (BrotherPresent) { BrotherRemaining -= dt; if (BrotherRemaining <= 0) { BrotherPresent = false; Say("Guilherme: vou dormir. Boa sorte aí."); Cue(HouseCue.Brother, .7f); } }
            if (DogPresent) { DogRemaining -= dt; if (DogRemaining <= 0) DogPresent = false; }
        }
        void RandomEvent()
        {
            int choice = random.Next(100);
            if (choice < 15 && !BrotherPresent)
            {
                Cue(HouseCue.Brother, .4f); BrotherPresent = true; BrotherRemaining = Range(45, 85);
                Say("Guilherme: calma, sou eu. Achou que era o Tico? Eu fico olhando.");
            }
            else if (choice < 29 && !DogPresent) { DogPresent = true; DogRemaining = 70; Cue(HouseCue.Door, .9f); }
            else if (choice < 38 && DogPresent) { Cue(HouseCue.Bark, .9f); AddNoise(28); }
            else if (choice < 44 && DogPresent) { Cue(HouseCue.Crash, .9f); AddNoise(34); }
            else if (choice < 57) Cue(HouseCue.Stair, .45f);
            else if (choice < 66 && AtComputer) { Cue(HouseCue.Notification, 1); AddNoise(14); Say("Joaobuild entrou na chamada. Só mais uma, né?"); }
            else if (choice < 73 && AtComputer) { Stress = Clamp(Stress + 9, 0, 100); Say("Carlos: foi mal, tava olhando o outro monitor."); }
            else if (choice < 81 && AtComputer) { Stress = Clamp(Stress + 8, 0, 100); Say("Munhak: esse cara tá muito estranho, mano."); }
            else if (choice < 88) { Cue(HouseCue.Creak, .95f); AddNoise(8); }
            else if (choice < 95 && BrotherPresent) Say("Guilherme: acho que ouvi ele... não, era a geladeira.");
            else { Cue(HouseCue.Knock, .3f); Say("Tico, ao longe: Gustavo?"); }
        }
    }

    [Serializable]
    public sealed class MatchStats
    {
        public int Kills, Deaths, Assists, Headshots, Shots, Hits, Clutches, MultiKills, Decisive, EarlyDeaths, TeamBestKills;
        public bool Mvp;
        public float Accuracy { get { return Shots > 0 ? (float)Hits / Shots : 0; } }
    }
    public static class PerformanceRating
    {
        public static int Gain(MatchStats s)
        {
            float score = (s.Kills - s.Deaths) * .65f + s.Assists * .18f;
            score += Math.Min(4, s.Headshots * .35f) + (s.Accuracy - .3f) * 5;
            score += (s.Mvp ? 2 : 0) + Math.Min(3, s.Clutches * 1.2f + s.MultiKills * .3f + s.Decisive * .35f);
            score -= Math.Min(3, s.EarlyDeaths * .4f);
            score += s.Kills >= s.TeamBestKills && s.Kills > 0 ? 1 : 0;
            return (int)Math.Round(NightSimulation.Clamp(23 + score, 15, 40));
        }
    }
}
