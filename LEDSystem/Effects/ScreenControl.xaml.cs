using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using LEDSystem.UI.Helpers;
using LEDSystem.Core.Interfaces;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (05.07.2020)
    public partial class ScreenControl : UserControl, IEffect
    {
        #region Variables
        private IHandler uiHandler;
        private string prefName = "effect5";
        private bool isUserInputAllowed = false;
        private int captureMethod = 0;
        private int captureScreen = 0;
        private Bitmap frameBitmap;
        private int screenResizeWidth;
        private int screenResizeHeight;
        private int screenResizePixels;
        #endregion

        #region Main
        public ScreenControl(IHandler uiHandler)
        {
            // Imposta il gestore fornito al controllo.
            this.uiHandler = uiHandler;
            // Inizializza i componenti grafici.
            InitializeComponent();
            // Carica le preferenze dell'utente.
            LoadPreferences();
        }
        private void LoadPreferences()
        {
            // Impedisce agli elementi di salvare i propri valori.
            isUserInputAllowed = false;

            // Carica le preferenze relative al metodo di cattura.
            CBox_CaptureMethod.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "captureMethod");

            // Carica tutti i dispositivi di uscita del dispositivo.);
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens) {
                CBox_CaptureScreen.Items.Add(screen.DeviceName);
            }

            // Carica le preferenze relative allo schermo da catturare.
            CBox_CaptureScreen.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "captureScreen");

            // Imposta la velocità del timer.
            NBox_Speed.Value = Core.Preferences.GetPreference<int>(prefName, "logicSpeed");

            // Permette agli elementi di salvare i propri valori.
            isUserInputAllowed = true;
        }
        #endregion

        #region DirectX
        private bool isDX11Inizialized = false;
        private bool isDX11Running = false;
        private int DX11Screen = 0;
        private SharpDX.Direct3D11.Device DX11Device;
        private Output1 DX11Output;
        private int DX11Width;
        private int DX11Height;
        private Texture2D DX11Texture;
        private OutputDuplication DX11DuplicatedOutput;

        private void InitializeDX11(int screen)
        {
            if (!isDX11Inizialized || DX11Screen != screen)
            {
                var factory = new Factory1();
                var adapter = factory.GetAdapter1(0);
                DX11Device = new SharpDX.Direct3D11.Device(adapter);
                screen = adapter.GetOutputCount() < screen ? 0 : screen;
                var output = adapter.GetOutput(screen);
                DX11Output = output.QueryInterface<Output1>();

                // Width/Height of desktop to capture
                DX11Width = output.Description.DesktopBounds.Right;
                DX11Height = output.Description.DesktopBounds.Bottom;

                // Create Staging texture CPU-accessible
                var textureDesc = new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = DX11Width,
                    Height = DX11Height,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = { Count = 1, Quality = 0 },
                    Usage = ResourceUsage.Staging
                };

                DX11Texture = new Texture2D(DX11Device, textureDesc);
                DX11DuplicatedOutput = DX11Output.DuplicateOutput(DX11Device);
                isDX11Inizialized = true;
            }

            // Avvia la cattura.
            StartDX11();
        }
        private void StartDX11()
        {
            if (isDX11Inizialized) {
                isDX11Running = true;
            }
        }
        private void ProcessDX11()
        {
            try
            {
                SharpDX.DXGI.Resource screenResource;
                OutputDuplicateFrameInformation duplicateFrameInformation;

                // Try to get duplicated frame within given time is ms
                if (DX11DuplicatedOutput.TryAcquireNextFrame(10, out duplicateFrameInformation, out screenResource) == Result.Ok)
                {
                    // copy resource into memory that can be accessed by the CPU
                    using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                        DX11Device.ImmediateContext.CopyResource(screenTexture2D, DX11Texture);

                    // Get the desktop capture texture
                    var mapSource = DX11Device.ImmediateContext.MapSubresource(DX11Texture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                    // Create Drawing.Bitmap
                    using (var bitmap = new Bitmap(DX11Width, DX11Height, PixelFormat.Format32bppArgb))
                    {
                        var boundsRect = new Rectangle(0, 0, DX11Width, DX11Height);

                        // Copy pixels from screen capture Texture to GDI bitmap
                        var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var sourcePtr = mapSource.DataPointer;
                        var destPtr = mapDest.Scan0;
                        for (int y = 0; y < DX11Height; y++)
                        {
                            // Copy a single line 
                            Utilities.CopyMemory(destPtr, sourcePtr, DX11Width * 4);

                            // Advance pointers
                            sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                            destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                        }

                        // Release source and dest locks
                        bitmap.UnlockBits(mapDest);
                        DX11Device.ImmediateContext.UnmapSubresource(DX11Texture, 0);

                        OnCapturedFrame(bitmap);
                    }
                    screenResource.Dispose();
                    DX11DuplicatedOutput.ReleaseFrame();
                }
            }
            catch
            {
                uiHandler.OnEffectError();
            }
        }
        private void StopDX11()
        {
            if (isDX11Inizialized) {
                isDX11Running = false;
                Thread.Sleep(250);
            }
        }
        private void FreeDX11()
        {
            // Arresta il metodo di cattura.
            StopDX11();

            // Rilascia le risorse occupate dal metodo di cattura.
            if (isDX11Inizialized) {
                DX11Device.Dispose();
                DX11Output.Dispose();
                DX11DuplicatedOutput.Dispose();
                DX11Texture.Dispose();
                isDX11Inizialized = false;
            }
        }
        #endregion

        #region GDI+
        private bool isGDIInizialized = false;
        private bool isGDIRunning = false;
        private int GDIScreen = 0;
        private System.Drawing.Size GDISize;
        private Rectangle GDIRegion;
        private Graphics GDIGraphics;
        private Bitmap GDIBitmap;
        private int GDIX;
        private int GDIY;

        private void InitializeGDI(int screen)
        {
            if (!isGDIInizialized || GDIScreen != screen)
            {
                screen = System.Windows.Forms.Screen.AllScreens.Length < screen ? 0 : screen;
                GDIRegion = System.Windows.Forms.Screen.AllScreens[screen].Bounds;
                GDIX = GDIRegion.Location.X;
                GDIY = GDIRegion.Location.Y;
                GDISize = GDIRegion.Size;
                GDIBitmap = new Bitmap(GDIRegion.Width, GDIRegion.Height, PixelFormat.Format32bppArgb);
                GDIGraphics = Graphics.FromImage(GDIBitmap);
                isGDIInizialized = true;
            }

            // Avvia la cattura.
            StartGDI();
        }
        private void StartGDI()
        {
            if (isGDIInizialized) {
                isGDIRunning = true;
            }
        }
        private void ProcessGDI()
        {
            try
            {
                GDIGraphics.CopyFromScreen(GDIX, GDIY, 0, 0, GDISize, CopyPixelOperation.SourceCopy);
                OnCapturedFrame(GDIBitmap);
            }
            catch
            {
                uiHandler.OnEffectError();
            }
        }
        private void StopGDI()
        {
            if (isGDIInizialized) {
                isGDIRunning = false;
                Thread.Sleep(250);
            }
        }
        private void FreeGDI()
        {
            // Arresta il metodo di cattura.
            StopGDI();

            // Rilascia le risorse occupate dal metodo di cattura.
            if (isGDIInizialized) {
                GDIGraphics.Dispose();
                GDIBitmap.Dispose();;
                isGDIInizialized = false;
            }
        }
        #endregion

        #region Shared
        private void OnCapturedFrame(Bitmap frame)
        {
            // Ridimensiona il frame.
            using (Graphics g = Graphics.FromImage(frameBitmap))
            {
                g.DrawImage(frame, 0, 0, screenResizeWidth, screenResizeHeight);

                // Calcola il colore dominante del frame.
                int oldRed = 0;
                int oldGreen = 0;
                int oldBlue = 0;

                BitmapData bitmapData = frameBitmap.LockBits(new Rectangle(0, 0, screenResizeWidth, screenResizeHeight),
                    ImageLockMode.ReadWrite, frameBitmap.PixelFormat);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(frameBitmap.PixelFormat) / 8;
                int byteCount = bitmapData.Stride * screenResizeWidth;
                byte[] pixels = new byte[byteCount];
                IntPtr ptrFirstPixel = bitmapData.Scan0;
                Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;

                for (int y = 0; y < heightInPixels; y++)
                {
                    int currentLine = y * bitmapData.Stride;
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        oldBlue += pixels[currentLine + x];
                        oldGreen += pixels[currentLine + x + 1];
                        oldRed += pixels[currentLine + x + 2];
                    }
                }

                Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
                frameBitmap.UnlockBits(bitmapData);

                oldRed /= screenResizePixels;
                oldGreen /= screenResizePixels;
                oldBlue /= screenResizePixels;

                // Invia il colore dominante alla scheda.
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, (byte)oldRed, (byte)oldGreen, (byte)oldBlue);
            }
        }
        #endregion

        #region Logic
        public void OnCreate()
        {
            // Definizione delle variabili condivise da entrambi i metodi di cattura.
            screenResizeWidth = 64;
            screenResizeHeight = 64;
            screenResizePixels = screenResizeWidth * screenResizeWidth;
            frameBitmap = new Bitmap(screenResizeWidth, screenResizeHeight);

            // Inzializza il metodo di cattura salvato.
            switch (captureMethod)
            {
                case 0:
                    InitializeDX11(captureScreen);
                    break;
                case 1:
                    InitializeGDI(captureScreen);
                    break;
            }
        }
        public void OnUpdate()
        {
            // Processa i dati in base al metodo di cattura selezionato.
            switch (captureMethod)
            {
                case 0:
                    if (isDX11Inizialized && isDX11Running) {
                        ProcessDX11();
                    }
                    break;
                case 1:
                    if (isGDIInizialized && isGDIRunning) {
                        ProcessGDI();
                    }
                    break;
            }
        }
        public void OnAction(Tint tint)
        {
        }
        public void OnClosed()
        {
            // Libera le risorse di entrambi i metodi di cattura.
            FreeDX11();
            FreeGDI();

            // Libera le risorse condivise.
            frameBitmap = null;
        }
        #endregion

        #region Events
        private void CBox_CaptureMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            captureMethod = CBox_CaptureMethod.SelectedIndex;

            if (isUserInputAllowed) {

                // Arresta entrambi i metodi di cattura.
                StopDX11();
                StopGDI();

                // Reinizializza il metodo di cattura con lo schermo selezionato.
                switch (captureMethod) {
                    case 0:
                        InitializeDX11(captureScreen);
                        break;
                    case 1:
                        InitializeGDI(captureScreen);
                        break;
                }

                Core.Preferences.SetPreference<int>(prefName, "captureMethod", captureMethod);
            }
        }
        private void CBox_CaptureScreen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            captureScreen = CBox_CaptureScreen.SelectedIndex;

            if (isUserInputAllowed) {

                // Arresta entrambi i metodi di cattura.
                FreeDX11();
                FreeGDI();

                // Reinizializza il metodo di cattura con lo schermo selezionato.
                switch (captureMethod)  {
                    case 0:
                        InitializeDX11(captureScreen);
                        break;
                    case 1:
                        InitializeGDI(captureScreen);
                        break;
                }

                Core.Preferences.SetPreference<int>(prefName, "captureScreen", captureScreen);
            }
        }
        private void NBox_Speed_ValueChanged(object sender, RoutedEventArgs e)
        {
            uiHandler.SetInterval(NBox_Speed.Value);

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "logicSpeed", NBox_Speed.Value);
            }
        }
        #endregion
    }
}
