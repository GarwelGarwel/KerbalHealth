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
        Rect reportPosition = new Rect(0.5f, 0.5f, 300, 50);
        PopupDialog reportWindow;  // Health Report window
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Health Report grid's labels
        DialogGUILabel shieldingLbl, exposureLbl;
        int colNum = 3;  // # of columns in Health Report
        static bool healthModulesEnabled = true;

        public void Start()
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthEditorReport.Start", Core.LogLevel.Important);
            GameEvents.onEditorShipModified.Add(delegate(ShipConstruct sc) { Invalidate(); });
            GameEvents.onEditorPodDeleted.Add(Invalidate);
            GameEvents.onEditorScreenChange.Add(delegate(EditorScreen s) { Invalidate(); });
            if (ToolbarManager.ToolbarAvailable && Core.UseBlizzysToolbar)
            {
                Core.Log("Registering Blizzy's Toolbar button...", Core.LogLevel.Important);
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthReport");
                toolbarButton.Text = "Kerbal Health Report";
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += (e) => { if (reportWindow == null) DisplayData(); else UndisplayData(); };
            }
            else
            {
                Core.Log("Registering AppLauncher button...", Core.LogLevel.Important);
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
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
                new DialogGUILabel("Name", true),
                new DialogGUILabel("Trend", true),
                new DialogGUILabel("Time Left", true)
            };
            // Initializing Health Report's grid with empty labels, to be filled in Update()
            for (int i = 0; i < ShipConstruction.ShipManifest.CrewCount * colNum; i++)
                gridContents.Add(new DialogGUILabel("", true));

            // Preparing factors checklist
            List<DialogGUIToggle> checklist = new List<DialogGUIToggle>();
            foreach (HealthFactor f in Core.Factors)
                checklist.Add(new DialogGUIToggle(f.IsEnabledInEditor, f.Title, (state) => { f.SetEnabledInEditor(state); Invalidate(); }));
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
                        new DialogGUILabel("Shielding: ", false),
                        shieldingLbl = new DialogGUILabel("N/A", true),
                        new DialogGUILabel("Exposure: ", false),
                        exposureLbl = new DialogGUILabel("N/A", true)),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel("", true),
                        new DialogGUILabel("Factors", true),
                        new DialogGUIButton("Reset", OnResetButtonSelected, false)),
                    new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(140, 30), new Vector2(20, 0), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 2, checklist.ToArray())),
                false, 
                HighLogic.UISkin, 
                false);
            Invalidate();
        }

        public static bool HealthModulesEnabled => healthModulesEnabled;

        public void OnResetButtonSelected()
        {
            foreach (HealthFactor f in Core.Factors)
                f.ResetEnabledInEditor();
            healthModulesEnabled = true;
            Invalidate();
        }

        string GetShielding() => (ShipConstruction.ShipManifest.CrewCount != 0) ? Core.KerbalHealthList.Find(ShipConstruction.ShipManifest.GetAllCrew(false)[0]).Shielding.ToString("F1") : "N/A";

        string GetExposure() => (ShipConstruction.ShipManifest.CrewCount != 0) ? Core.KerbalHealthList.Find(ShipConstruction.ShipManifest.GetAllCrew(false)[0]).Exposure.ToString("P1") : "N/A";

        public void UndisplayData()
        {
            if (reportWindow != null)
            {
                Vector3 v = reportWindow.RTrf.position;
                reportPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, 300, 50);
                reportWindow.Dismiss();
            }
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
                foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetAllCrew(false))
                {
                    if (pcm == null) continue;
                    gridContents[(i + 1) * colNum].SetOptionText(pcm.name);
                    khs = Core.KerbalHealthList.Find(pcm)?.Clone();
                    if (khs == null)
                    {
                        Core.Log("Could not create a clone of KerbalHealthStatus for " + pcm.name + ". KerbalHealthList contains " + Core.KerbalHealthList.Count + " records.", Core.LogLevel.Error);
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
                    i++;
                }
                shieldingLbl.SetOptionText(khs.Shielding.ToString("F1"));
                exposureLbl.SetOptionText(khs.Exposure.ToString("P1"));
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
