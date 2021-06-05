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
        // Health Monitor dimensions
        const int colNumMain = 8, colNumDetails = 6;

        const int colWidth = 100;
        const int colSpacing = 10;
        const int gridWidthList = colNumMain * (colWidth + colSpacing) - colSpacing;
        const int gridWidthDetails = colNumDetails * (colWidth + colSpacing) - colSpacing;

        // UT at last health update
        static double lastUpdated;

        // UT when (or after) next event check occurs
        static double nextEventTime;

        // Current Kerbal Health version
        Version version;

        // List of scheduled radstorms
        List<RadStorm> radStorms = new List<RadStorm>();

        // Button handles
        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;

        // List of displayed kerbal, sorted according to current settings
        SortedList<ProtoCrewMember, KerbalHealthStatus> kerbals;

        // Change flags
        bool dirty = false, crewChanged = false, vesselChanged = false;

        // Health Monitor window
        PopupDialog monitorWindow;

        // Saved position of the Health Monitor window
        Rect monitorPosition = new Rect(0.5f, 0.5f, gridWidthList, 200);

        // Health Monitor grid's labels
        List<DialogGUIBase> gridContent;

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
            {
                Core.Log($"Current mod version {currentVersion} is different from v{version} used to save the game. Kerbal Health has been recently updated.", LogLevel.Important);
                if (version < new Version("1.1.0") && KerbalHealthFactorsSettings.Instance.ConfinementBaseFactor != -3 && Planetarium.GetUniversalTime() > 0)
                {
                    Core.Log($"Confinement Factor is {KerbalHealthFactorsSettings.Instance.ConfinementBaseFactor} instead of -3. Automatically fixing.");
                    KerbalHealthFactorsSettings.Instance.ConfinementBaseFactor = -3;
                    Core.ShowMessage(Localizer.Format("#KH_Versionmsg110", currentVersion.ToString(3)), true);//"Kerbal Health has been updated to v" +  + ". Confinement factor value has been reset to -3. It is recommended that you load each crewed vessel briefly to update Kerbal Health cache."
                }

                if (version < new Version("1.2.1.2"))
                {
                    Core.Log($"Pre-1.3 radiation settings: {KerbalHealthRadiationSettings.Instance.InSpaceHighCoefficient:P0} / {KerbalHealthRadiationSettings.Instance.InSpaceLowCoefficient:P0} / {KerbalHealthRadiationSettings.Instance.StratoCoefficient:P0} / {KerbalHealthRadiationSettings.Instance.TroposphereCoefficient:P0} / {KerbalHealthRadiationSettings.Instance.GalacticRadiation:F0} / {KerbalHealthRadiationSettings.Instance.SolarRadiation:F0}");
                    KerbalHealthRadiationSettings.Instance.RadiationEffect = 0.1f;
                    KerbalHealthRadiationSettings.Instance.InSpaceLowCoefficient = 0.2f;
                    KerbalHealthRadiationSettings.Instance.StratoCoefficient = 0.2f;
                    KerbalHealthRadiationSettings.Instance.TroposphereCoefficient = 0.01f;
                    Core.ShowMessage(Localizer.Format("#KH_Versionmsg130", currentVersion.ToString()), true);//"Kerbal Health has been updated to v" + + ". Radiation settings have been reset. It is recommended that you load each crewed vessel briefly to update Kerbal Health cache."
                }

                if (version < new Version("1.3.8.1"))
                {
                    Core.Log($"Pre-1.3.9 Stress factor: {KerbalHealthFactorsSettings.Instance.StressFactor}");
                    KerbalHealthFactorsSettings.Instance.StressFactor = -2;
                    KerbalHealthRadiationSettings.Instance.SolarRadiation = 2500;
                    KerbalHealthRadiationSettings.Instance.GalacticRadiation = 12500;
                    KerbalHealthRadiationSettings.Instance.InSpaceHighCoefficient = 0.4f;
                    KerbalHealthFactorsSettings.Instance.TrainingEnabled = false;
                    Core.ShowMessage(Localizer.Format("#KH_Versionmsg139", currentVersion.ToString(3)), true);
                }

                if (version < new Version("1.4.6.101"))
                {
                    Core.Log($"Pre-1.5 Loneliness factor: {KerbalHealthFactorsSettings.Instance.LonelinessFactor}");
                    if (KerbalHealthFactorsSettings.Instance.LonelinessFactor == -1)
                        KerbalHealthFactorsSettings.Instance.LonelinessFactor = -2;
                    KerbalHealthGeneralSettings.Instance.UpdateInterval = 10;
                    Core.ShowMessage(Localizer.Format("#KH_Versionmsg150", currentVersion.ToString(3)), true);
                }

                version = currentVersion;
            }
            else Core.Log($"Kerbal Health v{version}");

            // This needs to be run even if the mod is disabled, so that its settings can be reset:
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);

            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log("KerbalHealthScenario.Start", LogLevel.Important);

            Core.KerbalHealthList.RegisterKerbals();
            vesselChanged = true;

            lastUpdated = Planetarium.GetUniversalTime();
            nextEventTime = lastUpdated + GetNextEventInterval();

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
                toolbarButton.OnClick += e =>
                {
                    if (monitorWindow == null)
                        DisplayData();
                    else UndisplayData();
                };
            }

            if (VesselNeedsCheckForUntrainedCrew(FlightGlobals.ActiveVessel))
                checkUntrainedKerbals = true;
        }

        public void OnDisable()
        {
            Core.Log("KerbalHealthScenario.OnDisable", LogLevel.Important);
            if (Core.IsInEditor)
                return;

            UndisplayData();

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
        /// <param name="action"></param>
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
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
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
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"OnKerbalAdded('{pcm.name}')", LogLevel.Important);
            if (pcm.type == ProtoCrewMember.KerbalType.Applicant || pcm.type == ProtoCrewMember.KerbalType.Unowned)
            {
                Core.Log($"The kerbal is {pcm.type}. Skipping.", LogLevel.Important);
                return;
            }
            Core.KerbalHealthList.Add(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalRemoved(ProtoCrewMember pcm)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"OnKerbalRemoved('{pcm.name}')", LogLevel.Important);
            Core.KerbalHealthList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalNameChanged(ProtoCrewMember pcm, string name1, string name2)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"OnKerbalNameChanged('{pcm.name}', '{name1}', '{name2}')", LogLevel.Important);
            Core.KerbalHealthList.Rename(name1, name2);
            dirty = true;
        }

        public void OnKerbalFrozen(Part part, ProtoCrewMember pcm)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"OnKerbalFrozen('{part.name}', '{pcm.name}')", LogLevel.Important);
            Core.KerbalHealthList[pcm].IsFrozen = true;
            dirty = true;
        }

        public void OnKerbalThaw(Part part, ProtoCrewMember pcm)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"OnKerbalThaw('{part.name}', '{pcm.name}')", LogLevel.Important);
            Core.KerbalHealthList[pcm].IsFrozen = false;
            dirty = true;
        }

        /// <summary>
        /// Checks if an anomaly has just been discovered and awards quirks to a random discoverer + clearing radiation
        /// </summary>
        /// <param name="n"></param>
        public void OnProgressComplete(ProgressNode n)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log($"OnProgressComplete({n.Id})");
            if (n is KSPAchievements.PointOfInterest poi && FlightGlobals.ActiveVessel.GetCrewCount() > 0)
            {
                Core.Log($"Reached anomaly: {poi.Id} on {poi.body}", LogLevel.Important);
                List<ProtoCrewMember> crew = FlightGlobals.ActiveVessel.GetVesselCrew();
                if (Core.rand.NextDouble() < KerbalHealthQuirkSettings.Instance.AnomalyQuirkChance)
                {
                    ProtoCrewMember pcm = crew[Core.rand.Next(crew.Count)];
                    Quirk quirk = Core.KerbalHealthList[pcm].AddRandomQuirk();
                    if (quirk != null)
                        Core.Log($"{pcm.name} was awarded {quirk.Title} quirk for discovering an anomaly.", LogLevel.Important);
                }

                if (Core.rand.NextDouble() < KerbalHealthRadiationSettings.Instance.AnomalyDecontaminationChance)
                {
                    ProtoCrewMember pcm = crew[Core.rand.Next(crew.Count)];
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

            if (selectedKHS == null)  // Showing list of all kerbals
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
                    bool healthFrozen = khs.IsFrozen || khs.IsDecontaminating;
                    double change = khs.HPChangeTotal;
                    string formatTag = "", formatUntag = "", s;
                    if (healthFrozen || change == 0 || (khs.BalanceHP - khs.NextConditionHP) * change < 0)
                        s = "—";
                    else
                    {
                        s = Core.ParseUT(khs.ETAToNextCondition, false, 100);
                        if (change < 0)
                        {
                            formatTag = khs.ETAToNextCondition< KSPUtil.dateTimeFormatter.Day ? "<color=red>" : "<color=orange>";
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

            else  // Showing details for one particular kerbal
            {
                ProtoCrewMember pcm = selectedKHS.PCM;
                if (pcm == null)
                {
                    selectedKHS = null;
                    Invalidate();
                }
                bool healthFrozen = selectedKHS.IsFrozen || selectedKHS.IsDecontaminating;
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
                gridContent[i].children[0].SetOptionText($"<color=white>{(((selectedKHS.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (selectedKHS.TrainingVessel != null)) ? $"{selectedKHS.TrainingLevel * 100:N0}%/{Core.TrainingCap * 100:N0}%" : Localizer.Format("#KH_NA"))}</color>");
                gridContent[i + 2].SetOptionText($"<color=white>{(healthFrozen ? Localizer.Format("#KH_NA") : $"{selectedKHS.Recuperation:F1}%{(selectedKHS.Decay != 0 ? $"/ {-selectedKHS.Decay:F1}%" : "")} ({selectedKHS.HPChangeMarginal:F2} HP)")}</color>");
                gridContent[i + 4].SetOptionText($"<color=white>{selectedKHS.Exposure:P1} / {selectedKHS.ShelterExposure:P1}</color>");
                gridContent[i + 6].SetOptionText($"<color=white>{selectedKHS.Radiation:N0}/day</color>");
                gridContent[i + 8].children[0].SetOptionText($"<color=white>{Core.PrefixFormat(selectedKHS.Dose, 6)}</color>");
                gridContent[i + 10].SetOptionText($"<color=white>{1 - selectedKHS.RadiationMaxHPModifier:P2}</color>");
            }
            dirty = false;
        }

        /// <summary>
        /// Shows Health monitor when the AppLauncher/Blizzy's Toolbar button is clicked
        /// </summary>
        public void DisplayData()
        {
            Core.Log("KerbalHealthScenario.DisplayData", LogLevel.Important);
            UpdateKerbals(true);
            if (selectedKHS == null)
            {
                Core.Log("No kerbal selected, showing overall list.");

                // Preparing a sorted list of kerbals
                kerbals = new SortedList<ProtoCrewMember, KerbalHealthStatus>(kerbalComparer);
                foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
                    kerbals.Add(khs.PCM, khs);

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
                monitorPosition.width = gridWidthList + 10;
                monitorWindow = PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("Health Monitor", "", Localizer.Format("#KH_HM_windowtitle"), HighLogic.UISkin, monitorPosition, layout), //"Health Monitor"
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
                monitorPosition.width = gridWidthDetails + 8;
                monitorWindow = PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog(
                        "Health Monitor",
                        "",
                        Localizer.Format("#KH_HM_Dwindowtitle"),
                        HighLogic.UISkin,
                        monitorPosition,
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
                    false);//"Health Details""Back"
            }
            dirty = true;
        }

        /// <summary>
        /// Hides the Health Monitor window
        /// </summary>
        public void UndisplayData()
        {
            if (monitorWindow != null)
            {
                Vector3 v = monitorWindow.RTrf.position;
                monitorPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, gridWidthList + 20, 50);
                monitorWindow.Dismiss();
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log("KerbalHealthScenario.OnSave", LogLevel.Important);
            if (!Core.IsInEditor)
                UpdateKerbals(true);
            node.AddValue("version", version.ToString());
            node.AddValue("nextEventTime", nextEventTime);
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
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!Core.ConfigLoaded)
                Core.LoadConfig();

            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;

            Core.Log("KerbalHealthScenario.OnLoad", LogLevel.Important);

            // If loading scenario for the first time, try to load settings from config
            if (!node.HasValue("nextEventTime") && LoadSettingsFromConfig())
                ScreenMessages.PostScreenMessage(Localizer.Format("#KH_MSG_CustomSettingsLoaded"), 5);

            version = new Version(node.GetString("version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            nextEventTime = node.GetDouble("nextEventTime", Planetarium.GetUniversalTime() + GetNextEventInterval());

            Core.KerbalHealthList.Clear();
            foreach (ConfigNode n in node.GetNodes(KerbalHealthStatus.ConfigNodeName))
                Core.KerbalHealthList.Add(new KerbalHealthStatus(n));
            Core.Log($"{Core.KerbalHealthList.Count} kerbals loaded.", LogLevel.Important);

            radStorms = new List<RadStorm>(node.GetNodes(RadStorm.ConfigNodeName).Select(n => new RadStorm(n)));
            Core.Log($"{radStorms.Count} radstorms loaded.", LogLevel.Important);

            lastUpdated = Planetarium.GetUniversalTime();
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
            appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
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
                else Core.Log("DeepFreeze not found.", LogLevel.Important);
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
                Core.Log("Disabling some Kerbalism features for better integration with Kerbal Health.", LogLevel.Important);
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
        /// <param name="v"></param>
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
                Core.Log($"{pcm.name} is trained {khs.TrainingLevel:P1} / {Core.TrainingCap:P1}.");
                if (khs.TrainingLevel < Core.TrainingCap)
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
                    Core.KerbalHealthList.Add(khs = new KerbalHealthStatus(pcm.name));
                khs.StartTraining(v.Parts, v.vesselName);
            }
        }

        /// <summary>
        /// Next event update is scheduled after a random period of time, between 0 and 2 days
        /// </summary>
        /// <returns></returns>
        double GetNextEventInterval() => Core.rand.NextDouble() * KSPUtil.dateTimeFormatter.Day * 2;

        void SpawnRadStorms()
        {
            Core.Log("ProcessRadStorms");
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
            Core.Log($"Current solar cycle phase: {Core.SolarCyclePhase:P2} through. Radstorm chance: {Core.RadStormChance:P2}.");

            foreach (RadStorm t in targets.Values)
                if (Core.rand.NextDouble() < Core.RadStormChance * KerbalHealthRadiationSettings.Instance.RadStormFrequency)
                {
                    RadStormType rst = Core.GetRandomRadStormType();
                    double delay = t.DistanceFromSun / rst.GetVelocity();
                    t.Magnitutde = rst.GetMagnitude();
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
            double timePassed = time - lastUpdated;
            if (!forced && (timePassed < KerbalHealthGeneralSettings.Instance.UpdateInterval || timePassed < KerbalHealthGeneralSettings.Instance.MinUpdateInterval * TimeWarp.CurrentRate))
                return;
            Core.Log($"UT is {time}. Updating for {timePassed} seconds.");
            Core.ClearCache();
            if (HighLogic.LoadedSceneIsFlight && vesselChanged)
            {
                Core.Log("Vessel has changed or just loaded. Ordering kerbals to train for it in-flight, and checking if anyone's on EVA.");
                foreach (Vessel v in FlightGlobals.VesselsLoaded)
                {
                    TrainVessel(v);
                    CheckEVA(v);
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
                        foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values.Where(khs => radStorms[i].Affects(khs.PCM)))
                        {
                            double d = m * KerbalHealthStatus.GetCosmicRadiationRate(khs.PCM.GetVessel()) * khs.ShelterExposure;
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
                    Core.ShowMessage(Localizer.Format("#KH_RadStorm_AnnualReport", (Core.SolarCyclePhase * 100).ToString("N1"), Math.Floor(time / Core.SolarCycleDuration + 1).ToString("N0"), (1 / Core.RadStormChance / KerbalHealthRadiationSettings.Instance.RadStormFrequency).ToString("N0")), false); //You are " +  + " through solar cycle " +  + ". Current mean time between radiation storms is " +  + " days.
            }

            Core.KerbalHealthList.Update(timePassed);
            lastUpdated = time;

            // Processing events. It can take several turns of event processing at high time warp
            while (time >= nextEventTime)
            {
                if (KerbalHealthQuirkSettings.Instance.ConditionsEnabled)
                {
                    Core.Log("Processing conditions...");
                    foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
                    {
                        ProtoCrewMember pcm = khs.PCM;
                        if (khs.IsFrozen || khs.IsDecontaminating || !pcm.IsTrackable())
                            continue;
                        for (int i = 0; i < khs.Conditions.Count; i++)
                        {
                            HealthCondition hc = khs.Conditions[i];
                            foreach (Outcome o in hc.Outcomes)
                                if (Core.rand.NextDouble() < o.GetChancePerDay(pcm) * KerbalHealthQuirkSettings.Instance.ConditionsChance)
                                {
                                    Core.Log($"Condition {hc.Name} has outcome: {o}");
                                    if (o.Condition.Length != 0)
                                        khs.AddCondition(o.Condition);
                                    if (o.RemoveOldCondition)
                                    {
                                        khs.RemoveCondition(hc);
                                        i--;
                                        break;
                                    }
                                }
                        }

                        foreach (HealthCondition hc in Core.HealthConditions.Values.Where(hc =>
                            hc.ChancePerDay > 0
                            && (hc.Stackable || !khs.HasCondition(hc))
                            && hc.IsCompatibleWith(khs.Conditions)
                            && hc.Logic.Test(pcm)
                            && Core.rand.NextDouble() < hc.GetChancePerDay(pcm) * KerbalHealthQuirkSettings.Instance.ConditionsChance))
                        {
                            Core.Log($"{khs.Name} acquires {hc.Name} condition.");
                            khs.AddCondition(hc);
                        }
                    }
                }

                if (KerbalHealthRadiationSettings.Instance.RadiationEnabled
                    && KerbalHealthRadiationSettings.Instance.RadStormsEnabled
                    && !KerbalHealthRadiationSettings.Instance.UseKerbalismRadiation)
                    SpawnRadStorms();

                nextEventTime += GetNextEventInterval();
                Core.Log($"Next event processing is scheduled at {KSPUtil.PrintDateCompact(nextEventTime, true)}.", LogLevel.Important);
            }
            dirty = true;
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
            UndisplayData();
            DisplayData();
        }

        void OnTrainingInfo()
        {
            if (selectedKHS == null)
                return;
            string msg = selectedKHS.TrainingVessel != null
               ? Localizer.Format(
                   "#KH_TI_KerbalTraining",
                   selectedKHS.Name,
                   selectedKHS.TrainingVessel,
                   selectedKHS.TrainingFor.Count,
                   (selectedKHS.TrainingLevel * 100).ToString("N1"),
                   (Core.TrainingCap * 100).ToString("N0"),
                   Core.ParseUT(selectedKHS.TrainingETA, false, 10))
               : Localizer.Format("#KH_TI_KerbalNotTraining", selectedKHS.Name);

            if (selectedKHS.TrainedVessels.Any())
            {
                msg += Localizer.Format("#KH_TI_TrainedVessels", selectedKHS.Name);
                foreach (KeyValuePair<string, double> kvp in selectedKHS.TrainedVessels)
                    msg += Localizer.Format("#KH_TI_TrainedVessel", kvp.Key, (kvp.Value * 100).ToString("N1"));
            }

            if (selectedKHS.FamiliarPartTypes.Any())
            {
                msg += Localizer.Format("#KH_TI_FamiliarParts", selectedKHS.Name);
                foreach (string s in selectedKHS.FamiliarPartTypes)
                    msg += $"\r\n- <color=white>{PartLoader.getPartInfoByName(s)?.title ?? s}</color>";
            }

            PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(
                    "Training Info",
                    msg,
                    Localizer.Format("#KH_TI_Title"),
                    HighLogic.UISkin,
                    new DialogGUIButton(Localizer.Format("#KH_TI_Close"), null, true)),
                false,
                HighLogic.UISkin);
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
                Core.Log($"User ordered to stop decontamination of {selectedKHS.Name}.");

                condition = () => selectedKHS.IsDecontaminating;
                ok = () =>
                {
                    selectedKHS.StopDecontamination();
                    Invalidate();
                };

                msg = Localizer.Format("#KH_DeconMsg1", selectedKHS.PCM.nameWithGender);
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
                    selectedKHS.PCM.nameWithGender,
                    (KerbalHealthRadiationSettings.Instance.DecontaminationHealthLoss * 100).ToString("N0"),
                    KerbalHealthRadiationSettings.Instance.DecontaminationRate.ToString("N0"),
                    Core.ParseUT(selectedKHS.Dose / KerbalHealthRadiationSettings.Instance.DecontaminationRate * 21600, false, 2));

                if (!selectedKHS.IsReadyForDecontamination)
                {
                    Core.Log($"{selectedKHS.Name} is {selectedKHS.PCM.rosterStatus}, has {selectedKHS.Health:P2} health and {selectedKHS.Conditions.Count} conditions. Game mode: {HighLogic.CurrentGame.Mode}. Astronaut Complex at level {ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)}, R&D at level {ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment)}.", LogLevel.Important);
                    msg += Localizer.Format("#KH_DeconMsg5", selectedKHS.PCM.nameWithGender);
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
