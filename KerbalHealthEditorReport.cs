using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalHealth
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class KerbalHealthEditorReport : MonoBehaviour
    {
        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;
        bool dirty = false;
        Rect reportPosition = new Rect(0.5f, 0.5f, 400, 50);
        PopupDialog reportWindow;  // Health Report window
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Health Report grid's labels
        DialogGUILabel spaceLbl, recupLbl, shieldingLbl, exposureLbl, shelterExposureLbl;
        int colNum = 4;  // # of columns in Health Report
        static bool healthModulesEnabled = true, trainingEnabled = true;

        public void Start()
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthEditorReport.Start", Core.LogLevel.Important);
            GameEvents.onEditorShipModified.Add(delegate(ShipConstruct sc) { Invalidate(); });
            GameEvents.onEditorPodDeleted.Add(Invalidate);
            GameEvents.onEditorScreenChange.Add(delegate(EditorScreen s) { Invalidate(); });
            if (Core.ShowAppLauncherButton)
            {
                Core.Log("Registering AppLauncher button...", Core.LogLevel.Important);
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }
            if (ToolbarManager.ToolbarAvailable)
            {
                Core.Log("Registering Blizzy's Toolbar button...", Core.LogLevel.Important);
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthReport");
                toolbarButton.Text = "Kerbal Health Report";
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += (e) => { if (reportWindow == null) DisplayData(); else UndisplayData(); };
            }
            Core.Log("KerbalHealthEditorReport.Start finished.", Core.LogLevel.Important);
        }

        public void DisplayData()
        {
            Core.Log("KerbalHealthEditorReport.DisplayData");
            if ((ShipConstruction.ShipManifest == null) || (!ShipConstruction.ShipManifest.HasAnyCrew()))
            {
                Core.Log("Ship is empty. Let's get outta here!", Core.LogLevel.Important);
                return;
            }
            gridContents = new List<DialogGUIBase>((Core.KerbalHealthList.Count + 1) * colNum)
            {
                // Creating column titles
                new DialogGUILabel("<b><color=\"white\">Name</color></b>", true),
                new DialogGUILabel("<b><color=\"white\">Trend</color></b>", true),
                new DialogGUILabel("<b><color=\"white\">Mission Time</color></b>", true),
                 new DialogGUILabel("<b><color=\"white\">Training Time</color></b>", true)
           };
            // Initializing Health Report's grid with empty labels, to be filled in Update()
            for (int i = 0; i < ShipConstruction.ShipManifest.CrewCount * colNum; i++)
                gridContents.Add(new DialogGUILabel("", true));

            // Preparing factors checklist
            List<DialogGUIToggle> checklist = new List<DialogGUIToggle>();
            foreach (HealthFactor f in Core.Factors)
                checklist.Add(new DialogGUIToggle(f.IsEnabledInEditor, f.Title, (state) => { f.SetEnabledInEditor(state); Invalidate(); }));
            if (Core.TrainingEnabled)
                checklist.Add(new DialogGUIToggle(true, "Trained", (state) => { trainingEnabled = state; Invalidate(); }));
            checklist.Add(new DialogGUIToggle(true, "Health modules", (state) => { healthModulesEnabled = state; Invalidate(); }));

            reportWindow = PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(
                    "Health Report",
                    "",
                    "Health Report",
                    HighLogic.UISkin,
                    reportPosition,
                    new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(80, 30), new Vector2(20, 0), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNum, gridContents.ToArray()),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel("<color=\"white\">Space: </color>", false),
                        spaceLbl = new DialogGUILabel("N/A", true),
                        new DialogGUILabel("<color=\"white\">Recuperation: </color>", false),
                        recupLbl = new DialogGUILabel("N/A", true)),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel("<color=\"white\">Shielding: </color>", false),
                        shieldingLbl = new DialogGUILabel("N/A", true),
                        new DialogGUILabel("<color=\"white\">Exposure: </color>", false),
                        exposureLbl = new DialogGUILabel("N/A", true),
                        new DialogGUILabel("<color=\"white\">Shelter Exposure: </color>", false),
                        shelterExposureLbl = new DialogGUILabel("N/A", true)),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel("", true),
                        new DialogGUILabel("Factors", true),
                        new DialogGUIButton("Reset", OnResetButtonSelected, false),
                        new DialogGUIButton("Train", OnTrainButtonSelected, () => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().TrainingEnabled, false)),
                    new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(190, 30), new Vector2(20, 0), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 2, checklist.ToArray())),
                false,
                HighLogic.UISkin,
                false);
            Invalidate();
        }

        public static bool HealthModulesEnabled => healthModulesEnabled;

        public static bool TrainingEnabled => trainingEnabled;

        public void OnResetButtonSelected()
        {
            foreach (HealthFactor f in Core.Factors)
                f.ResetEnabledInEditor();
            healthModulesEnabled = true;
            Invalidate();
        }

        public void OnTrainButtonSelected()
        {
            KerbalHealthStatus khs;
            Core.Log("OnTrainButtonSelected()");
            if (!Core.TrainingEnabled) return;
            List<string> s = new List<string>();
            List<string> f = new List<string>();
            foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetAllCrew(false))
            {
                if (pcm == null) continue;
                khs = Core.KerbalHealthList.Find(pcm);
                if (khs == null) continue;
                if (khs.CanTrainAtKSC)
                {
                    khs.StartTraining(EditorLogic.SortedShipList, EditorLogic.fetch.ship.shipName);
                    khs.AddCondition("Training");
                    s.Add(pcm.name);
                }
                else
                {
                    Core.Log(pcm.name + " can't train. They are " + pcm.rosterStatus + " and at " + khs.Health.ToString("P1") + " health.", Core.LogLevel.Important);
                    f.Add(pcm.name);
                }
            }
            string msg = "";
            if (s.Count > 0)
                if (s.Count == 1) msg = s[0] + " started training.";
                else
                {
                    msg = "The following kerbals started training:";
                    foreach (string k in s)
                        msg += "\r\n- " + k;
                }
            if (f.Count > 0)
            {
                if (msg != "") msg += "\r\n\n";
                if (f.Count == 1) msg += "<color=\"red\">" + f[0] + " can't train.</color>";
                else
                {
                    msg += "<color=\"red\">The following kerbals can't train:";
                    foreach (string k in f)
                        msg += "\r\n- " + k;
                    msg += "</color>";
                }
            }
            Core.ShowMessage(msg, false);
        }

        string GetShielding() => (ShipConstruction.ShipManifest.CrewCount != 0) ? Core.KerbalHealthList.Find(ShipConstruction.ShipManifest.GetAllCrew(false)[0]).VesselModifiers.Shielding.ToString("F1") : "N/A";

        string GetExposure() => (ShipConstruction.ShipManifest.CrewCount != 0) ? Core.KerbalHealthList.Find(ShipConstruction.ShipManifest.GetAllCrew(false)[0]).LastExposure.ToString("P1") : "N/A";

        public void UndisplayData()
        {
            if (reportWindow != null)
            {
                Vector3 v = reportWindow.RTrf.position;
                reportPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, 400, 50);
                reportWindow.Dismiss();
            }
        }

        double TrainingTime(KerbalHealthStatus khs, List<ModuleKerbalHealth> parts)
        {
            double c = 0;
            foreach (ModuleKerbalHealth mkh in parts)
                c += (Core.TrainingCap - khs.TrainingLevelForPart(mkh.id)) * khs.GetPartTrainingComplexity(mkh);
            return c / khs.TrainingPerDay * KSPUtil.dateTimeFormatter.Day;
        }

        public void Update()
        {
            if (!Core.ModEnabled)
            {
                if (reportWindow != null) reportWindow.Dismiss();
                return;
            }
            if ((reportWindow != null) && dirty)
            {
                if (gridContents == null)
                {
                    Core.Log("gridContents is null.", Core.LogLevel.Error);
                    return;
                }
                if (gridContents.Count != (ShipConstruction.ShipManifest.CrewCount + 1) * colNum)  // # of tracked kerbals has changed => close & reopen the window
                {
                    Core.Log("Kerbals' number has changed. Recreating the Health Report window.", Core.LogLevel.Important);
                    UndisplayData();
                    DisplayData();
                }
                // Fill the Health Report's grid with kerbals' health data
                int i = 0;
                KerbalHealthStatus khs = null;
                HealthModifierSet.VesselCache.Clear();

                List<ModuleKerbalHealth> trainingParts = Core.GetTrainingCapableParts(EditorLogic.SortedShipList);

                foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetAllCrew(false))
                {
                    if (pcm == null) continue;
                    gridContents[(i + 1) * colNum].SetOptionText(pcm.name);
                    khs = Core.KerbalHealthList?.Find(pcm)?.Clone();
                    if (khs == null)
                    {
                        Core.Log("Could not create a clone of KerbalHealthStatus for " + pcm.name + ". It is " + ((Core.KerbalHealthList?.Find(pcm) == null) ? "not " : "") + "found in KerbalHealthList, which contains " + Core.KerbalHealthList.Count + " records.", Core.LogLevel.Error);
                        i++;
                        continue;
                    }
                    khs.HP = khs.MaxHP;
                    double ch = khs.HealthChangePerDay();
                    double b = khs.GetBalanceHP();
                    string s = "";
                    if (b > 0) s = "-> " + b.ToString("F0") + " HP (" + (b / khs.MaxHP * 100).ToString("F0") + "%)";
                    else s = ch.ToString("F1") + " HP/day";
                    gridContents[(i + 1) * colNum + 1].SetOptionText(s);
                    if (b > khs.NextConditionHP()) s = "—";
                    else s = ((khs.LastRecuperation > khs.LastDecay) ? "> " : "") + Core.ParseUT(khs.TimeToNextCondition());
                    gridContents[(i + 1) * colNum + 2].SetOptionText(s);
                    gridContents[(i + 1) * colNum + 3].SetOptionText(Core.TrainingEnabled ? Core.ParseUT(TrainingTime(khs, trainingParts), false, 10) : "N/A");
                    i++;
                }
                spaceLbl.SetOptionText("<color=\"white\">" + khs.VesselModifiers.Space.ToString("F1") + "</color>");
                recupLbl.SetOptionText("<color=\"white\">" + khs.VesselModifiers.Recuperation.ToString("F1") + "%</color>");
                shieldingLbl.SetOptionText("<color=\"white\">" + khs.VesselModifiers.Shielding.ToString("F1") + "</color>");
                exposureLbl.SetOptionText("<color=\"white\">" + khs.LastExposure.ToString("P1") + "</color>");
                shelterExposureLbl.SetOptionText("<color=\"white\">" + Core.GetShelterExposure(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.CrewCount).ToString("P1") + "</color>");
                Core.Log("Shelter exposure: " + Core.GetShelterExposure(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.CrewCount).ToString("P2"), Core.LogLevel.Important);
                dirty = false;
            }
        }

        public void Invalidate() => dirty = true;

        public void OnDisable()
        {
            Core.Log("KerbalHealthEditorReport.OnDisable", Core.LogLevel.Important);
            UndisplayData();
            if (toolbarButton != null) toolbarButton.Destroy();
            if ((appLauncherButton != null) && (ApplicationLauncher.Instance != null))
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Core.Log("KerbalHealthEditorReport.OnDisable finished.", Core.LogLevel.Important);
        }
    }
}
