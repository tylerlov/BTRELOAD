

using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Collections;

namespace Mirage.Impostors.Demo
{
    public class DemoManager : MonoBehaviour
    {
        [Header("Camera")]
        public Transform cam;
        public bool lockCursor;

        [Range(0.1f, 10)] public float lookSensitivity;

        public float maxUpRotation;
        public float maxDownRotation;

        private float xRotation = 0;

        [Header("Movement")]
        public CharacterController controller;
        [Range(0.5f, 20)] public float walkSpeed;
        [Range(0.5f, 15)] public float strafeSpeed;

        public KeyCode sprintKey = KeyCode.LeftShift;
        [Range(1, 3)] public float sprintFactor;
        [Range(0.5f, 10)] public float jumpHeight;
        public int maxJumps;

        private Vector3 velocity = Vector3.zero;
        private int jumpsSinceLastLand = 0;

        [Header("Lights Throwing/Switching")]
        public GameObject lightBallPrefab;
        public GameObject[] cellarGameObjects;
        public Material glassMaterial;
        private Material defaultMaterial;
        private Queue<GameObject> lightInstances;
        private const int MAX_LIGHTS = 5;

        private float deltaTime = 0f;
        private bool impostorsEnabled = true;
        private GUIStyle smallTextStyle;
        private GUIStyle largeTextStyleRed;
        private GUIStyle largeTextStyleGreen;
        private Texture2D logo;
        private int totalTriangles;

        void Start()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            lightInstances = new Queue<GameObject>();
            smallTextStyle = new GUIStyle
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState
                {
                    textColor = Color.white,
                }
            };
            largeTextStyleRed = new GUIStyle
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState
                {
                    textColor = Color.red,
                }
            };
            largeTextStyleGreen = new GUIStyle
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState
                {
                    textColor = new Color(0.25f, 1f, 0.25f),
                }
            };
            defaultMaterial = (cellarGameObjects[0].GetComponent<MeshRenderer>().materials[0]);
            logo = Resources.Load<Texture2D>("MirageLogo");
            StartCoroutine(CountTrianglesPeriodically());
            Physics.IgnoreLayerCollision(1, 1);

        }

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            transform.Rotate(0, Input.GetAxis("Mouse X") * lookSensitivity, 0);
            xRotation -= Input.GetAxis("Mouse Y") * lookSensitivity;
            xRotation = Mathf.Clamp(xRotation, -maxUpRotation, maxDownRotation);
            cam.localRotation = Quaternion.Euler(xRotation, 0, 0);

            velocity.z = Input.GetAxis("Vertical") * walkSpeed;
            velocity.x = Input.GetAxis("Horizontal") * strafeSpeed;
            velocity = transform.TransformDirection(velocity);

            if (Input.GetKey(sprintKey)) { Sprint(); }

            // Apply manual gravity
            velocity.y += Physics.gravity.y * Time.deltaTime;

            if (controller.isGrounded && velocity.y < 0) { Land(); }

            if (Input.GetButtonDown("Jump"))
            {
                if (controller.isGrounded)
                {
                    Jump();
                }
                else if (jumpsSinceLastLand < maxJumps)
                {
                    Jump();
                }
            }

            controller.Move(velocity * Time.deltaTime);

            if (Input.GetButtonDown("Fire1"))
            {
                ThrowLight();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                impostorsEnabled = !impostorsEnabled;
#if UNITY_6000_0_OR_NEWER
                LODGroup[] lodgroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
#else
                LODGroup[] lodgroups = FindObjectsOfType(typeof(LODGroup)) as LODGroup[];
#endif
                if (impostorsEnabled)
                {
                    foreach (LODGroup lg in lodgroups)
                    {
                        lg.enabled = true;
                        lg.GetLODs()[1].renderers[0].gameObject.SetActive(true);
                    }
                }
                else
                {
                    foreach (LODGroup lg in lodgroups)
                    {
                        lg.GetLODs()[1].renderers[0].gameObject.SetActive(false);
                        lg.enabled = false;
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
#if UNITY_6000_0_OR_NEWER
                Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                Light[] lights = FindObjectsOfType(typeof(Light), true) as Light[];
#endif
                foreach (Light light in lights)
                {
                    light.enabled = !light.enabled;
                }
                foreach (GameObject cellarGameObject in cellarGameObjects)
                    if (RenderSettings.sun.isActiveAndEnabled)
                    {
                        cellarGameObject.GetComponent<MeshRenderer>().material = glassMaterial;
                    }
                    else
                    {
                        cellarGameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                    }
            }
        }

        void OnGUI()
        {
            int w = Screen.width, h = Screen.height;

            Rect containerRect = new Rect(8, h - 110, w - 16, 102);
            string text;
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            text = string.Format((impostorsEnabled ? " enabled : " : " disabled : ") + "{0:0.0} ms ({1:0.} fps)", msec, fps);
            GUILayout.BeginArea(new Rect(8, 8, w - 16, h - 16));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(logo, largeTextStyleGreen, GUILayout.Width(200), GUILayout.Height(100));
            GUILayout.Label(text, impostorsEnabled ? largeTextStyleGreen : largeTextStyleRed, GUILayout.Height(100));
            GUILayout.EndHorizontal();
            GUILayout.Label("" + totalTriangles + " triangles rendered", smallTextStyle);

            GUILayout.Label("Click to throw point lights", smallTextStyle);
            GUILayout.Label("Press E to switch " + (impostorsEnabled ? "Off" : "On") + " Mirage impostors", smallTextStyle);
            GUILayout.Label("Press R to switch lighting", smallTextStyle);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void Sprint()
        {
            velocity.z *= sprintFactor;
            velocity.x *= sprintFactor;
        }

        private void Jump()
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
            jumpsSinceLastLand++;
        }

        private void Land()
        {
            velocity.y = 0;
            jumpsSinceLastLand = 0;
        }

        private void ThrowLight()
        {
            if (lightBallPrefab != null)
            {
                GameObject light = Instantiate(lightBallPrefab);
                Physics.IgnoreCollision(light.GetComponent<Collider>(), GetComponent<Collider>());
                light.transform.position = transform.position + transform.up * 0.75f + transform.right * 0.1f + transform.forward * 0.1f;

                float randomHue = Random.Range(0f, 1f);
                Color randomColor = Color.HSVToRGB(randomHue, 0.5f, 1f);
                light.GetComponent<Light>().color = randomColor;
                light.GetComponent<MeshRenderer>()?.material.SetColor("_Color", randomColor);
                if (RenderSettings.sun.isActiveAndEnabled)
                    light.GetComponent<Light>().enabled = false;
                light.GetComponent<Rigidbody>()?.AddForce(700f * (Camera.main.transform.forward + Camera.main.transform.up * 0.5f).normalized);
                lightInstances.Enqueue(light);
                if (lightInstances.Count > MAX_LIGHTS)
                    Destroy(lightInstances.Dequeue());
            }
        }

        private IEnumerator CountTrianglesPeriodically()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
#if UNITY_6000_0_OR_NEWER
                MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
#else
                MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
#endif
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
                int trianglesCount = 0;
                foreach (var renderer in renderers)
                {

                    if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                    {
                        if (renderer.isVisible)
                        {
                            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                            if (meshFilter && meshFilter.sharedMesh)
                            {
                                trianglesCount += meshFilter.sharedMesh.triangles.Length / 3;
                            }
                        }
                    }
                }
                totalTriangles = trianglesCount;
            }
        }
    }
}
