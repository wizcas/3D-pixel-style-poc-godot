using System;
using Godot;

public partial class Player : CharacterBody3D
{
  [Export] public float MoveSpeedScale = 1.5f;
  [Export] public float JumpVelocity = 4.5f;

  // Get the gravity from the project settings to be synced with RigidBody nodes.
  public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
  private Node3D _rig;
  private AnimationTree _animationTree;
  private Transform3D _rigT = new Transform3D();

  private Transform3D pt => GlobalTransform;
  private Camera3D _cam;

  private Vector3 _lookDirection;
  private Vector2 _lastMousePos;
  private ImmediateMesh _debugMesh = new ImmediateMesh();

  public override void _Ready()
  {
    base._Ready();
    _animationTree = GetNode<AnimationTree>("AnimationTree");
    SetMoveAnimationVector(Vector2.Zero);
    _rig = GetNode<Node3D>("Model");
    _rigT = _rig.Transform;
    _rigT.origin = Vector3.Zero;
    _cam = GetViewport().GetCamera3d();
  }

  public override void _Process(double delta)
  {
    base._Process(delta);
    var mouseVP = GetViewport().GetMousePosition();
    var lookVector = Input.GetVector("look_left", "look_right", "look_up", "look_down");
    if (lookVector != Vector2.Zero)
    {
      GD.Print("look v:", lookVector);
      _lookDirection = -new Vector3(lookVector.x, 0, lookVector.y).Normalized();
    }
    else if (!_lastMousePos.IsEqualApprox(mouseVP))
    {
      var thisVP = _cam.UnprojectPosition(GlobalPosition);
      var screenDir = (thisVP - mouseVP).Normalized();
      _lookDirection = new Vector3(screenDir.x, 0, screenDir.y);
    }


    var start = GlobalPosition + Vector3.Up * .1f;
    _debugMesh.ClearSurfaces();
    _debugMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
    _debugMesh.SurfaceSetColor(Colors.Red);
    _debugMesh.SurfaceAddVertex(start);
    _debugMesh.SurfaceAddVertex(start + _lookDirection * 10);
    _debugMesh.SurfaceEnd();
  }

  public override void _PhysicsProcess(double delta)
  {
    if (_lookDirection != Vector3.Zero)
    {
      LookAt(GlobalPosition + _lookDirection, Vector3.Up);
    }
    // global rotation
    var basis = GlobalTransform.basis;

    // Adjust the input vector to blend2D position
    Vector2 inputVector = Input.GetVector("move_left", "move_right", "move_up", "move_down");

    // Rotate the input vector so that the directions are relative to
    //  the world's coordinate system rather than the character's.
    var worldMoveVector = basis * new Vector3(inputVector.x, 0, -inputVector.y);
    var animMoveVector = new Vector2(worldMoveVector.x, worldMoveVector.z);

    // Play the animation to calculate the root motion.
    SetMoveAnimationVector(animMoveVector);
    SetMoveAnimationScale(MoveSpeedScale);

    Vector3 velocity = Velocity;

    var rootMotion = _animationTree.GetRootMotionTransform();
    // the root motion has initial scale of 2.0. why?
    // Orthonormalize it to remove the scale.
    _rigT = (_rigT * rootMotion).Orthonormalized();

    // Translate horizontally by the root motion offset.
    velocity = (basis * (_rigT.origin - _rig.Transform.origin) / (float)delta) + Vector3.Up * velocity.y;
    // Add the gravity.
    if (!IsOnFloor())
      velocity.y -= gravity * (float)delta;
    // Handle Jump.
    if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
      velocity.y = JumpVelocity;

    Velocity = velocity;
    MoveAndSlide();

    // Reset the root motion offset so that it won't accumulate in the next frame.
    _rigT.origin = Vector3.Zero;
    // Apply rig's rotation.
    _rigT.Orthonormalized();
    _rig.Transform = new Transform3D(_rigT.basis, _rigT.origin);
  }

  private void SetMoveAnimationVector(Vector2 v)
  {
    _animationTree.Set("parameters/blend-move/blend_position", v);
  }
  private void SetMoveAnimationScale(float scale)
  {
    _animationTree.Set("parameters/move-speed/scale", MoveSpeedScale);
  }
}
