using UnityEngine;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UnifiedPlayerController
{
    public enum CameraMode { FirstPerson, ThirdPerson }

    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class UnifiedPlayerController : MonoBehaviour
    {
        [Header("General")]
        public CameraMode cameraMode = CameraMode.FirstPerson; // Default camera mode
        public CinemachineCamera FirstPersonCamera; // References to your Cinemachine cameras
        public CinemachineCamera ThirdPersonCamera; // References to your Cinemachine cameras
        public KeyCode SwitchCameraKey = KeyCode.C; // Key to switch between camera modes

        [Header("First Person Settings")]
        public float FP_MoveSpeed = 2.0f;           // Speed when not sprinting
        public float FP_SprintSpeed = 6.0f;         // Speed when sprinting
        public float FP_RotationSpeed = 1.0f;       // Speed of camera rotation
        public float FP_SpeedChangeRate = 10.0f;    // Rate at which speed changes
        public float FP_TopClamp = 65.0f;           // Maximum camera pitch angle
        public float FP_BottomClamp = -75.0f;       // Minimum camera pitch angle

        [Header("Third Person Settings")]
        public float TP_MoveSpeed = 2.0f;           // Speed when not sprinting
        public float TP_SprintSpeed = 6.0f;         // Speed when sprinting
        [Range(0.0f, 0.3f)]
        public float TP_RotationSmoothTime = 0.12f; // Smooth time for rotation
        public float TP_SpeedChangeRate = 10.0f;    // Rate at which speed changes
        public float TP_TopClamp = 90.0f;           // Maximum camera pitch angle for third person
        public float TP_BottomClamp = -50.0f;       // Minimum camera pitch angle for third person

        [Header("Jump and Gravity")]
        public float JumpHeight = 1.2f;             // Height of the jump
        public float Gravity = -9.81f;              // Gravity applied to the player (default is -9.81f, but can be adjusted for more control)
        public float JumpTimeout = 0.1f;            // Timeout before the player can jump again
        public float FallTimeout = 0.15f;           // Timeout before the player can fall again

        [Header("Grounded Settings")]
        public bool Grounded = true;                // Whether the player is grounded
        public float GroundedOffset = -0.14f;       // Offset for the grounded check (how far below the player the check is made)
        public float GroundedRadius = 0.5f;         // Radius of the sphere used for the grounded check
        public LayerMask GroundLayers;              // Layers considered as ground for the grounded check

        [Header("Cinemachine")]
        public Transform FirstPersonCameraTarget;   // The transform the FP camera follows (e.g., head)
        public Transform ThirdPersonCameraTarget;   // The transform the TP camera follows (e.g., behind player)
        // public float TopClamp = 90.0f;
        // public float BottomClamp = -90.0f;
        public float CameraAngleOverride = 0.0f;    // Override for camera angle in third person
        public bool LockCameraPosition = false;     // Whether to lock the camera position in third person

        [Header("Audio")]
        public AudioClip LandingAudioClip;          // Audio clip played when landing
        public AudioClip[] FootstepAudioClips;      // Array of audio clips for footstep sounds
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
        private float _targetRotation = 0.0f;       // for third person rotation

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

        private void Update()
        {
            // Camera Switching Logic
            if (Input.GetKeyDown(SwitchCameraKey))
            {
                cameraMode = cameraMode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson;

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
                    }
                }
                // Reset animator parameters to idle
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

        private void FirstPersonUpdate()
        {
            // Place your FirstPersonController logic here (movement, rotation, animation, etc.)
            // Use FP_MoveSpeed, FP_SprintSpeed, etc.
			_hasAnimator = TryGetComponent(out _animator);

			JumpAndGravity();
			GroundedCheck();
			FirstPersonMove();
		}

		private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

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

        private void FirstPersonCameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetPitch += _input.look.y * FP_RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * FP_RotationSpeed * deltaTimeMultiplier;

                // Use FP clamp values
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, FP_BottomClamp, FP_TopClamp);

                FirstPersonCameraTarget.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

		private void FirstPersonMove()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? FP_SprintSpeed : FP_MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * FP_SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * FP_SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

			// update animator if using character
            if (_hasAnimator)
            {

				// Normalize animation blend for blend tree (0 = idle, 0.5 = walk, 1 = run)
				float normalizedBlend = 0f;
				if (_animationBlend > 0f)
				{
    				if (_animationBlend <= FP_MoveSpeed)
        				normalizedBlend = Mathf.InverseLerp(0f, FP_MoveSpeed, _animationBlend) * 0.5f;
    				else
        				normalizedBlend = 0.5f + Mathf.InverseLerp(FP_MoveSpeed, FP_SprintSpeed, _animationBlend) * 0.5f;
				}
				_animator.SetFloat("Speed", normalizedBlend);
				// After calculating normalizedBlend
				Debug.Log($"_animationBlend: {_animationBlend}, normalizedBlend: {normalizedBlend}, FP_MoveSpeed: {FP_MoveSpeed}, FP_SprintSpeed: {FP_SprintSpeed}");
				// Check if the player is moving backwards.
				// Adjust the time scale (speed) of the animator based on input.
				float direction = 0f;
				if (_input.move.y > 0.01f)
					direction = 1f;
				else if (_input.move.y < -0.01f)
					direction = -1f;
				// Optionally, for strafing, you could use _input.move.x as well

				_animator.SetFloat("Direction", direction); // Make sure to have a "Direction" parameter in your animator.
				
				// Now update your animator parameters as usual.
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
		}

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

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
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

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }


        private void ThirdPersonUpdate()
        {
            // Place your ThirdPersonController logic here (movement, rotation, animation, etc.)
            // Use TP_MoveSpeed, TP_SprintSpeed, etc.
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            ThirdPersonMove();
        }

        // private void AssignAnimationIDs()
        // {
        //     _animIDSpeed = Animator.StringToHash("Speed");
        //     _animIDGrounded = Animator.StringToHash("Grounded");
        //     _animIDJump = Animator.StringToHash("Jump");
        //     _animIDFreeFall = Animator.StringToHash("FreeFall");
        //     _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        // }

        // private void GroundedCheck()
        // {
        //     // set sphere position, with offset
        //     Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
        //         transform.position.z);
        //     Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
        //         QueryTriggerInteraction.Ignore);

        //     // update animator if using character
        //     if (_hasAnimator)
        //     {
        //         _animator.SetBool(_animIDGrounded, Grounded);
        //     }
        // }

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

        private void ThirdPersonMove()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? TP_SprintSpeed : TP_MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * TP_SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * TP_SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    TP_RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        // private void JumpAndGravity()
        // {
        //     if (Grounded)
        //     {
        //         // reset the fall timeout timer
        //         _fallTimeoutDelta = FallTimeout;

        //         // update animator if using character
        //         if (_hasAnimator)
        //         {
        //             _animator.SetBool(_animIDJump, false);
        //             _animator.SetBool(_animIDFreeFall, false);
        //         }

        //         // stop our velocity dropping infinitely when grounded
        //         if (_verticalVelocity < 0.0f)
        //         {
        //             _verticalVelocity = -2f;
        //         }

        //         // Jump
        //         if (_input.jump && _jumpTimeoutDelta <= 0.0f)
        //         {
        //             // the square root of H * -2 * G = how much velocity needed to reach desired height
        //             _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

        //             // update animator if using character
        //             if (_hasAnimator)
        //             {
        //                 _animator.SetBool(_animIDJump, true);
        //             }
        //         }

        //         // jump timeout
        //         if (_jumpTimeoutDelta >= 0.0f)
        //         {
        //             _jumpTimeoutDelta -= Time.deltaTime;
        //         }
        //     }
        //     else
        //     {
        //         // reset the jump timeout timer
        //         _jumpTimeoutDelta = JumpTimeout;

        //         // fall timeout
        //         if (_fallTimeoutDelta >= 0.0f)
        //         {
        //             _fallTimeoutDelta -= Time.deltaTime;
        //         }
        //         else
        //         {
        //             // update animator if using character
        //             if (_hasAnimator)
        //             {
        //                 _animator.SetBool(_animIDFreeFall, true);
        //             }
        //         }

        //         // if we are not grounded, do not jump
        //         _input.jump = false;
        //     }

        //     // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        //     if (_verticalVelocity < _terminalVelocity)
        //     {
        //         _verticalVelocity += Gravity * Time.deltaTime;
        //     }
        // }

        // private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        // {
        //     if (lfAngle < -360f) lfAngle += 360f;
        //     if (lfAngle > 360f) lfAngle -= 360f;
        //     return Mathf.Clamp(lfAngle, lfMin, lfMax);
        // }

        // private void OnDrawGizmosSelected()
        // {
        //     Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        //     Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        //     if (Grounded) Gizmos.color = transparentGreen;
        //     else Gizmos.color = transparentRed;

        //     // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        //     Gizmos.DrawSphere(
        //         new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
        //         GroundedRadius);
        // }

        // private void OnFootstep(AnimationEvent animationEvent)
        // {
        //     if (animationEvent.animatorClipInfo.weight > 0.5f)
        //     {
        //         if (FootstepAudioClips.Length > 0)
        //         {
        //             var index = Random.Range(0, FootstepAudioClips.Length);
        //             AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
        //         }
        //     }
        // }

        // private void OnLand(AnimationEvent animationEvent)
        // {
        //     if (animationEvent.animatorClipInfo.weight > 0.5f)
        //     {
        //         AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        //     }
        // }
        // You can add helper methods for shared logic, jumping, gravity, etc.
    }
}