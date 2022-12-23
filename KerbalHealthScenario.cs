using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KerbalHealth
{
    /// <summary>
    /// Main class for processing kerbals' health
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR)]
    public class KerbalHealthScenario : ScenarioModule
    {
        // Current Kerbal Health version
        Version version;

        // UT at last health update
        static double lastUpdated;

        // List of scheduled radstorms
        List<RadStorm> radStorms = new List<RadStorm>();

        // Button handles
        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;

        // Health Monitor dimensions
        const int colNumMain = 8, colNumDetails = 6;
        const int colWidth = 100;
        const int colSpacing = 10;
        const int gridWidthList = colNumMain * (colWidth + colSpacing) - colSpacing;
        const int gridWidthDetails = colNumDetails * (colWidth + colSpacing) - colSpacing;

        // Health Monitor window
        PopupDialog monitorWindow;

        // Saved position of the Health Monitor window
        static Rect windowPosition = new Rect(0.5f, 0.5f, gridWidthList, 200);

        // Health Monitor grid's labels
        List<DialogGUIBase> gridContent;

        // List of displayed kerbal, sorted according to current settings
        SortedList<ProtoCrewMember, KerbalHealthStatus> kerbals;

        // Change flags
        bool dirty = false, crewChanged = false, vesselChanged = false;

        // Currently selected kerbal for details view, null if list is shown
        KerbalHealthStatus selectedKHS = null;

        // Current page in the list of kerbals
        int page = 1;

        // Whether the current vessel should be checked for untrained kerbals, to show notification
        bool checkUntrainedKerbals = false;

        // Message handle for untrained kerbals warning
        ScreenMessage untrainedKerbalsWarningMessage;

        // Comparer object for sorting kerbals in the Health Monitor
        KerbalComparer kerbalComparer = new KerbalComparer(KerbalHealthGeneralSettings.Instance.SortByLocation);

        // Profiling timers
#if DEBUG
        IterationTimer mainloopTimer = new IterationTimer("MAIN LOOP");
        IterationTimer updateTimer = new IterationTimer("GUI UPDATE", 25);
        static IterationTimer saveTimer = new IterationTimer("SAVE");
        static IterationTimer loadTimer = new IterationTimer("LOAD");
#endif

        int LinesPerPage => KerbalHealthGeneralSettings.Instance.LinesPerPage;

        bool ShowPages => Core.KerbalHealthList.Count > LinesPerPage;

        int PageCount => (int)Math.Ceiling((double)(Core.KerbalHealthList.Count) / LinesPerPage);

        int FirstLine => (page - 1) * LinesPerPage;

        int LineCount => Math.Min(Core.KerbalHealthList.Count - FirstLine, LinesPerPage);

        public void Start()
        {
            if (Core.IsInEditor)
                return;

            // Automatically updating settings from older versions
            Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != currentVersion)
                version = currentVersion;
            else Core.Log($"Kerbal Health v{version}");

#if DEBUG
            Core.Log("Debug mode", LogLevel.Important);
#endif

            // This needs to be run even if the mod is disabled, so that its settings can be reset:
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);

            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log("KerbalHealthScenario.Start", LogLevel.Important);

            Core.KerbalHealthList.RegisterKerbals();
            vesselChanged = true;

            lastUpdated = Planetarium.GetUniversalTime();

            GameEvents.onCrewOnEva.Add(OnKerbalEva);
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
            GameEvents.onCrewKilled.Add(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Add(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Add(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Add(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Add(OnKerbalRemoved);
            GameEvents.onKerbalNameChanged.Add(OnKerbalNameChanged);
            GameEvents.OnProgressComplete.Add(OnProgressComplete);
            GameEvents.onVesselWasModified.Add(onVesselWasModified);

            SetupDeepFreeze();
            SetupKerbalism();

            if (CLS.Enabled)
                Core.Log("ConnectedLivingSpace detected.");

            if (KerbalHealthGeneralSettings.Instance.ShowAppLauncherButton)
                RegisterAppLauncherButton();

            if (ToolbarManager.ToolbarAvailable)
            {
                Core.Log("Registering Toolbar button...");
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthMonitor");
                toolbarButton.Text = "Kerbal Health Monitor";
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += _ => OnAppLauncherClicked();
            }

            if (VesselNeedsCheckForUntrainedCrew(FlightGlobals.ActiveVessel))
                checkUntrainedKerbals = true;
        }

        public void OnDisable()
        {
            Core.Log("KerbalHealthScenario.OnDisable", LogLevel.Important);
            if (Core.IsInEditor)
                return;

            HideWindow();

            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);
            GameEvents.onCrewOnEva.Remove(OnKerbalEva);
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);
            GameEvents.onCrewKilled.Remove(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Remove(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Remove(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Remove(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Remove(OnKerbalRemoved);
            GameEvents.onKerbalNameChange.Remove(OnKerbalNameChanged);
            GameEvents.OnProgressComplete.Remove(OnProgressComplete);
            GameEvents.onVesselWasModified.Remove(onVesselWasModified);

            EventData<Part, ProtoCrewMember> dfEvent;
            dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalFrozen");
            if (dfEvent != null)
                dfEvent.Remove(OnKerbalFrozen);
            dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalThaw");
            if (dfEvent != null)
                dfEvent.Remove(OnKerbalThaw);

            UnregisterAppLauncherButton();
            if (toolbarButton != null)
                toolbarButton.Destroy();
        }

        /// <summary>
        /// Called to check and reset Kerbal Health settings, if needed
        /// </summary>
        public void OnGameSettingsApplied()
        {
            Core.Log("OnGameSettingsApplied", LogLevel.Important);
            if (KerbalHealthGeneralSettings.Instance.ResetSettings)
            {
                KerbalHealthGeneralSettings.Instance.Reset();
                KerbalHealthFactorsSettings.Instance.Reset();
                KerbalHealthQuirkSettings.Instance.Reset();
                KerbalHealthRadiationSettings.Instance.Reset();

                LoadSettingsFromConfig();

                ScreenMessages.PostScreenMessage(Localizer.Format("#KH_MSG_SettingsReset"), 5);
            }

            if (KerbalHealthGeneralSettings.Instance.modEnabled && KerbalHealthGeneralSettings.Instance.ShowAppLauncherButton && appLauncherButton == null)
                RegisterAppLauncherButton();

            if (!KerbalHealthGeneralSettings.Instance.ShowAppLauncherButton || !KerbalHealthGeneralSettings.Instance.modEnabled)
                UnregisterAppLauncherButton();

            if (KerbalHealthGeneralSettings.Instance.modEnabled)
                SetupKerbalism();

            kerbalComparer = new KerbalComparer(KerbalHealthGeneralSettings.Instance.SortByLocation);
        }

        /// <summary>
        /// Marks the kerbal as being on EVA to apply EVA-only effects
        /// </summary>
        public void OnKerbalEva(GameEvents.FromToAction<Part, Part> action)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"{action.to.protoModuleCrew[0].name} went on EVA from {action.from.name}.", LogLevel.Important);
            Core.KerbalHealthList[action.to.protoModuleCrew[0]].IsOnEVA = true;
            vesselChanged = true;
            UpdateKerbals(true);
        }

        public void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> action)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"onCrewBoardVessel(<'{action.from.name}', '{action.to.name}'>)");
            foreach (ProtoCrewMember pcm in action.to.protoModuleCrew)
                Core.KerbalHealthList[pcm].IsOnEVA = false;
            vesselChanged = true;
            UpdateKerbals(true);
        }

        public void OnCrewKilled(EventReport er)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || er == null)
                return;
            Core.Log($"OnCrewKilled(<'{er.msg}', {er.sender}, {er.other}>)", LogLevel.Important);
            Core.KerbalHealthList.Remove(er.sender);
            dirty = crewChanged = true;
        }

        public void OnCrewmemberHired(ProtoCrewMember pcm, int i)
        {
            Core.Log($"OnCrewmemberHired('{pcm.name}', {i})", LogLevel.Important);
            dirty = crewChanged = true;
        }

        public void OnCrewmemberSacked(ProtoCrewMember pcm, int i)
        {
            Core.Log($"OnCrewmemberSacked('{pcm.name}', {i})", LogLevel.Important);
            Core.KerbalHealthList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalAdded(ProtoCrewMember pcm)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || pcm == null)
                return;
            Core.Log($"OnKerbalAdded('{pcm.name}')", LogLevel.Important);
            if (pcm.type == ProtoCrewMember.KerbalType.Applicant || pcm.type == ProtoCrewMember.KerbalType.Unowned)
            {
                Core.Log($"The kerbal is {pcm.type}. Skipping.", LogLevel.Important);
                return;
            }
            Core.KerbalHealthList.Add(pcm);
            dirty = crewChanged = true;
        }

        public void OnKerbalRemoved(ProtoCrewMember pcm)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || pcm == null)
                return;
            Core.Log($"OnKerbalRemoved('{pcm.name}')", LogLevel.Important);
            Core.KerbalHealthList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalNameChanged(ProtoCrewMember pcm, string name1, string name2)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || pcm == null)
                return;
            Core.Log($"OnKerbalNameChanged('{pcm.name}', '{name1}', '{name2}')", LogLevel.Important);
            Core.KerbalHealthList.Rename(name1, name2);
            dirty = true;
        }

        public void OnKerbalFrozen(Part part, ProtoCrewMember pcm)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || pcm == null)
                return;
            Core.Log($"OnKerbalFrozen('{part.name}', '{pcm.name}')", LogLevel.Important);
            Core.KerbalHealthList[pcm].AddCondition(KerbalHealthStatus.Condition_Frozen);
            dirty = true;
        }

        public void OnKerbalThaw(Part part, ProtoCrewMember pcm)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || pcm == null)
                return;
            Core.Log($"OnKerbalThaw('{part.name}', '{pcm.name}')", LogLevel.Important);
            Core.KerbalHealthList[pcm].RemoveCondition(KerbalHealthStatus.Condition_Frozen);
            dirty = true;
        }

        /// <summary>
        /// Checks if an anomaly has just been discovered and awards quirks to a random discoverer + clearing radiation
        /// </summary>
        public void OnProgressComplete(ProgressNode n)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"OnProgressComplete({n.Id})");
            if (n is KSPAchievements.PointOfInterest poi && FlightGlobals.ActiveVessel.GetCrewCount() > 0)
            {
                Core.Log($"Reached anomaly: {poi.Id} on {poi.body}", LogLevel.Important);
                List<ProtoCrewMember> crew = FlightGlobals.ActiveVessel.GetVesselCrew();
                if (Core.Rand.NextDouble() < KerbalHealthQuirkSettings.Instance.AnomalyQuirkChance)
                {
                    ProtoCrewMember pcm = crew[Core.Rand.Next(crew.Count)];
                    Quirk quirk = Core.KerbalHealthList[pcm].AddRandomQuirk();
                    if (quirk != null)
                        Core.Log($"{pcm.name} was awarded {quirk.Title} quirk for discovering an anomaly.", LogLevel.Important);
                }

                if (Core.Rand.NextDouble() < KerbalHealthRadiationSettings.Instance.AnomalyDecontaminationChance)
                {
                    ProtoCrewMember pcm = crew[Core.Rand.Next(crew.Count)];
                    Core.Log($"Clearing {pcm.name}'s radiation dose of {Core.KerbalHealthList[pcm].Dose:N0} BED.");
                    Core.ShowMessage(Localizer.Format("#KH_MSG_AnomalyDecontamination", pcm.nameWithGender, Core.PrefixFormat(Core.KerbalHealthList[pcm].Dose)), pcm);
                    Core.KerbalHealthList[pcm].Dose = 0;
                }
            }
        }

        public void onVesselWasModified(Vessel v)
        {
            Core.Log($"onVesselWasModified('{v.vesselName}')");
            vesselChanged = true;
        }

        public void FixedUpdate()
        {
            if (KerbalHealthGeneralSettings.Instance.modEnabled && !Core.IsInEditor)
                UpdateKerbals(false);
        }

        /// <summary>
        /// Displays actual values in Health Monitor
        /// </summary>
        public void Update()
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
            {
                if (monitorWindow != null)
                    monitorWindow.Dismiss();
                return;
            }

            if (monitorWindow == null || !dirty)
                return;

            if (gridContent == null)
            {
                Core.Log("KerbalHealthScenario.gridContent is null.", LogLevel.Error);
                monitorWindow.Dismiss();
                return;
            }

