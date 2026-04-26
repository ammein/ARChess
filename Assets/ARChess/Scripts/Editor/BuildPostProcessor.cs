using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ARChess.Scripts.Editor
{
    public static class PostBuildProcessor
    {
        [PostProcessBuild(999)] // High number ensures it runs after other plugins
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            // Only run for iOS builds
            if (target != BuildTarget.iOS) return;

            // 1. Info.plist Handling
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);

            // 2. PBXProject Handling
            string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            string[] allTargets = { mainTarget, frameworkTarget };

            foreach (string targetGuid in allTargets)
            {
                if (string.IsNullOrEmpty(targetGuid)) continue;

                // 1. Force ONLY iPhone (1=iPhone, excludes 2=iPad and 7=visionOS)
                project.SetBuildProperty(targetGuid, "TARGETED_DEVICE_FAMILY", "1");

                // 2. Clear platforms list to ONLY iOS (Removes xros, macos, catalyst)
                project.SetBuildProperty(targetGuid, "SUPPORTED_PLATFORMS", "iphoneos iphonesimulator");
                project.SetBuildProperty(targetGuid, "SDKROOT", "iphoneos");

                // 3. Explicitly Disable the Platform-specific Flags
                project.SetBuildProperty(targetGuid, "SUPPORTS_XR_OS", "NO");
                project.SetBuildProperty(targetGuid, "SUPPORTS_MACCATALYST", "NO");
                project.SetBuildProperty(targetGuid, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD", "NO");
                project.SetBuildProperty(targetGuid, "IS_APPLE_SILICON_ONLY", "NO");

                // 4. Force "SDK Variant" to iOS only (This often hides the "Apple Vision" label)
                project.SetBuildProperty(targetGuid, "SDKVARIANT", "iphoneos");

                // 5. Clean up Deployment Targets
                // Use an empty string for the ones you want to hide
                project.SetBuildProperty(targetGuid, "XROS_DEPLOYMENT_TARGET", "");
                project.SetBuildProperty(targetGuid, "MACOSX_DEPLOYMENT_TARGET", "");

                // 6. Strip RealityKit (keeping ARKit for your AR Foundation)
                RemoveFramework(project, targetGuid, "RealityKit.framework");
            }

            // 7. APPLY TO PROJECT LEVEL TOO
            // Sometimes Unity sets these at the project level, which overrides target levels
            string projectGuid = project.ProjectGuid();
            project.SetBuildProperty(projectGuid, "TARGETED_DEVICE_FAMILY", "1");
            project.SetBuildProperty(projectGuid, "SUPPORTED_PLATFORMS", "iphoneos iphonesimulator");


            project.WriteToFile(projectPath);
            Debug.Log("AR Build Optimized: visionOS/Mac/iPad stripped. AR Foundation (iPhone) preserved.");
        }
        
        private static void RemoveFramework(PBXProject project, string targetGuid, string framework)
        {
            if (project.ContainsFramework(targetGuid, framework))
            {
                project.RemoveFrameworkFromProject(targetGuid, framework);
            }
        }
    }
}
