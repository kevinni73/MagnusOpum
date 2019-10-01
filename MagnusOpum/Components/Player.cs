using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagnusOpum.Components {
    class Player : Component, ITriggerListener, IUpdatable {
        public float moveSpeed = 300;
        public float rollSpeed = 250;
        public float gravity = 1500;
        public float jumpHeight = 16 * 6;

        public float terminalVelocity = 500;
        public float fastFallVelocity = 600;

        SpriteAnimator _animator;
        TiledMapMover _tileMover;
        Mover _mover;
        BoxCollider _boxCollider;
        Vector2 _velocity;
        TiledMapMover.CollisionState _collisionState = new TiledMapMover.CollisionState();
        FollowCamera _followCam;
        Dictionary<string, SpriteAnimator.LoopMode> _animatorLoop = new Dictionary<string, SpriteAnimator.LoopMode>();

        VirtualButton _jumpInput;
        VirtualButton _attackInput;
        VirtualButton _rollInput;
        VirtualIntegerAxis _xAxisInput;
        VirtualIntegerAxis _yAxisInput;

        bool jumpKeyHeld = false;

        public override void OnAddedToEntity() {
            Texture2D texture = Entity.Scene.Content.Load<Texture2D>("Character Animations/Adventurer-1.5/adventurer-v1.5-Sheet");
            var sprites = Sprite.SpritesFromAtlas(texture, 50, 37);

            _tileMover = Entity.GetComponent<TiledMapMover>();
            _mover = Entity.GetComponent<Mover>();
            _boxCollider = Entity.GetComponent<BoxCollider>();
            _animator = Entity.AddComponent(new SpriteAnimator(sprites[0]));
            _followCam = Entity.GetComponent<FollowCamera>();

            _animator.AddAnimation("idle", 6, new[]{
                sprites[38],
                sprites[39],
                sprites[40],
                sprites[41],
            });

            _animator.AddAnimation("run", new[]{
                sprites[8],
                sprites[9],
                sprites[10],
                sprites[11],
                sprites[12],
                sprites[13],
            });

            _animator.AddAnimation("jump", new[]{
                sprites[14],
                sprites[15],
                sprites[16],
                sprites[17],
            });
            _animatorLoop["jump"] = SpriteAnimator.LoopMode.ClampForever;

            _animator.AddAnimation("falling", new[]{
                sprites[22],
                sprites[23],
            });

            _animator.AddAnimation("attack", 15, new [] {
                //sprites[47], lower startup
                sprites[48],
                sprites[49],
                sprites[50],
                sprites[51],
                sprites[52],
            });
            _animatorLoop["attack"] = SpriteAnimator.LoopMode.Once;

            _animator.AddAnimation("roll", 15, new [] {
                sprites[18],
                sprites[19],
                sprites[20],
                sprites[21],
                sprites[18],
                sprites[19],
            });
            _animatorLoop["roll"] = SpriteAnimator.LoopMode.Once;

            _animator.AddAnimation("crouch", new [] {
                sprites[4],
                sprites[5],
                sprites[6],
                sprites[7],
            });

            _animator.OnAnimationCompletedEvent += onFinishAnimation;

            setupInput();
        }

        void onFinishAnimation(string animation) {
            if (animation == "attack") {
                _animator.Play("idle");
            }
            else if (animation == "roll") {
                _boxCollider.SetHeight(32);
                var _ = new Vector2();
                _tileMover.TestCollisions(ref _, _boxCollider.Bounds, _collisionState);
                if (_collisionState.Above) {
                    _boxCollider.SetHeight(16);
                    _animator.Play("crouch");
                }
            }
        }

        string transitionAnimation(string previous, string next) {
            if (previous == next) {
                return next;
            }
            if (previous == "crouch") {
                _boxCollider.SetLocalOffset(new Vector2(_boxCollider.LocalOffset.X, 0));

                if (next != "roll") {
                    _boxCollider.SetHeight(32);
                    var _ = new Vector2();
                    _tileMover.TestCollisions(ref _, _boxCollider.Bounds, _collisionState);
                    if (_collisionState.Above) {
                        _boxCollider.SetHeight(16);
                        _boxCollider.SetLocalOffset(new Vector2(_boxCollider.LocalOffset.X, 8));
                        return "crouch";
                    }
                }
            }
            else if (previous == "roll") {
                _boxCollider.Transform.Position = new Vector2(_boxCollider.Transform.Position.X, _boxCollider.Transform.Position.Y - 8);
                _boxCollider.SetHeight(32);
                var _ = new Vector2();
                _tileMover.TestCollisions(ref _, _boxCollider.Bounds, _collisionState);
                if (_collisionState.Above) {
                    _boxCollider.SetHeight(16);
                    _boxCollider.SetLocalOffset(new Vector2(_boxCollider.LocalOffset.X, 8));
                    return "crouch";
                }
            }

            if (next == "crouch") {
                _boxCollider.SetHeight(16);
                _boxCollider.SetLocalOffset(new Vector2(_boxCollider.LocalOffset.X, 8));
            }
            else if (next == "roll") {
                _boxCollider.Transform.Position = new Vector2(_boxCollider.Transform.Position.X, _boxCollider.Transform.Position.Y + 8);
                _boxCollider.SetHeight(16);
                _boxCollider.SetLocalOffset(new Vector2(_boxCollider.LocalOffset.X, 0));
            }

            return next;
        }

        public override void OnRemovedFromEntity() {
            base.OnRemovedFromEntity();

            // deregister virtual input
            _xAxisInput.Deregister();
            _yAxisInput.Deregister();
            _jumpInput.Deregister();
            _attackInput.Deregister();
            _rollInput.Deregister();
        }

        void setupInput() {
            // setup input for jumping. we will allow z on the keyboard or a on the gamepad
            _jumpInput = new VirtualButton();
            _jumpInput.Nodes.Add(new Nez.VirtualButton.KeyboardKey(Keys.Space));
            _jumpInput.Nodes.Add(new Nez.VirtualButton.GamePadButton(0, Buttons.A));

            _attackInput = new VirtualButton();
            _attackInput.Nodes.Add(new Nez.VirtualButton.GamePadButton(0, Buttons.X));

            _rollInput = new VirtualButton();
            _rollInput.Nodes.Add(new Nez.VirtualButton.GamePadButton(0, Buttons.B));

            // horizontal input from dpad, left stick or keyboard left/right
            _xAxisInput = new VirtualIntegerAxis();
            _xAxisInput.Nodes.Add(new Nez.VirtualAxis.GamePadDpadLeftRight());
            _xAxisInput.Nodes.Add(new Nez.VirtualAxis.GamePadLeftStickX());
            _xAxisInput.Nodes.Add(new Nez.VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D));

            _yAxisInput = new VirtualIntegerAxis();
            _yAxisInput.Nodes.Add(new Nez.VirtualAxis.GamePadDpadUpDown());
            _yAxisInput.Nodes.Add(new Nez.VirtualAxis.GamePadLeftStickY());
            _yAxisInput.Nodes.Add(new Nez.VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.W, Keys.S));
        }


        void IUpdatable.Update() {
            // handle movement and animations
            Vector2 moveDir = new Vector2(_xAxisInput.Value, _yAxisInput.Value);
            string animation = _animator.CurrentAnimationName;

            if (_animator.CurrentAnimationName == "attack") {
                return;
            }

            if (_animator.CurrentAnimationName == "roll" && _animator.CurrentFrame != _animator.CurrentAnimation.Sprites.Count() - 1) {
                if (_animator.FlipX) {
                    _velocity.X = -rollSpeed;
                }
                else {
                    _velocity.X = rollSpeed;
                }
            }
            else {
                if (_rollInput.IsPressed && _collisionState.Below) {
                    if (_animator.FlipX) {
                        _velocity.X = -moveSpeed;
                    }
                    else {
                        _velocity.X = moveSpeed;
                    }

                    animation = "roll";
                }
                else if (_attackInput.IsPressed && _collisionState.Below) {
                    animation = "attack";
                }
                else {
                    if (moveDir.Y > 0) {
                        if (_collisionState.Below) {
                            animation = "crouch";
                            if (moveDir.X < 0) {
                                _animator.FlipX = true;
                            }
                            else {
                                _animator.FlipX = false;
                            }
                        }
                        else if (_animator.CurrentAnimationName == "falling") {
                            _velocity.Y = fastFallVelocity;
                        }
                    }
                    else {
                        if (moveDir.X < 0) {
                            if (_collisionState.Below) {
                                animation = "run";
                            }
                            _animator.FlipX = true;
                            _velocity.X = -moveSpeed;
                        }
                        else if (moveDir.X > 0) {
                            if (_collisionState.Below) {
                                animation = "run";
                            }
                            _animator.FlipX = false;
                            _velocity.X = moveSpeed;
                        }
                        else {
                            _velocity.X = 0;
                            if (_collisionState.Below) {
                                animation = "idle";
                            }
                        }
                    }

                    if (_collisionState.Below && _jumpInput.IsPressed) {
                        animation = "jump";
                        jumpKeyHeld = true;
                        _velocity.Y = -Mathf.Sqrt(2f * jumpHeight * gravity);
                    }
                    else if (_jumpInput.IsReleased) {
                        jumpKeyHeld = false;
                    }

                    if (_collisionState.Above) {
                        _velocity.Y = 0;
                    }

                    if (!_collisionState.Below & _velocity.Y > 0) {
                        animation = "falling";
                    }
                }
            }

            if (_animator.CurrentAnimationName == "jump" && !jumpKeyHeld) {
                _velocity.Y /= 2;
            }

            // apply gravity
            if (_velocity.Y < terminalVelocity) {
                _velocity.Y += gravity * Time.DeltaTime;
                if (_velocity.Y > terminalVelocity) {
                    _velocity.Y = terminalVelocity;
                }
            }

            animation = transitionAnimation(_animator.CurrentAnimationName, animation);
            if (animation != "crouch") {
                // move
                Vector2 movement = _velocity * Time.DeltaTime;
                CollisionResult collisionResult;
                _tileMover.TestCollisions(ref movement, _boxCollider.Bounds, _collisionState);
                _mover.Move(movement, out collisionResult);
            }

            // reset gravity acceleration
            if (_collisionState.Below) {
                _velocity.Y = 0;
            }

            if (animation != null && !_animator.IsAnimationActive(animation)) {
                if (_animatorLoop.ContainsKey(animation)) {
                    _animator.Play(animation, _animatorLoop[animation]);
                }
                else {
                    _animator.Play(animation);
                }
            }
        }

        #region ITriggerListener implementation
        public void OnTriggerEnter(Collider other, Collider local) {
            Debug.Log("triggerEnter: {0}", other.Entity.Name);
        }

        public void OnTriggerExit(Collider other, Collider local) {
            Debug.Log("triggerExit: {0}", other.Entity.Name);
        }
        #endregion
    }
}
