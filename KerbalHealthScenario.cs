using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalHealth
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT)]
    public class KerbalHealthScenario : ScenarioModule
    {
        public static KerbalHealthList KerbalHealthList { get; set; } = new KerbalHealthList();

        public static double UpdateInterval { get; set; } = 1;  // # of game seconds between updates
        static double lastUpdated;  // UT at last health update

        ApplicationLauncherButton button;

        public void DisplayData()
        {
            Log.Post("KerbalHealthScenario.DisplayData");
            //ScreenMessages.PostScreenMessage("" + KerbalHealthList.Count + " kerbals' health tracked.");
            foreach (KerbalHealthStatus khs in KerbalHealthList)
                ScreenMessages.PostScreenMessage(khs.Name + "\t" + khs.HealthPercentage.ToString("F2") + "% (" + khs.Health.ToString("F2") + ")\t" + KSPUtil.PrintDateDeltaCompact(khs.TimeToNextCondition(), true, false) + " left");
        }

        public void Start()
        {
            Log.Post("KerbalHealth " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            KerbalHealthList.RegisterKerbals();
            GameEvents.onKerbalAdded.Add(KerbalHealthList.Add);
            GameEvents.onKerbalRemoved.Add(KerbalHealthList.Remove);
            Log.Post("Registering toolbar button...");
            Texture2D icon = new Texture2D(38, 38);
            icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
            button = ApplicationLauncher.Instance.AddModApplication(DisplayData, DisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            lastUpdated = Planetarium.GetUniversalTime();
            Log.Post("KerbalHealthScenario.Start finished.");
        }

        void UpdateKerbals(bool forced = false)
        {
            double timePassed = Planetarium.GetUniversalTime() - lastUpdated;
            if (forced || (timePassed >= UpdateInterval))
            {
                Log.Post("UT is " + Planetarium.GetUniversalTime() + ". Last updated at " + lastUpdated + ". Updating for " + timePassed + " seconds.");
                KerbalHealthList.Update(timePassed);
                lastUpdated = Planetarium.GetUniversalTime();
            }
        }

        public void FixedUpdate()
        {
            UpdateKerbals();
        }

        public void OnDisable()
        {
            Log.Post("KerbalHealthScenario.OnDisable");
            GameEvents.onKerbalAdded.Remove(KerbalHealthList.Add);
            GameEvents.onKerbalRemoved.Remove(KerbalHealthList.Remove);
            ApplicationLauncher.Instance.RemoveModApplication(button);
            Log.Post("KerbalHealthScenario.OnDisable finished.");
        }

        public override void OnSave(ConfigNode node)
        {
            Log.Post("KerbalHealthScenario.OnSave");
            UpdateKerbals(true);
            int i = 0;
            foreach (KerbalHealthStatus khs in KerbalHealthList)
            {
                Log.Post("Saving " + khs.Name + "'s health.");
                node.AddNode(khs.ConfigNode);
                i++;
            }
            Log.Post("KerbalHealthScenario.OnSave complete. " + i + " kerbal(s) saved.");
        }

        public override void OnLoad(ConfigNode node)
        {
            Log.Post("KerbalHealthScenario.OnLoad");
            int i = 0;
            foreach (ConfigNode n in node.GetNodes())
            {
                if (n.id == "KerbalHealthStatus")
                {
                    KerbalHealthList.Add(new KerbalHealthStatus(n));
                    i++;
                }
            }
            lastUpdated = Planetarium.GetUniversalTime();
            Log.Post("" + i + " kerbal(s) loaded.");
        }
    }
}
