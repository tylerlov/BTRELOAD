using System.Collections.Generic;
using UnityEngine;


namespace FIMSpace.FOptimizing
{
    public class OptDemo_CamerasSwitcher : MonoBehaviour
    {
        public List<Camera> Cameras;
        private Camera currentCamera;

        void Start()
        {
            currentCamera = Camera.main;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCamera(Cameras[0]);

            if (Input.GetKeyDown(KeyCode.Alpha2)) if (Cameras.Count > 1) SwitchCamera(Cameras[1]);

            if (Input.GetKeyDown(KeyCode.Alpha3)) if (Cameras.Count > 2) SwitchCamera(Cameras[2]);

            if (Input.GetKeyDown(KeyCode.Alpha4)) if (Cameras.Count > 3) SwitchCamera(Cameras[3]);
        }


        private void SwitchCamera(Camera newCam)
        {
            if (newCam == null) return;
            if (currentCamera != null) currentCamera.gameObject.SetActive(false);
            newCam.gameObject.SetActive(true);
            currentCamera = newCam;
            OptimizersManager.SetNewMainCamera(newCam);
        }

    }


#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(OptDemo_CamerasSwitcher))]
    public class OptDemo_MultiCamerasEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            UnityEditor.EditorGUILayout.HelpBox("Press 1,2,3,4 keys to switch cameras from 'Cameras' list", UnityEditor.MessageType.Info);
        }
    }
#endif

}