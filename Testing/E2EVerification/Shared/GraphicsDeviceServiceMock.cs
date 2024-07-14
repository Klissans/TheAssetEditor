﻿using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;

namespace E2EVerification.Shared
{
    public class GraphicsDeviceServiceMock : IGraphicsDeviceService
    {
        GraphicsDevice? _GraphicsDevice;
        Form? HiddenForm;

        public GraphicsDeviceServiceMock()
        {
            HiddenForm = new Form()
            {
                Visible = false,
                ShowInTaskbar = false
            };

            var Parameters = new PresentationParameters()
            {
                BackBufferWidth = 1280,
                BackBufferHeight = 720,
                DeviceWindowHandle = HiddenForm.Handle,
                PresentationInterval = PresentInterval.Immediate,
                IsFullScreen = false
            };

            _GraphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, Parameters);
        }

        public GraphicsDevice GraphicsDevice
        {
            get { return _GraphicsDevice!; }
        }

#pragma warning disable CS0067 
        public event EventHandler<EventArgs>? DeviceCreated;
        public event EventHandler<EventArgs>? DeviceDisposing;
        public event EventHandler<EventArgs>? DeviceReset;
        public event EventHandler<EventArgs>? DeviceResetting;
#pragma warning restore CS0067 
        public void Release()
        {
            if (_GraphicsDevice != null)
            {
                _GraphicsDevice.Dispose();
                _GraphicsDevice = null;
            }

            if (HiddenForm != null)
            {
                HiddenForm.Close();
                HiddenForm.Dispose();
                HiddenForm = null;
            }
        }
    }
}
