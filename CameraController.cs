using System;
using Godot;

public partial class CameraController : SpringArm3D
{
  [Export] Node3D Pivot;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
    if (Pivot != null)
    {
      GlobalPosition = Pivot.GlobalPosition;
    }
  }
}
