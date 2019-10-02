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

        TmxMap _tiledMap;

        public override void Initialize() {
            ClearColor = Color.LightGray;
            _tiledMap = Content.LoadTiledMap("Content/map.tmx");
            var spawn = _tiledMap.GetObjectGroup("Objects").Objects["spawn"];

            Entity tiledEntity = CreateEntity("tiled-map");
            var tiledRenderer = new TiledMapRenderer(_tiledMap, "Ground");
            tiledRenderer.SetLayerToRender("Ground");
            tiledEntity.AddComponent(tiledRenderer);

            AddPlayer(new Vector2(spawn.X, spawn.Y));
        }

        public void AddPlayer(Vector2 spawn) {
            var player = CreateEntity("player", new Vector2(spawn.X, spawn.Y));
            player.AddComponent(new Player());
            player.AddComponent(new BoxCollider(-8, -16, 16, 32));
            player.AddComponent(new TiledMapMover(_tiledMap.GetLayer<TmxLayer>("Ground")));
            player.AddComponent(new Mover());
        }
    }
}
