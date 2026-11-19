using UnityEngine;

namespace CNC.DigitalTwin
{
    public enum AppMode
    {
        Dashboard = 0,
        TeachingTour = 1
    }

    public enum CameraViewMode
    {
        BirdEyeOrbit = 0,
        FirstPersonWalk = 1,
        StationFocus = 2,
        AutoTour = 3
    }

    public enum WorkshopHall
    {
        SpinningHall = 0,
        InspectionWarehouse = 1
    }

    /// <summary>
    /// Cross-scene launch parameters passed from Play.unity to SampleScene.unity.
    /// Defaults to FirstPersonWalk so user can immediately walk with WASD upon entry.
    /// </summary>
    public static class AppLaunchContext
    {
        public static AppMode InitialMode = AppMode.Dashboard;
        public static CameraViewMode InitialCameraMode = CameraViewMode.FirstPersonWalk;
        public static int InitialStationIndex = -1;
        public static float MouseSensitivity = 2.0f;
    }
}
