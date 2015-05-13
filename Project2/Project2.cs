using System;
using System.Text;
using SharpDX;


namespace Project2
{
    // Use these namespaces here to override SharpDX.Direct3D11
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;
    using System.Collections.Generic;

    /// <summary>
    /// Simple Project2 game using SharpDX.Toolkit.
    /// </summary>
    public class Project2 : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;
        private SpriteBatch spriteBatch;
        private SpriteFont arial16Font;

        private KeyboardManager keyboard;
        private KeyboardState keyboardState;

        private BasicEffect basicEffect;

        private float cameraRotation;
        private bool enterPressed; //the enter key allows us to cycle through various animations

        private MD3 model;

        /// <summary>
        /// Initializes a new instance of the <see cref="Project2" /> class.
        /// </summary>
        public Project2()
        {
            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager
            Content.RootDirectory = "Content";

            keyboard = new KeyboardManager(this);
        }

        protected override void Initialize()
        {
            // Modify the title of the window
            Window.Title = "Project2";

            basicEffect = ToDisposeContent(new BasicEffect(GraphicsDevice));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Instantiate a SpriteBatch
            spriteBatch = ToDisposeContent(new SpriteBatch(GraphicsDevice));

            // Loads a sprite font
            // The [Arial16.xml] file is defined with the build action [ToolkitFont] in the project
            arial16Font = Content.Load<SpriteFont>("Arial16");

            model = new MD3(GraphicsDevice, "model.txt"); //model is, fittingly, the name of the model to be rendered

            basicEffect.LightingEnabled = true;
            basicEffect.TextureEnabled = true;
            basicEffect.VertexColorEnabled = false;
            basicEffect.DirectionalLight0.Enabled = true;
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
            basicEffect.DirectionalLight0.Direction = new Vector3(-1, -1, 0);
            basicEffect.DirectionalLight0.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f); //set the lighting

            cameraRotation = 0;
            enterPressed = false; //the camera starts unrotated and enter starts unpressed

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            model.Update(gameTime.ElapsedGameTime.Milliseconds / 1000.0f);

            // Get the current state of the keyboard
            keyboardState = keyboard.GetState();

            var pressedKeys = new List<Keys>();
            keyboardState.GetDownKeys(pressedKeys);

            if (pressedKeys.Contains(Keys.Left)) //the left and right keys are used for camera rotation
                cameraRotation += 0.1f;
            if (pressedKeys.Contains(Keys.Right))
                cameraRotation -= 0.1f;
            if (pressedKeys.Contains(Keys.Enter) && ! enterPressed)
            {
                enterPressed = true;
                model.nextAnimation(); //enter scrolls through animations
            }
            if (!pressedKeys.Contains(Keys.Enter))
                enterPressed = false; //enterPressed prevents holding enter from rapidly cycling through animations, requiring a new press of enter for each animation
                

            // Calculates the world and the view based on the model size
            basicEffect.Projection = Matrix.PerspectiveFovRH(0.9f, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 200.0f);
            basicEffect.View = Matrix.LookAtRH(new Vector3(100.0f * (float)Math.Sin(cameraRotation), 0.0f, 100.0f * (float)Math.Cos(cameraRotation)), new Vector3(0, 0.0f, 0), Vector3.UnitY); //updates view based on camera rotation
        }

        protected override void Draw(GameTime gameTime)
        {
            // Use time in seconds directly
            var time = (float)gameTime.TotalGameTime.TotalSeconds;

            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // ------------------------------------------------------------------------
            // Draw the some 2d text
            // ------------------------------------------------------------------------
            spriteBatch.Begin();
            var text = new StringBuilder();

            // Display pressed keys
            var pressedKeys = new List<Keys>();
            keyboardState.GetDownKeys(pressedKeys);
            text.Append("Key Pressed: [");
            foreach (var key in pressedKeys)
            {
                text.Append(key.ToString());
                text.Append(" "); //show the user what keys are being pressed
            }
            text.Append("]").AppendLine();

            spriteBatch.DrawString(arial16Font, text.ToString(), new Vector2(16, 16), Color.White);
            spriteBatch.End();

            // ------------------------------------------------------------------------
            // Draw the 3d model
            // ------------------------------------------------------------------------
            basicEffect.World = Matrix.RotationX(-(float)Math.PI / 2.0f) *
                                Matrix.RotationY(-(float)Math.PI / 2.0f);
            GraphicsDevice.SetDepthStencilState(GraphicsDevice.DepthStencilStates.Default); //enables the Z-buffer
            model.Render(basicEffect, Matrix.Identity, Matrix.Identity); //render the model

            base.Draw(gameTime);
        }
    }
}
