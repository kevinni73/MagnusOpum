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
        public Vector2 currentRoomSize;
        public uint tileSize = 16;

        public override void initialize() {
            var roomSizes = new Dictionary<uint, Vector2>();
            roomSizes.Add(0, new Vector2(40, 23));
            currentRoomSize = tileSize * roomSizes[currentRoom];

            addRenderer(new DefaultRenderer());
            clearColor = Color.LightGray;

            TiledMap tiledMap = content.Load<TiledMap>("map");
            TiledObjectGroup objectLayer = tiledMap.getObjectGroup("Objects");
            TiledObject spawn = objectLayer.objectWithName("spawn");

            Entity tiledEntity = createEntity("tiled-map");
            TiledMapComponent tiledMapCollision = tiledEntity.addComponent(new TiledMapComponent(tiledMap, "Ground", true));
            TiledMapComponent tiledMapTransitionTriggers = tiledEntity.addComponent(new TiledMapComponent(tiledMap, "Transitions")); // TODO: Collision here

            Entity player = createEntity("player", new Vector2(spawn.x, spawn.y));
            var playerComponent = new Player();
            playerComponent.currentRoomSize = currentRoomSize;
            player.addComponent(playerComponent);
            player.addComponent(new BoxCollider(-8, -16, 16, 32));
            player.addComponent(new TiledMapMover(tiledMapCollision.collisionLayer));
            player.addComponent(new Mover());

            //camera.position = player.position;
            FollowCamera followCam = new FollowCamera(player, camera);
            followCam.mapLockEnabled = true;
            followCam.mapSize = currentRoomSize;
            player.addComponent(followCam);
        }
    }
}
