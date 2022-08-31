using ABI_RC.Core.InteractionSystem;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core;
using ABI_RC.Systems.MovementSystem;
using ABI_RC.Core.Networking.API;

namespace UIRevertPatch

{
	/// <summary>
	/// Sorry if my code looks horrible, I tried my best to get this to work, and there are serveral issues! If you want to fix this up please make a pull request
	/// and I'll happily credit and include your pull request~
	/// 
	/// 
	/// Thanks to LJ / ljoonal https://git.ljoonal.xyz/ that made me to get this to work
	/// CVR Modding Corner: https://discord.gg/2WR6rGVzht
	/// ChilloutVR Modding Group: https://discord.gg/dndGPM3bxu
	/// 
	/// 
	/// </summary>
	
	public static class BuildInfo
	{
		public const string Name = "UIRevertPatchMod";
		public const string Description = null;
		public const string Author = "Shin";
		public const string Company = null;
		public const string Version = "1.0.0";
		public const string DownloadLink = null;
	}
	#region MelonMod
	public class UIRevertPatch : MelonMod
	{
		public override void OnApplicationStart()
		{
			Instance.PatchAll();
			MelonLogger.Msg("We are back to the past, Mod loaded successfully!");	
		}

		public static HarmonyLib.Harmony Instance = new HarmonyLib.Harmony("UIRevertPatch");

		#endregion


		#region PatchUI
		[HarmonyPatch(typeof(ViewManager), nameof(ViewManager.UiStateToggle), typeof(bool))]
		
		public static class UiStatePatch
        {
			[HarmonyPostfix]
			static void UIHack(ViewManager __instance, bool show)
			{
				{
					if (MovementSystem.Instance != null)
					{
						MovementSystem.Instance.canMove = show;
						__instance.desktopControllerRay.enabled = show;
						CVRInputManager.Instance.inputEnabled = show;
						RootLogic.Instance.ToggleMouse(!show);
						PlayerSetup.Instance._movementSystem.disableCameraControl = !show;
					}
					if (!__instance.isGameMenuOpen())
					{
						MovementSystem.Instance.canMove = true;
						PlayerSetup.Instance._movementSystem.disableCameraControl = false;
						CVRInputManager.Instance.inputEnabled = true;
						RootLogic.Instance.ToggleMouse(false);
						__instance.desktopControllerRay.enabled = true;
					}
				}
			}
		}
		 
        #endregion

        #region UpdatePatch
        [HarmonyPatch(typeof(ViewManager), "Update")]
		
		public static class UiUpdatePatch
        {
			[HarmonyPrefix]
			static bool UIUpdateDisabler(ViewManager __instance)
			{
				Traverse.Create(__instance).Field("_deltaTime").SetValue(Time.deltaTime);
				__instance.PushMenuUpdates();
				ApiConnection.WorkMessageQueue();
				return false;
			}
		}

		#endregion

		#region UpdateMenuPosPatch

		[HarmonyPatch(typeof(ViewManager), "UpdateMenuPosition")]
		public static class UpdateMenuPosPatch
		{
			[HarmonyPrefix]
			static bool UpdatedMenuPosChanger(ViewManager __instance)
			{
				GameObject theMainUi = GameObject.Find("Cohtml/CohtmlWorldView");
				float screenaspect = Traverse.Create(__instance).Field("cachedScreenAspectRatio").GetValue<float>();
				float cachedAvatarHeight = Traverse.Create(__instance).Field("cachedAvatarHeight").GetValue<float>();
				float ScaleFactor = Traverse.Create(__instance).Field("scaleFactor").GetValue<float>();

				if ((float)Screen.width / (float)Screen.height != screenaspect && cachedAvatarHeight != 0f)
				{
					__instance.SetScale(cachedAvatarHeight);
				}

				if (__instance.uiCollider != null && !__instance.uiCollider.enabled && PlayerSetup.Instance != null && PlayerSetup.Instance._movementSystem != null && PlayerSetup.Instance._movementSystem.rotationPivot != null)
				{
					Transform rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
					float num = Mathf.Abs(rotationPivot.localRotation.eulerAngles.z);
					float settingsFloat = MetaPort.Instance.settings.GetSettingsFloat("GeneralMinimumMenuTilt");
					if (MetaPort.Instance.isUsingVr && (num <= settingsFloat || num >= 360f - settingsFloat))
					{
						theMainUi.transform.rotation = Quaternion.LookRotation(rotationPivot.forward, Vector3.up);
					}
					else
					{
						theMainUi.transform.eulerAngles = new Vector3(rotationPivot.eulerAngles.x, rotationPivot.eulerAngles.y, rotationPivot.eulerAngles.z);
					}
					theMainUi.transform.position = rotationPivot.position + rotationPivot.forward * 1f * ScaleFactor;
					Traverse.Create(__instance).Field("needsMenuPositionUpdate").SetValue(false);
				}
				return false;
			}
		}
		
		#endregion
	}
}
