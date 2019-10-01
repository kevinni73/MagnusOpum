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

        public override void Initialize() {
            roomSizes = new Dictionary<uint, Vector2>();
            roomSizes.Add(0, new Vector2(40, 23));
            roomSizes.Add(1, new Vector2(60, 23)); // TODO: rooms need to store direction pointer to another room
            currentRoomRect = new RectangleF(new Vector2(0, 0), tileSize * roomSizes[currentRoom]);

            AddRenderer(new DefaultRenderer());
            ClearColor = Color.LightGray;

            var tiledMap = Content.LoadTiledMap("Content/map.tmx");
            var spawn = tiledMap.GetObjectGroup("Objects").Objects["spawn"];

            Entity tiledEntity = CreateEntity("tiled-map");
            tiledEntity.AddComponent(new TiledMapRenderer(tiledMap, "Ground"));
            //tiledEntity.AddComponent(new TiledMapComponent(tiledMap, "Transitions"));

            player = CreateEntity("player", new Vector2(spawn.X, spawn.Y));
            player.AddComponent(new Player());
            player.AddComponent(new BoxCollider(-8, -16, 16, 32));
            player.AddComponent(new TiledMapMover(tiledMap.GetLayer<TmxLayer>("Ground")));
            player.AddComponent(new Mover());

            FollowCamera followCam = new FollowCamera(player, Camera);
            followCam.MapLockEnabled = true;
            followCam.MapSize = currentRoomRect.Size;
            player.AddComponent(followCam);
        }

        public override void Update() {
            base.Update();

            // check if player is in room
            RectangleF playerBounds = player.GetComponent<BoxCollider>().Bounds;
            var followCam = player.GetComponent<FollowCamera>();
            if (playerBounds.Left > currentRoomRect.Right) {
                currentRoom += 1;
                followCam.ShiftMap(new Vector2(currentRoomRect.Width, 0));

                currentRoomRect.Offset(currentRoomRect.Width, 0);
                currentRoomRect.Size = tileSize * roomSizes[currentRoom];

                followCam.MapSize = currentRoomRect.Size;
            }
            else if (playerBounds.Right < currentRoomRect.Left) {
                currentRoom -= 1;
                currentRoomRect.Size = tileSize * roomSizes[currentRoom];
                followCam.ShiftMap(new Vector2(-currentRoomRect.Width, 0));
                currentRoomRect.Offset(-currentRoomRect.Width, 0);

                followCam.MapSize = currentRoomRect.Size;
            }
            else {
                return;
            }
        }
    }
}
