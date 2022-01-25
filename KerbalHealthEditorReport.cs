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
        Dictionary<string, bool> kerbalsToTrain = new Dictionary<string, bool>();

        int CLSSpacesCount => CLS.Enabled ? CLS.CLSAddon.Vessel.Spaces.Count : 0;

        public static bool HealthModulesEnabled { get; private set; } = true;

        public static bool SimulateTrained { get; private set; } = true;

        #region LIFE CYCLE

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
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(OnAppLauncherClicked, OnAppLauncherClicked, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                Core.Log("Registering Toolbar button...");
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthReport");
                toolbarButton.Text = Localizer.Format("#KH_ER_ButtonTitle");
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += _ => OnAppLauncherClicked();
            }

            Core.KerbalHealthList.RegisterKerbals();
            Core.Log("KerbalHealthEditorReport.Start finished.");
        }

        public void OnDisable()
        {
            Core.Log("KerbalHealthEditorReport.OnDisable", LogLevel.Important);
            HideWindow();
            if (toolbarButton != null)
                toolbarButton.Destroy();
            if (appLauncherButton != null && ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Core.Log("KerbalHealthEditorReport.OnDisable finished.");
        }

        #endregion LIFE CYCLE

        #region DIALOG WINDOW

        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;

        bool dirty = false;

        enum WindowMode
        {
            HealthReport = 0,
            Training
        }

        const int reportsColumnCount = 3;
        int trainingColumnCount = 4;
        PopupDialog window;
        WindowMode windowMode = WindowMode.HealthReport;
        static Vector2 windowPosition = new Vector2(0.5f, 0.5f);
        List <DialogGUIBase> gridContent;
        DialogGUILabel clsSpaceNameLbl, spaceLbl, recupLbl, shieldingLbl, exposureLbl, shelterExposureLbl;
        int clsSpaceIndex = 0;

        ICLSSpace CLSSpace => CLSSpacesCount > clsSpaceIndex ? CLS.CLSAddon.Vessel.Spaces[clsSpaceIndex] : null;

        bool HasTrainableParts => Core.GetTrainableParts(EditorLogic.SortedShipList).Any();

        public void ShowWindow()
        {
            Core.Log("KerbalHealthEditorReport.DisplayData", LogLevel.Important);
            if (ShipConstruction.ShipManifest == null)
            {
                HideWindow();
                return;
            }

            // Health Report window
            if (windowMode == WindowMode.HealthReport)
            {
                if (!ShipConstruction.ShipManifest.HasAnyCrew())
                {
                    HideWindow();
                    return;
                }

                gridContent = new List<DialogGUIBase>((ShipConstruction.ShipManifest.CrewCount + 1) * reportsColumnCount)
                {
                    // Creating column titles
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_Name")}</color></b>", true),
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_Trend")}</color></b>", true),
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_MissionTime")}</color></b>", true)
                };

                // Initializing Health Report's grid with empty labels, to be filled in Update()
                for (int i = 0; i < ShipConstruction.ShipManifest.CrewCount * reportsColumnCount; i++)
                    gridContent.Add(new DialogGUILabel("", true));

                // Preparing factors checklist
                List<DialogGUIToggle> checklist = new List<DialogGUIToggle>(Core.Factors.Select(f => new DialogGUIToggle(f.IsEnabledInEditor, f.Title, state =>
                {
                    f.SetEnabledInEditor(state);
                    Invalidate();
                })));

                if (KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                    checklist.Add(new DialogGUIToggle(SimulateTrained, Localizer.Format("#KH_ER_Trained"), state =>
                    {
                        SimulateTrained = state;
                        Invalidate();
                    }));

                checklist.Add(new DialogGUIToggle(HealthModulesEnabled, Localizer.Format("#KH_ER_HealthModules"), state =>
                {
                    HealthModulesEnabled = state;
                    Invalidate();
                }));

                window = PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog(
                        "HealthReport",
                        "",
                        Localizer.Format("#KH_ER_Windowtitle"),
                        HighLogic.UISkin,
                        new Rect(windowPosition.x, windowPosition.y, reportsColumnCount * 120 + 20, 10),
                        new DialogGUIGridLayout(
                            new RectOffset(3, 3, 3, 3),
                            new Vector2(110, 30),
                            new Vector2(10, 0),
                            GridLayoutGroup.Corner.UpperLeft,
                            GridLayoutGroup.Axis.Horizontal,
                            TextAnchor.MiddleCenter,
                            GridLayoutGroup.Constraint.FixedColumnCount,
                            reportsColumnCount,
                            gridContent.ToArray()),
                        CLS.Enabled
                        ? new DialogGUIHorizontalLayout(
                            TextAnchor.MiddleCenter,
                            new DialogGUIButton("<", OnPreviousCLSSpaceButtonSelected, () => CLSSpacesCount > 1, false),
                            clsSpaceNameLbl = new DialogGUILabel("", true),
                            new DialogGUIButton(">", OnNextCLSSpaceButtonSelected, () => CLSSpacesCount > 1, false))
                        : new DialogGUIBase(),
                        new DialogGUIHorizontalLayout(
                            new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Space")}</color>", false),
                            spaceLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                            new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Recuperation")}</color>", false),
                            recupLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true)),
                        new DialogGUIHorizontalLayout(
                            new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Shielding")}</color>", false),
                            shieldingLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                            new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_Exposure")}</color>", false),
                            exposureLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                            new DialogGUILabel($"<color=white>{Localizer.Format("#KH_ER_ShelterExposure")}</color>", false),
                            shelterExposureLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true)),
                        new DialogGUIHorizontalLayout(TextAnchor.MiddleCenter, new DialogGUILabel(Localizer.Format("#KH_ER_Factors"))),
                        new DialogGUIGridLayout(
                            new RectOffset(3, 3, 3, 3),
                            new Vector2(110, 30),
                            new Vector2(10, 0),
                            GridLayoutGroup.Corner.UpperLeft,
                            GridLayoutGroup.Axis.Horizontal,
                            TextAnchor.MiddleCenter,
                            GridLayoutGroup.Constraint.FixedColumnCount,
                            3,
                            checklist.ToArray()),
                        new DialogGUIHorizontalLayout(
                            true,
                            false,
                            new DialogGUIButton(Localizer.Format("#KH_ER_TrainingMode"), SwitchToTrainingMode, () => KerbalHealthFactorsSettings.Instance.TrainingEnabled && HasTrainableParts, true),
                            new DialogGUIButton(Localizer.Format("#KH_ER_Reset"), OnResetButtonSelected, false))),
                    false,
                    HighLogic.UISkin,
                    false);
                Invalidate();
            }

            // Training Info window
            else
            {
                dirty = false;
                IList<ModuleKerbalHealth> trainableParts = new List<ModuleKerbalHealth>();
                foreach (ModuleKerbalHealth tp in Core.GetTrainableParts(EditorLogic.SortedShipList))
                    if (!trainableParts.Any(tp2 => tp2.PartName == tp.PartName))
                        trainableParts.Add(tp);
                List<KerbalHealthStatus> kerbals = Core.KerbalHealthList.Values
                    .Where(khs => khs.ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                    .ToList();
                if (trainableParts.Count == 0)
                {
                    Core.Log($"No trainable parts found.", LogLevel.Important);
                    SwitchToReportMode();
                    return;
                }

                // Creating column titles
                trainingColumnCount = trainableParts.Count + 3;
                gridContent = new List<DialogGUIBase>((kerbals.Count + 1) * trainingColumnCount)
                {
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_Name")}</color></b>", true),
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_TrainingTime")}</color></b>", true),
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_ER_TotalTraining")}</color></b>", true)
                };
                for (int i = 0; i < trainableParts.Count; i++)
                    gridContent.Add(new DialogGUILabel($"<b><color=white>{Core.GetPartTitle(trainableParts[i].PartName)}{(trainableParts[i].complexity != 1 ? $" ({trainableParts[i].complexity:P0})" : "")}</color></b>", true));

                // Filling out the rows
                kerbalsToTrain.Clear();
                int kerbalTrainingStatus = 0;
                foreach (KerbalHealthStatus kerbal in kerbals)
                {
                    if (kerbal.GetTrainingLevel() >= Core.TrainingCap)
                        kerbalTrainingStatus = 1;
                    else if (!kerbal.CanTrainAtKSC)
                        kerbalTrainingStatus = 2;
                    else if (kerbal.IsTrainingAtKSC)
                        kerbalTrainingStatus = 3;
                    else kerbalTrainingStatus = 4;

                    if (kerbalTrainingStatus >= 3)
                    {
                        kerbalsToTrain.Add(kerbal.Name, kerbalTrainingStatus == 4);
                        gridContent.Add(new DialogGUIToggle(kerbalTrainingStatus == 4, $"<b>{kerbal.FullName}</b>", state =>
                        {
                            Core.Log($"kerbalsToTrain['{kerbal.Name}'] = {state}");
                            kerbalsToTrain[kerbal.Name] = state;
                        }));
                    }
                    else gridContent.Add(new DialogGUILabel($"<b>{kerbal.FullName}</b>", true));

                    switch (kerbalTrainingStatus)
                    {
                        case 1:
                            gridContent.Add(new DialogGUILabel($"<b><color=yellow>—</color></b>", true));
                            break;

                        case 2:
                            gridContent.Add(new DialogGUILabel($"<b><color=red>{Localizer.Format("#KH_ER_CantTrain")}</color></b>", true));
                            break;

                        case 3:
                            //gridContent.Add(new DialogGUILabel($"<b><color=red>{Localizer.Format("#KH_ER_AlreadyTraining")}</color></b>", true));
                            //break;

                        case 4:
                            gridContent.Add(new DialogGUILabel($"<b><color=white>{Core.ParseUT(kerbal.TrainingETAFor(trainableParts), false, 10)}</color></b>", true));
                            break;
                    }

                    gridContent.Add(new DialogGUILabel($"<b>{kerbal.GetTrainingLevel():P1} / {Core.TrainingCap:P0}</b>", true));
                    for (int j = 0; j < trainableParts.Count; j++)
                    {
                        TrainingPart tp = kerbal.TrainingParts.Find(tp2 => tp2.Name == trainableParts[j].PartName);
                        if (tp != null)
                            gridContent.Add(new DialogGUILabel(tp.Level.ToString("P1"), true));
                        else gridContent.Add(new DialogGUILabel("", true));
                    }
                }

                window = PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog(
                        "HealthReport",
                        "",
                        Localizer.Format("#KH_ER_TrainingInfo_Title"),
                        HighLogic.UISkin,
                        new Rect(windowPosition.x, windowPosition.y, trainingColumnCount * 120 + 20, 10),
                        new DialogGUIGridLayout(
                            new RectOffset(5, 5, 5, 5),
                            new Vector2(110, 30),
                            new Vector2(10, 0),
                            GridLayoutGroup.Corner.UpperLeft,
                            GridLayoutGroup.Axis.Horizontal,
                            TextAnchor.MiddleCenter,
                            GridLayoutGroup.Constraint.FixedColumnCount,
                            trainingColumnCount,
                            gridContent.ToArray()),
                        new DialogGUIHorizontalLayout(
                            true,
                            false,
                            new DialogGUIButton(Localizer.Format("#KH_ER_ReportMode"), SwitchToReportMode, true),
                            new DialogGUIButton(Localizer.Format("#KH_ER_Train"), OnTrainButtonSelected, () => kerbalsToTrain.Any(kvp => kvp.Value), true))),
                    false,
                    HighLogic.UISkin,
                    false);
            }
            appLauncherButton?.SetTrue(false);
        }

        public void HideWindow()
        {
            if (window != null)
            {
                windowPosition = new Vector2(window.RTrf.anchoredPosition.x / Screen.width + 0.5f, window.RTrf.anchoredPosition.y / Screen.height + 0.5f);
                window.Dismiss();
            }
            appLauncherButton?.SetFalse(false);
        }

        public void RedrawWindow()
        {
            HideWindow();
            ShowWindow();
        }

        public void Invalidate() => dirty = true;

        public void Update()
        {
            if (window == null || !dirty)
                return;

            if (!KerbalHealthGeneralSettings.Instance.modEnabled || ShipConstruction.ShipManifest == null || ShipConstruction.ShipManifest.CrewCount == 0 || gridContent == null)
            {
                HideWindow();
                return;
            }

            // # of tracked kerbals has changed => close & reopen the window
            if (windowMode == WindowMode.Training || gridContent.Count != (ShipConstruction.ShipManifest.CrewCount + 1) * reportsColumnCount)
            {
                Core.Log("Kerbals' number has changed. Recreating the Health Report window.", LogLevel.Important);
                RedrawWindow();
                return;
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

            IList<ModuleKerbalHealth> trainingParts = Core.GetTrainableParts(EditorLogic.SortedShipList);

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
                string color = "";
                if (clsSpace != null && pcm.GetCLSSpace() == clsSpace)
                {
                    Core.Log($"{pcm.name} is in the current {clsSpace.Name} CLS space.");
                    color = "<color=white>";
                }
                else color = "<color=yellow>";

                gridContent[(i + 1) * reportsColumnCount].SetOptionText($"{color}{khs.FullName}</color>");
                khs.HP = khs.MaxHP;
                // Making this call here, so that BalanceHP doesn't have to:
                double changePerDay = khs.HPChangeTotal;
                double balanceHP = khs.BalanceHP;
                string s = balanceHP > 0
                    ? Localizer.Format("#KH_ER_BalanceHP", balanceHP.ToString("F1"), (balanceHP / khs.MaxHP * 100).ToString("F1"))
                    : Localizer.Format("#KH_ER_HealthPerDay", changePerDay.ToString("F1"));
                gridContent[(i + 1) * reportsColumnCount + 1].SetOptionText($"{color}{s}</color>");
                s = balanceHP > khs.NextConditionHP
                    ? "—"
                    : (khs.Recuperation > khs.Decay ? "> " : "") + Core.ParseUT(khs.ETAToNextCondition, false, 100);
                gridContent[(i + 1) * reportsColumnCount + 2].SetOptionText($"{color}{s}</color>");
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

        #endregion DIALOG WINDOW

        #region EVENT HANDLERS

        void OnAppLauncherClicked()
        {
            if (window != null)
                HideWindow();
            else ShowWindow();
        }

        void OnResetButtonSelected()
        {
            Core.Log("OnResetButtonSelected", LogLevel.Important);
            foreach (HealthFactor f in Core.Factors)
                f.ResetEnabledInEditor();
            HealthModulesEnabled = true;
            SimulateTrained = true;
            Invalidate();
        }

        void SwitchToTrainingMode()
        {
            Core.Log("OnSwitchToTrainingMode");
            windowMode = WindowMode.Training;
            RedrawWindow();
        }

        void SwitchToReportMode()
        {
            Core.Log("OnSwitchToReportMode");
            windowMode = WindowMode.HealthReport;
            RedrawWindow();
        }

        void OnTrainButtonSelected()
        {
            Core.Log("OnTrainButtonSelected", LogLevel.Important);
            if (!KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                return;

            foreach (KeyValuePair<string, bool> kvp in kerbalsToTrain)
                Core.Log($"{kvp.Key}: {kvp.Value}");

            int count = 0;
            foreach (string kerbal in kerbalsToTrain.Where(kvp => kvp.Value).Select(kvp => kvp.Key))
            {
                Core.Log($"Starting training of {kerbal}");
                KerbalHealthStatus khs = Core.KerbalHealthList[kerbal];
                if (khs == null)
                {
                    Core.Log($"{kerbal} is marked for training but not present in KerbalHealthList!", LogLevel.Error);
                    continue;
                }
                khs.AddCondition(KerbalHealthStatus.Condition_Training);
                khs.StartTraining(EditorLogic.SortedShipList, EditorLogic.fetch.ship.shipName);
                count++;
            }

            if (count > 0)
                ScreenMessages.PostScreenMessage(Localizer.Format("#KH_ER_KerbalsStartedTraining", count));
        }

        void OnPreviousCLSSpaceButtonSelected() => SetCLSSpaceIndex(clsSpaceIndex - 1);

        void OnNextCLSSpaceButtonSelected() => SetCLSSpaceIndex(clsSpaceIndex + 1);

        #endregion EVENT HANDLERS
    }
}
