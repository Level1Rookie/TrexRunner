﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using TrexRunner.Graphics;
using TrexRunner.Entities;
using TrexRunner.System;

namespace TrexRunner
{
    public class TRexGame : Game
    {
        private const string ASSET_NAME_SPRITESHEET = "trex";
        private const string ASSET_NAME_SFX_HIT = "hit";
        private const string ASSET_NAME_SFX_SCORE_REACHED = "score-reached";
        private const string ASSET_NAME_SFX_BUTTON_PRESS = "button-press";

        public const int WINDOW_WIDTH = 600;
        public const int WINDOW_HEIGHT = 150;

        public const int TREX_START_POS_Y = WINDOW_HEIGHT - 16;
        public const int TREX_START_POS_X = 1;
        private const int FADE_IN_ANIMATION_SPEED = 800;
        private const int SCORE_BOARD_POS_X = WINDOW_WIDTH - 130;
        private const int SCORE_BOARD_POS_Y = 10;

        private SoundEffect _sfxHit;
        private SoundEffect _sfxButtonPress;
        private SoundEffect _sfxScoreReached;

        private Texture2D _spriteSheetTexture;
        private Texture2D _fadeInTexture;

        private float _fadeInTexturePosX;

        private Trex _trex;
        private ScoreBoard _scoreBoard;
        private InputController _inputController;

        private GroundManager _groundManager;
        private ObstacleManager _obstacleManager;
        private SkyManager _skyManager;
        private GameOverScreen _gameOverScreen;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private EntityManager _entityManager;

        private KeyboardState _previousKeyboardState;

        public GameState State { get; private set; }

        public TRexGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _entityManager = new EntityManager();
            State = GameState.Initial;
            _fadeInTexturePosX = Trex.TREX_DEFAULT_SPRITE_WIDTH;
        }

        protected override void Initialize()
        {

            base.Initialize();

            _graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            _graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
            _graphics.ApplyChanges();

        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _sfxButtonPress = Content.Load<SoundEffect>(ASSET_NAME_SFX_BUTTON_PRESS);
            _sfxHit = Content.Load<SoundEffect>(ASSET_NAME_SFX_HIT);
            _sfxScoreReached = Content.Load<SoundEffect>(ASSET_NAME_SFX_SCORE_REACHED);
            _spriteSheetTexture = Content.Load<Texture2D>(ASSET_NAME_SPRITESHEET);
            _fadeInTexture = new Texture2D(GraphicsDevice, 1, 1);
            _fadeInTexture.SetData(new Color[] { Color.White });

            _trex = new Trex(_spriteSheetTexture, new Vector2(TREX_START_POS_X, TREX_START_POS_Y - Trex.TREX_DEFAULT_SPRITE_HEIGHT), _sfxButtonPress);
            _trex.DrawOrder = 10;
            _trex.JumpComplete += trex_JumpComplete;
            _trex.Died += trex_Died;

            _scoreBoard = new ScoreBoard(_spriteSheetTexture, new Vector2(SCORE_BOARD_POS_X, SCORE_BOARD_POS_Y), _trex, _sfxScoreReached);

            _inputController = new InputController(_trex);

            _groundManager = new GroundManager(_spriteSheetTexture, _entityManager, _trex);
            _obstacleManager = new ObstacleManager(_spriteSheetTexture, _entityManager, _trex, _scoreBoard);
            _skyManager = new SkyManager(_trex, _spriteSheetTexture, _entityManager, _scoreBoard);

            _gameOverScreen = new GameOverScreen(_spriteSheetTexture, this);
            _gameOverScreen.Position = new Vector2(WINDOW_WIDTH / 2 - GameOverScreen.GAME_OVER_SPRITE_WIDTH / 2, WINDOW_HEIGHT - 80);

            _entityManager.AddEntity(_trex);
            _entityManager.AddEntity(_groundManager);
            _entityManager.AddEntity(_obstacleManager);
            _entityManager.AddEntity(_scoreBoard);
            _entityManager.AddEntity(_gameOverScreen);
            _entityManager.AddEntity(_skyManager);
            

            _groundManager.Initialize();
        }

        private void trex_Died(object sender, global::System.EventArgs e)
        {
            State = GameState.GameOver;
            _obstacleManager.IsEnabled = false;
            _gameOverScreen.IsEnabled = true;

            _sfxHit.Play();
        }

        private void trex_JumpComplete(object sender, global::System.EventArgs e)
        {
            if (State == GameState.Transition)
            {
                State = GameState.Playing;
                _trex.Initialize();

                _obstacleManager.IsEnabled = true;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);

            KeyboardState keyboardState = Keyboard.GetState();

            if (State == GameState.Playing)
                _inputController.ProcessControls(gameTime);

            else if (State == GameState.Transition)
                _fadeInTexturePosX += FADE_IN_ANIMATION_SPEED * (float) gameTime.ElapsedGameTime.TotalSeconds;

            else if (State == GameState.Initial)
            {
                bool isStartKeyPressed = keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.Space);
                bool wasStartKeyPressed = _previousKeyboardState.IsKeyDown(Keys.Up) || _previousKeyboardState.IsKeyDown(Keys.Space);
                if (isStartKeyPressed && !wasStartKeyPressed)
                {
                    StartGame();
                }
            }
            _entityManager.Update(gameTime);

            _previousKeyboardState = keyboardState;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            _spriteBatch.Begin();
            _entityManager.Draw(_spriteBatch, gameTime);

            if(State == GameState.Initial || State == GameState.Transition)
            {
                _spriteBatch.Draw(_fadeInTexture, new Rectangle((int)_fadeInTexturePosX, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public bool StartGame()
        {
            if (State != GameState.Initial)
                return false;

            State = GameState.Transition;
            _trex.BeginJump();

            return true;
        }

        public bool Replay()
        {
            if (State != GameState.GameOver)
                return false;

            State = GameState.Playing;
            _trex.Initialize();
            _obstacleManager.Reset();
            _obstacleManager.IsEnabled = true;
            _groundManager.Initialize();
            _gameOverScreen.IsEnabled = false;
            _scoreBoard.Score = 0;

            _inputController.BlockInputTemporarily();

            return true;
        }
    }
}
