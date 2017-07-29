using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalHealth
{
    /// <summary>
    /// Main class for processing kerbals' health and health changes
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT)]
    public class KerbalHealthScenario : ScenarioModule
    {
        static double lastUpdated;  // UT at last health update
        static double nextEventTime;  // UT when (or after) next event check occurs

        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;
        bool dirty = false;
        const int colNum = 5;  // # of columns in Health Monitor
        Rect monitorPosition = new Rect(0.5f, 0.5f, colNum * 120, 50);
        PopupDialog monitorWindow;  // Health Monitor window
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Health Monitor grid's labels

        public void Start()
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.Start", Core.LogLevel.Important);
            Core.Log(Core.Factors.Count + " factors initialized.");
            Core.KerbalHealthList.RegisterKerbals();
            GameEvents.onCrewOnEva.Add(OnKerbalEva);
            if (ToolbarManager.ToolbarAvailable && Core.UseBlizzysToolbar)
            {
                Core.Log("Registering Blizzy's Toolbar button...", Core.LogLevel.Important);
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthMonitor");
                toolbarButton.Text = "Kerbal Health Monitor";
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += (e) => { if (monitorWindow == null) DisplayData(); else UndisplayData(); };
            }
            else
            {
                Core.Log("Registering AppLauncher button...", Core.LogLevel.Important);
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }
            lastUpdated = Planetarium.GetUniversalTime();
            nextEventTime = lastUpdated + GetNextEventInterval();
            Core.Log("KerbalHealthScenario.Start finished.", Core.LogLevel.Important);
        }

        /// <summary>
        /// Marks the kerbal as being on EVA, to apply EVA-only effects
        /// </summary>
        /// <param name="action"></param>
        public void OnKerbalEva(GameEvents.FromToAction<Part, Part> action)
        {
            if (!Core.ModEnabled) return;
            Core.Log(action.to.protoModuleCrew[0].name + " went on EVA from " + action.from.name + ".", Core.LogLevel.Important);
            Core.KerbalHealthList.Find(action.to.protoModuleCrew[0]).IsOnEVA = true;
            UpdateKerbals(true);
        }

        /// <summary>
        /// Next event update is scheduled after a random period of time, between 0 and 2 days
        /// </summary>
        /// <returns></returns>
        double GetNextEventInterval()
        { return Core.rand.NextDouble() * KSPUtil.dateTimeFormatter.Day * 2; }

        void UpdateKerbals(bool forced)
        {
            double time = Planetarium.GetUniversalTime();
            double timePassed = time - lastUpdated;
            if (forced || ((timePassed >= Core.UpdateInterval) && (timePassed >= Core.MinUpdateInterval * TimeWarp.CurrentRate)))
            {
                Core.Log("UT is " + time + ". Updating for " + timePassed + " seconds.");
                //if (!DFWrapper.InstanceExists)
                //{
                //    Core.Log("Initializing DFWrapper...");
                //    DFWrapper.InitDFWrapper();
                //}
                Core.KerbalHealthList.Update(timePassed);
                lastUpdated = time;
                if (Core.EventsEnabled)
                    while (time >= nextEventTime)  // Can take several turns of event processing at high time warp
                    {
                        Core.Log("Processing events...");
                        Core.KerbalHealthList.ProcessEvents();
                        nextEventTime += GetNextEventInterval();
                        Core.Log("Next event processing is scheduled at " + KSPUtil.PrintDateCompact(nextEventTime, true), Core.LogLevel.Important);
                    }
                dirty = true;
            }
        }

        public void FixedUpdate()
        { if (Core.ModEnabled) UpdateKerbals(false); }

        /// <summary>
        /// Shows Health monitor when the AppLauncher button is enabled
        /// </summary>
        public void DisplayData()
        {
            Core.Log("KerbalHealthScenario.DisplayData", Core.LogLevel.Important);
            UpdateKerbals(true);
            gridContents = new System.Collections.Generic.List<DialogGUIBase>((Core.KerbalHealthList.Count + 1) * colNum);
            // Creating column titles
            gridContents.Add(new DialogGUILabel("Name", true));
            gridContents.Add(new DialogGUILabel("Condition", true));
            gridContents.Add(new DialogGUILabel("Health", true));
            gridContents.Add(new DialogGUILabel("Change/day", true));
            gridContents.Add(new DialogGUILabel("Time Left", true));
            // Initializing Health Monitor's grid with empty labels, to be filled in Update()
            for (int i = 0; i < Core.KerbalHealthList.Count * colNum; i++) gridContents.Add(new DialogGUILabel("", true));
            dirty = true;
            monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("Health Monitor", "", "Health Monitor", HighLogic.UISkin, monitorPosition, new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(100, 30), new Vector2(20, 0), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNum, gridContents.ToArray())), false, HighLogic.UISkin, false);
        }

        /// <summary>
        /// Hides the Health Monitor window
        /// </summary>
        public void UndisplayData()
        {
            if (monitorWindow != null)
            {
                Vector3 v = monitorWindow.RTrf.position;
                monitorPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, colNum * 120, 50);
                monitorWindow.Dismiss();
            }
        }

        public void Update()
        {
            if (!Core.ModEnabled)
            {
                if (monitorWindow != null) monitorWindow.Dismiss();
                return;
            }
            if ((monitorWindow != null) && dirty)
            {
                if (gridContents == null)
                {
                    Core.Log("KerbalHealthScenario.gridContents is null.", Core.LogLevel.Error);
                    return;
                }
                if (gridContents.Count != (Core.KerbalHealthList.Count + 1) * colNum)  // # of tracked kerbals has changed => close & reopen the window
                {
                    Core.Log("Kerbals' number has changed. Recreating the Health Monitor window.", Core.LogLevel.Important);
                    UndisplayData();
                    DisplayData();
                }
                // Fill the Health Monitor's grid with kerbals' health data
                for (int i = 0; i < Core.KerbalHealthList.Count; i++)
                {
                    KerbalHealthStatus khs = Core.KerbalHealthList[i];
                    double ch = khs.LastChangeTotal;
                    gridContents[(i + 1) * colNum].SetOptionText(khs.Name);
                    gridContents[(i + 1) * colNum + 1].SetOptionText(khs.ConditionString);
                    gridContents[(i + 1) * colNum + 2].SetOptionText((100 * khs.Health).ToString("F2") + "% (" + khs.HP.ToString("F2") + ")");
                    gridContents[(i + 1) * colNum + 3].SetOptionText((khs.Health >= 1) ? "—" : (((ch > 0) ? "+" : "") + ch.ToString("F2")));
                    double b = khs.GetBalanceHP();
                    string s = "";
                    if (b > khs.NextConditionHP()) s = "—";
                    else s = ((b > 0) ? "> " : "") + Core.ParseUT(khs.TimeToNextCondition());
                    gridContents[(i + 1) * colNum + 4].SetOptionText(s);
                }
                dirty = false;
            }
        }

        public void OnDisable()
        {
            Core.Log("KerbalHealthScenario.OnDisable", Core.LogLevel.Important);
            UndisplayData();
            GameEvents.onCrewOnEva.Remove(OnKerbalEva);
            if (toolbarButton != null) toolbarButton.Destroy();
            if ((appLauncherButton != null) && (ApplicationLauncher.Instance != null))
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Core.Log("KerbalHealthScenario.OnDisable finished.", Core.LogLevel.Important);
        }

        public override void OnSave(ConfigNode node)
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.OnSave", Core.LogLevel.Important);
            UpdateKerbals(true);
            int i = 0;
            node.AddValue("nextEventTime", nextEventTime);
            foreach (KerbalHealthStatus khs in Core.KerbalHealthList)
            {
                Core.Log("Saving " + khs.Name + "'s health.");
                node.AddNode(khs.ConfigNode);
                i++;
            }
            Core.Log("KerbalHealthScenario.OnSave complete. " + i + " kerbal(s) saved.", Core.LogLevel.Important);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.OnLoad", Core.LogLevel.Important);
            Core.KerbalHealthList.Clear();
            int i = 0;
            if (node.HasValue("nextEventTime")) nextEventTime = double.Parse(node.GetValue("nextEventTime"));
            else nextEventTime = Planetarium.GetUniversalTime() + GetNextEventInterval();
            foreach (ConfigNode n in node.GetNodes("KerbalHealthStatus"))
            {
                Core.KerbalHealthList.Add(new KerbalHealthStatus(n));
                i++;
            }
            lastUpdated = Planetarium.GetUniversalTime();
            Core.Log("" + i + " kerbal(s) loaded.", Core.LogLevel.Important);
        }
    }
}
