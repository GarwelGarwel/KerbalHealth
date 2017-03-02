using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalHealth
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT)]
    public class KerbalHealthScenario : ScenarioModule
    {
        static double lastUpdated;  // UT at last health update
        static double nextEventTime;  // UT when (or after) next event check occurs

        ApplicationLauncherButton button;
        bool dirty = false;
        PopupDialog monitorWindow;  // Health Monitor window
        DialogGUIGridLayout monitorGrid;  // Health Monitor grid
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Health Monitor grid's labels
        int colNum = 4;  // # of columns in Health Monitor

        public void Start()
        {
            Core.Log("KerbalHealth.Start", Core.LogLevels.Important);
            Core.Log(Core.Factors.Count + " factors initialized.");
            Core.KerbalHealthList.RegisterKerbals();
            GameEvents.onKerbalAdded.Add(Core.KerbalHealthList.Add);
            GameEvents.onKerbalRemoved.Add(Core.KerbalHealthList.Remove);
            GameEvents.onCrewOnEva.Add(OnKerbalOnEva);
            Core.Log("Registering toolbar button...", Core.LogLevels.Important);
            Texture2D icon = new Texture2D(38, 38);
            icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
            button = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData , null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            lastUpdated = Planetarium.GetUniversalTime();
            nextEventTime = lastUpdated + GetNextEventInterval();
            Core.Log("KerbalHealthScenario.Start finished.", Core.LogLevels.Important);
        }

        public void OnKerbalOnEva(GameEvents.FromToAction<Part, Part> action)
        {
            Core.Log(action.to.protoModuleCrew[0].name + " went on EVA from " + action.from.name + ".", Core.LogLevels.Important);
            Core.KerbalHealthList.Find(action.to.protoModuleCrew[0]).IsOnEVA = true;
        }

        // Next event update is scheduled after a random period of time, between 0 and 2 days
        double GetNextEventInterval()
        { return Core.rand.NextDouble() * 21600 * 2; }

        void UpdateKerbals(bool forced = false)
        {
            Core.Log("KerbalHealthScenario.UpdateKerbals(" + forced + ")");
            double timePassed = Planetarium.GetUniversalTime() - lastUpdated;
            if (forced || (timePassed >= Core.UpdateInterval * TimeWarp.CurrentRate))
            {
                Core.Log("UT is " + Planetarium.GetUniversalTime() + ". Updating for " + timePassed + " seconds.");
                bool processEvents = Planetarium.GetUniversalTime() >= nextEventTime;
                Core.KerbalHealthList.Update(timePassed);
                lastUpdated = Planetarium.GetUniversalTime();
                while (Planetarium.GetUniversalTime() >= nextEventTime)  // Can take several turns of event processing at high time warp
                {
                    Core.KerbalHealthList.ProcessEvents();
                    nextEventTime += GetNextEventInterval();
                    Core.Log("Next event processing is scheduled at UT " + nextEventTime, Core.LogLevels.Important);
                }
                dirty = true;
            }
        }

        public void FixedUpdate()
        { UpdateKerbals(); }

        public void DisplayData()  // Called when the AppLauncher button is enabled
        {
            Core.Log("DisplayData", Core.LogLevels.Important);
            gridContents = new System.Collections.Generic.List<DialogGUIBase>((Core.KerbalHealthList.Count + 1) * colNum);
            // Creating column titles
            gridContents.Add(new DialogGUILabel("Name", true));
            gridContents.Add(new DialogGUILabel("Health", true));
            gridContents.Add(new DialogGUILabel("Condition", true));
            gridContents.Add(new DialogGUILabel("Time Left", true));
            // Initializing Health Monitor's grid with empty labels, to be filled in Update()
            for (int i = 0; i < Core.KerbalHealthList.Count * colNum; i++) gridContents.Add(new DialogGUILabel("", true));
            monitorGrid = new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(100, 30), new Vector2(20, 0), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNum, gridContents.ToArray());
            dirty = true;
            monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("", "Health Monitor", HighLogic.UISkin, 500, monitorGrid), false, HighLogic.UISkin, false);
        }

        public void Update()
        {
            if ((monitorWindow != null) && dirty)
            {
                if (monitorGrid == null)
                {
                    Core.Log("monitorGrid is null.", Core.LogLevels.Error);
                    return;
                }
                if (gridContents == null)
                {
                    Core.Log("gridContents is null.", Core.LogLevels.Error);
                    return;
                }
                if (gridContents.Count != (Core.KerbalHealthList.Count + 1) * colNum)  // # of tracked kerbals has changed => close & reopen the window
                {
                    Core.Log("Kerbals' number has changed. Recreating the Health Monitor window.", Core.LogLevels.Important);
                    UndisplayData();
                    DisplayData();
                }
                // Fill the Health Monitor's grid with kerbals' health data
                for (int i = 0; i < Core.KerbalHealthList.Count; i++)
                {
                    KerbalHealthStatus khs = Core.KerbalHealthList[i];
                    gridContents[(i + 1) * colNum].SetOptionText(khs.Name);
                    double ch = khs.HealthChangePerDay();
                    string trend = "~ ";
                    if (ch > 0) trend = "↑ ";
                    if (ch < 0) trend = "↓ ";
                    gridContents[(i + 1) * colNum + 1].SetOptionText(trend + (100 * khs.Health).ToString("F2") + "% (" + khs.HP.ToString("F2") + ")");
                    gridContents[(i + 1) * colNum + 2].SetOptionText(khs.Condition.ToString());
                    double b = khs.GetBalanceHP();
                    string s = "";
                    if (b > khs.NextConditionHP()) s = "—";
                    else s = ((b > 0) ? "> " : "") + Core.ParseUT(khs.TimeToNextCondition());
                    gridContents[(i + 1) * colNum + 3].SetOptionText(s);
                }
                dirty = false;
            }
        }

        public void UndisplayData()
        {
            if (monitorWindow != null) monitorWindow.Dismiss();
        }

        public void OnDisable()
        {
            Core.Log("KerbalHealthScenario.OnDisable", Core.LogLevels.Important);
            UndisplayData();
            GameEvents.onKerbalAdded.Remove(Core.KerbalHealthList.Add);
            GameEvents.onKerbalRemoved.Remove(Core.KerbalHealthList.Remove);
            GameEvents.onCrewOnEva.Remove(OnKerbalOnEva);
            if (ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(button);
            Core.Log("KerbalHealthScenario.OnDisable finished.", Core.LogLevels.Important);
        }

        public override void OnSave(ConfigNode node)
        {
            Core.Log("KerbalHealthScenario.OnSave", Core.LogLevels.Important);
            UpdateKerbals(true);
            int i = 0;
            node.AddValue("nextEventTime", nextEventTime);
            foreach (KerbalHealthStatus khs in Core.KerbalHealthList)
            {
                Core.Log("Saving " + khs.Name + "'s health.");
                node.AddNode(khs.ConfigNode);
                i++;
            }
            Core.Log("KerbalHealthScenario.OnSave complete. " + i + " kerbal(s) saved.", Core.LogLevels.Important);
        }

        public override void OnLoad(ConfigNode node)
        {
            Core.Log("KerbalHealthScenario.OnLoad", Core.LogLevels.Important);
            Core.KerbalHealthList.Clear();
            int i = 0;
            nextEventTime = Double.Parse(node.GetValue("nextEventTime"));
            foreach (ConfigNode n in node.GetNodes())
                if (n.name == "KerbalHealthStatus")
                {
                    Core.KerbalHealthList.Add(new KerbalHealthStatus(n));
                    i++;
                }
            lastUpdated = Planetarium.GetUniversalTime();
            Core.Log("" + i + " kerbal(s) loaded.", Core.LogLevels.Important);
        }
    }
}
