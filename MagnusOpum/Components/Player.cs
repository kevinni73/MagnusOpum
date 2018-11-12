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

        public Vector2 currentRoomSize;

        enum Animations {
            None,
            Idle,
            Run,
            Attack,
            Jump,
            Falling,
            Roll,
            Crouch,
        }

        Sprite<Animations> _animation;
        TiledMapMover _tileMover;
        Mover _mover;
        BoxCollider _boxCollider;
        Vector2 _velocity;
        TiledMapMover.CollisionState _collisionState = new TiledMapMover.CollisionState();
        FollowCamera _followCam;

        VirtualButton _jumpInput;
        VirtualButton _attackInput;
        VirtualButton _rollInput;
        VirtualIntegerAxis _xAxisInput;
        VirtualIntegerAxis _yAxisInput;

        bool jumpKeyHeld = false;
        bool shiftOnce = false;

        public override void onAddedToEntity() {
            Texture2D texture = entity.scene.content.Load<Texture2D>("Character Animations/Adventurer-1.5/adventurer-v1.5-Sheet");
            List<Subtexture> subtextures = Subtexture.subtexturesFromAtlas(texture, 50, 37);

            _tileMover = entity.getComponent<TiledMapMover>();
            _mover = entity.getComponent<Mover>();
            _boxCollider = entity.getComponent<BoxCollider>();
            _animation = entity.addComponent(new Sprite<Animations>(subtextures[0]));
            _followCam = entity.getComponent<FollowCamera>();

            _animation.addAnimation(Animations.Idle, new SpriteAnimation(new List<Subtexture>() {
                subtextures[38],
                subtextures[39],
                subtextures[40],
                subtextures[41],
            }).setFps(6));

            _animation.addAnimation(Animations.Run, new SpriteAnimation(new List<Subtexture>() {
                subtextures[8],
                subtextures[9],
                subtextures[10],
                subtextures[11],
                subtextures[12],
                subtextures[13],
            }));

            _animation.addAnimation(Animations.Jump, new SpriteAnimation(new List<Subtexture>() {
                subtextures[14],
                subtextures[15],
                subtextures[16],
                subtextures[17],
            }));

            _animation.getAnimation(Animations.Jump).setLoop(false);
            _animation.getAnimation(Animations.Jump).completionBehavior = AnimationCompletionBehavior.RemainOnFinalFrame;

            _animation.addAnimation(Animations.Falling, new SpriteAnimation(new List<Subtexture>() {
                subtextures[22],
                subtextures[23],
            }));

            _animation.addAnimation(Animations.Attack, new SpriteAnimation(new List<Subtexture>() {
                //subtextures[47], lower startup
                subtextures[48],
                subtextures[49],
                subtextures[50],
                subtextures[51],
                subtextures[52],
            }).setLoop(false).setFps(15));

            _animation.addAnimation(Animations.Roll, new SpriteAnimation(new List<Subtexture>() {
                subtextures[18],
                subtextures[19],
                subtextures[20],
                subtextures[21],
                subtextures[18],
                subtextures[19],
            }).setLoop(false).setFps(15));

            _animation.addAnimation(Animations.Crouch, new SpriteAnimation(new List<Subtexture>() {
                subtextures[4],
                subtextures[5],
                subtextures[6],
                subtextures[7],
            }));

            _animation.onAnimationCompletedEvent += onFinishAnimation;

            setupInput();
        }

        void onFinishAnimation(Animations animation) {
            if (animation == Animations.Attack) {
                _animation.currentAnimation = Animations.Idle;
            }
            else if (animation == Animations.Roll) {
                _boxCollider.setHeight(32);
                var _ = new Vector2();
                _tileMover.testCollisions(ref _, _boxCollider.bounds, _collisionState);
                if (_collisionState.above) {
                    _boxCollider.setHeight(16);
                    _animation.currentAnimation = Animations.Crouch;
                }
            }
        }

        Animations transitionAnimation(Animations previous, Animations next) {
            if (previous == next) {
                return next;
            }
            if (previous == Animations.Crouch) {
                _boxCollider.setLocalOffset(new Vector2(_boxCollider.localOffset.X, 0));

                if (next != Animations.Roll) {
                    _boxCollider.setHeight(32);
                    var _ = new Vector2();
                    _tileMover.testCollisions(ref _, _boxCollider.bounds, _collisionState);
                    if (_collisionState.above) {
                        _boxCollider.setHeight(16);
                        _boxCollider.setLocalOffset(new Vector2(_boxCollider.localOffset.X, 8));
                        return Animations.Crouch;
                    }
                }
            }
            else if (previous == Animations.Roll) {
                _boxCollider.transform.position = new Vector2(_boxCollider.transform.position.X, _boxCollider.transform.position.Y - 8);
                _boxCollider.setHeight(32);
                var _ = new Vector2();
                _tileMover.testCollisions(ref _, _boxCollider.bounds, _collisionState);
                if (_collisionState.above) {
                    _boxCollider.setHeight(16);
                    _boxCollider.setLocalOffset(new Vector2(_boxCollider.localOffset.X, 8));
                    return Animations.Crouch;
                }
            }

            if (next == Animations.Crouch) {
                _boxCollider.setHeight(16);
                _boxCollider.setLocalOffset(new Vector2(_boxCollider.localOffset.X, 8));
            }
            else if (next == Animations.Roll) {
                _boxCollider.transform.position = new Vector2(_boxCollider.transform.position.X, _boxCollider.transform.position.Y + 8);
                _boxCollider.setHeight(16);
                _boxCollider.setLocalOffset(new Vector2(_boxCollider.localOffset.X, 0));
            }

            return next;
        }

        public override void onRemovedFromEntity() {
            base.onRemovedFromEntity();

            // deregister virtual input
            _xAxisInput.deregister();
            _yAxisInput.deregister();
            _jumpInput.deregister();
            _attackInput.deregister();
            _rollInput.deregister();
        }

        void setupInput() {
            // setup input for jumping. we will allow z on the keyboard or a on the gamepad
            _jumpInput = new VirtualButton();
            _jumpInput.nodes.Add(new Nez.VirtualButton.KeyboardKey(Keys.Space));
            _jumpInput.nodes.Add(new Nez.VirtualButton.GamePadButton(0, Buttons.A));

            _attackInput = new VirtualButton();
            _attackInput.nodes.Add(new Nez.VirtualButton.GamePadButton(0, Buttons.X));

            _rollInput = new VirtualButton();
            _rollInput.nodes.Add(new Nez.VirtualButton.GamePadButton(0, Buttons.B));

            // horizontal input from dpad, left stick or keyboard left/right
            _xAxisInput = new VirtualIntegerAxis();
            _xAxisInput.nodes.Add(new Nez.VirtualAxis.GamePadDpadLeftRight());
            _xAxisInput.nodes.Add(new Nez.VirtualAxis.GamePadLeftStickX());
            _xAxisInput.nodes.Add(new Nez.VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D));

            _yAxisInput = new VirtualIntegerAxis();
            _yAxisInput.nodes.Add(new Nez.VirtualAxis.GamePadDpadUpDown());
            _yAxisInput.nodes.Add(new Nez.VirtualAxis.GamePadLeftStickY());
            _yAxisInput.nodes.Add(new Nez.VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.W, Keys.S));
        }


        void IUpdatable.update() {
            // handle movement and animations
            Vector2 moveDir = new Vector2(_xAxisInput.value, _yAxisInput.value);
            Animations animation = _animation.currentAnimation;

            if (_animation.currentAnimation == Animations.Attack) {
                return;
            }

            if (_animation.currentAnimation == Animations.Roll && _animation.currentFrame != _animation.getAnimation(Animations.Roll).frames.Count() - 1) {
                if (_animation.flipX) {
                    _velocity.X = -rollSpeed;
                }
                else {
                    _velocity.X = rollSpeed;
                }
            }
            else {
                if (_rollInput.isPressed && _collisionState.below) {
                    if (_animation.flipX) {
                        _velocity.X = -moveSpeed;
                    }
                    else {
                        _velocity.X = moveSpeed;
                    }

                    animation = Animations.Roll;
                }
                else if (_attackInput.isPressed && _collisionState.below) {
                    animation = Animations.Attack;
                }
                else {
                    if (moveDir.Y > 0) {
                        if (_collisionState.below) {
                            animation = Animations.Crouch;
                            if (moveDir.X < 0) {
                                _animation.flipX = true;
                            }
                            else {
                                _animation.flipX = false;
                            }
                        }
                        else if (_animation.currentAnimation == Animations.Falling) {
                            _velocity.Y = fastFallVelocity;
                        }
                    }
                    else {
                        if (moveDir.X < 0) {
                            if (_collisionState.below) {
                                animation = Animations.Run;
                            }
                            _animation.flipX = true;
                            _velocity.X = -moveSpeed;
                        }
                        else if (moveDir.X > 0) {
                            if (_collisionState.below) {
                                animation = Animations.Run;
                            }
                            _animation.flipX = false;
                            _velocity.X = moveSpeed;
                        }
                        else {
                            _velocity.X = 0;
                            if (_collisionState.below) {
                                animation = Animations.Idle;
                            }
                        }
                    }

                    if (_collisionState.below && _jumpInput.isPressed) {
                        animation = Animations.Jump;
                        jumpKeyHeld = true;
                        _velocity.Y = -Mathf.sqrt(2f * jumpHeight * gravity);
                    }
                    else if (_jumpInput.isReleased) {
                        jumpKeyHeld = false;
                    }

                    if (_collisionState.above) {
                        _velocity.Y = 0;
                    }

                    if (!_collisionState.below & _velocity.Y > 0) {
                        animation = Animations.Falling;
                    }
                }
            }

            if (_animation.currentAnimation == Animations.Jump && !jumpKeyHeld) {
                _velocity.Y /= 2;
            }

            // apply gravity
            if (_velocity.Y < terminalVelocity) {
                _velocity.Y += gravity * Time.deltaTime;
                if (_velocity.Y > terminalVelocity) {
                    _velocity.Y = terminalVelocity;
                }
            }

            animation = transitionAnimation(_animation.currentAnimation, animation);
            if (animation != Animations.Crouch) {
                // move
                Vector2 movement = _velocity * Time.deltaTime;
                CollisionResult collisionResult;
                _tileMover.testCollisions(ref movement, _boxCollider.bounds, _collisionState);
                _mover.move(movement, out collisionResult);
            }

            // reset gravity acceleration
            if (_collisionState.below) {
                _velocity.Y = 0;
            }

            if (!_animation.isAnimationPlaying(animation) && animation != Animations.None) {
                _animation.play(animation);
            }

            if (!shiftOnce && transform.position.X - (_boxCollider.width / 2f) >= currentRoomSize.X) {
                _followCam.shiftMap(new Vector2(currentRoomSize.X, 0));
                shiftOnce = true;
            }
        }

        #region ITriggerListener implementation
        public void onTriggerEnter(Collider other, Collider local) {
            Debug.log("triggerEnter: {0}", other.entity.name);
        }

        public void onTriggerExit(Collider other, Collider local) {
            Debug.log("triggerExit: {0}", other.entity.name);
        }
        #endregion
    }
}
