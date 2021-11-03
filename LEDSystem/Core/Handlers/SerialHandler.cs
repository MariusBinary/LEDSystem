using System;
using System.Windows;
using System.Net.Sockets;
using System.IO.Ports;
using LEDSystem.Core.Interfaces;

namespace LEDSystem.Core.Handlers
{
    public class SerialHandler
    {
        #region Variables
        private Window uiContext;
        private ISerial serialInterface;
        private SafeSerialPort serialPort;
        private UdpClient udpClient;
        private int connectionType = -1;
        private int lightsCount = 0;
        private byte[] packet;
        #endregion

        #region Main
        /// <summary>
        /// Punto di ingresso dell gestore.
        /// </summary>
        public SerialHandler(Window uiContext)
        {
            this.uiContext = uiContext;
            this.serialInterface = uiContext as ISerial;
        }
        #endregion

        #region Controls
        /// <summary>
        /// Effettua una connessione con attraverso la porta seriale indicata.
        /// </summary>
        public void Connect(int connectionType, string param1, int param2)
        {
            try
            {
                // Aggiorna il tipo di connesione.
                this.connectionType = connectionType;

                if (connectionType == 0)
                {
                    // Avvia la connessione di tipo USB.
                    serialPort = new SafeSerialPort {
                        PortName = param1,
                        BaudRate = param2,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One
                    };
                    serialPort.Open();
                }
                else if (connectionType == 1)
                {
                    // Avvia la connessione di tipo LAN.
                    udpClient = new UdpClient();
                    udpClient.Connect(param1, param2);
                }

                uiContext.Dispatcher.Invoke(new Action(delegate {
                    serialInterface.OnBoardConnect();
                }));
            }
            catch
            {
                uiContext.Dispatcher.Invoke(new Action(delegate
                {
                    serialInterface.OnBoardError();
                }));
            }
        }
        /// <summary>
        /// Effettua una disconessione e un rilascio della porta seriale.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (connectionType == 0)
                {
                    // Chiude la connessione di tipo USB.
                    serialPort.Close();
                    serialPort.Dispose();
                }
                else if (connectionType == 1)
                {
                    // Chiude la connessione di tipo LAN.
                    udpClient.Close();
                    udpClient.Dispose();
                }

                uiContext.Dispatcher.Invoke(new Action(delegate {
                    serialInterface.OnBoardDisconnect();
                }));
            }
            catch
            {
                uiContext.Dispatcher.Invoke(new Action(delegate {
                    serialInterface.OnBoardError();
                }));
            }
        }
        /// <summary>
        /// Effettua una disconessione e un rilascio della porta seriale.
        /// </summary>
        public void SetLights(int lightsCount)
        {
            this.lightsCount = lightsCount;
            this.packet = new byte[lightsCount * 5];
        }
        #endregion

        #region Write
        /// <summary>
        /// Scrive sulla porta seriale l'array di byte ricevuto.
        /// </summary>
        public void Write(byte[] data)
        {
            try
            {
                if (connectionType == 0)
                {
                    // Invia i dati sulla connessione di tipo USB.
                    serialPort.Write(data, 0, data.Length);
                }
                else if (connectionType == 1)
                {
                    // Invia i dati sulla connessione di tipo LAN.
                    udpClient.Send(data, data.Length);
                }
            }
            catch
            {
                uiContext.Dispatcher.Invoke(new Action(delegate {
                    serialInterface.OnBoardError();
                }));
            }
        }
        /// <summary>
        /// Decodifica i dati di output e li scrive sulla porta seriale.
        /// </summary>
        public void Write(int type, int index, byte code, byte alpha, byte red, byte green, byte blue)
        {
            switch (type)
            {
                case 0:
                    for (int i = 0; i < lightsCount; i++) {
                        packet[(i * 5)] = code;
                        packet[(i * 5) + 1] = alpha;
                        packet[(i * 5) + 2] = red;
                        packet[(i * 5) + 3] = green;
                        packet[(i * 5) + 4] = blue;
                    }
                    Write(packet);
                    break;
                case 1:
                    for (int i = 0; i < lightsCount; i++) {
                        packet[(i * 5)] = (i == index) ? code : (byte)0x00;
                        packet[(i * 5) + 1] = (i == index) ? alpha : (byte)0x00;
                        packet[(i * 5) + 2] = (i == index) ? red : (byte)0x00;
                        packet[(i * 5) + 3] = (i == index) ? green : (byte)0x00;
                        packet[(i * 5) + 4] = (i == index) ? blue : (byte)0x00;
                    }
                    Write(packet);
                    break;
                case 2:
                    packet[(index * 5)] = code;
                    packet[(index * 5) + 1] = alpha;
                    packet[(index * 5) + 2] = red;
                    packet[(index * 5) + 3] = green;
                    packet[(index * 5) + 4] = blue;
                    break;
                case 3:
                    Write(packet);
                    break;
            }
        }
        #endregion
    }
}
