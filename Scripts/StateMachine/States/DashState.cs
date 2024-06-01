using BushyCore;
using Godot;
using System;
using GodotUtilities;
using static MovementComponent;
using System.Diagnostics;

namespace BushyCore
{
	[Scene]
	public partial class DashState : BaseState
	{
		private Vector2 constantVelocity;
		private float direction;
		[Node]
    	private Timer DurationTimer;
		private int state;
		Vector2 destination;
	
		public override void _Notification(int what)
        {
            if (what == NotificationSceneInstantiated)
            {
                this.AddToGroup();
                this.WireNodes();
            }
        }

		protected override void StateEnterInternal(params StateConfig.IBaseStateConfig[] configs)
		{
			SetupFromConfigs(configs);
			direction = movementComponent.FacingDirection.Normalized().X;
			movementComponent.Velocities[VelocityType.Gravity] = Vector2.Zero;
			state = 0;
			actionsComponent.JumpActionStart += JumpActionRequested;
			actionsComponent.CanDash = false;
			DurationTimer.WaitTime = characterVariables.DashInitTime;
			DurationTimer.Start();
		}

		public override void StateExit()
        {
            base.StateExit();
			actionsComponent.JumpActionStart -= JumpActionRequested;
        }
		private void SetupFromConfigs(StateConfig.IBaseStateConfig[] configs)
		{
			foreach (var config in configs)
			{
				if (config is StateConfig.InitialVelocityVectorConfig velocityConfig)
				{
					constantVelocity = velocityConfig.Velocity.Normalized() * this.characterVariables.DashVelocity;
				}
			}
		}
    	public override void StateUpdateInternal(double delta)
    	{
			if(state > 0)
			{
				var slopeVerticalComponent = Mathf.Tan(movementComponent.FloorAngle) * constantVelocity.X;
				movementComponent.Velocities[VelocityType.MainMovement] = new Vector2(constantVelocity.X * direction, slopeVerticalComponent);
			}
    	}

    	protected override void VelocityUpdate() {}

		public void JumpActionRequested()
		{
			if (actionsComponent.CanJump)
			{
				DurationTimer.Stop();
				movementComponent.Velocities[VelocityType.MainMovement] = new Vector2(characterVariables.DashJumpSpeed * direction,0);
				RunAndEndState(() => actionsComponent.Jump(this.characterVariables.DashJumpSpeed));
			}
		} 

		void _on_duration_timer_timeout()
		{
			GD.Print("time");
			switch(state)
			{
			case 0:
					DurationTimer.Stop();
					DurationTimer.WaitTime = characterVariables.DashTime;
					constantVelocity.X = characterVariables.DashVelocity;
					animationComponent.Play("dash_end");
					DurationTimer.Start();
					break;
			case 1:
					DurationTimer.Stop();
					constantVelocity.X = characterVariables.DashExitVelocity;
					DurationTimer.WaitTime = characterVariables.DashExitTime;
					DurationTimer.Start();
					break;
			case 2:
					DurationTimer.Stop();
					RunAndEndState(() => actionsComponent.Fall());
					break;
			}
			state ++;
		}
	}
}