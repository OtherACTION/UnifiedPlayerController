# UnifiedPlayerController

This is a Unified Player Controller for Unity 6.
This includes a Unified Player Controller that has both a First Person and Third Person control settings all into one unified script with an added hardcoded SwitchCamera function that the key can be changed in the Inspector window with many other settings.

# Features

-UnifiedPlayerController-

+ Customizable Player Speed for First/Third Person separately.
+ Camera switcher via hardcoded key (default = C).
+ First/Third Person controller.
+ Adjustable jump height & gravity settings (default -9.81 to simulate real world gravity).
+ Assign your CinemachineCamera for First/Third Person.

-DynamicFollowHead-

+ Dynamically follows the head of the character (can be offset for fine tuning).
+ First Person Camera will follow the animation so it stays consistent.
+ Smooth slider to adjust how much the camera is affected (higher the value, the more closely the camera follows the animation).

-ThirdPersonZoom-

+ Uses the Cinemachine Third Person Follow component.
+ Mainly used for Third Person camera.
+ Adjusts the distance the camera is away from the character (using Scroll Wheel).

# ScreenShots

![UnifiedPlayerController](https://github.com/user-attachments/assets/84862083-b17a-496a-be24-74fe9070ef1c)
![DynamicFollowHead](https://github.com/user-attachments/assets/b71f3711-1815-40de-b7d1-2cb38e04252f)
![ThirdPersonCameraZoom](https://github.com/user-attachments/assets/292b05b5-60b4-4573-a2a4-31159be4a4e4)


# Includes

UnifiedController

Camera
* DynamicFollowHead.cs
* ThirdPersonCameraZoom.cs

Controller
* UnifiedPlayerController.cs

Inputs
* StarterAssets.inputactions
* StarterAssetsInputs.cs

Prefab
* Camera Root.prefab
* FP Camera.prefab
* Main Camera.prefab
* TP Camera.prefab

# License

This has been released under the [MIT License](https://github.com/OtherACTION/UnifiedPlayerController/blob/main/LICENSE).
