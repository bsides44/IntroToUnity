#if INCLUDE_CONTENT_MANAGER
using Unity.ContentManager;
using UnityEngine;
#endif

namespace UnityEditor.MARS
{
    [InitializeOnLoad]
    class MarsContentManagerUtils
    {
#if INCLUDE_CONTENT_MANAGER
        const string k_MarsContentProductGuid = "fd31995f4d6b88e4eb6e89ee7b5ebcd2";
        const string k_NewVersionDialogTitle = "Open Content Manager?";
        const string k_NewVersionDialogMessage =
            "A new version of MARS has been installed, and new simulation environments may be available in Content " +
            "Manager. Would you like to check now? You can access Content Manager at any time with Window > Content " +
            "Manager.";
#else
        const string k_ContentManagerMenuName = "Window/Content Manager...";
        const int k_ContentManagerMenuPriority = 1500;
        const string k_ContentManagerPackage = "com.unity.content-manager";
        const string k_InstallDialogTitle = "Install Content Manager?";
        const string k_InstallDialogMessage =
            "MARS uses Content Manager to manage simulation environments, add-on functionality, and other optional " +
            "content. Would you like to install Content Manager? This may take a few minutes.";
#endif

        static MarsContentManagerUtils()
        {
            PackageVersionWatcher.packageUpdated += PromptOpenContentManager;
        }

        static void PromptOpenContentManager()
        {
#if INCLUDE_CONTENT_MANAGER
            if (EditorUtility.DisplayDialog(k_NewVersionDialogTitle, k_NewVersionDialogMessage, "Open", "Cancel"))
                TryOpenContentManagerWithMars();
#else
            PromptAddContentManagerPackage();
#endif
        }

#if !INCLUDE_CONTENT_MANAGER
        [MenuItem(k_ContentManagerMenuName, priority = k_ContentManagerMenuPriority)]
        public static void PromptAddContentManagerPackage()
        {
            if (EditorUtility.DisplayDialog(k_InstallDialogTitle, k_InstallDialogMessage, "Install", "Cancel"))
                PackageManager.Client.Add(k_ContentManagerPackage);
        }
#endif

        internal static void TryOpenContentManagerWithMars()
        {
#if INCLUDE_CONTENT_MANAGER
            var marsContentProduct = AssetDatabase.GUIDToAssetPath(k_MarsContentProductGuid);

            ContentProduct content = null;

            if (string.IsNullOrEmpty(marsContentProduct))
                Debug.LogWarning("MARS Content Manager asset not found.");
            else
                content = AssetDatabase.LoadAssetAtPath<ContentProduct>(marsContentProduct);

            ContentManagerWindow.OpenWithWithProduct(content);
#else
            PromptAddContentManagerPackage();
#endif
        }
    }
}
