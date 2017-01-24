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

        ApplicationLauncherButton button;
        PopupDialog monitorWindow;  // Health Monitor window
        DialogGUIGridLayout monitorGrid;  // Health Monitor grid
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Health Monitor grid's labels
        int colNum = 4;  // # of columns in Health Monitor

        public void Start()
        {
            Log.Post("KerbalHealth " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Core.KerbalHealthList.RegisterKerbals();
            GameEvents.onKerbalAdded.Add(Core.KerbalHealthList.Add);
            GameEvents.onKerbalRemoved.Add(Core.KerbalHealthList.Remove);
            GameEvents.onCrewOnEva.Add(OnKerbalOnEva);
            Log.Post("Registering toolbar button...");
            Texture2D icon = new Texture2D(38, 38);
            icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
            button = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData , null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            lastUpdated = Planetarium.GetUniversalTime();
            Log.Post("KerbalHealthScenario.Start finished.");
        }

        public void OnKerbalOnEva(GameEvents.FromToAction<Part, Part> action)
        {
            Log.Post(action.to.protoModuleCrew[0].name + " went on EVA from " + action.from.name + " to " + action.to.name);
            Core.KerbalHealthList.Find(action.to.protoModuleCrew[0]).IsOnEVA = true;
        }

        void UpdateKerbals(bool forced = false)
        {
            double timePassed = Planetarium.GetUniversalTime() - lastUpdated;
            if (forced || (timePassed >= Core.UpdateInterval))
            {
                Log.Post("UT is " + Planetarium.GetUniversalTime() + ". Last updated at " + lastUpdated + ". Updating for " + timePassed + " seconds.");
                Core.KerbalHealthList.Update(timePassed);
                lastUpdated = Planetarium.GetUniversalTime();
            }
        }

        public void FixedUpdate()
        {
            UpdateKerbals();
        }

        public void DisplayData()  // Called when the AppLauncher button is enabled
        {
            Log.Post("DisplayData");
            gridContents = new System.Collections.Generic.List<DialogGUIBase>((Core.KerbalHealthList.Count + 1) * colNum);
            // Creating column titles
            gridContents.Add(new DialogGUILabel("Name", true));
            gridContents.Add(new DialogGUILabel("Health", true));
            gridContents.Add(new DialogGUILabel("Condition", true));
            gridContents.Add(new DialogGUILabel("Time Left", true));
            // Initializing Health Monitor's grid with empty labels, to be filled in Update()
            for (int i = 0; i < Core.KerbalHealthList.Count * colNum; i++)  
                gridContents.Add(new DialogGUILabel("", true));
            monitorGrid = new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(100, 30), new Vector2(20, 0), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNum, gridContents.ToArray());
            Update();
            monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("", "Health Monitor", HighLogic.UISkin, 500, monitorGrid), false, HighLogic.UISkin, false);
        }

        public void Update()
        {
            if (monitorWindow != null)
            {
                if (monitorGrid == null)
                {
                    Log.Post("monitorGrid is null.", Log.LogLevel.Error);
                    return;
                }
                if (gridContents == null)
                {
                    Log.Post("gridContents is null.", Log.LogLevel.Error);
                    return;
                }
                if (gridContents.Count != (Core.KerbalHealthList.Count + 1) * colNum)  // # of tracked kerbals has changed => close & reopen the window
                {
                    Log.Post("Kerbals' number has changed. Recreating the Health Monitor window.");
                    UndisplayData();
                    DisplayData();
                }
                // Fill the Health Monitor's grid with kerbals' health data
                for (int i = 0; i < Core.KerbalHealthList.Count; i++)
                {
                    KerbalHealthStatus khs = Core.KerbalHealthList[i];
                    gridContents[(i + 1) * colNum].SetOptionText(khs.Name);
                    double ch = KerbalHealthStatus.HealthChangePerDay(khs.PCM, false);
                    string trend = "~ ";
                    if (ch > 0) trend = "↑ ";
                    if (ch < 0) trend = "↓ ";
                    gridContents[(i + 1) * colNum + 1].SetOptionText(trend + (100 * khs.Health).ToString("F2") + "% (" + khs.HP.ToString("F2") + ")");
                    gridContents[(i + 1) * colNum + 2].SetOptionText(khs.Condition.ToString());
                    gridContents[(i + 1) * colNum + 3].SetOptionText(KSPUtil.PrintDateDeltaCompact(khs.TimeToNextCondition(), true, false));
                }
            }
        }

        public void UndisplayData()
        {
            if (monitorWindow != null) monitorWindow.Dismiss();
       }

        public void OnDisable()
        {
            Log.Post("KerbalHealthScenario.OnDisable");
            UndisplayData();
            GameEvents.onKerbalAdded.Remove(Core.KerbalHealthList.Add);
            GameEvents.onKerbalRemoved.Remove(Core.KerbalHealthList.Remove);
            GameEvents.onCrewOnEva.Remove(OnKerbalOnEva);
            if (ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(button);
            Log.Post("KerbalHealthScenario.OnDisable finished.");
        }

        public override void OnSave(ConfigNode node)
        {
            Log.Post("KerbalHealthScenario.OnSave");
            UpdateKerbals(true);
            int i = 0;
            foreach (KerbalHealthStatus khs in Core.KerbalHealthList)
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
            Core.KerbalHealthList.Clear();
            int i = 0;
            foreach (ConfigNode n in node.GetNodes())
            {
                if (n.name == "KerbalHealthStatus")
                {
                    Core.KerbalHealthList.Add(new KerbalHealthStatus(n));
                    i++;
                }
            }
            lastUpdated = Planetarium.GetUniversalTime();
            Log.Post("" + i + " kerbal(s) loaded.");
        }
    }
}