#if DEBUG
            updateTimer.Start();
#endif

            // Showing list of all kerbals
            if (selectedKHS == null)
            {
                if (crewChanged)
                {
                    Core.KerbalHealthList.RegisterKerbals();
                    Invalidate();
                    crewChanged = false;
                }

                // Fill the Health Monitor's grid with kerbals' health data
                for (int i = 0; i < LineCount; i++)
                {
                    KerbalHealthStatus khs = kerbals.Values[FirstLine + i];
                    bool healthFrozen = khs.IsFrozen;
                    double change = khs.HPChangeTotal;
                    string formatTag = "", formatUntag = "", s;
                    if (healthFrozen || change == 0 || (khs.BalanceHP - khs.NextConditionHP) * change < 0)
                        if (khs.IsTrainingAtKSC)
                            s = Core.ParseUT(khs.CurrentTrainingETA, false, 10);
                        else s = "—";
                    else
                    {
                        s = Core.ParseUT(khs.ETAToNextCondition, false, 100);
                        if (change < 0)
                        {
                            formatTag = khs.HP <= khs.CriticalHP ? "<color=red>" : "<color=orange>";
                            formatUntag = "</color>";
                        }
                    }
                    gridContent[(i + 1) * colNumMain].SetOptionText(formatTag + khs.FullName + formatUntag);
                    gridContent[(i + 1) * colNumMain + 1].SetOptionText(formatTag + khs.LocationString + formatUntag);
                    gridContent[(i + 1) * colNumMain + 2].SetOptionText(formatTag + khs.ConditionString + formatUntag);
                    gridContent[(i + 1) * colNumMain + 3].SetOptionText($"{formatTag}{100 * khs.Health:F2}% ({khs.HP:F2}){formatUntag}");
                    gridContent[(i + 1) * colNumMain + 4].SetOptionText(formatTag + (healthFrozen || khs.Health >= 1 ? "—" : Core.SignValue(change, "F2")) + formatUntag);
                    gridContent[(i + 1) * colNumMain + 5].SetOptionText(formatTag + s + formatUntag);
                    gridContent[((i + 1) * colNumMain) + 6].SetOptionText($"{formatTag}{Core.PrefixFormat(khs.Dose, 3)}{(khs.Radiation != 0 ? $" ({Localizer.Format("#KH_HM_perDay", Core.PrefixFormat(khs.Radiation, 3, true))})" : "")}{formatUntag}");
                }
            }

            // Showing details for one particular kerbal
            else
            {
                ProtoCrewMember pcm = selectedKHS.ProtoCrewMember;
                if (pcm == null)
                {
                    selectedKHS = null;
                    Invalidate();
                }
                bool healthFrozen = selectedKHS.IsFrozen;
                gridContent[1].SetOptionText($"<color=white>{selectedKHS.Name}</color>");
                gridContent[3].SetOptionText($"<color=white>{pcm.experienceLevel} {pcm.trait}</color>");
                gridContent[5].SetOptionText($"<color=white>{selectedKHS.ConditionString}</color>");

                string s = "";
                foreach (Quirk q in selectedKHS.Quirks.Where(q => q.IsVisible))
                    s += (s.Length != 0 ? ", " : "") + q.Title;
                if (s.Length == 0)
                    s = Localizer.Format("#KH_HM_DNone");//None
                gridContent[7].SetOptionText($"<color=white>{s}</color>");

                gridContent[9].SetOptionText($"<color=white>{selectedKHS.MaxHP:F2}</color>");
                gridContent[11].SetOptionText($"<color=white>{selectedKHS.HP:F2} ({selectedKHS.Health:P2})</color>");
                gridContent[13].SetOptionText($"<color=white>{(healthFrozen ? "—" : selectedKHS.HPChangeTotal.ToString("F2"))}</color>");

                int i = 15;
                foreach (HealthFactor f in Core.Factors)
                {
                    gridContent[i].SetOptionText($"<color=white>{selectedKHS.GetFactorHPChange(f):N2}</color>");
                    i += 2;
                }
                gridContent[i].children[0].SetOptionText($"<color=white>{(selectedKHS.TrainingVessel != null ? $"{selectedKHS.GetTrainingLevel():P0}{(selectedKHS.IsTrainingAtKSC ? $"/{Core.TrainingCap:P0}" : "")}" : Localizer.Format("#KH_NA"))}</color>");
                gridContent[i + 2].SetOptionText($"<color=white>{(healthFrozen ? Localizer.Format("#KH_NA") : $"{selectedKHS.Recuperation:F1}%{(selectedKHS.Decay != 0 ? $"/ {-selectedKHS.Decay:F1}%" : "")} ({selectedKHS.HPChangeMarginal:F2} HP)")}</color>");
                gridContent[i + 4].SetOptionText($"<color=white>{selectedKHS.Exposure:P1} / {selectedKHS.ShelterExposure:P1}</color>");
                gridContent[i + 6].SetOptionText($"<color=white>{selectedKHS.Radiation:N0}/day</color>");
                gridContent[i + 8].children[0].SetOptionText($"<color=white>{Core.PrefixFormat(selectedKHS.Dose, 6)}</color>");
                gridContent[i + 10].SetOptionText($"<color=white>{1 - selectedKHS.RadiationMaxHPModifier:P2}</color>");
            }
            dirty = false;

#if DEBUG
            updateTimer.Stop();
#endif
        }

        /// <summary>
        /// Shows Health monitor when the AppLauncher/Blizzy's Toolbar button is clicked
        /// </summary>
        public void ShowWindow()
        {
            Core.Log("KerbalHealthScenario.DisplayData", LogLevel.Important);
            UpdateKerbals(true);
            if (selectedKHS == null)
            {
                Core.Log("No kerbal selected, showing overall list.");

                // Preparing a sorted list of kerbals
                kerbals = new SortedList<ProtoCrewMember, KerbalHealthStatus>(kerbalComparer);
                foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
                    kerbals.Add(khs.ProtoCrewMember, khs);

                DialogGUILayoutBase layout = new DialogGUIVerticalLayout(true, true);
                if (page > PageCount)
                    page = PageCount;
                if (ShowPages)
                    layout.AddChild(new DialogGUIHorizontalLayout(
                        true,
                        false,
                        new DialogGUIButton("<<", FirstPage, () => page > 1, true),
                        new DialogGUIButton("<", PageUp, () => page > 1, false),
                        new DialogGUIHorizontalLayout(TextAnchor.LowerCenter, new DialogGUILabel(Localizer.Format("#KH_HM_Page", page, PageCount))),
                        new DialogGUIButton(">", PageDown, () => page < PageCount, false),
                        new DialogGUIButton(">>", LastPage, () => page < PageCount, true)));
                gridContent = new List<DialogGUIBase>((Core.KerbalHealthList.Count + 1) * colNumMain)
                {
                    // Creating column titles
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_HM_Name")}</color></b>", true),//Name
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_HM_Location")}</color></b>", true),//Location
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_HM_Condition")}</color></b>", true),//Condition
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_HM_Health")}</color></b>", true),//Health
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_HM_Changeperday")}</color></b>", true),//Change/day
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_HM_TimeLeft")}</color></b>", true),//Time Left
                    new DialogGUILabel($"<b><color=white>{Localizer.Format("#KH_HM_Radiation")}</color></b>", true),//Radiation
                    new DialogGUILabel("", true)
                };

                // Initializing Health Monitor's grid with empty labels, to be filled in Update()
                for (int i = 0; i < LineCount; i++)
                {
                    for (int j = 0; j < colNumMain - 1; j++)
                        gridContent.Add(new DialogGUILabel("", true));
                    gridContent.Add(new DialogGUIButton<int>(Localizer.Format("#KH_HM_Details"), n =>
                    {
                        selectedKHS = kerbals.Values[FirstLine + n];
                        Invalidate();
                    }, i));
                }

                layout.AddChild(new DialogGUIGridLayout(
                    new RectOffset(0, 0, 0, 0),
                    new Vector2(colWidth, 30),
                    new Vector2(colSpacing, 10),
                    UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                    UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                    TextAnchor.MiddleCenter,
                    UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                    colNumMain,
                    gridContent.ToArray()));
                windowPosition.width = gridWidthList + 10;
                monitorWindow = PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("Health Monitor", "", Localizer.Format("#KH_HM_windowtitle"), HighLogic.UISkin, windowPosition, layout), //"Health Monitor"
                    false,
                    HighLogic.UISkin,
                    false);
            }

            else
            {
                // Creating the grid for detailed view, which will be filled in Update method
                Core.Log($"Showing details for {selectedKHS.Name}.");
                gridContent = new List<DialogGUIBase>();
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DName")));//"Name:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DLevel")));//"Level:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DCondition")));//"Condition:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DQuirks")));//"Quirks:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DMaxHP")));//"Max HP:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DHp")));//"HP:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DHPChange")));//"HP Change:"
                gridContent.Add(new DialogGUILabel(""));
                foreach (HealthFactor f in Core.Factors)
                {
                    gridContent.Add(new DialogGUILabel($"{f.Title}:"));
                    gridContent.Add(new DialogGUILabel(""));
                }
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DTraining")));
                gridContent.Add(new DialogGUIHorizontalLayout(
                    new DialogGUILabel(""),
                    new DialogGUIButton("?", OnTrainingInfo, 20, 20, false)));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DRecuperation")));//"Recuperation:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DExposure")));//"Exposure:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DRadiation")));//"Radiation:"
                gridContent.Add(new DialogGUILabel(""));
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DLifetimeDose")));//"Lifetime Dose:"
                gridContent.Add(new DialogGUIHorizontalLayout(
                    new DialogGUILabel(""),
                    new DialogGUIButton(Localizer.Format("#KH_HM_DDecon"), OnDecontamination, 50, 20, false)));//"Decon"
                gridContent.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DRadHPLoss")));//"Rad HP Loss:"
                gridContent.Add(new DialogGUILabel(""));
                windowPosition.width = gridWidthDetails + 8;
                monitorWindow = PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog(
                        "Health Monitor",
                        "",
                        Localizer.Format("#KH_HM_Dwindowtitle"),
                        HighLogic.UISkin,
                        windowPosition,
                        new DialogGUIVerticalLayout(
                            new DialogGUIGridLayout(
                                new RectOffset(3, 3, 3, 3),
                                new Vector2(colWidth, 40),
                                new Vector2(colSpacing, 10),
                                UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                                UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                                TextAnchor.MiddleCenter,
                                UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                                colNumDetails,
                                gridContent.ToArray()),
                            new DialogGUIButton(
                                Localizer.Format("#KH_HM_backbtn"),
                                () =>
                                {
                                    selectedKHS = null;
                                    Invalidate();
                                },
                                gridWidthDetails,
                                20,
                                false))),
                    false, HighLogic.UISkin,
                    false);
            }
            dirty = true;
        }

        /// <summary>
        /// Hides the Health Monitor window
        /// </summary>
        public void HideWindow()
        {
            if (monitorWindow != null)
            {
                windowPosition.position = new Vector2(monitorWindow.RTrf.anchoredPosition.x / Screen.width + 0.5f, monitorWindow.RTrf.anchoredPosition.y / Screen.height + 0.5f);
                monitorWindow.Dismiss();
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log("KerbalHealthScenario.OnSave", LogLevel.Important);

#if DEBUG
            saveTimer.Start();
#endif

            if (!Core.IsInEditor)
                UpdateKerbals(true);
            node.AddValue("version", version.ToString());
            ConfigNode n2;
            foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
            {
                khs.Save(n2 = new ConfigNode(KerbalHealthStatus.ConfigNodeName));
                node.AddNode(n2);
            }
            foreach (RadStorm rs in radStorms.Where(rs => rs.Target != RadStormTargetType.None))
            {
                rs.Save(n2 = new ConfigNode(RadStorm.ConfigNodeName));
                node.AddNode(n2);
            }

#if DEBUG
            saveTimer.Stop();
#endif
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;

            Core.Log("KerbalHealthScenario.OnLoad", LogLevel.Important);

#if DEBUG
            loadTimer.Start();
#endif

            // If loading scenario for the first time, try to load settings from config
            if (!Core.ConfigLoaded)
                Core.LoadConfig();

            if (!node.HasValue("nextEventTime") && LoadSettingsFromConfig())
                ScreenMessages.PostScreenMessage(Localizer.Format("#KH_MSG_CustomSettingsLoaded"), 5);

            version = new Version(node.GetString("version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));

            Core.KerbalHealthList.Clear();
            foreach (ConfigNode n in node.GetNodes(KerbalHealthStatus.ConfigNodeName))
                Core.KerbalHealthList.Add(new KerbalHealthStatus(n));
            Core.Log($"{Core.KerbalHealthList.Count} kerbals loaded.", LogLevel.Important);

            radStorms = new List<RadStorm>(node.GetNodes(RadStorm.ConfigNodeName).Select(n => new RadStorm(n)));
            Core.Log($"{radStorms.Count} radstorms loaded.", LogLevel.Important);

            lastUpdated = Planetarium.GetUniversalTime();

#if DEBUG
            loadTimer.Stop();
#endif
        }

        void CheckEVA(Vessel v)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || v == null)
                return;
            Core.Log($"CheckEVA('{v.vesselName}')");
            if (v.isEVA)
                Core.Log($"{v.vesselName} is EVA.");
            foreach (KerbalHealthStatus khs in v.GetVesselCrew().Select(pcm => Core.KerbalHealthList[pcm]).Where(khs => khs != null))
                khs.IsOnEVA = v.isEVA;
        }

        void RegisterAppLauncherButton()
        {
            Core.Log("Registering AppLauncher button...");
            Texture2D icon = new Texture2D(38, 38);
            icon.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
            appLauncherButton = ApplicationLauncher.Instance.AddModApplication(OnAppLauncherClicked, OnAppLauncherClicked, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
        }

        void UnregisterAppLauncherButton()
        {
            if (appLauncherButton != null && ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
        }

        bool LoadSettingsFromConfig()
        {
            Core.Log("LoadSettingsFromConfig", LogLevel.Important);
            ConfigNode settingsNode = GameDatabase.Instance.GetMergedConfigNodes("KERBALHEALTH_CONFIG")?.GetNode("SETTINGS");
            if (settingsNode == null)
            {
                Core.Log("KERBALHEALTH_CONFIG/SETTINGS node not found.", LogLevel.Important);
                return false;
            }
            Core.Log($"KERBALHEALTH_CONFIG node: {settingsNode}");

            KerbalHealthGeneralSettings.Instance.ApplyConfig(settingsNode);
            KerbalHealthFactorsSettings.Instance.ApplyConfig(settingsNode);
            KerbalHealthQuirkSettings.Instance.ApplyConfig(settingsNode);
            KerbalHealthRadiationSettings.Instance.ApplyConfig(settingsNode);

            Core.Log($"Current difficulty preset is {HighLogic.CurrentGame.Parameters.preset}.", LogLevel.Important);
            if (HighLogic.CurrentGame.Parameters.preset != GameParameters.Preset.Custom && settingsNode.HasNode(HighLogic.CurrentGame.Parameters.preset.ToString()))
            {
                settingsNode = settingsNode.GetNode(HighLogic.CurrentGame.Parameters.preset.ToString());
                KerbalHealthGeneralSettings.Instance.ApplyConfig(settingsNode);
                KerbalHealthFactorsSettings.Instance.ApplyConfig(settingsNode);
                KerbalHealthQuirkSettings.Instance.ApplyConfig(settingsNode);
                KerbalHealthRadiationSettings.Instance.ApplyConfig(settingsNode);
            }

            return true;
        }

        private void SetupDeepFreeze()
        {
            if (!DFWrapper.InstanceExists)
            {
                Core.Log("Initializing DFWrapper...", LogLevel.Important);
                DFWrapper.InitDFWrapper();
                if (DFWrapper.InstanceExists)
                    Core.Log("DFWrapper initialized.", LogLevel.Important);
                else Core.Log("DeepFreeze not found.");
            }

            if (DFWrapper.InstanceExists)
            {
                EventData<Part, ProtoCrewMember> dfEvent;
                dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalFrozen");
                if (dfEvent != null)
                    dfEvent.Add(OnKerbalFrozen);
                else Core.Log("Could not find onKerbalFrozen event!", LogLevel.Error);
                dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalThaw");
                if (dfEvent != null)
                    dfEvent.Add(OnKerbalThaw);
                else Core.Log("Could not find onKerbalThaw event!", LogLevel.Error);
            }
        }

        void SetupKerbalism()
        {
            if (KerbalHealthGeneralSettings.Instance.modEnabled && KerbalHealthGeneralSettings.Instance.KerbalismIntegration && Kerbalism.Found && !Kerbalism.IsSetup)
            {
                Core.Log("Initializing Kerbalism integration...", LogLevel.Important);
                Kerbalism.SetRuleProperty("radiation", "degeneration", KerbalHealthRadiationSettings.Instance.RadiationEnabled ? 0 : 1);
                Kerbalism.SetRuleProperty("stress", "degeneration", 0);
                Kerbalism.FeatureComfort = false;
                Kerbalism.FeatureLivingSpace = false;
                Core.Log($"Kerbalism radiation degeneration: {Kerbalism.GetRuleProperty("radiation", "degeneration")}");
                Core.Log($"Kerbalism stress degeneration: {Kerbalism.GetRuleProperty("stress", "degeneration")}");
                Core.Log($"Kerbalism Comfort feature is {(Kerbalism.FeatureComfort ? "enabled" : "disabled")}.");
                Core.Log($"Kerbalism Living Space feature is {(Kerbalism.FeatureLivingSpace ? "enabled" : "disabled")}.");
                Kerbalism.IsSetup = true;
            }
        }

        bool VesselNeedsCheckForUntrainedCrew(Vessel v) =>
            KerbalHealthFactorsSettings.Instance.TrainingEnabled
            && HighLogic.LoadedSceneIsFlight
            && v.situation == Vessel.Situations.PRELAUNCH;

        /// <summary>
        /// Checks the given vessel and displays an alert if any of the crew isn't fully trained
        /// </summary>
        void CheckUntrainedCrewWarning(Vessel v)
        {
            if (v == null)
                return;
            Core.Log($"CheckUntrainedCrewWarning('{v.vesselName}')");
            if (!VesselNeedsCheckForUntrainedCrew(v))
            {
                Core.Log("Disabling untrained crew warning.");
                checkUntrainedKerbals = false;
                untrainedKerbalsWarningMessage.duration = 0;
                return;
            }

            string msg = "";
            int n = 0;
            foreach (ProtoCrewMember pcm in v.GetVesselCrew())
            {
                KerbalHealthStatus khs = Core.KerbalHealthList[pcm];
                if (khs == null)
                {
                    Core.Log($"KerbalHealthStatus for {pcm.name} in {v.vesselName} not found!", LogLevel.Error);
                    continue;
                }
                Core.Log($"{pcm.name} is trained {khs.GetTrainingLevel():P1} / {Core.TrainingCap:P1}.");
                if (khs.GetTrainingLevel() < Core.TrainingCap)
                {
                    msg += (msg.Length == 0 ? "" : ", ") + pcm.name;
                    n++;
                }
            }
            Core.Log($"{n} kerbals are untrained: {msg}");
            if (n == 0)
                return;
            untrainedKerbalsWarningMessage = new ScreenMessage(
                Localizer.Format(n == 1 ? "#KH_TrainingAlert1" : "#KH_TrainingAlertMany", msg),
                KerbalHealthGeneralSettings.Instance.UpdateInterval,
                ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage(untrainedKerbalsWarningMessage);
        }

        void TrainVessel(Vessel v)
        {
            if (v == null)
                return;
            Core.Log($"KerbalHealthScenario.TrainVessel('{v.vesselName}')");
            foreach (ProtoCrewMember pcm in v.GetVesselCrew())
            {
                KerbalHealthStatus khs = Core.KerbalHealthList[pcm];
                if (khs == null)
                    Core.KerbalHealthList.Add(khs = new KerbalHealthStatus(pcm));
                khs.StartTraining(v.Parts, khs.IsOnEVA ? Localizer.Format("#KH_Spacesuit") : v.vesselName);
            }
        }

        void SpawnRadStorms(double interval)
        {
            Core.Log($"SpawnRadStorms({interval:F2})");
            Dictionary<int, RadStorm> targets = new Dictionary<int, RadStorm>
            { { Planetarium.fetch.Home.name.GetHashCode(), new RadStorm(Planetarium.fetch.Home) } };

            foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Kerbals(ProtoCrewMember.RosterStatus.Assigned))
            {
                Vessel v = pcm.GetVessel();
                if (v == null)
                    continue;
                CelestialBody body = v.mainBody;
                Core.Log($"{pcm.name} is in {v.vesselName} in {body.name}'s SOI.");

                int targetKey;
                if (body == Planetarium.fetch.Sun)
                {
                    targetKey = (int)v.persistentId;
                    if (!targets.ContainsKey(targetKey))
                        targets.Add(targetKey, new RadStorm(v));
                }
                else
                {
                    body = body.GetPlanet();
                    targetKey = body.name.GetHashCode();
                    if (!targets.ContainsKey(targetKey))
                        targets.Add(targetKey, new RadStorm(body));
                }
            }
            Core.Log($"{targets.Count} potential radstorm targets found.");
            Core.Log($"Current solar cycle phase: {Core.SolarCyclePhase:P2} through. Radstorm MTBE: {Core.RadStormMTBE:N0} days.");

            foreach (RadStorm t in targets.Values)
                if (Core.EventHappens(Core.RadStormMTBE / KerbalHealthRadiationSettings.Instance.RadStormFrequency * KSPUtil.dateTimeFormatter.Day, interval))
                {
                    RadStormType rst = Core.GetRandomRadStormType();
                    double delay = t.DistanceFromSun / rst.GetRandomVelocity();
                    t.Magnitutde = rst.GetRandomMagnitude();
                    Core.Log($"Radstorm will hit {t.Name} travel distance: {t.DistanceFromSun:F0} m; travel time: {delay:N0} s; magnitude {t.Magnitutde:N0}.");
                    t.Time = Planetarium.GetUniversalTime() + delay;
                    Core.ShowMessage(Localizer.Format("#KH_RadStorm_Alert", rst.Name, t.Name, KSPUtil.PrintDate(t.Time, true)), true);//A radiation storm of <color=yellow>" + rst.Name + "</color> strength is going to hit <color=yellow>" + t.Name + "</color> on <color=yellow>" + KSPUtil.PrintDate(t.Time, true) + "</color>!
                    radStorms.Add(t);
                }
        }

        /// <summary>
        /// The main method for updating all kerbals' health and processing events
        /// </summary>
        /// <param name="forced">Whether to process kerbals regardless of the amount of time passed</param>
        void UpdateKerbals(bool forced)
        {
            double time = Planetarium.GetUniversalTime();
            float interval = (float)(time - lastUpdated);
            if (!forced && (interval < KerbalHealthGeneralSettings.Instance.UpdateInterval || interval < KerbalHealthGeneralSettings.Instance.MinUpdateInterval * TimeWarp.CurrentRate))
                return;

#if DEBUG
            mainloopTimer.Start();
#endif

            Core.Log($"UT is {time}. Updating for {interval} seconds.");
            Core.ClearCache();
            if (HighLogic.LoadedSceneIsFlight && vesselChanged)
            {
                Core.Log("Vessel has changed or just loaded. Ordering kerbals to train for it in-flight, and checking if anyone's on EVA.");
                foreach (Vessel v in FlightGlobals.VesselsLoaded)
                {
                    CheckEVA(v);
                    TrainVessel(v);
                }
                vesselChanged = false;
            }
            if (checkUntrainedKerbals)
                CheckUntrainedCrewWarning(FlightGlobals.ActiveVessel);

            // Processing radiation storms' effects
            if (KerbalHealthRadiationSettings.Instance.RadiationEnabled && KerbalHealthRadiationSettings.Instance.RadStormsEnabled && !KerbalHealthRadiationSettings.Instance.UseKerbalismRadiation)
            {
                for (int i = 0; i < radStorms.Count; i++)
                    if (time >= radStorms[i].Time)
                    {
                        int j = 0;
                        double m = radStorms[i].Magnitutde * KerbalHealthStatus.GetSolarRadiationProportion(radStorms[i].DistanceFromSun) * KerbalHealthRadiationSettings.Instance.RadStormMagnitude;
                        Core.Log($"Radstorm {i} hits {radStorms[i].Name} with magnitude of {m} ({radStorms[i].Magnitutde} before modifiers).", LogLevel.Important);
                        string s = Localizer.Format("#KH_RadStorm_report1", Core.PrefixFormat(m, 5), radStorms[i].Name);
                        foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values.Where(khs => radStorms[i].Affects(khs.ProtoCrewMember)))
                        {
                            double d = m * KerbalHealthStatus.GetCosmicRadiationRate(khs.ProtoCrewMember.GetVessel()) * khs.ShelterExposure;
                            khs.AddDose(d);
                            Core.Log($"The radstorm irradiates {khs.Name} by {d:N0} BED.");
                            s += Localizer.Format("#KH_RadStorm_report2", khs.Name, Core.PrefixFormat(d, 5));
                            j++;
                        }
                        if (j > 0)
                            Core.ShowMessage(s, true);
                        radStorms.RemoveAt(i--);
                    }
                if (Core.GetYear(time) > Core.GetYear(lastUpdated))
                    Core.ShowMessage(Localizer.Format("#KH_RadStorm_AnnualReport", (Core.SolarCyclePhase * 100).ToString("N1"), Math.Floor(time / Core.SolarCycleDuration + 1).ToString("N0"), (Core.RadStormMTBE / KerbalHealthRadiationSettings.Instance.RadStormFrequency).ToString("N0")), false); //You are " +  + " through solar cycle " +  + ". Current mean time between radiation storms is " +  + " days.
            }

            Core.KerbalHealthList.Update(interval);
            lastUpdated = time;

            // Processing events. It can take several turns of event processing at high time warp
            if (KerbalHealthQuirkSettings.Instance.ConditionsEnabled)
            {
                Core.Log("Processing conditions...");
                foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
                {
                    ProtoCrewMember pcm = khs.ProtoCrewMember;
                    if (khs.IsFrozen || !pcm.IsTrackable())
                        continue;
                    for (int i = khs.Conditions.Count - 1; i >= 0; i--)
                    {
                        HealthCondition hc = khs.Conditions[i];
                        Core.Log($"Processing {khs.Name}'s {hc.Name} condition.");
                        for (int j = 0; j < hc.Outcomes.Count; j++)
                        {
                            float mtbe = (float)hc.Outcomes[j].GetMTBE(pcm) / KerbalHealthQuirkSettings.Instance.EventFrequency;
                            Core.Log($"MTBE of outcome #{j}: {mtbe:N1} days.");
                            if (Core.EventHappens(mtbe * KSPUtil.dateTimeFormatter.Day, interval))
                            {
                                Core.Log($"Condition {hc.Name} has outcome: {hc.Outcomes[j]}.");
                                if (hc.Outcomes[j].Condition.Length != 0)
                                    khs.AddCondition(hc.Outcomes[j].Condition);
                                if (hc.Outcomes[j].RemoveOldCondition)
                                {
                                    khs.RemoveCondition(hc);
                                    break;
                                }
                            }
                        }
                    }

                    foreach (HealthCondition hc in Core.HealthConditions.Values.Where(hc =>
                        (hc.Stackable || !khs.HasCondition(hc))
                        && hc.IsCompatibleWith(khs.Conditions)
                        && hc.Logic.Test(pcm)
                        && Core.EventHappens(hc.GetMTBE(pcm) / KerbalHealthQuirkSettings.Instance.EventFrequency * KSPUtil.dateTimeFormatter.Day, interval)))
                    {
                        Core.Log($"{khs.Name} acquires {hc.Name} condition.");
                        khs.AddCondition(hc);
                    }
                }

                if (KerbalHealthRadiationSettings.Instance.RadiationEnabled
                    && KerbalHealthRadiationSettings.Instance.RadStormsEnabled
                    && !KerbalHealthRadiationSettings.Instance.UseKerbalismRadiation)
                    SpawnRadStorms(interval);
            }
            dirty = true;

#if DEBUG
            mainloopTimer.Stop();
#endif
        }

        void FirstPage()
        {
            dirty = page != PageCount;
            page = 1;
            if (!dirty)
                Invalidate();
        }

        void PageUp()
        {
            dirty = page != PageCount;
            if (page > 1)
                page--;
            if (!dirty)
                Invalidate();
        }

        void PageDown()
        {
            if (page < PageCount)
                page++;
            if (page == PageCount)
                Invalidate();
            else dirty = true;
        }

        void LastPage()
        {
            page = PageCount;
            Invalidate();
        }

        void Invalidate()
        {
            HideWindow();
            ShowWindow();
        }

        void OnAppLauncherClicked()
        {
            if (monitorWindow != null)
                HideWindow();
            else ShowWindow();
        }

        void OnTrainingInfo()
        {
            if (selectedKHS == null)
                return;

            Core.Log($"OnTrainingInfo for {selectedKHS.Name}", LogLevel.Important);

            string msg;
            if (selectedKHS.IsTrainingAtKSC)
                msg = Localizer.Format("#KH_TI_TrainingKSC",
                   selectedKHS.Name,
                   selectedKHS.TrainingVessel,
                   selectedKHS.TrainedParts.Count(tp => tp.TrainingNow),
                   selectedKHS.GetTrainingLevel().ToString("P2"),
                   Core.TrainingCap.ToString("P0"),
                   selectedKHS.LastRealTrainingPerDay.ToString("P2"),
                   Core.ParseUT(selectedKHS.CurrentTrainingETA, false, 10));
            else if (selectedKHS.TrainingVessel != null)
                msg = Localizer.Format(
                   "#KH_TI_KerbalTrainingInFlight",
                   selectedKHS.Name,
                   selectedKHS.TrainingVessel,
                   selectedKHS.TrainedParts.Count(tp => tp.TrainingNow),
                   selectedKHS.GetTrainingLevel().ToString("P2"),
                   selectedKHS.LastRealTrainingPerDay.ToString("P2"));
            else msg = Localizer.Format("#KH_TI_KerbalNotTraining", selectedKHS.Name);

            List<DialogGUIBase> elements = new List<DialogGUIBase>();
            if (selectedKHS.TrainingVessel != null || selectedKHS.TrainedParts.Any(tp => tp.Level >= 0.001f))
            {
                elements.Add(new DialogGUILabel(Localizer.Format("#KH_TI_TrainedParts", selectedKHS.Name), true));
                foreach (PartTrainingInfo tp in selectedKHS.TrainedParts.Where(tp => tp.Level >= 0.001f || tp.TrainingNow))
                {
                    string tag = "", untag = "";
                    if (tp.TrainingNow)
                    {
                        tag = "<b>";
                        untag = "</b>";
                    }
                    elements.Add(new DialogGUIHorizontalLayout(300, 10, new DialogGUILabel($"{tag}{tp.Label}{untag}", 250), new DialogGUILabel($"{tag}{tp.Level:P2}{untag}", 50)));
                }
            }
            if (selectedKHS.IsTrainingAtKSC)
                elements.Add(new DialogGUIButton(Localizer.Format("#KH_TI_StopTraining"), () => selectedKHS.StopTraining(null), true));
            elements.Add(new DialogGUIButton(Localizer.Format("#KH_TI_Close"), null, true));

            PopupDialog.SpawnPopupDialog(new MultiOptionDialog("TrainingInfo", msg, Localizer.Format("#KH_TI_Title"), HighLogic.UISkin, elements.ToArray()), false, HighLogic.UISkin);
        }

        void OnDecontamination()
        {
            if (selectedKHS == null)
                return;
            string msg = "<color=white>";
            Func<bool> condition = () => false;
            Callback ok = null;

            if (selectedKHS.IsDecontaminating)
            {
                Core.Log($"User ordered to stop decontamination of {selectedKHS.Name}.", LogLevel.Important);
                condition = () => selectedKHS.IsDecontaminating;
                ok = () =>
                {
                    selectedKHS.StopDecontamination();
                    Invalidate();
                };
                msg = Localizer.Format("#KH_DeconMsg1", selectedKHS.ProtoCrewMember.nameWithGender);
            }
            else
            {
                condition = () => selectedKHS.IsReadyForDecontamination;
                ok = () =>
                {
                    selectedKHS.StartDecontamination();
                    Invalidate();
                };

                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && KerbalHealthRadiationSettings.Instance.RequireUpgradedFacilityForDecontamination)
                    msg += Localizer.Format(
                        "#KH_DeconMsg2",
                        KerbalHealthRadiationSettings.Instance.DecontaminationAstronautComplexLevel,
                        KerbalHealthRadiationSettings.Instance.DecontaminationRNDLevel);

                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                    msg += Localizer.Format(
                        "#KH_DeconMsg3",
                        HighLogic.CurrentGame.Mode == Game.Modes.CAREER ? Localizer.Format("#KH_DeconMsg3_CAREERMode", KerbalHealthRadiationSettings.Instance.DecontaminationFundsCost.ToString("N0")) : "",
                        KerbalHealthRadiationSettings.Instance.DecontaminationScienceCost.ToString("N0"));

                msg += Localizer.Format(
                    "#KH_DeconMsg4",
                    selectedKHS.ProtoCrewMember.nameWithGender,
                    KerbalHealthRadiationSettings.Instance.DecontaminationHealthLoss.ToString("P0"),
                    KerbalHealthRadiationSettings.Instance.DecontaminationRate.ToString("N0"),
                    Core.ParseUT(selectedKHS.Dose / KerbalHealthRadiationSettings.Instance.DecontaminationRate * 21600, false, 2),
                    KerbalHealthRadiationSettings.Instance.DecontaminationMinHealth.ToString("P0"));

                if (!selectedKHS.IsReadyForDecontamination)
                {
                    Core.Log($"{selectedKHS.Name} is {selectedKHS.ProtoCrewMember.rosterStatus}, has {selectedKHS.Health:P2} health and {selectedKHS.Conditions.Count} conditions. Game mode: {HighLogic.CurrentGame.Mode}. Astronaut Complex at level {ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)}, R&D at level {ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment)}.", LogLevel.Important);
                    msg += Localizer.Format("#KH_DeconMsg5", selectedKHS.ProtoCrewMember.nameWithGender);
                }
            }

            PopupDialog.SpawnPopupDialog(new MultiOptionDialog(
                "Decontamination",
                msg,
                Localizer.Format("#KH_DeconWinTitle"),
                HighLogic.UISkin,
                new DialogGUIButton(Localizer.Format("#KH_DeconWinOKbtn"), ok, condition, true),
                new DialogGUIButton(Localizer.Format("#KH_DeconWinCancelbtn"), null, true)),
                false,
                HighLogic.UISkin);
        }
    }
}
