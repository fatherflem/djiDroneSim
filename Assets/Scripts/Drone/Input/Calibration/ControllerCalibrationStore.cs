using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DroneSim.Drone.Input.Calibration
{
    /// <summary>
    /// Saves one calibration per controller model on this machine, as JSON under
    /// persistentDataPath/ControllerCalibration. Classroom PCs each keep their own.
    /// </summary>
    public static class ControllerCalibrationStore
    {
        public const string FolderName = "ControllerCalibration";

        public static string Directory => Path.Combine(Application.persistentDataPath, FolderName);

        public static string PathFor(string deviceName)
        {
            return Path.Combine(Directory, SanitizeFileName(deviceName) + ".json");
        }

        public static ControllerCalibrationProfile Load(string deviceName)
        {
            string path = PathFor(deviceName);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                ControllerCalibrationProfile profile = JsonUtility.FromJson<ControllerCalibrationProfile>(File.ReadAllText(path));
                if (profile == null || !profile.IsUsable)
                {
                    Debug.LogWarning($"Ignoring unusable controller calibration at {path}.");
                    return null;
                }

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not read controller calibration at {path}: {ex.Message}");
                return null;
            }
        }

        public static bool Save(ControllerCalibrationProfile profile)
        {
            if (profile == null)
            {
                return false;
            }

            string path = PathFor(profile.deviceName);
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllText(path, JsonUtility.ToJson(profile, true));
                Debug.Log($"Saved controller calibration for '{profile.deviceName}' to {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Could not save controller calibration to {path}: {ex.Message}");
                return false;
            }
        }

        public static bool Delete(string deviceName)
        {
            string path = PathFor(deviceName);
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }

        public static string SanitizeFileName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return "unknown_controller";
            }

            var builder = new StringBuilder(deviceName.Length);
            foreach (char c in deviceName.Trim())
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }

            return builder.ToString();
        }
    }
}
