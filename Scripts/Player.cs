using Godot;
using System;

public partial class Player : Area2D {
	// VARIABLES SECTION
	[Signal]
	public delegate void FirePlayerProjectileEventHandler();
	[Signal]
	public delegate void PlayerDiedEventHandler();
	[Export]
	public int Speed { get; set; } = 250;
	[Export]
	public const int DefaultHealth = 300;
	public int Health { get; private set; } = DefaultHealth;
	private float AttackCD { get; set; } = 0.1f;
	public Vector2 ScreenSize;
	public static Player Instance { get; private set; }
	private float attackTimer = 0;
	private static Vector2 defaultScale;
	private Vector2 velocity = Vector2.Zero;
	private AnimatedSprite2D sprite;
	private CollisionShape2D hitbox;
	private const float iframesLength = 1.5f;
	private float iframeTimer = 0f;

	// SIGNALS SECTION
	[Signal]
	public delegate void HitEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		if (Instance != null) {
			GD.Print("Error, more than one player instance");
		}
		Instance = this;
		ScreenSize = GetViewportRect().Size;
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		hitbox = GetNode<CollisionShape2D>("CollisionShape2D");
		defaultScale = Scale;
		Connect("body_entered", Callable.From((Node2D body) => OnBodyEntered(body)));
		InitializePlayer();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (Game.Paused) return;
		// process player movement
		ProcessInput();
		Position += velocity * (float) delta;
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);
		ProcessAnimation();

		// process projectile firing
		if (attackTimer > AttackCD) {
			attackTimer = 0;
			EmitSignal(SignalName.FirePlayerProjectile);
		} else attackTimer += (float) delta;

		// handle iframes
		if (iframeTimer > iframesLength) {
			hitbox.Disabled = false;
			iframeTimer = 0;
			sprite.Animation = "default";
		} else if (hitbox.Disabled) iframeTimer += (float) delta;
	}

	// SIGNAL HANDLERS SECTION
	private void OnBodyEntered(Node2D body) {
		if (Game.Paused) return;
		if (body.GetClass() == "CharacterBody2D")  {
			Projectile projectile = (Projectile) body;
			// ignore if projectile is player projectile
			if (projectile.Sprite.Animation == "playerProjectile") return;
			Health -= projectile.Damage;
		} else if (body.GetClass() == "RigidBody2D") {
			Enemy enemy = (Enemy) body;
			Health -= enemy.Attack;
		}
		
		// send signal that player got hit to game loop
		EmitSignal(SignalName.Hit);

		// check if player is dead
		if (Health <= 0) {
			sprite.Animation = "damaged";
			EmitSignal(SignalName.PlayerDied);
			return;
		}

		// disable hitbox for iFrames
		hitbox.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
	}

	// METHODS SECTION

	public void InitializePlayer() {
		Health = DefaultHealth;
		Position = new Vector2(x: ScreenSize.X / 2, y: ScreenSize.Y / 1.2f);
		hitbox.Disabled = false;
	}

	private void ProcessAnimation() {
		// process X
		if (velocity.X < 0) {
			if (hitbox.Disabled) sprite.Animation = "damaged";
			else sprite.Animation = "left";
			if (sprite.Rotation > -0.3f) sprite.Rotate(-0.3f);
		} else if (velocity.X > 0) {
			if (hitbox.Disabled) sprite.Animation = "damaged";
			else sprite.Animation = "right";
			if (sprite.Rotation < 0.3f) sprite.Rotate(0.3f);
		} else {
			if (hitbox.Disabled) sprite.Animation = "damaged";
			else sprite.Animation = "default";
			sprite.Rotation = 0;
		}

		// process Y
		if (velocity.Y != 0) {
			Scale = new Vector2(defaultScale.X - 0.1f, defaultScale.Y + 0.2f);
		} else {
			Scale = defaultScale;
		}
	}

	private void ProcessInput() {
		velocity = Vector2.Zero;

		if (Input.IsActionPressed("moveRight")) velocity.X += 1;
		if (Input.IsActionPressed("moveLeft")) velocity.X -= 1;
		if (Input.IsActionPressed("moveUp")) velocity.Y -= 1;
		if (Input.IsActionPressed("moveDown")) velocity.Y += 1;

		if (velocity.Length() > 0) {
			velocity = velocity.Normalized() * Speed;
		}
	}
}
