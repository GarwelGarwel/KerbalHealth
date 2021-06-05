using ConnectedLivingSpace;
using KSP.Localization;
using KSP.UI.Screens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KerbalHealth
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class KerbalHealthEditorReport : MonoBehaviour
    {
        static bool healthModulesEnabled = true;
        static bool simulateTrained = true;

        int clsSpaceIndex = 0;

        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;
        bool dirty = false;
        Rect reportPosition = new Rect(0.5f, 0.5f, 420, 50);

        // Health Report window
        PopupDialog reportWindow;

        // Health Report grid's labels
        List<DialogGUIBase> gridContent;

        DialogGUILabel clsSpaceNameLbl, spaceLbl, recupLbl, shieldingLbl, exposureLbl, shelterExposureLbl;

        // # of columns in Health Report
        int colNum = 4;

        int CLSSpacesCount => CLS.Enabled ? CLS.CLSAddon.Vessel.Spaces.Count : 0;

        ICLSSpace CLSSpace => CLSSpacesCount > clsSpaceIndex ? CLS.CLSAddon.Vessel.Spaces[clsSpaceIndex] : null;

        public static bool HealthModulesEnabled => healthModulesEnabled;

        public static bool SimulateTrained => simulateTrained;

        public void Start()
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log("KerbalHealthEditorReport.Start", LogLevel.Important);

            GameEvents.onEditorShipModified.Add(_ => Invalidate());
            GameEvents.onEditorPodDeleted.Add(Invalidate);
            GameEvents.onEditorScreenChange.Add(_ => Invalidate());

            if (KerbalHealthGeneralSettings.Instance.ShowAppLauncherButton)
            {
                Core.Log("Registering AppLauncher button...");
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                Core.Log("Registering Toolbar button...");
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthReport");
                toolbarButton.Text = Localizer.Format("#KH_ER_ButtonTitle");
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += e =>
                {
                    if (reportWindow == null)
                        DisplayData();
                    else UndisplayData();
                };
            }

            Core.KerbalHealthList.RegisterKerbals();
            Core.Log("KerbalHealthEditorReport.Start finished.");
        }

        public void DisplayData()
        {
            Core.Log("KerbalHealthEditorReport.DisplayData", LogLevel.Important);
            if (ShipConstruction.ShipManifest == null || !ShipConstruction.ShipManifest.HasAnyCrew())
                return;

            gridContent = new List<DialogGUIBase>((ShipConstruction.ShipManifest.CrewCount + 1) * colNum)
            {
                // Creating column titles
                 new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_Name")}</color></b>", true),//Name
                 new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_Trend")}</color></b>", true),//Trend
                 new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_MissionTime")}</color></b>", true),//Mission Time
                 new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_TrainingTime")}</color></b>", true)//Training Time
            };

            // Initializing Health Report's grid with empty labels, to be filled in Update()
            for (int i = 0; i < ShipConstruction.ShipManifest.CrewCount * colNum; i++)
                gridContent.Add(new DialogGUILabel("", true));

            // Preparing factors checklist
            List<DialogGUIToggle> checklist = new List<DialogGUIToggle>(Core.Factors.Select(f => new DialogGUIToggle(f.IsEnabledInEditor, f.Title, state =>
            {
                f.SetEnabledInEditor(state);
                Invalidate();
            })));

            if (KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                checklist.Add(new DialogGUIToggle(simulateTrained, Localizer.Format("#KH_ER_Trained"), state =>
                {
                    simulateTrained = state;
                    Invalidate();
                }));

            checklist.Add(new DialogGUIToggle(healthModulesEnabled, Localizer.Format("#KH_ER_HealthModules"), state =>
            {
                healthModulesEnabled = state;
                Invalidate();
            }));

            reportWindow = PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(
                    "HealthReport",
                    "",
                    Localizer.Format("#KH_ER_Windowtitle"),//Health Report
                    HighLogic.UISkin,
                    reportPosition,
                    new DialogGUIGridLayout(
                        new RectOffset(3, 3, 3, 3),
                        new Vector2(90, 30),
                        new Vector2(10, 0),
                        GridLayoutGroup.Corner.UpperLeft,
                        GridLayoutGroup.Axis.Horizontal,
                        TextAnchor.MiddleCenter,
                        GridLayoutGroup.Constraint.FixedColumnCount,
                        colNum,
                        gridContent.ToArray()),
                    CLS.Enabled
                    ? new DialogGUIHorizontalLayout(
                        TextAnchor.MiddleCenter,
                        new DialogGUIButton("<", OnPreviousCLSSpaceButtonSelected, () => CLSSpacesCount > 1, false),
                        clsSpaceNameLbl = new DialogGUILabel("", true),
                        new DialogGUIButton(">", OnNextCLSSpaceButtonSelected, () => CLSSpacesCount > 1, false))
                    : new DialogGUIBase(),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Space")}</color>", false),//Space:
                        spaceLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                        new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Recuperation")}</color>", false),//Recuperation:
                        recupLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true)),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Shielding")}</color>", false),//Shielding:
                        shieldingLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                        new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Exposure")}</color>", false),
                        exposureLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                        new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_ShelterExposure")}</color>", false),
                        shelterExposureLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true)),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel("", true),
                        new DialogGUILabel(Localizer.Format("#KH_ER_Factors"), true),
                        new DialogGUIButton(Localizer.Format("#KH_ER_Train"), OnTrainButtonSelected, () => KerbalHealthFactorsSettings.Instance.TrainingEnabled, false),
                        new DialogGUIButton(Localizer.Format("#KH_ER_Reset"), OnResetButtonSelected, false)),
                    new DialogGUIGridLayout(
                        new RectOffset(3, 3, 3, 3),
                        new Vector2(130, 30),
                        new Vector2(10, 0),
                        GridLayoutGroup.Corner.UpperLeft,
                        GridLayoutGroup.Axis.Horizontal,
                        TextAnchor.MiddleCenter,
                        GridLayoutGroup.Constraint.FixedColumnCount,
                        3,
                        checklist.ToArray())),
                false,
                HighLogic.UISkin,
                false);
            Invalidate();
        }

        void OnResetButtonSelected()
        {
            Core.Log("OnResetButtonSelected", LogLevel.Important);
            foreach (HealthFactor f in Core.Factors)
                f.ResetEnabledInEditor();
            healthModulesEnabled = true;
            simulateTrained = true;
            Invalidate();
        }

        void OnTrainButtonSelected()
        {
            Core.Log("OnTrainButtonSelected", LogLevel.Important);
            if (!KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                return;

            List<string> s = new List<string>();
            List<string> f = new List<string>();

            foreach (KerbalHealthStatus khs in ShipConstruction.ShipManifest.GetAllCrew(false)
                .Select(pcm => Core.KerbalHealthList[pcm])
                .Where(khs => khs != null))
                if (khs.CanTrainAtKSC)
                {
                    khs.StartTraining(EditorLogic.SortedShipList, EditorLogic.fetch.ship.shipName);
                    khs.AddCondition(KerbalHealthStatus.Condition_Training);
                    s.Add(khs.Name);
                }
                else
                {
                    Core.Log($"{khs.Name} can't train. They are {khs.PCM.rosterStatus} and at {khs.Health:P1} health.", LogLevel.Important);
                    f.Add(khs.Name);
                }

            string msg = "";
            if (s.Any())
                if (s.Count == 1)
                    msg = Localizer.Format("#KH_ER_KerbalStartedTraining", s[0]); // + " started training.
                else
                {
                    msg = Localizer.Format("#KH_ER_KerbalsStartedTraining"); //The following kerbals started training:
                    foreach (string k in s)
                        msg += $"\r\n- {k}";
                }

            if (f.Any())
            {
                if (msg.Length != 0)
                    msg += "\r\n\n";
                if (f.Count == 1)
                    msg += $"<color=red>{Localizer.Format("#KH_ER_KerbalCantTrain", f[0])}"; //* can't train.
                else
                {
                    msg += $"<color=red>{Localizer.Format("#KH_ER_KerbalsCantTrain")}";  //The following kerbals can't train:
                    foreach (string k in f)
                        msg += $"\r\n- {k}";
                }
                msg += "</color>";
            }
            Core.ShowMessage(msg, false);
        }

        void SetCLSSpaceIndex(int i)
        {
            if (CLSSpacesCount != 0)
            {
                if (CLSSpace != null)
                    CLSSpace.Highlight(false);
                clsSpaceIndex = i % CLSSpacesCount;
                if (clsSpaceIndex < 0)
                    clsSpaceIndex += CLSSpacesCount;
                CLSSpace.Highlight(true);
            }
            else clsSpaceIndex = 0;
            Invalidate();
        }

        void OnPreviousCLSSpaceButtonSelected() => SetCLSSpaceIndex(clsSpaceIndex - 1);

        void OnNextCLSSpaceButtonSelected() => SetCLSSpaceIndex(clsSpaceIndex + 1);

        public void UndisplayData()
        {
            if (reportWindow != null)
            {
                Vector3 v = reportWindow.RTrf.position;
                reportPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, 420, 50);
                reportWindow.Dismiss();
            }
        }

        public void Update()
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || ShipConstruction.ShipManifest == null || ShipConstruction.ShipManifest.CrewCount == 0)
            {
                if (reportWindow != null)
                    reportWindow.Dismiss();
                return;
            }

            if (reportWindow != null && dirty)
            {
                if (gridContent == null)
                {
                    Core.Log("gridContent is null.", LogLevel.Error);
                    return;
                }

                // # of tracked kerbals has changed => close & reopen the window
                if (gridContent.Count != (ShipConstruction.ShipManifest.CrewCount + 1) * colNum)
                {
                    Core.Log("Kerbals' number has changed. Recreating the Health Report window.", LogLevel.Important);
                    UndisplayData();
                    DisplayData();
                }

                // Fill the Health Report's grid with kerbals' health data
                int i = 0;
                KerbalHealthStatus khs = null;
                ICLSSpace clsSpace = CLSSpace;
                if (CLS.Enabled)
                {
                    if (clsSpaceIndex >= CLSSpacesCount)
                        SetCLSSpaceIndex(CLSSpacesCount - 1);
                    if (clsSpace?.Name != null)
                    {
                        if (clsSpace.Name.Length == 0)
                            clsSpace.Name = clsSpace.Parts[0].Part.name;
                        clsSpaceNameLbl.SetOptionText(Localizer.Format("#KH_ER_CLSSpace", clsSpace.Name));
                    }
                    else clsSpaceNameLbl.SetOptionText("");
                    Core.Log($"Selected CLS space index: {clsSpaceIndex}/{CLSSpacesCount}; space: {clsSpace?.Name ?? "N/A"}");
                }

                List<ModuleKerbalHealth> trainingParts = Core.GetTrainingCapableParts(EditorLogic.SortedShipList);

                foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetAllCrew(false).Where(pcm => pcm != null))
                {
                    khs = Core.KerbalHealthList[pcm]?.Clone();
                    if (khs == null)
                    {
                        Core.Log($"Could not create a clone of KerbalHealthStatus for {pcm.name}. It is {(Core.KerbalHealthList[pcm] == null ? "not " : "")}found in KerbalHealthList, which contains {Core.KerbalHealthList.Count} records.", LogLevel.Error);
                        i++;
                        continue;
                    }

                    khs.SetDirty();
                    string colorTag = "";
                    if (clsSpace != null && pcm.GetCLSSpace() == clsSpace)
                    {
                        Core.Log($"{pcm.name} is in the current {clsSpace.Name} CLS space.");
                        colorTag = "<color=white>";
                    }
                    else colorTag = "<color=yellow>";

                    gridContent[(i + 1) * colNum].SetOptionText($"{colorTag}{khs.FullName}</color>");
                    khs.HP = khs.MaxHP;
                    // Making this call here, so that BalanceHP doesn't have to:
                    double changePerDay = khs.HPChangeTotal;
                    double balanceHP = khs.BalanceHP;
                    string s = balanceHP > 0
                        ? Localizer.Format("#KH_ER_BalanceHP", balanceHP.ToString("F1"), (balanceHP / khs.MaxHP * 100).ToString("F1"))
                        : Localizer.Format("#KH_ER_HealthPerDay", changePerDay.ToString("F1"));
                    gridContent[(i + 1) * colNum + 1].SetOptionText($"{colorTag}{s}</color>");
                    s = balanceHP > khs.NextConditionHP
                        ? "—"
                        : (khs.Recuperation > khs.Decay ? "> " : "") + Core.ParseUT(khs.ETAToNextCondition, false, 100);
                    gridContent[(i + 1) * colNum + 2].SetOptionText($"{colorTag}{s}</color>");
                    gridContent[(i + 1) * colNum + 3].SetOptionText($"{colorTag}{(KerbalHealthFactorsSettings.Instance.TrainingEnabled ? Core.ParseUT(TrainingTime(khs, trainingParts), false, 100) : Localizer.Format("#KH_NA"))}</color>");
                    i++;
                }

                HealthEffect vesselEffects = new HealthEffect(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.CrewCount, clsSpace);
                Core.Log($"{(clsSpace != null ? clsSpace.Name : "Vessel's")} effects:\n{vesselEffects}");

                spaceLbl.SetOptionText($"<color=white>{vesselEffects.Space:F1}</color>");
                recupLbl.SetOptionText($"<color=white>{vesselEffects.EffectiveRecuperation:P1}</color>");
                shieldingLbl.SetOptionText($"<color=white>{vesselEffects.Shielding:F1}</color>");
                exposureLbl.SetOptionText($"<color=white>{vesselEffects.VesselExposure:P1}</color>");
                if (KerbalHealthRadiationSettings.Instance.RadiationEnabled && !(Kerbalism.Found && KerbalHealthRadiationSettings.Instance.UseKerbalismRadiation))
                    shelterExposureLbl.SetOptionText($"<color=white>{vesselEffects.ShelterExposure:P1}</color>");

                dirty = false;
            }
        }

        public void Invalidate() => dirty = true;

        public void OnDisable()
        {
            Core.Log("KerbalHealthEditorReport.OnDisable", LogLevel.Important);
            UndisplayData();
            if (toolbarButton != null)
                toolbarButton.Destroy();
            if (appLauncherButton != null && ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Core.Log("KerbalHealthEditorReport.OnDisable finished.");
        }

        double TrainingTime(KerbalHealthStatus khs, List<ModuleKerbalHealth> modules) =>
            modules.Sum(mkh => (Core.TrainingCap - khs.TrainingLevelForPart(mkh.id)) * khs.GetPartTrainingComplexity(mkh))
            / khs.TrainingPerDay
            * KSPUtil.dateTimeFormatter.Day;
    }
}
