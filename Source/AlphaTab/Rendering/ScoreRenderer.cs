﻿/*
 * This file is part of alphaTab.
 * Copyright (c) 2014, Daniel Kuschny and Contributors, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or at your option any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */
using System;
using AlphaTab.Model;
using AlphaTab.Platform;
using AlphaTab.Rendering.Layout;
using AlphaTab.Rendering.Utils;

namespace AlphaTab.Rendering
{
    /// <summary>
    /// This is the main wrapper of the rendering engine which 
    /// can render a single track of a score object into a notation sheet.
    /// </summary>
    public class ScoreRenderer : IScoreRenderer
    {
        private string _currentLayoutMode;

        public ICanvas Canvas { get; set; }
        public Score Score { get; set; }
        public Track[] Tracks { get; set; }

        public ScoreLayout Layout { get; set; }

        public RenderingResources RenderingResources { get; set; }
        public Settings Settings { get; set; }

        public BoundsLookup BoundsLookup { get; set; }

        public ScoreRenderer(Settings settings)
        {
            Settings = settings;
            RenderingResources = new RenderingResources(1);
            if (settings.Engine == null || !Environment.RenderEngines.ContainsKey(settings.Engine))
            {
                Canvas = Environment.RenderEngines["default"]();
            }
            else
            {
                Canvas = Environment.RenderEngines[settings.Engine]();
            }
            RecreateLayout();
        }

        private bool RecreateLayout()
        {
            if (_currentLayoutMode != Settings.Layout.Mode)
            {
                if (Settings.Layout == null || !Environment.LayoutEngines.ContainsKey(Settings.Layout.Mode))
                {
                    Layout = Environment.LayoutEngines["default"](this);
                }
                else
                {
                    Layout = Environment.LayoutEngines[Settings.Layout.Mode](this);
                }
                _currentLayoutMode = Settings.Layout.Mode;
                return true;
            }
            return false;
        }

        public void Render(Track track)
        {
            Score = track.Score;
            Tracks = new []{track};
            Invalidate();
        }

        public void RenderMultiple(Track[] tracks)
        {
            if (tracks.Length == 0)
            {
                Score = null;
            }
            else
            {
                Score = tracks[0].Score;
            }

            Tracks = tracks;
            Invalidate();
        }


        public void UpdateSettings(Settings settings)
        {
            Settings = settings;
        }

        public void Invalidate()
        {
            BoundsLookup = new BoundsLookup();
            if (Tracks.Length == 0) return;
            if (RenderingResources.Scale != Settings.Scale)
            {
                RenderingResources.Init(Settings.Scale);
                Canvas.LineWidth = Settings.Scale;
            }
            Canvas.Resources = RenderingResources;
            OnPreRender();
            RecreateLayout();
            LayoutAndRender();
        }

        public void Resize(int width)
        {
            if (RecreateLayout())
            {
                Invalidate();
            }
            else if (Layout.SupportsResize)
            {
                OnPreRender();
                Settings.Width = width;
                Layout.Resize();
                Layout.RenderAnnotation();
                OnRenderFinished();
                OnPostRender();
            }
        }

        private void LayoutAndRender()
        {
            Layout.LayoutAndRender();
            Layout.RenderAnnotation();
            OnRenderFinished();
            OnPostRender();
        }

        public event Action<RenderFinishedEventArgs> PreRender;
        protected virtual void OnPreRender()
        {
            var result = Canvas.OnPreRender();
            var handler = PreRender;
            var args = new RenderFinishedEventArgs();
            args.TotalWidth = 0;
            args.TotalHeight = 0;
            args.Width = 0;
            args.Height = 0;
            args.RenderResult = result;

            if (handler != null) handler(args);
        }

        public event Action<RenderFinishedEventArgs> PartialRenderFinished;

        public virtual void OnPartialRenderFinished(RenderFinishedEventArgs e)
        {
            Action<RenderFinishedEventArgs> handler = PartialRenderFinished;
            if (handler != null) handler(e);
        }

        public event Action<RenderFinishedEventArgs> RenderFinished;
        protected virtual void OnRenderFinished()
        {
            var result = Canvas.OnRenderFinished();
            Action<RenderFinishedEventArgs> handler = RenderFinished;
            if (handler != null) handler(new RenderFinishedEventArgs
            {
                RenderResult = result,
                TotalHeight = Layout.Height,
                TotalWidth = Layout.Width
            });
        }

        public event Action PostRenderFinished;
        protected virtual void OnPostRender()
        {
            Action handler = PostRenderFinished;
            if (handler != null) handler();
        }

    }
}