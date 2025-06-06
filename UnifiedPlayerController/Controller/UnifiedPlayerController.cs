using UnityEngine;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UnifiedPlayerController
{
    /// <summary>
    /// Main player controller supporting both first-person and third-person camera modes.
    /// Handles movement, camera switching, jumping, gravity, animation, and audio.
    /// </summary>
    /// <remarks>
    /// This class provides a unified player controller that can switch between first-person and third-person camera modes.
    /// It includes settings for movement speed, camera rotation, jump height, gravity, and grounded checks.
    /// It also manages audio feedback for landing and footstep sounds.
    /// It uses Unity's CharacterController for movement and can be extended with custom input handling.
    /// Supports both the legacy Input System and the new Input System.
    /// Provides methods for camera rotation, movement, and grounded checks.
    /// Supports audio feedback for landing and footstep sounds.
    /// Provides a flexible camera system with Cinemachine integration for smooth transitions and controls.
    /// </remarks>
    /// <seealso cref="CinemachineCamera"/>
    /// <seealso cref="UnifiedPlayerInputs"/>
    /// <seealso cref="BasicRigidBodyPush"/>
    /// <seealso cref="DynamicFollowHead"/>
    /// <seealso cref="ThirdPersonCameraZoom"/>
    /// <seealso cref="CharacterController"/>
    /// <seealso cref="Animator"/>
    public enum CameraMode { FirstPerson, ThirdPerson }

    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class UnifiedPlayerController : MonoBehaviour
    {
        [Header("General")]
        /// <summary>
        /// The current camera mode (FirstPerson or ThirdPerson).
        /// </summary>
        public CameraMode cameraMode = CameraMode.FirstPerson;

        /// <summary>
        /// Reference to the first-person Cinemachine camera.
        /// </summary>
        public CinemachineCamera FirstPersonCamera;

        /// <summary>
        /// Reference to the third-person Cinemachine camera.
        /// </summary>
        public CinemachineCamera ThirdPersonCamera;

        /// <summary>
        /// Key used to switch between camera modes.
        /// </summary>
        public KeyCode SwitchCameraKey = KeyCode.C;

        //--- First Person Settings ---
        [Header("First Person Settings")]
        /// <summary>
        /// Speed settings for first-person movement and camera rotation.
        /// </summary>
        public float FP_MoveSpeed = 2.0f;           // Speed when not sprinting
        
        /// <summary>
        /// Speed when sprinting in first-person mode.
        /// </summary>
        public float FP_SprintSpeed = 6.0f;         // Speed when sprinting
        
        /// <summary>
        /// Rotation speed for the first-person camera.
        /// </summary>
        public float FP_RotationSpeed = 1.0f;       // Speed of camera rotation

        /// <summary>
        /// Smooth time for camera rotation in first-person mode.
        /// </summary>
        public float FP_SpeedChangeRate = 10.0f;    // Rate at which speed changes

        /// <summary>
        /// Maximum and minimum camera pitch angles for first-person mode.
        /// </summary>
        public float FP_TopClamp = 65.0f;           // Maximum camera pitch angle

        /// <summary>
        /// Minimum camera pitch angle for first-person mode.
        /// </summary>
        public float FP_BottomClamp = -75.0f;       // Minimum camera pitch angle

        // --- Third Person Settings ---
        [Header("Third Person Settings")]

        /// <summary>
        /// Movement speed in third-person mode when not sprinting.
        /// </summary>
        public float TP_MoveSpeed = 2.0f;

        /// <summary>
        /// Movement speed in third-person mode when sprinting.
        /// </summary>
        public float TP_SprintSpeed = 6.0f;

        /// <summary>
        /// Smoothing time for character rotation in third-person mode.
        /// Lower values make rotation more responsive.
        /// </summary>
        [Range(0.0f, 0.3f)]
        public float TP_RotationSmoothTime = 0.12f;

        /// <summary>
        /// Rate at which the character's speed changes in third-person mode.
        /// Higher values make speed changes more responsive.
        /// </summary>
        public float TP_SpeedChangeRate = 10.0f;

        /// <summary>
        /// Maximum vertical camera angle (pitch) in third-person mode.
        /// Prevents the camera from looking too far up.
        /// </summary>
        public float TP_TopClamp = 90.0f;

        /// <summary>
        /// Minimum vertical camera angle (pitch) in third-person mode.
        /// Prevents the camera from looking too far down.
        /// </summary>
        public float TP_BottomClamp = -50.0f;

        /// <summary>
        /// If true, enables camera-relative movement in third-person mode.
        /// When enabled, movement direction is based on the camera's orientation.
        /// </summary>
        public bool cameraRelativeMovementEnabled = true;

        // --- Shared Settings ---
        [Header("Jump and Gravity")]
        /// <summary>
        /// Height of the jump in units.
        /// This determines how high the player can jump.
        /// </summary>
        public float JumpHeight = 1.2f;             // Height of the jump
        
        /// <summary>
        /// Gravity applied to the player.
        /// This affects how quickly the player falls.
        /// </summary>
        public float Gravity = -9.81f;              // Gravity applied to the player (default is -9.81f, but can be adjusted for more control)
        
        /// <summary>
        /// Timeout before the player can jump again after jumping.
        /// This prevents spamming jumps.
        /// </summary>
        public float JumpTimeout = 0.1f;            // Timeout before the player can jump again
        
        /// <summary>
        /// Timeout before the player can fall again after jumping.
        /// This prevents immediate falling after jumping.
        /// </summary>
        public float FallTimeout = 0.15f;           // Timeout before the player can fall again

        [Header("Grounded Settings")]
        
        /// <summary>
        /// Whether the player is currently grounded.
        /// This is used to determine if the player can jump or not.
        /// </summary>
        public bool Grounded = true;                // Whether the player is grounded
        
        /// <summary>
        /// Offset for the grounded check, in meters.
        /// This determines how far below the player the grounded check is made.
        /// </summary>
        public float GroundedOffset = -0.14f;       // Offset for the grounded check (how far below the player the check is made)
        
        /// <summary>
        /// Radius of the sphere used for the grounded check, in meters.
        /// This determines the size of the sphere used to check if the player is grounded.
        /// </summary>
        public float GroundedRadius = 0.5f;         // Radius of the sphere used for the grounded check
        
        /// <summary>
        /// Layers considered as ground for the grounded check.
        /// This allows you to specify which layers should be treated as ground.
        /// </summary>
        public LayerMask GroundLayers;              // Layers considered as ground for the grounded check

        [Header("Cinemachine")]

        /// <summary>
        /// Transform targets for the first-person and third-person cameras.
        /// These are the points the cameras will follow.
        /// </summary>
        public Transform FirstPersonCameraTarget;   // The transform the FP camera follows (e.g., head)
        
        /// <summary>
        /// Transform target for the third-person camera.
        /// This is the point the TP camera will follow, typically behind the player.
        /// </summary>
        public Transform ThirdPersonCameraTarget;   // The transform the TP camera follows (e.g., behind player)
        // public float TopClamp = 90.0f;
        // public float BottomClamp = -90.0f;
        
        /// <summary>
        /// Override for the camera angle in third-person mode.
        /// This allows you to adjust the camera angle independently of the player's rotation.
        /// </summary>
        public float CameraAngleOverride = 0.0f;    // Override for camera angle in third person
        
        /// <summary>
        /// Whether to lock the camera position in third-person mode.
        /// If true, the camera will not move with the player.
        /// </summary>
        public bool LockCameraPosition = false;     // Whether to lock the camera position in third person

        [Header("Audio")]
        /// <summary>
        /// Audio clip played when the player lands after a jump.
        /// This is used to provide feedback when the player lands on the ground.
        /// </summary>
        public AudioClip LandingAudioClip;          // Audio clip played when landing
        
        /// <summary>
        /// Array of audio clips for footstep sounds.
        /// These are played when the player moves on the ground.
        /// </summary>
        public AudioClip[] FootstepAudioClips;      // Array of audio clips for footstep sounds
        
        /// <summary>
        /// Volume of the footstep audio.
        /// This controls how loud the footstep sounds are when played.
        /// </summary>
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f; // Volume of footstep audio

        // ... (other shared fields, audio, jump, gravity, etc.)
        private CharacterController _controller;    // reference to the character controller
        private Animator _animator;                 // reference to the animator
        private UnifiedPlayerInputs _input;         // reference to the input script
        private GameObject _mainCamera;             // reference to the main camera
        #if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;           // reference to the PlayerInput component
        #endif

        // cinemachine
        private float _cinemachineTargetYaw;        // for third person camera yaw
		private float _cinemachineTargetPitch;      // for first person camera pitch

		// player
		private float _speed;                       // the current speed of the player
		private float _animationBlend;              // the current animation blend value
		private float _rotationVelocity;            // the current rotation velocity of the player
		private float _verticalVelocity;            // the current vertical velocity of the player
		private float _terminalVelocity = 53.0f;    // the terminal velocity of the player
        // private float _targetRotation = 0.0f;       // for third person rotation

		// timeout deltatime
		private float _jumpTimeoutDelta;            // the current jump timeout delta
		private float _fallTimeoutDelta;            // the current fall timeout delta

		// animation IDs
        private int _animIDSpeed;                   // Animator parameter ID for speed
        private int _animIDGrounded;                // Animator parameter ID for grounded state
        private int _animIDJump;                    // Animator parameter ID for jump state
        private int _animIDFreeFall;                // Animator parameter ID for free fall state
        private int _animIDMotionSpeed;             // Animator parameter ID for motion speed

		private const float _threshold = 0.01f;     // threshold for input detection

		private bool _hasAnimator;                  // whether the player has an animator component

        /// <summary>
        /// Checks if the current input device is a mouse.
        /// This is used to determine how to handle input for camera rotation.
        /// </summary>
        /// <remarks>
        /// This property checks the current control scheme of the PlayerInput component
        /// to determine if the input is coming from a mouse.
        /// It is only applicable when the new Input System is enabled.
        /// Returns true if the current control scheme is "KeyboardMouse", false otherwise.
        /// </remarks>
        /// <seealso cref="_playerInput"/>
        /// <seealso cref="FirstPersonCameraRotation"/>
        /// <seealso cref="ThirdPersonCameraRotation"/>
		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}
        

        // ... (other private fields, timeouts, etc.)
        
        /// <summary>
        /// Unity Awake callback. Initializes component references, assigns animation IDs,
        /// and ensures the correct camera is enabled at the start based on the selected camera mode.
        /// </summary>
        /// <remarks>
        /// This method is called when the script instance is being loaded.
        /// It initializes the CharacterController, Animator, and input components,
        /// assigns animation IDs, and sets the initial state of the cameras.
        /// It also ensures that the correct camera is active based on the initial camera mode.
        /// </remarks>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="FirstPersonCamera"/>
        /// <seealso cref="ThirdPersonCamera"/>
        /// <seealso cref="FirstPersonCameraTarget"/>
        /// <seealso cref="ThirdPersonCameraTarget"/>
        /// <seealso cref="cameraMode"/>
        /// <seealso cref="_controller"/>
        /// <seealso cref="_animator"/>
        /// <seealso cref="_input"/>
        /// <seealso cref="_mainCamera"/>
        /// <seealso cref="_playerInput"/>
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _input = GetComponent<UnifiedPlayerInputs>();
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        #if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>(); // <-- Add this line
        #endif
            AssignAnimationIDs();
            // Ensure only the correct camera is enabled at start
            if (FirstPersonCamera != null && ThirdPersonCamera != null)
            {
                if (cameraMode == CameraMode.FirstPerson)
                {
                    FirstPersonCamera.gameObject.SetActive(true);
                    ThirdPersonCamera.gameObject.SetActive(false);
                }
                else
                {
                    FirstPersonCamera.gameObject.SetActive(false);
                    ThirdPersonCamera.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Unity Update callback. Handles camera switching, animator resets, and delegates update logic
        /// to the appropriate first-person or third-person update method based on the current camera mode.
        /// </summary>
        /// <remarks>
        /// This method checks for input to switch camera modes and updates the active camera accordingly.
        /// It also resets the animator parameters to idle state when switching cameras to prevent animation glitches.
        /// It calls the appropriate update method for the current camera mode,
        /// either <see cref="FirstPersonUpdate"/> or <see cref="ThirdPersonUpdate"/>.
        /// </remarks>
        /// <seealso cref="LateUpdate"/>
        /// <seealso cref="FirstPersonUpdate"/>
        /// <seealso cref="ThirdPersonUpdate"/>
        /// <seealso cref="FirstPersonCamera"/>
        /// <seealso cref="ThirdPersonCamera"/>
        /// <seealso cref="FirstPersonCameraTarget"/>
        /// <seealso cref="ThirdPersonCameraTarget"/>
        /// <seealso cref="SwitchCameraKey"/>
        /// <seealso cref="cameraMode"/>
        /// <seealso cref="_animator"/>
        /// <seealso cref="_input"/>
        /// <seealso cref="_hasAnimator"/>
        /// <seealso cref="FirstPersonCameraRotation"/>
        /// <seealso cref="ThirdPersonCameraRotation"/>
        /// <seealso cref="FirstPersonMove"/>
        /// <seealso cref="ThirdPersonMove"/>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="JumpAndGravity"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="cameraRelativeMovementEnabled"/>
        /// <seealso cref="CameraAngleOverride"/>
        /// <seealso cref="LockCameraPosition"/>
        private void Update()
        {
            // --- Camera Switching Logic ---
            // Checks if the camera switch key is pressed and toggles between first-person and third-person modes.
            if (Input.GetKeyDown(SwitchCameraKey))
            {
                cameraMode = cameraMode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson;

                // --- Camera Activation ---
                // Enables the correct camera and sets its follow/look targets based on the selected mode.
                if (FirstPersonCamera != null && ThirdPersonCamera != null)
                {
                    if (cameraMode == CameraMode.FirstPerson)
                    {
                        FirstPersonCamera.gameObject.SetActive(true);
                        ThirdPersonCamera.gameObject.SetActive(false);
                        FirstPersonCamera.Follow = FirstPersonCameraTarget;
                        FirstPersonCamera.LookAt = FirstPersonCameraTarget;
                    }
                    else
                    {
                        FirstPersonCamera.gameObject.SetActive(false);
                        ThirdPersonCamera.gameObject.SetActive(true);
                        ThirdPersonCamera.Follow = ThirdPersonCameraTarget;
                        ThirdPersonCamera.LookAt = ThirdPersonCameraTarget;
                        _cinemachineTargetYaw = transform.eulerAngles.y;                      
                    }
                }
                // --- Animator Reset ---
                // Resets animator parameters to idle state when switching cameras to prevent animation glitches.
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, 0f);
                    _animator.SetFloat("Speed", 0f);
                    _animator.SetFloat(_animIDMotionSpeed, 0f);
                    _animator.SetFloat("Direction", 0f); // If you use this parameter
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                    _animator.SetBool(_animIDGrounded, true);
                }
            }

            // --- Mode-Specific Update ---
            // Calls the appropriate update method for the current camera mode.
            switch (cameraMode)
            {
                case CameraMode.FirstPerson:
                    FirstPersonUpdate();
                    break;
                case CameraMode.ThirdPerson:
                    ThirdPersonUpdate();
                    break;
            }
        }

        /// <summary>
        /// Unity LateUpdate callback. Handles camera rotation and movement updates
        /// after all Update methods have been called.
        /// </summary>
        /// <remarks>
        /// This method is called after all Update methods to ensure that camera rotation
        /// and movement are applied after all other updates, allowing for smooth camera transitions.
        /// It checks the current camera mode and calls the appropriate rotation method.
        /// </remarks>
        /// <seealso cref="Update"/>
        /// <seealso cref="FirstPersonUpdate"/>
        /// <seealso cref="ThirdPersonUpdate"/>
        /// <seealso cref="FirstPersonCameraRotation"/>
        /// <seealso cref="ThirdPersonCameraRotation"/>
        private void LateUpdate()
        {
            switch (cameraMode)
            {
                case CameraMode.FirstPerson:
                    FirstPersonCameraRotation();
                    // FirstPersonMove();
                    break;
                case CameraMode.ThirdPerson:
                    ThirdPersonCameraRotation();
                    // ThirdPersonMove();
                    break;
            }
        }

        /// <summary>
        /// Handles the first-person update logic, including movement, rotation,
        /// jumping, gravity, and grounded checks.
        /// This method is called every frame when the camera mode is set to FirstPerson.
        /// </summary>
        /// <remarks>
        /// This method is responsible for processing player input for movement and camera rotation,
        /// applying gravity, checking if the player is grounded, and updating the animator parameters.
        /// It uses the assigned speed, rotation speed, and other settings defined in the class.
        /// It also handles the player's jump logic and applies gravity to the player character.
        /// </remarks>
        /// <seealso cref="ThirdPersonUpdate"/>
        /// <seealso cref="FirstPersonCameraRotation"/>
        /// <seealso cref="FirstPersonMove"/>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="JumpAndGravity"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="_animator"/>
        /// <seealso cref="_input"/>
        /// <seealso cref="_controller"/>
        /// <seealso cref="_speed"/>
        /// <seealso cref="_animationBlend"/>
        /// <seealso cref="_rotationVelocity"/>
        /// <seealso cref="_verticalVelocity"/>
        /// <seealso cref="_terminalVelocity"/>
        /// <seealso cref="_cinemachineTargetPitch"/>
        /// <seealso cref="_cinemachineTargetYaw"/>
        /// <seealso cref="_hasAnimator"/>
        private void FirstPersonUpdate()
        {
            // Place your FirstPersonController logic here (movement, rotation, animation, etc.)
            // Use FP_MoveSpeed, FP_SprintSpeed, etc.
			_hasAnimator = TryGetComponent(out _animator);

			JumpAndGravity();
			GroundedCheck();
			FirstPersonMove();
		}

        /// <summary>
        /// Assigns animation IDs for the animator parameters used in the player controller.
        /// This method is called during Awake to ensure the IDs are set up before use.
        /// </summary>
        /// <remarks>
        /// This method uses Animator.StringToHash to convert string parameter names
        /// to integer hashes for performance.
        /// </remarks>
        /// <seealso cref="_animator"/>
        /// <seealso cref="_animIDSpeed"/>
        /// <seealso cref="_animIDGrounded"/>
        /// <seealso cref="_animIDJump"/>
        /// <seealso cref="_animIDFreeFall"/>
        /// <seealso cref="_animIDMotionSpeed"/>
		private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        /// <summary>
        /// Checks if the player is grounded by performing a sphere check below the player.
        /// This method updates the Grounded property and animator state if applicable.
        /// The sphere is positioned at the player's feet with an offset defined by GroundedOffset.
        /// The radius of the sphere is defined by GroundedRadius, and it checks against the layers defined in GroundLayers.
        /// </summary>
        /// <remarks>
        /// This method is called every frame to ensure the player's grounded state is up-to-date.
        /// It uses Physics.CheckSphere to determine if the player is touching the ground.
        /// It also updates the animator's grounded state if the player has an animator component.
        /// </remarks>
        /// <seealso cref="Grounded"/>
        /// <seealso cref="GroundedOffset"/>
        /// <seealso cref="GroundedRadius"/>
        /// <seealso cref="GroundLayers"/>
        /// <seealso cref="_hasAnimator"/>
        /// <seealso cref="_animator"/>
        /// <seealso cref="_animIDGrounded"/>
        /// <seealso cref="_controller"/>
        /// <seealso cref="JumpAndGravity"/>
        /// <seealso cref="FirstPersonMove"/>
        /// <seealso cref="FirstPersonCameraRotation"/>
        /// <seealso cref="ThirdPersonCameraRotation"/>
        /// <seealso cref="ThirdPersonMove"/>
        /// <seealso cref="cameraRelativeMovementEnabled"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="OnFootstep"/>
        /// <seealso cref="OnLand"/>
        /// <seealso cref="JumpHeight"/>
        /// <seealso cref="Gravity"/>
        /// <seealso cref="JumpTimeout"/>
        /// <seealso cref="FallTimeout"/>
        /// <seealso cref="_terminalVelocity"/>
        /// <seealso cref="_speed"/>
        /// <seealso cref="_animationBlend"/>
		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

			// update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
		}

        /// <summary>
        /// Handles first-person camera rotation based on player input.
        /// This method updates the camera's pitch and the player's yaw based on mouse input.
        /// It applies clamping to the camera pitch to prevent excessive upward or downward looking.
        /// It also rotates the player character based on the input.
        /// </summary>
        /// <remarks>
        /// This method is called every frame while in first-person mode.
        /// It uses <see cref="_input"/> for look input and updates the camera target's rotation.
        /// It assumes that the FirstPersonCameraTarget is assigned in the inspector.
        /// </remarks>
        /// <seealso cref="FirstPersonMove"/>
        /// <seealso cref="JumpAndGravity"/>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="_input"/>
        /// <seealso cref="_cinemachineTargetPitch"/>
        /// <seealso cref="_rotationVelocity"/>
        /// <seealso cref="_threshold"/>
        /// <seealso cref="ClampAngle"/>
        /// <seealso cref="FirstPersonCameraTarget"/>
        /// <seealso cref="IsCurrentDeviceMouse"/>
        /// <seealso cref="FirstPersonUpdate"/>
        private void FirstPersonCameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetPitch += _input.look.y * FP_RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * FP_RotationSpeed * deltaTimeMultiplier;

                // Use FP clamp values
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, FP_BottomClamp, FP_TopClamp);

                if (FirstPersonCameraTarget != null)
                {
                    FirstPersonCameraTarget.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                }
                else
                {
                    Debug.LogWarning("FirstPersonCameraTarget is not assigned in the inspector!");
                }
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        /// <summary>
        /// Handles first-person movement based on player input.
        /// Calculates the target speed based on sprinting, adjusts the player's speed,
        /// combines input direction with player orientation, and moves the character controller.
        /// Also updates animator parameters for speed and direction.
        /// </summary>
        /// <remarks>
        /// This method is called every frame while in first-person mode. It uses <see cref="_input"/> for movement and sprint input,
        /// and updates the <see cref="_controller"/> and <see cref="_animator"/> components accordingly.
        /// </remarks>
        /// <seealso cref="FirstPersonCameraRotation"/>
        /// <seealso cref="JumpAndGravity"/>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="_input"/>
        /// <seealso cref="_controller"/>
        /// <seealso cref="_animator"/>
        /// <seealso cref="FP_MoveSpeed"/>
        /// <seealso cref="FP_SprintSpeed"/>
        /// <seealso cref="FP_RotationSpeed"/>
        /// <seealso cref="FP_SpeedChangeRate"/>
        /// <seealso cref="FP_TopClamp"/>
        /// <seealso cref="FP_BottomClamp"/>
        /// <seealso cref="_speed"/>
        /// <seealso cref="ClampAngle"/>
        private void FirstPersonMove()
        {
            float targetSpeed = _input.sprint ? FP_SprintSpeed : FP_MoveSpeed;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // Smoothly adjust speed to target
            if (Mathf.Abs(_speed - targetSpeed * inputMagnitude) > speedOffset)
            {
                _speed = Mathf.MoveTowards(_speed, targetSpeed * inputMagnitude, FP_SpeedChangeRate * Time.deltaTime);
            }
            else
            {
                _speed = targetSpeed * inputMagnitude;
            }

            // Blend animation for smooth transitions
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * FP_SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // Combine input with player orientation
            Vector3 inputDirection = (transform.right * _input.move.x + transform.forward * _input.move.y);
            if (inputDirection.sqrMagnitude > 0.01f)
                inputDirection = inputDirection.normalized;
            else
                inputDirection = Vector3.zero;

            // Move the character controller
            _controller.Move(inputDirection * (_speed * inputMagnitude * Time.deltaTime) +
                            new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // Update animator parameters if present
            if (_hasAnimator)
            {
                float normalizedBlend = 0f;
                if (_animationBlend > 0f)
                {
                    if (_animationBlend <= TP_MoveSpeed)
                        normalizedBlend = Mathf.InverseLerp(0f, TP_MoveSpeed, _animationBlend) * 0.5f;
                    else
                        normalizedBlend = 0.5f + Mathf.InverseLerp(TP_MoveSpeed, TP_SprintSpeed, _animationBlend) * 0.5f;
                }
                normalizedBlend = Mathf.Round(normalizedBlend * 100f) / 100f;
                _animator.SetFloat("Speed", normalizedBlend);

                // Use the actual input value for smooth blending
                _animator.SetFloat("Direction", _input.move.y);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        /// <summary>
        /// Handles jumping and gravity for the player.
        /// Calculates vertical velocity based on jump height and gravity,
        /// updates grounded state, and manages jump and fall timeouts.
        /// This method is called every frame to apply gravity and handle jumping logic.
        /// </summary>
        /// <remarks>
        /// This method checks if the player is grounded, applies gravity over time,
        /// and allows the player to jump if they are grounded and the jump timeout has elapsed.
        /// It also updates the animator parameters for jumping and free fall states.
        /// Modifies <see cref="_verticalVelocity"/>, <see cref="_jumpTimeoutDelta"/>, <see cref="_fallTimeoutDelta"/>, and animator parameters.
        /// Assumes <see cref="GroundedCheck"/> has been called this frame to update the <see cref="Grounded"/> property.
        /// </remarks>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="FirstPersonMove"/>
        /// <seealso cref="ThirdPersonMove"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="OnFootstep"/>
        /// <seealso cref="OnLand"/>
        /// <seealso cref="FootstepAudioClips"/>
        /// <seealso cref="FootstepAudioVolume"/>
        /// <seealso cref="JumpHeight"/>
        /// <seealso cref="Gravity"/>
        /// <seealso cref="JumpTimeout"/>
        /// <seealso cref="FallTimeout"/>
        /// <seealso cref="GroundedOffset"/>
        /// <seealso cref="GroundedRadius"/>
        /// <seealso cref="GroundLayers"/>
        /// <seealso cref="_terminalVelocity"/>
        /// <seealso cref="_hasAnimator"/>
        /// <seealso cref="_animIDJump"/>
        private void JumpAndGravity()
        {
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
				}

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
					
					// update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}
				else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

        /// <summary>
        /// Clamps an angle to a specified range.
        /// This method ensures the angle is within the specified minimum and maximum values,
        /// and wraps it around if it exceeds 360 degrees.
        /// /// </summary>
        /// <param name="lfAngle">The angle to clamp.</param>
        /// <param name="lfMin">The minimum angle value.</param>
        /// <param name="lfMax">The maximum angle value.</param>
        /// <returns>The clamped angle within the specified range.</returns>
        /// <remarks>
        /// This method is useful for ensuring angles remain within a valid range,
        /// especially for camera rotations or character orientations.
        /// It prevents angles from exceeding 360 degrees or going below -360 degrees,
        /// and clamps them to the specified minimum and maximum values.
        /// </remarks>
		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

        /// <summary>
        /// Unity editor callback that draws gizmos when the object is selected.
        /// Visualizes the grounded check sphere at the player's feet, color-coded by grounded state.
        /// </summary>
        /// <remarks>
        /// This method is called by Unity only in the editor when the GameObject is selected.
        /// The gizmo helps visualize the position and radius of the grounded check,
        /// using green when grounded and red when not grounded.
        /// </remarks>
		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}

        /// <summary>
        /// Unity Animation Event callback for footstep sounds.
        /// Plays a random footstep audio clip when the footstep animation event is triggered.
        /// </summary>
        /// <param name="animationEvent">The animation event that triggered this callback.</param>
        /// <remarks>
        /// This method is called by Unity when the footstep animation event occurs.
        /// It checks the weight of the animation clip to ensure it is significant before playing a sound.
        /// </remarks>
        /// <seealso cref="OnLand"/>
        /// <seealso cref="FootstepAudioClips"/>
        /// <seealso cref="FootstepAudioVolume"/>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="FirstPersonUpdate"/>
        /// <seealso cref="ThirdPersonUpdate"/>
		private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        /// <summary>
        /// Unity Animation Event callback for landing sounds.
        /// Plays the landing audio clip when the landing animation event is triggered.
        /// </summary>
        /// <param name="animationEvent">The animation event that triggered this callback.</param>
        /// <remarks>
        /// This method is called by Unity when the landing animation event occurs.
        /// It checks the weight of the animation clip to ensure it is significant before playing a sound.
        /// </remarks>
        /// <seealso cref="OnFootstep"/>
        /// <seealso cref="LandingAudioClip"/>
        /// <seealso cref="FootstepAudioVolume"/>
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        /// <summary>
        /// Handles the third-person update logic, including movement, rotation,
        /// jumping, gravity, and grounded checks.
        /// This method is called every frame when the camera mode is set to ThirdPerson.
        /// </summary>
        /// <remarks>
        /// This method contains the logic for third-person movement, camera rotation,
        /// and jumping. It uses the same principles as FirstPersonUpdate but applies them
        /// to third-person controls. It also handles camera-relative movement if enabled.
        /// </remarks>
        /// <seealso cref="FirstPersonUpdate"/>
        /// <seealso cref="ThirdPersonCameraRotation"/>
        /// <seealso cref="ThirdPersonMove"/>
        /// <seealso cref="JumpAndGravity"/>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="OnFootstep"/>
        /// <seealso cref="OnLand"/>
        /// <seealso cref="cameraRelativeMovementEnabled"/>
        private void ThirdPersonUpdate()
        {
            // Place your ThirdPersonController logic here (movement, rotation, animation, etc.)
            // Use TP_MoveSpeed, TP_SprintSpeed, etc.
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            ThirdPersonMove();
        }

        /// <summary>
        /// Handles third-person camera rotation based on player input.
        /// This method updates the camera's yaw and pitch based on mouse input,
        /// applies clamping to the camera pitch, and sets the camera's rotation.
        /// </summary>
        /// <remarks>
        /// This method is called every frame while in third-person mode.
        /// It uses the input from the player to adjust the camera's yaw and pitch,
        /// and applies clamping to the pitch to prevent excessive upward or downward looking.
        /// It also sets the rotation of the third-person camera target based on the calculated angles.
        /// </remarks>
        /// <seealso cref="ClampAngle"/>
        /// <seealso cref="ThirdPersonMove"/>
        /// <seealso cref="cameraRelativeMovementEnabled"/>
        private void ThirdPersonCameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // Use TP clamp values
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, TP_BottomClamp, TP_TopClamp);

            ThirdPersonCameraTarget.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        /// <summary>
        /// Handles third-person movement based on player input.
        /// Calculates the target speed based on sprinting, adjusts the player's speed,
        /// combines input direction with camera orientation,
        /// and moves the character controller.
        /// Also updates animator parameters for speed and direction.
        /// </summary>
        /// <remarks>
        /// This method is called every frame while in third-person mode.
        /// It uses <see cref="_input"/> for movement and sprint input,
        /// and updates the <see cref="_controller"/> and <see cref="_animator"/> components accordingly.
        /// It supports camera-relative movement if enabled, allowing the player to move in the direction of the camera.
        /// </remarks>
        /// <seealso cref="ClampAngle"/>
        /// <seealso cref="ThirdPersonCameraRotation"/>
        /// <seealso cref="JumpAndGravity"/>
        /// <seealso cref="GroundedCheck"/>
        /// <seealso cref="AssignAnimationIDs"/>
        /// <seealso cref="OnFootstep"/>
        /// <seealso cref="OnLand"/>
        /// <seealso cref="FirstPersonMove"/>
        /// <seealso cref="FirstPersonUpdate"/>
        /// <seealso cref="ThirdPersonUpdate"/>
        /// <seealso cref="cameraRelativeMovementEnabled"/>
        /// <seealso cref="TP_MoveSpeed"/>
        /// <seealso cref="TP_SprintSpeed"/>
        /// <seealso cref="TP_RotationSmoothTime"/>
        /// <seealso cref="TP_SpeedChangeRate"/>
        /// <seealso cref="TP_TopClamp"/>
        /// <seealso cref="TP_BottomClamp"/>
        /// <seealso cref="CameraMode"/>
        private void ThirdPersonMove()
        {
            float targetSpeed = _input.sprint ? TP_SprintSpeed : TP_MoveSpeed;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (Mathf.Abs(_speed - targetSpeed * inputMagnitude) > speedOffset)
            {
                _speed = Mathf.MoveTowards(_speed, targetSpeed * inputMagnitude, FP_SpeedChangeRate * Time.deltaTime);
            }
            else
            {
                _speed = targetSpeed * inputMagnitude;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * TP_SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // --- CAMERA RELATIVE MOVEMENT FOR THIRD PERSON ---
            Vector3 inputDirection = Vector3.zero;
            Vector3 moveInput = new Vector3(_input.move.x, 0.0f, _input.move.y);

            // Add this field to your class if not present:
            // public bool cameraRelativeMovementEnabled = true;

            if (cameraRelativeMovementEnabled)
            {
                // Camera-relative movement
                Vector3 camForward = _mainCamera.transform.forward;
                Vector3 camRight = _mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                // If moving backward, flip the input direction so the character always faces away from the camera,
                // but movement and rotation are still camera-relative
                bool movingBackward = _input.move.y < -0.01f;
                Vector3 inputDir = movingBackward
                    ? (camRight * moveInput.x - camForward * moveInput.z).normalized // flip Z for backward
                    : (camRight * moveInput.x + camForward * moveInput.z).normalized;

                if (_input.move != Vector2.zero && Mathf.Abs(_input.move.y) > 0.01f)
                {
                    float desiredRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, desiredRotation, ref _rotationVelocity, TP_RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }

                // Use the original input direction for movement (so the character walks backward when moving backward)
                inputDirection = (camRight * moveInput.x + camForward * moveInput.z).normalized;
            }
            else
            {
                // World-relative movement
                if (_input.move != Vector2.zero)
                {
                    Vector3 inputDir = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                    float desiredRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, desiredRotation, ref _rotationVelocity, TP_RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                    inputDirection = inputDir;
                }
            }

            _controller.Move(inputDirection.normalized * (_speed * inputMagnitude * Time.deltaTime) +
                            new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                // Normalize animation blend for blend tree (0 = idle, 0.5 = walk, 1 = run)
                float normalizedBlend = 0f;
                if (_animationBlend > 0f)
                {
                    if (_animationBlend <= TP_MoveSpeed)
                        normalizedBlend = Mathf.InverseLerp(0f, TP_MoveSpeed, _animationBlend) * 0.5f;
                    else
                        normalizedBlend = 0.5f + Mathf.InverseLerp(TP_MoveSpeed, TP_SprintSpeed, _animationBlend) * 0.5f;
                }
                normalizedBlend = Mathf.Round(normalizedBlend * 100f) / 100f;
                _animator.SetFloat("Speed", normalizedBlend);
                _animator.SetFloat("Direction", _input.move.y);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }
    }
}