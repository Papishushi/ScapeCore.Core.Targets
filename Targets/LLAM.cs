/*
 * -*- encoding: utf-8 with BOM -*-
 * .▄▄ ·  ▄▄·  ▄▄▄·  ▄▄▄·▄▄▄ .     ▄▄·       ▄▄▄  ▄▄▄ .
 * ▐█ ▀. ▐█ ▌▪▐█ ▀█ ▐█ ▄█▀▄.▀·    ▐█ ▌▪▪     ▀▄ █·▀▄.▀·
 * ▄▀▀▀█▄██ ▄▄▄█▀▀█  ██▀·▐▀▀▪▄    ██ ▄▄ ▄█▀▄ ▐▀▀▄ ▐▀▀▪▄
 * ▐█▄▪▐█▐███▌▐█ ▪▐▌▐█▪·•▐█▄▄▌    ▐███▌▐█▌.▐▌▐█•█▌▐█▄▄▌
 *  ▀▀▀▀ ·▀▀▀  ▀  ▀ .▀    ▀▀▀     ·▀▀▀  ▀█▄▀▪.▀  ▀ ▀▀▀ 
 * https://github.com/Papishushi/ScapeCore
 * 
 * MIT License
 *
 * Copyright (c) 2023 Daniel Molinero Lucas
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ScapeCore.Core.Batching.Events;
using ScapeCore.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using static ScapeCore.Core.Debug.Debugger;
using static ScapeCore.Traceability.Logging.LoggingColor;

namespace ScapeCore.Core.Targets
{

    //Low Level Automation Module
    public class LLAM : Game, IScapeCoreHost
    {
        private long _si, _ui, _ri;
        private GraphicsDeviceManager? _graphics;
        private SpriteBatch? _spriteBatch;

        public GraphicsDeviceManager Graphics { get => _graphics; }
        public SpriteBatch? SpriteBatch { get => _spriteBatch; }
        public static WeakReference<LLAM?> Instance { get; private set; }
        private GameTime? _time;
        public GameTime? Time { get => _time; }

        private readonly HashSet<IScapeCoreManager?> _managers = [];
        List<IScapeCoreManager?> IScapeCoreHost.Managers { get => [.. _managers]; init => _managers = [.. value]; }

        public event UpdateBatchEventHandler? OnUpdate;
        public event StartBatchEventHandler? OnStart;
        public event LoadBatchEventHandler? OnLoad;
        public event RenderBatchEventHandler? OnRender;

        static LLAM() => Instance ??= new(null);

        public LLAM()
        {
            ConstructorLogic();
        }

        public void Reset()
        {
            Instance.SetTarget(null);
            _graphics = null;
            Content.RootDirectory = string.Empty;
            IsMouseVisible = default;
            IsFixedTimeStep = default;

            ConstructorLogic();
            _managers.Clear();
        }

        private void ConstructorLogic()
        {
            SCLog.Log(INFORMATION, "Constructing Game...");

            SCLog.Log(DEBUG, "Setting singleton pattern.");
            if (Instance.TryGetTarget(out var target))
            {
                var ex = new InvalidOperationException("There is already a valid LLAM instance set up.");
                SCLog.Log(ERROR, ex.Message);
                throw ex;
            }
            else
                Instance.SetTarget(this);

            SCLog.Log(DEBUG, "Singleton pattern was set.");

            _graphics = new(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = false;
        }

        protected override void Initialize()
        {
            SCLog.Log(INFORMATION, "Initializing...");
            static void successLoad(object a, StartBatchEventArgs b) => SCLog.Log(INFORMATION, "Load Success!");
            OnStart += successLoad;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            SCLog.Log(INFORMATION, "Loading Content...");
            _spriteBatch = new(GraphicsDevice);
            var args = new LoadBatchEventArgs($"Load process | Patch size {OnLoad?.GetInvocationList().Length ?? 0}");
            OnLoad?.Invoke(this, args);
            OnLoad = null;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            _time = gameTime;
            OnStart?.Invoke(this, new(string.Empty));
            SCLog.Log(VERBOSE, $"{{{@GetHashCode()}}}\tStart cycle number\t{_si++}\t|\tPatch size\t{OnStart?.GetInvocationList().Length ?? 0}");
            OnStart = null;
            OnUpdate?.Invoke(this, new(gameTime, string.Empty));
            SCLog.Log(VERBOSE, $"{{{@GetHashCode()}}}\tUpdate cycle number\t{_ui++}\t|\tPatch size\t{OnUpdate?.GetInvocationList().Length ?? 0}");
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            //Debug.WriteLine("Draw...");
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _ = new FPSMetric();
            //Log.Debug("{fps}", FPSMetric.FPS);

            //Render Patches
            _spriteBatch!.Begin();
            OnRender?.Invoke(this, new(gameTime, string.Empty));
            SCLog.Log(VERBOSE, $"{{{@GetHashCode()}}}\tRender cycle number\t{_ri++}\t|\tPatch size\t{OnRender?.GetInvocationList().Length ?? 0}");
            _spriteBatch!.End();

            base.Draw(gameTime);
        }

        protected override void EndRun()
        {
            Unload(managers: [.. _managers]);
            Debug.Debugger.Default?.DisposeAsync().AsTask().Wait();
            base.EndRun();
        }

        public void Load(IScapeCoreManager manager)
        {
            try
            {
                if (_managers.Add(manager))
                    SCLog.Log(DEBUG, $"Loading {Yellow}{manager}{Traceability.Logging.LoggingColor.Default} ...");
                else
                    SCLog.Log(DEBUG, $"{Red}{manager} load failed ...{Traceability.Logging.LoggingColor.Default}");
            }
            catch (Exception ex)
            {
                SCLog.Log(ERROR, $"{Red}Manager load error: {ex.Message}\n{ex.InnerException?.Message}{Traceability.Logging.LoggingColor.Default}");
                throw;
            }

            SCLog.Log(DEBUG, "Manager was correctly loaded.");
        }

        public void Unload(IScapeCoreManager manager)
        {
            try
            {
                if (_managers.Remove(manager))
                {
                    SCLog.Log(DEBUG, $"Unloading {Yellow}{manager}{Traceability.Logging.LoggingColor.Default} ...");
                    if (!manager.ExtractDependencies())
                    {
                        SCLog.Log(ERROR, $"{Red}Manager unload error: Failed to extract dependencies for manager {manager}{Traceability.Logging.LoggingColor.Default} ");
                        return;
                    }    
                }
                else
                    SCLog.Log(DEBUG, $"{Red}{manager} unload failed ...{Traceability.Logging.LoggingColor.Default}");
            }
            catch (Exception ex)
            {
                SCLog.Log(ERROR, $"{Red}Manager unload error: {ex.Message}\n{ex.InnerException?.Message}{Traceability.Logging.LoggingColor.Default}");
                throw;
            }

            SCLog.Log(DEBUG, "Manager was correctly unloaded.");
        }

        public void Load(params IScapeCoreManager[] managers)
        {
            foreach (var manager in managers)
                Load(manager);
        }

        public void Unload(params IScapeCoreManager[] managers)
        {
            foreach (var manager in managers)
                Unload(manager);
        }
    }
}
