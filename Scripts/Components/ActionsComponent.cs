using System.Diagnostics;
using BushyCore;
using Godot;

// The main idea behind this is to abstract how a StateMachine will obtain commands to transition, and also to mock it's inputs
// In the future, we can use this to inject behavior into an enemy's state machine, or any other npc. This base class is meant
// to be as generic as possible and will only have basic movement logic. We can assume most state machines will have states for
// Ground, Jump, Fall and possibly dash. Since all these can be modified via CharacterVariables, we can reuse this logic
// Input Managers will update the ACtionsComponent
public partial class ActionsComponent : Node
{
	public bool CanJump { get; set; }
	public bool CanDash { get; set; }
	public double LastDashTime { get; set; }

	[Signal]
	public delegate void JumpActionStartEventHandler();
	[Signal]
	public delegate void JumpActionEndEventHandler();
	[Signal]
	public delegate void DashActionStartEventHandler();
	[Signal]
	public delegate void DashActionEndEventHandler();
	[Signal]
	public delegate void MovementDirectionChangeEventHandler(Vector2 vector2);
	[Signal]
	public delegate void AttackActionStartEventHandler();

	private Vector2 _MovementDirection;
	public Vector2 MovementDirection { 
		get { return _MovementDirection; }
		set { 
			_MovementDirection = value;
			EmitSignal(SignalName.MovementDirectionChange, value);
		} 
	}

	private bool _IsJumpRequested;
	public bool IsJumpRequested { 
		get { return _IsJumpRequested; } 
		set {
			_IsJumpRequested = value;

			if (_IsJumpRequested) 
				EmitSignal(SignalName.JumpActionStart);
		} 
	}
	private bool _IsJumpCancelled;
	public bool IsJumpCancelled {
		get { return _IsJumpCancelled; }
		set {
			_IsJumpCancelled = value;
			if (value) EmitSignal(SignalName.JumpActionEnd);
		}
	}
	public bool IsSpeeding { get; private set;}

	private bool _IsDashRequested;
	public bool IsDashRequested { 
		get { return _IsDashRequested; } 
		set {
			_IsDashRequested = value;
		
			if (value)
				IsSpeeding = value;
			
			if (_IsDashRequested) 
				EmitSignal(SignalName.DashActionStart);
		} 
	}

	private bool _IsDashCancelled;
	public bool IsDashCancelled { 
		get { return _IsDashCancelled; } 
		set {
			_IsDashCancelled = value;

			if (value)
				IsSpeeding = false;
			
			if (_IsDashCancelled) 
				EmitSignal(SignalName.DashActionEnd);
		}
	}

	
	private bool _IsAttackRequested;
	public bool IsAttackRequested {
		get { return _IsAttackRequested; } 
		set {
			_IsAttackRequested = value;
			
			if (_IsAttackRequested) 
				EmitSignal(SignalName.AttackActionStart);
		} 
	}
	private StateMachine stateMachine;

	public void SetStateMachine(StateMachine sm) 
	{
		this.stateMachine = sm;
	}

	// State machine changes WILL throw state end exceptions. After this method is called, state will end immediately 
	public void Jump(params StateConfig.IBaseStateConfig[] configs)
	{
		this.stateMachine.ChangeState<JumpState>(configs);
	}

	public void Jump(float horizontalVelocity, bool isConstantVel = false, bool canEnterHedge = false)
	{
		this.stateMachine.ChangeState<JumpState>(StateConfig.InitialVelocityVector(new Vector2(horizontalVelocity,0), isConstantVel, canEnterHedge));
	}
	// State machine changes WILL throw state end exceptions. After this method is called, state will end immediately
	public void Fall()
	{
		this.stateMachine.ChangeState<AirState>(StateConfig.InitialVerticalVelocity(0));
	}
	// State machine changes WILL throw state end exceptions. After this method is called, state will end immediately
	public void Fall(Vector2 previousVel, bool isConstantHorizontal = false, bool canFallIntoHedge = false)
	{
		this.stateMachine.ChangeState<AirState>(StateConfig.InitialVelocityVector(previousVel, isConstantHorizontal, canFallIntoHedge));
	}
	// State machine changes WILL throw state and axceptions. After this method is called, state will end immediately
	public void Dash(Vector2 dashDir)
	{
		this.LastDashTime = Time.GetTicksMsec();
		this.stateMachine.ChangeState<DashState>(StateConfig.InitialVelocityVector(dashDir));
	}
	public void Land(params StateConfig.IBaseStateConfig[] configs)
	{
		this.stateMachine.ChangeState<GroundedState>(configs);
	}

	public void EnterHedge(HedgeNode hedgeBody, Vector2 direction)
	{
		this.stateMachine.ChangeState<HedgeState>(StateConfig.InitialHedgeCollider(hedgeBody, direction));	
	}
	public void EnterHedge(params StateConfig.IBaseStateConfig[] configs)
	{
		this.stateMachine.ChangeState<HedgeState>(configs);
	}

	public void MainAttack(params StateConfig.IBaseStateConfig[] configs)
	{
		this.stateMachine.ChangeState<BattleState>(configs);
	}
}
