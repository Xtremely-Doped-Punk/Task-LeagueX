using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Task
{
    public class PlayerInputHandler : MonoBehaviour
    {
        // scale values to match up new input system to old input system
        //#if ENABLE_INPUT_SYSTEM
        public const float MOUSE_AXIS_SCALER = 10;

        string primaryBinding = "<Touchscreen>/delta";
        string secondaryBinding = "<Touchscreen>/touch1/delta";

        int Android_CamBindIndex;
        InputActionSetupExtensions.BindingSyntax Android_Cam_Binding;

        // singleton
        public static PlayerInputHandler Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
                Destroy(this.gameObject);
        }

        /*
            Old Input System to New Input System Mapping
        
        Camera Controls: Axis => { "Mouse X" , "Mouse Y" }
        Movement Controls: Axis => { "Horizontal" , "Vertical" }
        Sprint: BtnHold => { LeftShift }
        Jump Controls: BtnDown => { Space }
        Switch Weapons: Axis => { "Mouse ScrollWheel" }; BtnDown => { number keys resp }
        Cursor Mode: BtnDown => { Esc }
        Fire: BtnDown, BtnHold => { LeftMouseBtn }
        Reload: BtnDown => { R }
        Score Board View: BtnDown, BtnUp => { Tab }

        ---------------------------------------------------------------------

            New Input System Mapping Cross Platform

        Camera Controls: Right Stick
        Movement Controls: Left Stick
        Jump Controls: Button West
        Switch Weapons: D-Pad Up, D-Pad Down
        Fire: D-Pad Left, D-Pad Right
        Reload: Button East
        Score Board View:

         */

        private GameInputActionMap _InputActions_;
        private GameInputActionMap.PlayerControllerActions _PlayerInput_;

        // public assesables (note BtnDown, BtnUp) => they need to be implemented as events
        // or should be resetted every frame after their use is invoked (thus here they are public set)
        public float MouseX_Axis => inp_Look.x / MOUSE_AXIS_SCALER;
        public float MouseY_Axis => inp_Look.y / MOUSE_AXIS_SCALER;
        public float Horizontal_Axis => inp_Move.x;
        public float Vertical_Axis => inp_Move.y;
        [field: SerializeField] public float Mouse_ScrollWheel { get; private set; } = 0f;
        [field: SerializeField] public bool LeftMouse_BtnHold { get; private set; } = false;
        [field: SerializeField] public bool Shift_BtnHold { get; set; } = false; // android custom ui

        // Per frame updates can be directly accesed from input handler
        public bool Space_BtnDown => _PlayerInput_.Jump.WasPerformedThisFrame();
        public bool LeftMouseBtn_BtnDown => _PlayerInput_.Fire.WasPressedThisFrame();
        public bool R_BtnDown => _PlayerInput_.Reload.WasPressedThisFrame();
        public bool WeaponNext => _PlayerInput_.WeaponSwitchNext.WasPressedThisFrame();
        public bool WeaponPrev => _PlayerInput_.WeaponSwitchPrev.WasPressedThisFrame();
        public bool Tab_BtnDown => _PlayerInput_.ScoreBoard.WasPressedThisFrame();
        public bool Tab_BtnUp => _PlayerInput_.ScoreBoard.WasReleasedThisFrame();


        // private calc, serialized just for reference/debugging
        [SerializeField] private Vector2 inp_Move; public float moveDeltaMagnitude => inp_Move.magnitude;
        [SerializeField] private Vector2 inp_Look;


        // Work around for button hold technique
        //public bool Shift_BtnHold => actions_Shift.Any(x => x.IsPressed());
        //[SerializeField] InputAction[] actions_Shift = _PlayerInput_.Sprint.actionMap.actions.ToArray(); // initialize on enable

        // Another work around for reset after reference
        //[SerializeField] private bool inp_Jump = false;
        //public bool Space_BtnDown { get { var temp = inp_Jump; inp_Jump = false; return temp; } } // if accessed, it resets

        private void OnEnable()
        {
            if (_InputActions_ == null)
                _InputActions_ = new GameInputActionMap();

            _PlayerInput_ = _InputActions_.PlayerController;

            /*
            foreach (var bindings in _PlayerInput_.Look.bindings)
            {
                Debug.Log(bindings);
            }*/

            // declare lamda fns to save locally
            _PlayerInput_.Move.performed += HandleMovement;
            _PlayerInput_.Look.performed += HandleCamera;

            _PlayerInput_.Sprint.performed += InpActCB => { Shift_BtnHold = InpActCB.action.IsPressed(); };
            _PlayerInput_.Sprint.canceled += InpActCB => { Shift_BtnHold = InpActCB.action.IsPressed(); };

            _PlayerInput_.Fire.performed += InpActCB => { LeftMouse_BtnHold = InpActCB.action.IsPressed(); };
            _PlayerInput_.Fire.canceled += InpActCB => { LeftMouse_BtnHold = InpActCB.action.IsPressed(); };

            _PlayerInput_.WeaponScroll.performed += InpActCB => Mouse_ScrollWheel = InpActCB.ReadValue<float>();

            /*
            _PlayerInput_.Jump.performed += InpActCB => { Space_BtnDown = InpActCB.action.IsPressed(); }; 
            _PlayerInput_.Reload.performed += InpActCB => { R_BtnDown = InpActCB.action.IsPressed(); };
            _PlayerInput_.WeaponSwitchNext.performed += InpActCB => { WeaponNext = InpActCB.action.IsPressed(); };
            _PlayerInput_.WeaponSwitchPrev.performed += InpActCB => { WeaponPrev = InpActCB.action.IsPressed(); };
            */
            // in late update, need to reset the per frame accesors

            _InputActions_.Enable();
            _PlayerInput_.Enable();


            // harcoded for now, later can be made dynamic in rebinding
            // for now, control scheme: "Android", unter Look action map,
            // binding "<Touchscreen>/delta" will be switched to 2ndary screen touch when primary touch is controlling on screen stick
            Android_CamBindIndex = _PlayerInput_.Look.GetBindingIndex(group: "Android", path: primaryBinding);
            Android_Cam_Binding = _PlayerInput_.Look.ChangeBinding(Android_CamBindIndex);
        }

        private void HandleMovement(InputAction.CallbackContext InpActCB)
        {
            inp_Move = InpActCB.ReadValue<Vector2>();
        }

        private void HandleCamera(InputAction.CallbackContext InpActCB)
        {
            inp_Look = InpActCB.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            if (_InputActions_ == null) return;
            _InputActions_.Disable();
            _PlayerInput_.Disable();
        }

        /* better way found
        private void LateUpdate()
        {
            // reset all one frame only inputs here
            //Space_BtnDown = LeftMouseBtn_BtnDown = R_BtnDown = WeaponNext = WeaponPrev = false;
        }*/

        public void CamUI_EnterOverride()
        {
            // previously whenever on-screen stick was held, cam controls was disabled
            _PlayerInput_.Look.performed -= HandleCamera; inp_Look = Vector2.zero;
            Android_Cam_Binding.WithPath(secondaryBinding);
            _PlayerInput_.Look.performed += HandleCamera;
        }
        public void CamUI_ExitOverride()
        {
            _PlayerInput_.Look.performed -= HandleCamera; inp_Look = Vector2.zero;
            Android_Cam_Binding.WithPath(primaryBinding);
            _PlayerInput_.Look.performed += HandleCamera;
        }

        public void AutoSprint_TakeOverride(Vector2 dir)
        {
            _PlayerInput_.Move.performed -= HandleMovement;
            inp_Move = dir.normalized;
        }
        public void AutoSprint_GiveOverride()
        {
            _PlayerInput_.Move.performed += HandleMovement;
        }

        public void Reset()
        {
            inp_Move = Vector2.zero;
            inp_Look = Vector2.zero;
            Mouse_ScrollWheel = 0f;
            Shift_BtnHold = LeftMouse_BtnHold = false;
        }
    }
}