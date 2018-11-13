using MagnusOpum.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;

namespace MagnusOpum {
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MagnusOpum : Core {

        public MagnusOpum() : base(640 * 2, 360 * 2) {
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            //IsFixedTimeStep = true;
            //debugRenderEnabled = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            base.Initialize();

            Scene.setDefaultDesignResolution(640, 368, Scene.SceneResolutionPolicy.ExactFit);
            scene = new GameScene();
        }
    }
}
