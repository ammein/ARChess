using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ARChess.Scripts.Editor
{
    public static class PostBuildProcessor
    {
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            // 1. Info.plist Handling
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            
            // --- NEW: Added Local Network Privacy Permission ---
            string lanMessage = "This game requires local network access to find and connect to other players on your Wi-Fi.";
            plist.root.SetString("NSLocalNetworkUsageDescription", lanMessage);
            // ---------------------------------------------------

            plist.WriteToFile(plistPath);

            // 2. PBXProject Handling
            string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            string[] allTargets = { mainTarget, frameworkTarget };

            // Check if we are building for the simulator
            bool isSimulator = UnityEditor.PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK;

            foreach (string targetGuid in allTargets)
            {
                if (string.IsNullOrEmpty(targetGuid)) continue;

                // Force ONLY iPhone (1=iPhone, excludes 2=iPad and 7=visionOS)
                project.SetBuildProperty(targetGuid, "TARGETED_DEVICE_FAMILY", "1");
                
                // Explicitly disable alternative platform targets
                project.SetBuildProperty(targetGuid, "SUPPORTS_XR_OS", "NO");
                project.SetBuildProperty(targetGuid, "SUPPORTS_MACCATALYST", "NO");
                project.SetBuildProperty(targetGuid, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD", "NO");
                project.SetBuildProperty(targetGuid, "IS_APPLE_SILICON_ONLY", "NO");
                project.SetBuildProperty(targetGuid, "XROS_DEPLOYMENT_TARGET", "");
                project.SetBuildProperty(targetGuid, "MACOSX_DEPLOYMENT_TARGET", "");

                // Always strip RealityKit
                RemoveFramework(project, targetGuid, "RealityKit.framework");

                // Dynamic environment variables to clear the "SDK Not Found" issues in Xcode 26
                if (isSimulator)
                {
                    project.SetBuildProperty(targetGuid, "SUPPORTED_PLATFORMS", "iphonesimulator");
                    project.SetBuildProperty(targetGuid, "SDKROOT", "iphoneos"); 
                    project.SetBuildProperty(targetGuid, "SDKVARIANT", "iphonesimulator");

                    // Add this line to force the simulator linker to bypass missing AR symbols
                    project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-Wl,-flat_namespace,-U,_UnityARKit*");
                    project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-Wl,-undefined,dynamic_lookup");

                    RemoveSimulatorIncompatibleFiles(project, targetGuid);
                }
                else
                {
                    project.SetBuildProperty(targetGuid, "SUPPORTED_PLATFORMS", "iphoneos");
                    project.SetBuildProperty(targetGuid, "SDKROOT", "iphoneos");
                    project.SetBuildProperty(targetGuid, "SDKVARIANT", "iphoneos");
                }
            }

            // Apply platform limitations to the Project Level
            string projectGuid = project.ProjectGuid();
            project.SetBuildProperty(projectGuid, "TARGETED_DEVICE_FAMILY", "1");
            project.SetBuildProperty(projectGuid, "SUPPORTED_PLATFORMS", isSimulator ? "iphonesimulator" : "iphoneos");

            project.WriteToFile(projectPath);
            Debug.Log($"AR Build Optimized. Mode: {(isSimulator ? "Simulator (ARKit Stripped)" : "Device")}");
        }
        
        private static void RemoveFramework(PBXProject project, string targetGuid, string framework)
        {
            if (project.ContainsFramework(targetGuid, framework))
            {
                project.RemoveFrameworkFromProject(targetGuid, framework);
            }
        }

        private static void RemoveSimulatorIncompatibleFiles(PBXProject project, string targetGuid)
        {
            // 1. Remove Core ARKit Binary Wrapper
            string arkitCoreGuid = project.FindFileGuidByProjectPath("Libraries/com.unity.xr.arkit/Runtime/iOS/Xcode2600/libUnityARKit.a");
            if (!string.IsNullOrEmpty(arkitCoreGuid))
            {
                project.RemoveFileFromBuild(targetGuid, arkitCoreGuid);
                Debug.Log("Successfully unlinked libUnityARKit.a for Simulator.");
            }

            // 2. Remove ARKit Face Tracking Binary Wrapper
            string arkitFaceGuid = project.FindFileGuidByProjectPath("Libraries/com.unity.xr.arkit/Runtime/FaceTracking/iOS/Xcode2600/libUnityARKitFaceTracking.a");
            if (!string.IsNullOrEmpty(arkitFaceGuid))
            {
                project.RemoveFileFromBuild(targetGuid, arkitFaceGuid);
                Debug.Log("Successfully unlinked libUnityARKitFaceTracking.a for Simulator.");
            }
        }
    }
}