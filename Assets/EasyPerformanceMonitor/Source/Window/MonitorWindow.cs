// Microsoft
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace GUPS.EasyPerformanceMonitor.Window
{
    /// <summary>
    /// Represents a monitor window, functioning as a canvas to render monitor elements. 
    /// The window can be toggled for display by the user.
    /// </summary>
    /// <remarks>
    /// The <see cref="MonitorWindow"/> class provides functionality for managing the appearance and behavior 
    /// of a monitor window, including settings for toggle keys, rendering, and the positioning of monitor elements.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class MonitorWindow : MonoBehaviour
    {
        [Header("Monitor Window - Settings")]
        /// <summary>
        /// The monitor window name.
        /// </summary>
        public String Name;

        [Header("Monitor Window - Toggle Keys")]
#if ENABLE_INPUT_SYSTEM
        // New input system backends are enabled.

        /// <summary>
        /// Required input action to perform to toggle the monitor window.
        /// </summary>
        [Tooltip("Required input action to perform to toggle the monitor window (New Input system).")]
        public UnityEngine.InputSystem.InputAction ToggleAction;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        // Old input backends are enabled.

        /// <summary>
        /// Require the user to press also the 'control'-key to toggle the monitor window.
        /// </summary>
        [Tooltip("Require the user to press also the 'control'-key to toggle the monitor window (Old Input system).")]
        public bool UseControl = false;

        /// <summary>
        /// Require the user to press also the 'shift'-key to toggle the monitor window.
        /// </summary>
        [Tooltip("Require the user to press also the 'shift'-key to toggle the monitor window (Old Input system).")]
        public bool UseShift = false;

        /// <summary>
        /// Require the user to press also the 'alt'-key to toggle the monitor window.
        /// </summary>
        [Tooltip("Require the user to press also the 'alt'-key to toggle the monitor window (Old Input system).")]
        public bool UseAlt = false;

        /// <summary>
        /// Required key to press to toggle the monitor window.
        /// </summary>
        [Tooltip("Required key to press to toggle the monitor window (Old Input system).")]
        public KeyCode ToggleKey = KeyCode.F1;
#endif

        [Header("Monitor Window - Rendering")]
        /// <summary>
        /// The monitor canvas, rendering the monitor window.
        /// </summary>
        [Tooltip("The monitor canvas, rendering the monitor window.")]
        public Canvas MonitorCanvas;

        /// <summary>
        /// The monitor canvas rect transform.
        /// </summary>
        private RectTransform RectTransform { get => this.MonitorCanvas.GetComponent<RectTransform>(); }

        /// <summary>
        /// The monitor canvas scaler.
        /// </summary>
        private CanvasScaler CanvasScaler { get => this.MonitorCanvas.GetComponent<CanvasScaler>(); }

        /// <summary>
        /// Get the reference resolution of the monitor canvas. If the render mode is world space, the reference resolution 
        /// is the canvas rect size, otherwise the reference resolution is the canvas scaler reference resolution.
        /// </summary>
        private Vector2 ReferenceResolution
        {
            get
            {
                if(this.MonitorCanvas.renderMode == RenderMode.WorldSpace)
                {
                    // Return the size of the rect transform the canvas is in.
                    return this.RectTransform.rect.size;
                }
                else
                {
                    // Return the reference resolution of the canvas scaler.
                    return this.CanvasScaler.referenceResolution;
                }
            }
        }

        /// <summary>
        /// The monitor window position.
        /// </summary>
        [Tooltip("The monitor window position.")]
        public EMonitorWindowPosition MonitorPosition = EMonitorWindowPosition.Top_Left;

        /// <summary>
        /// The monitor elements initial x offset.
        /// </summary>
        [Tooltip("The monitor elements initial x offset.")]
        public int InitialOffsetX = 0;

        /// <summary>
        /// The monitor elements initial y offset.
        /// </summary>
        [Tooltip("The monitor elements initial y offset.")]
        public int InitialOffsetY = 0;

        /// <summary>
        /// The monitor elements width.
        /// </summary>
        [Tooltip("The monitor elements width.")]
        public int ElementWidth = 100;

        /// <summary>
        /// The monitor elements height.
        /// </summary>
        [Tooltip("The monitor elements height.")]
        public int ElementHeight = 100;

        /// <summary>
        /// The monitor elements spacing / margin.
        /// </summary>
        [Tooltip("The monitor elements spacing / margin.")]
        public int ElementSpacing = 10;

        /// <summary>
        /// When the new input system is enabled, enable the toogle action.
        /// </summary>
        protected virtual void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            // New input system backends are enabled.

            this.ToggleAction.Enable();
            this.ToggleAction.performed += this.ToggleActionOnPerformed;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        // New input system backends are enabled.

        /// <summary>
        /// When the user pressed the toggle action, toggle the monitor window.
        /// </summary>
        /// <param name="context"></param>
        private void ToggleActionOnPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // Toggle the monitor window.
            this.Toggle();
        }
#endif

        /// <summary>
        /// Initially place the monitor elements.
        /// </summary>
        protected virtual void Start()
        {
            // Place the monitor elements.
            this.PlaceMonitorElements();
        }

        /// <summary>
        /// When the old input system is enabled, validate and toggle the window if the user pressed the toggle key to open or close the monitor.
        /// </summary>
        protected virtual void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            // Old input backends are enabled.

            // Validate and toggle the monitor.
            if (this.GetToggleKeysPressed())
            {
                this.Toggle();
            }
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        // Old input backends are enabled.

        /// <summary>
        /// Validate if the user pressed the toggle key to open or close the monitor.
        /// </summary>
        /// <returns></returns>
        private bool GetToggleKeysPressed()
        {
            if (this.UseControl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                return false;
            }

            if (this.UseShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                return false;
            }

            if (this.UseAlt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
            {
                return false;
            }

            return Input.GetKeyUp(this.ToggleKey);
        }
#endif

        /// <summary>
        /// Toggle the monitor window.
        /// </summary>
        public void Toggle()
        {
            this.Toggle(!this.MonitorCanvas.enabled);
        }

        /// <summary>
        /// Toggle the monitor window.
        /// </summary>
        /// <param name="_Show">Show the monitor window or hide it.</param>
        public void Toggle(bool _Show)
        {
            this.MonitorCanvas.enabled = _Show;
        }

        /// <summary>
        /// Place the monitor elements based on the monitor position.
        /// </summary>
        public void PlaceMonitorElements()
        {
            // If the monitor position is free, do not replace the monitor elements.
            if(this.MonitorPosition == EMonitorWindowPosition.Free)
            {
                return;
            }

            // Get all active monitor elements.
            List<RectTransform> var_RectTransforms = new List<RectTransform>();

            // Iterate all canvas child transforms.
            foreach (Transform var_Child in this.MonitorCanvas.transform)
            {
                // Skip inactive game objects.
                if (!var_Child.gameObject.activeSelf)
                {
                    continue;
                }

                // Add the child rect transform to the list.
                var_RectTransforms.Add(var_Child as RectTransform);
            }

            // Place the monitor elements.
            float var_X = 0;
            float var_Y = 0;

            // Init the positions - Top.
            switch (this.MonitorPosition)
            {
                case EMonitorWindowPosition.Top:
                    {
                        // Init the positions.
                        var_X = this.ElementSpacing + this.InitialOffsetX;
                        var_Y = - (this.ElementSpacing + this.InitialOffsetY);

                        break;
                    }
                case EMonitorWindowPosition.Top_Left:
                    {
                        // Init the positions.
                        var_X = this.ElementSpacing + this.InitialOffsetX;
                        var_Y = - (this.ElementSpacing + this.InitialOffsetY);

                        break;
                    }
                    case EMonitorWindowPosition.Top_Right:
                    {
                        // Init the positions.
                        var_X = - (this.ElementWidth + this.ElementSpacing + this.InitialOffsetX);
                        var_Y = - (this.ElementSpacing + this.InitialOffsetY);

                        break;
                    }
            }

            // Place the monitor elements top.
            for (int i = 0; i < var_RectTransforms.Count; i++)
            {
                // Resize the monitor elements.
                var_RectTransforms[i].sizeDelta = new Vector2(this.ElementWidth, this.ElementHeight);

                // Place the monitor elements.
                switch (this.MonitorPosition)
                {
                    case EMonitorWindowPosition.Top:
                        {
                            // Set anchor.
                            var_RectTransforms[i].anchorMin = new Vector2(0, 1);
                            var_RectTransforms[i].anchorMax = new Vector2(0, 1);

                            // Set pivot.
                            var_RectTransforms[i].pivot = new Vector2(0, 1);

                            // Place the monitor element.
                            var_RectTransforms[i].anchoredPosition = new Vector3(var_X, var_Y, 0);

                            // Increase the y position, if the element x position is out of the screen.
                            if (var_X + this.ElementWidth * 2 > this.ReferenceResolution.x)
                            {
                                var_Y -= this.ElementHeight + this.ElementSpacing;
                                var_X = this.ElementSpacing;
                            }
                            else
                            {
                                // Increase the x position.
                                var_X += this.ElementWidth + this.ElementSpacing;
                            }

                            break;
                        }
                    case EMonitorWindowPosition.Top_Left:
                        {
                            // Set anchor.
                            var_RectTransforms[i].anchorMin = new Vector2(0, 1);
                            var_RectTransforms[i].anchorMax = new Vector2(0, 1);

                            // Set pivot.
                            var_RectTransforms[i].pivot = new Vector2(0, 1);

                            // Place the monitor element.
                            var_RectTransforms[i].anchoredPosition = new Vector3(var_X, var_Y, 0);

                            // Increase the x position, if the element y position is out of the screen.
                            if (var_Y - this.ElementHeight * 2 < -this.ReferenceResolution.y)
                            {
                                var_X += this.ElementWidth + this.ElementSpacing;
                                var_Y = -this.ElementSpacing;
                            }
                            else
                            {
                                // Increase the y position.
                                var_Y -= this.ElementHeight + this.ElementSpacing;
                            }

                            break;
                        }
                    case EMonitorWindowPosition.Top_Right:
                        {
                            // Set anchor.
                            var_RectTransforms[i].anchorMin = new Vector2(1, 1);
                            var_RectTransforms[i].anchorMax = new Vector2(1, 1);

                            // Set pivot.
                            var_RectTransforms[i].pivot = new Vector2(0, 1);

                            // Place the monitor element.
                            var_RectTransforms[i].anchoredPosition = new Vector3(var_X, var_Y, 0);

                            // Increase the x position, if the element y position is out of the screen.
                            if (var_Y - this.ElementHeight * 2 < -this.ReferenceResolution.y)
                            {
                                var_X -= this.ElementWidth + this.ElementSpacing;
                                var_Y = -this.ElementSpacing;
                            }
                            else
                            {
                                // Increase the y position.
                                var_Y -= this.ElementHeight + this.ElementSpacing;
                            }

                            break;
                        }
                }
            }

            // Init the positions - Bottom.
            switch (this.MonitorPosition)
            {
                case EMonitorWindowPosition.Bottom:
                    {
                        // Init the positions.
                        var_X = this.ElementSpacing + this.InitialOffsetX;
                        var_Y = this.ElementHeight + this.ElementSpacing + this.InitialOffsetY;

                        break;
                    }
                case EMonitorWindowPosition.Bottom_Left:
                    {
                        // Init the positions.
                        var_X = this.ElementSpacing + this.InitialOffsetX;
                        var_Y = this.ElementHeight + this.ElementSpacing + this.InitialOffsetY;

                        break;
                    }
                case EMonitorWindowPosition.Bottom_Right:
                    {
                        // Init the positions.
                        var_X = - (this.ElementWidth + this.ElementSpacing + this.InitialOffsetX);
                        var_Y = this.ElementHeight + this.ElementSpacing + this.InitialOffsetY;

                        break;
                    }
            }

            // Place the monitor elements bottom.
            for (int i = var_RectTransforms.Count - 1; i >= 0; i--)
            {
                // Place the monitor elements.
                switch (this.MonitorPosition)
                {
                    case EMonitorWindowPosition.Bottom:
                        {
                            // Set anchor.
                            var_RectTransforms[i].anchorMin = new Vector2(0, 0);
                            var_RectTransforms[i].anchorMax = new Vector2(0, 0);

                            // Set pivot.
                            var_RectTransforms[i].pivot = new Vector2(0, 1);

                            // Place the monitor element.
                            var_RectTransforms[i].anchoredPosition = new Vector3(var_X, var_Y, 0);

                            // Increase the y position, if the element x position is out of the screen.
                            if (var_X + this.ElementWidth * 2 > this.ReferenceResolution.x)
                            {
                                var_Y += this.ElementHeight + this.ElementSpacing;
                                var_X = this.ElementSpacing;
                            }
                            else
                            {
                                // Increase the x position.
                                var_X += this.ElementWidth + this.ElementSpacing;
                            }

                            break;
                        }
                    case EMonitorWindowPosition.Bottom_Left:
                        {
                            // Set anchor.
                            var_RectTransforms[i].anchorMin = new Vector2(0, 0);
                            var_RectTransforms[i].anchorMax = new Vector2(0, 0);

                            // Set pivot.
                            var_RectTransforms[i].pivot = new Vector2(0, 1);

                            // Place the monitor element.
                            var_RectTransforms[i].anchoredPosition = new Vector3(var_X, var_Y, 0);

                            // Increase the x position, if the element y position is out of the screen.
                            if (var_Y + this.ElementHeight * 2 > this.ReferenceResolution.y)
                            {
                                var_X += this.ElementWidth + this.ElementSpacing;
                                var_Y = this.ElementHeight + this.ElementSpacing;
                            }
                            else
                            {
                                // Increase the y position.
                                var_Y += this.ElementHeight + this.ElementSpacing;
                            }

                            break;
                        }
                    case EMonitorWindowPosition.Bottom_Right:
                        {
                            // Set anchor.
                            var_RectTransforms[i].anchorMin = new Vector2(1, 0);
                            var_RectTransforms[i].anchorMax = new Vector2(1, 0);

                            // Set pivot.
                            var_RectTransforms[i].pivot = new Vector2(0, 1);

                            // Place the monitor element.
                            var_RectTransforms[i].anchoredPosition = new Vector3(var_X, var_Y, 0);

                            // Increase the x position, if the element y position is out of the screen.
                            if (var_Y + this.ElementHeight * 2 > this.ReferenceResolution.y)
                            {
                                var_X -= this.ElementWidth + this.ElementSpacing;
                                var_Y = this.ElementHeight + this.ElementSpacing;
                            }
                            else
                            {
                                // Increase the y position.
                                var_Y += this.ElementHeight + this.ElementSpacing;
                            }

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Refresh the monitor window on editor value changed.
        /// </summary>
        public virtual void RefreshWindow()
        {
            // Replace the monitor elements on refresh.
            this.PlaceMonitorElements();
        }

        /// <summary>
        /// When the new input system is enabled, disable the toogle action.
        /// </summary>
        protected virtual void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            // New input system backends are enabled.

            this.ToggleAction.Disable();
            this.ToggleAction.performed -= this.ToggleActionOnPerformed;
#endif
        }
    }
}
