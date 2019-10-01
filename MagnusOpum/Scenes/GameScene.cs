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
        public override void Initialize() {
            ClearColor = Color.LightGray;
            var tiledMap = Content.LoadTiledMap("Content/map.tmx");
            var spawn = tiledMap.GetObjectGroup("Objects").Objects["spawn"];

            Entity tiledEntity = CreateEntity("tiled-map");
            var tiledRenderer = new TiledMapRenderer(tiledMap, "Ground");
            tiledRenderer.SetLayerToRender("Ground");
            tiledEntity.AddComponent(tiledRenderer);

            var player = CreateEntity("player", new Vector2(spawn.X, spawn.Y));
            player.AddComponent(new Player());
            player.AddComponent(new BoxCollider(-8, -16, 16, 32));
            player.AddComponent(new TiledMapMover(tiledMap.GetLayer<TmxLayer>("Ground")));
        }
    }
}
