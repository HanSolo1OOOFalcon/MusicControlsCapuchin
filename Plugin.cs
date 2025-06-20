using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using LibCpp2IL.Elf;
using Locomotion;
using MusicControlsCapuchin.Patches;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

namespace MusicControlsCapuchin
{
    [BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
    public class Init : BasePlugin
    {
        public static Init instance;
        public Harmony harmonyInstance;

        public override void Load()
        {
            harmonyInstance = HarmonyPatcher.Patch(ModInfo.GUID);
            instance = this;

            ClassInjector.RegisterTypeInIl2Cpp<ButtonCollider>();

            AddComponent<Plugin>();
        }

        public override bool Unload()
        {
            if (harmonyInstance != null)
                HarmonyPatcher.Unpatch(harmonyInstance);

            return true;
        }
    }

    public class Plugin : MonoBehaviour
    {
        // thanks for letting me port this to capuchin, graze!
        // best regards,
        // hansolo1000falcon

        public static Plugin instance;

        internal enum VirtualKeyCodes : uint
        {
            NEXT_TRACK = 0xB0,
            PREVIOUS_TRACK = 0xB1,
            PLAY_PAUSE = 0xB3,
        };

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);

        internal static void SendKey(VirtualKeyCodes virtualKeyCode) => keybd_event((uint)virtualKeyCode, 0, 0, 0);
        public static void NextTrack() => SendKey(VirtualKeyCodes.NEXT_TRACK);
        public static void PreviousTrack() => SendKey(VirtualKeyCodes.PREVIOUS_TRACK);
        public static void PlayPause() => SendKey(VirtualKeyCodes.PLAY_PAUSE);

        private bool wasLeftClickPressed;
        private bool hasInitialized = false;
        private GameObject canvasObject;
        public bool shouldPlayOrPause, shouldNextTrack, shouldPreviousTrack;
        private void FixedUpdate()
        {
            if (!hasInitialized && Player.Instance != null)
                Init();
            
            bool leftClick = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftIsClicked) && leftIsClicked;

            if (leftClick && !wasLeftClickPressed)
            {
                canvasObject.SetActive(true);
                canvasObject.transform.position = Player.Instance.LeftHand.transform.position + -Player.Instance.LeftHand.transform.up * 0.2f;
                canvasObject.transform.rotation = Player.Instance.LeftHand.transform.rotation * Quaternion.Euler(0f, -90f, 0f);
            }
            else if (!leftClick && wasLeftClickPressed)
            {
                canvasObject.SetActive(false);

                if (shouldPlayOrPause)
                    PlayPause();
                else if (shouldNextTrack)
                    NextTrack();
                else if (shouldPreviousTrack)
                    PreviousTrack();

                shouldPlayOrPause = false;
                shouldNextTrack = false;
                shouldPreviousTrack = false;
            }

            wasLeftClickPressed = leftClick;
        }

        private void Init()
        {
            hasInitialized = true;

            canvasObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canvasObject.name = "MusicControlsCapuchinCanvas";
            canvasObject.GetComponent<BoxCollider>().enabled = false;
            canvasObject.transform.localScale = new Vector3(0.3f, 0.1f, 0.5f);
            canvasObject.transform.position = Vector3.zero;
            canvasObject.transform.rotation = Quaternion.identity;

            GameObject pause = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pause.name = "PauseButton";
            pause.transform.SetParent(canvasObject.transform);
            pause.transform.localPosition = new Vector3(0.15f, 0.1f, 0);
            pause.transform.localScale = new Vector3(0.4f, 1f, 0.2f);
            pause.transform.localRotation = Quaternion.identity;
            pause.GetComponent<BoxCollider>().isTrigger = true;
            pause.AddComponent<ButtonCollider>().isPlayOrPause = true;
            pause.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Color");
            pause.GetComponent<Renderer>().material.color = new Color32(75, 78, 84, 255);

            GameObject next = GameObject.CreatePrimitive(PrimitiveType.Cube);
            next.name = "NextButton";
            next.transform.SetParent(canvasObject.transform);
            next.transform.localPosition = new Vector3(-0.3f, 0.1f, -0.25f);
            next.transform.localScale = new Vector3(0.4f, 1f, 0.2f);
            next.transform.localRotation = Quaternion.identity;
            next.GetComponent<BoxCollider>().isTrigger = true;
            next.AddComponent<ButtonCollider>().isNextTrack = true;
            next.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Color");
            next.GetComponent<Renderer>().material.color = new Color32(75, 78, 84, 255);

            GameObject previous = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previous.name = "PreviousButton";
            previous.transform.SetParent(canvasObject.transform);
            previous.transform.localPosition = new Vector3(-0.3f, 0.1f, 0.25f);
            previous.transform.localScale = new Vector3(0.4f, 1f, 0.2f);
            previous.transform.localRotation = Quaternion.identity;
            previous.GetComponent<BoxCollider>().isTrigger = true;
            previous.AddComponent<ButtonCollider>().isPreviousTrack = true;
            previous.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Color");
            previous.GetComponent<Renderer>().material.color = new Color32(75, 78, 84, 255);
        }

        private void Start() => instance = this;
    }

    public class ButtonCollider : MonoBehaviour
    {
        public bool isPlayOrPause = false, isNextTrack = false, isPreviousTrack = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other == Player.Instance.LeftCollider)
            {
                if (isPlayOrPause)
                {
                    Plugin.instance.shouldPlayOrPause = true;
                    Plugin.instance.shouldNextTrack = false;
                    Plugin.instance.shouldPreviousTrack = false;
                }
                else if (isNextTrack)
                {
                    Plugin.instance.shouldNextTrack = true;
                    Plugin.instance.shouldPreviousTrack = false;
                    Plugin.instance.shouldPlayOrPause = false;
                }
                else if (isPreviousTrack)
                {
                    Plugin.instance.shouldPreviousTrack = true;
                    Plugin.instance.shouldNextTrack = false;
                    Plugin.instance.shouldPlayOrPause = false;
                }
            }
        }
    }
}
