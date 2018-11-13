using MagnusOpum.Components;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagnusOpum.Scenes {
    class GameScene : Scene {
        public uint currentRoom = 0;
        public RectangleF currentRoomRect;
        public uint tileSize = 16;
        public Entity player;
        public Dictionary<uint, Vector2> roomSizes;

        public override void initialize() {
            roomSizes = new Dictionary<uint, Vector2>();
            roomSizes.Add(0, new Vector2(40, 23));
            roomSizes.Add(1, new Vector2(60, 23)); // TODO: rooms need to store direction pointer to another room
            currentRoomRect = new RectangleF(new Vector2(0, 0), tileSize * roomSizes[currentRoom]);

            addRenderer(new DefaultRenderer());
            clearColor = Color.LightGray;

            TiledMap tiledMap = content.Load<TiledMap>("map");
            TiledObjectGroup objectLayer = tiledMap.getObjectGroup("Objects");
            TiledObject spawn = objectLayer.objectWithName("spawn");

            Entity tiledEntity = createEntity("tiled-map");
            TiledMapComponent tiledMapCollision = tiledEntity.addComponent(new TiledMapComponent(tiledMap, "Ground", true));
            TiledMapComponent tiledMapTransitionTriggers = tiledEntity.addComponent(new TiledMapComponent(tiledMap, "Transitions"));

            player = createEntity("player", new Vector2(spawn.x, spawn.y));
            var playerComponent = new Player();
            player.addComponent(playerComponent);
            player.addComponent(new BoxCollider(-8, -16, 16, 32));
            player.addComponent(new TiledMapMover(tiledMapCollision.collisionLayer));
            player.addComponent(new Mover());
            
            FollowCamera followCam = new FollowCamera(player, camera);
            followCam.mapLockEnabled = true;
            followCam.mapSize = currentRoomRect.size;
            player.addComponent(followCam);
        }

        public override void update() {
            base.update();

            // check if player is in room
            RectangleF playerBounds = player.getComponent<BoxCollider>().bounds;
            var followCam = player.getComponent<FollowCamera>();
            if (playerBounds.left > currentRoomRect.right) {
                currentRoom += 1;
                followCam.shiftMap(new Vector2(currentRoomRect.width, 0));

                currentRoomRect.offset(currentRoomRect.width, 0);
                currentRoomRect.size = tileSize * roomSizes[currentRoom];
                
                followCam.mapSize = currentRoomRect.size;
            }
            else if (playerBounds.right < currentRoomRect.left) {
                currentRoom -= 1;
                currentRoomRect.size = tileSize * roomSizes[currentRoom];
                followCam.shiftMap(new Vector2(-currentRoomRect.width, 0));
                currentRoomRect.offset(-currentRoomRect.width, 0);

                followCam.mapSize = currentRoomRect.size;
            }
            else {
                return;
            }
        }
    }
}
