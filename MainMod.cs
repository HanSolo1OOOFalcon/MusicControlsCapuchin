using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.Injection;
using Il2CppLocomotion;
using MusicControlsCapuchin;
using MelonLoader;
using UnityEngine;
using UnityEngine.XR;

[assembly: MelonInfo(typeof(MainMod), ModInfo.NAME, ModInfo.VERSION, ModInfo.AUTHOR)]

namespace MusicControlsCapuchin
{
    public class MainMod : MelonMod
    {
        // thanks for letting me port this to capuchin, graze!
        // best regards,
        // hansolo1000falcon

        public override void OnInitializeMelon() => ClassInjector.RegisterTypeInIl2Cpp<ButtonCollider>();

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
        private bool hasInitialized;
        private GameObject canvasObject;
        public static bool shouldPlayOrPause, shouldNextTrack, shouldPreviousTrack;
        public override void OnFixedUpdate()
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
                    MainMod.shouldPlayOrPause = true;
                    MainMod.shouldNextTrack = false;
                    MainMod.shouldPreviousTrack = false;
                }
                else if (isNextTrack)
                {
                    MainMod.shouldNextTrack = true;
                    MainMod.shouldPreviousTrack = false;
                    MainMod.shouldPlayOrPause = false;
                }
                else if (isPreviousTrack)
                {
                    MainMod.shouldPreviousTrack = true;
                    MainMod.shouldNextTrack = false;
                    MainMod.shouldPlayOrPause = false;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == Player.Instance.LeftCollider)
            {
                if (isPlayOrPause)
                    MainMod.shouldPlayOrPause = false;
                else if (isNextTrack)
                    MainMod.shouldNextTrack = false;
                else if (isPreviousTrack)
                    MainMod.shouldPreviousTrack = false;
            }
        }
    }
}