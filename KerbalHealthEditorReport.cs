using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalHealth
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class KerbalHealthEditorReport : MonoBehaviour
    {
        ApplicationLauncherButton button;
        PopupDialog reportWindow;  // Health Report window
        DialogGUIGridLayout reportGrid;  // Health Report grid
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Health Report grid's labels
        int colNum = 3;  // # of columns in Health Report

        public void Start()
        {
            Log.Post("KerbalHealthEditorReport.Start");
            Texture2D icon = new Texture2D(38, 38);
            icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
            button = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            Log.Post("KerbalHealthEditorReport.Start finished.");
        }

        public void DisplayData()
        {
            Log.Post("KerbalHealthEditorReport.DisplayData");
            if ((ShipConstruction.ShipManifest == null) || (!ShipConstruction.ShipManifest.HasAnyCrew()))
            {
                Log.Post("Ship is empty. Let's get outta here!", Log.LogLevel.Warning);
                return;
            }
            gridContents = new System.Collections.Generic.List<DialogGUIBase>((KerbalHealthScenario.KerbalHealthList.Count + 1) * colNum);
            // Creating column titles
            gridContents.Add(new DialogGUILabel("Name", true));
            gridContents.Add(new DialogGUILabel("HP/day", true));
            gridContents.Add(new DialogGUILabel("Time Left", true));
            // Initializing Health Report's grid with empty labels, to be filled in Update()
            for (int i = 0; i < ShipConstruction.ShipManifest.CrewCount * colNum; i++)
                gridContents.Add(new DialogGUILabel("", true));
            reportGrid = new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(80, 30), new Vector2(20, 0), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNum, gridContents.ToArray());
            Update();
            reportWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("", "Health Report", HighLogic.UISkin, 300, reportGrid), false, HighLogic.UISkin, false);
        }

        public void UndisplayData()
        {
            if (reportWindow != null) reportWindow.Dismiss();
        }

        public void Update()
        {
            if (reportWindow != null)
            {
                if (reportGrid == null)
                {
                    Log.Post("reportGrid is null.", Log.LogLevel.Error);
                    return;
                }
                if (gridContents == null)
                {
                    Log.Post("gridContents is null.", Log.LogLevel.Error);
                    return;
                }
                if (gridContents.Count != (ShipConstruction.ShipManifest.CrewCount + 1) * colNum)  // # of tracked kerbals has changed => close & reopen the window
                {
                    Log.Post("Kerbals' number has changed. Recreating the Health Report window.");
                    UndisplayData();
                    DisplayData();
                }
                 // Fill the Health Report's grid with kerbals' health data
               int i = 0;
                foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetAllCrew(false))  
                {
                    if (pcm == null) continue;
                    gridContents[(i + 1) * colNum].SetOptionText(pcm.name);
                    gridContents[(i + 1) * colNum + 1].SetOptionText(KerbalHealthStatus.HealthChangePerDay(pcm, true).ToString("F0"));
                    gridContents[(i + 1) * colNum + 2].SetOptionText(KSPUtil.PrintDateDeltaCompact(KerbalHealthScenario.KerbalHealthList.Find(pcm).TimeToNextCondition(true), true, false));
                    i++;
                }
            }
        }

        public void OnDisable()
        {
            Log.Post("KerbalHealthEditorReport.OnDisable");
            UndisplayData();
            if (ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(button);
            Log.Post("KerbalHealthEditorReport.OnDisable finished.");
        }
    }
}
