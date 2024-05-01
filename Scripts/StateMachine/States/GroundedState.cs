using Godot;
using GodotUtilities;
using System;
using System.Diagnostics;
using static MovementComponent;

namespace BushyCore 
{
	[Scene]
	public partial class GroundedState : BaseState
	{
		private double horizontalVelocity;
		private double verticalVelocity;

		[Node]
		private RayCast2D GroundRayCastL;
		[Node]
		private RayCast2D GroundRayCastR;

		public override void _Ready()
		{
			base._Ready();

            this.AddToGroup();
            this.WireNodes();

			GroundRayCastL.Enabled = false;
			GroundRayCastR.Enabled = false;
		}

		public override void StateEnter(params StateConfig.IBaseStateConfig[] configs)
		{
			base.StateEnter(configs);

			actionsComponent.CanJump = actionsComponent.CanDash = true;
			movementComponent.Velocities[VelocityType.Gravity] = Vector2.Zero;

			horizontalVelocity = movementComponent.Velocities[VelocityType.MainMovement].X;
		}

		public override void StateUpdateInternal(double delta)
		{
			// Having a downwards velocity constantly helps snapping the character to the ground
			// We have to keep in mind that while using move and slide this WILL impact the characterś movement horizontally
			verticalVelocity = -10f;

			HandleMovement(delta);
			CheckTransitions();

			var slopeVerticalComponent = Mathf.Tan(movementComponent.FloorAngle) * (float) horizontalVelocity;
			movementComponent.Velocities[VelocityType.Gravity] = movementComponent.FloorNormal * (float) verticalVelocity * 10;
			movementComponent.Velocities[VelocityType.MainMovement] = new Vector2((float)horizontalVelocity, slopeVerticalComponent);
		}

		void CheckTransitions()
		{
			if (actionsComponent.IsJumpRequested)
			{
				actionsComponent.Jump();
			}
			
			if (movementComponent.SnappedToFloor) return;
			
			movementComponent.Velocities[VelocityType.Gravity] = Vector2.Zero;
			Debug.WriteLine("CAUSE IM FREEEE FALLING");
			actionsComponent.Fall();
		}

		void HandleMovement(double deltaTime)
		{
			Vector2 direction = actionsComponent.MovementDirection;
			var vars = characterVariables;
			if (direction.X != 0)
			{
				horizontalVelocity += direction.X * vars.GroundHorizontalAcceleration * deltaTime;
				//if the input direction is opposite of the current direction, we also add a deceleration
				if (direction.X * horizontalVelocity < 0)
				{
					horizontalVelocity += direction.X * vars.GroundHorizontalTurnDeceleration * deltaTime;
				}
			}
			else //if we're not doing any input, we decelerate to 0
			{
				var deceleration = vars.GroundHorizontalDeceleration * deltaTime * (horizontalVelocity > 0 ? -1 : 1);
				if (Mathf.Abs(deceleration) < Mathf.Abs(horizontalVelocity))
				{
					horizontalVelocity += deceleration;
				}
				else
				{
					horizontalVelocity = 0;
				}
			}
			horizontalVelocity = Mathf.Clamp(horizontalVelocity, -vars.GroundHorizontalMovementSpeed, vars.GroundHorizontalMovementSpeed);
		}
	}

}
