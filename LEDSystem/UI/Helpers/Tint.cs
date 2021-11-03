using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json.Linq;

namespace LEDSystem.UI.Helpers
{
    public class Tint
    {
        #region RGB Variables
        private byte _red = 255;
        public byte Red
        {
            get { return _red; }
            set { _red = value;
                UpdateFromRGB(_red, _green, _blue);
            }
        }
        private byte _green = 255;
        public byte Green
        {
            get { return _green; }
            set { _green = value;
                UpdateFromRGB(_red, _green, _blue);
            }
        }
        private byte _blue = 255;
        public byte Blue
        {
            get { return _blue; }
            set { _blue = value;
                UpdateFromRGB(_red, _green, _blue);
            }
        }
        #endregion

        #region HSV Variables
        private double _hue = 360;
        public double Hue
        {
            get { return _hue; }
            set { _hue = value; 
                UpdateFromHSV(_hue, _saturation, _value); 
            }
        }
        private double _saturation = 100;
        public double Saturation
        {
            get { return _saturation; }
            set { _saturation = value;
                UpdateFromHSV(_hue, _saturation, _value);
            }
        }
        private double _value = 100;
        public double Value
        {
            get { return _value; }
            set { _value = value;
                UpdateFromHSV(_hue, _saturation, _value);
            }
        }
        #endregion

        #region HEX Variables
        private string _hex = "#FFFFFF";
        public string HEX
        {
            get { return _hex; }
            set { _hex = value;
                UpdateFromHEX(_hex);
            }
        }
        #endregion

        #region Gradient Variables
        private int _type = 0;
        public int Type
        {
            get { return _type; }
            set { _type = value; }
        }
        private List<Tint> _points = null;
        public List<Tint> Points
        {
            get { return _points; }
            set { _points = value; }
        }
        private double _offset = -1;
        public double Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        #endregion

        #region Main
        /// <summary>
        /// Costruttore della classe senza parametri.
        /// </summary>
        public Tint()
        {

        }
        /// <summary>
        /// Costruttore della classe partendo da un valore HEX fornito.
        /// </summary>
        public Tint(string hex)
        {
            HEX = hex;
        }
        /// <summary>
        /// Costruttore della classe partendo da un valore RGB fornito.
        /// </summary>
        public Tint(byte r, byte g, byte b)
        {
            Red = r;
            Green = g;
            Blue = b;
        }
        /// <summary>
        /// Costruttore della classe partendo da un valore RGB + offset fornito.
        /// </summary>
        public Tint(byte r, byte g, byte b, double offset)
        {
            Red = r;
            Green = g;
            Blue = b;
            Offset = offset;
        }
        /// <summary>
        /// Aggiorna i valori HSV e HEX in base ai nuovi valori RGB forniti.
        /// </summary>
        private void UpdateFromRGB(byte r, byte g, byte b)
        {
            // Aggiorno il valore HSV
            int max = Math.Max(r, Math.Max(g, b));
            int min = Math.Min(g, Math.Min(g, b));

            _hue = Math.Round(GetHueFromRGB(r, g, b), 2);
            _saturation = ((max == 0) ? 0 : 1d - (1d * min / max)) * 100;
            _saturation = Math.Round(_saturation, 2);
            _value = Math.Round(((max / 255d) * 100), 2);

            // Aggiorno il valore HEX
            _hex =  "#" + _red.ToString("X2") + _green.ToString("X2") + _blue.ToString("X2");
        }
        /// <summary>
        /// Aggiorna i valori RGB e HEX in base ai nuovi valori HSV forniti.
        /// </summary>
        private void UpdateFromHSV(double h, double s, double v)
        {
            // Aggiorno il valore RGB
            s = (s * 1) / 100;
            v = (v * 1) / 100;

            int hi = (int)Math.Floor(h / 60.0) % 6;
            double f = (h / 60.0) - Math.Floor(h / 60.0);

            double p = v * (1.0 - s);
            double q = v * (1.0 - (f * s));
            double t = v * (1.0 - ((1.0 - f) * s));

            switch (hi)
            {
                case 0:
                    _red = (byte)(v * 255.0);
                    _green = (byte)(t * 255.0);
                    _blue = (byte)(p * 255.0);
                    break;
                case 1:
                    _red = (byte)(q * 255.0);
                    _green = (byte)(v * 255.0);
                    _blue = (byte)(p * 255.0);
                    break;
                case 2:
                    _red = (byte)(p * 255.0);
                    _green = (byte)(v * 255.0);
                    _blue = (byte)(t * 255.0);
                    break;
                case 3:
                    _red = (byte)(p * 255.0);
                    _green = (byte)(q * 255.0);
                    _blue = (byte)(v * 255.0);
                    break;
                case 4:
                    _red = (byte)(t * 255.0);
                    _green = (byte)(p * 255.0);
                    _blue = (byte)(v * 255.0);
                    break;
                case 5:
                    _red = (byte)(v * 255.0);
                    _green = (byte)(p * 255.0);
                    _blue = (byte)(q * 255.0);
                    break;
                default:
                    _red = 0x00;
                    _green = 0x00;
                    _blue = 0x00;
                    break;
            }

            // Aggiorno il valore HEX
            _hex = "#" + _red.ToString("X2") + _green.ToString("X2") + _blue.ToString("X2");
        }
        /// <summary>
        /// Aggiorna i valori RGB e HSV in base ai nuovi valori HEX forniti.
        /// </summary>
        private void UpdateFromHEX(string hex)
        {
            // Verifica se il valore esadecimale fornito è valido
            if (!Regex.IsMatch(hex, @"^#([A-Fa-f0-9]{8}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$") && hex.Length <= 9)
                return;

            // Aggiorno il valore RGB
            int r, g, b;

            if (hex.Length > 7)
            {
                r = ParseHexChar(hex[3]) * 16 + ParseHexChar(hex[4]);
                g = ParseHexChar(hex[5]) * 16 + ParseHexChar(hex[6]);
                b = ParseHexChar(hex[7]) * 16 + ParseHexChar(hex[8]);
            }
            else if (hex.Length > 5)
            {
                r = ParseHexChar(hex[1]) * 16 + ParseHexChar(hex[2]);
                g = ParseHexChar(hex[3]) * 16 + ParseHexChar(hex[4]);
                b = ParseHexChar(hex[5]) * 16 + ParseHexChar(hex[6]);
            }
            else if (hex.Length > 4)
            {
                r = ParseHexChar(hex[2]);
                r = r + r * 16;
                g = ParseHexChar(hex[3]);
                g = g + g * 16;
                b = ParseHexChar(hex[4]);
                b = b + b * 16;
            }
            else
            {
                r = ParseHexChar(hex[1]);
                r = r + r * 16;
                g = ParseHexChar(hex[2]);
                g = g + g * 16;
                b = ParseHexChar(hex[3]);
                b = b + b * 16;
            }

            _red = (byte)r;
            _green = (byte)g;
            _blue = (byte)b;

            // Aggiorno il valore HSV
            int max = Math.Max((byte)r, Math.Max((byte)g, (byte)b));
            int min = Math.Min((byte)g, Math.Min((byte)g, (byte)b));

            _hue = Math.Round(GetHueFromRGB((byte)r, (byte)g, (byte)b), 2);
            _saturation = ((max == 0) ? 0 : 1d - (1d * min / max)) * 100;
            _saturation = Math.Round(_saturation, 2);
            _value = Math.Round(((max / 255d) * 100), 2);
        }
        #endregion

        #region Internal Functions
        ///<summary> 
        /// Metodo di clonazione, ritorna una classe avente gli stessi valori di questa ma 
        /// indipendente da questa se modificata.
        ///</summary>
        public Tint Clone(Tint tint = null)
        {
            if (tint == null)
            {
                return Deserialize(Serialize(this));
            }
            else
            {
                return Deserialize(Serialize(tint));
            }
        }
        ///<summary> 
        /// Metodo di uguaglianza, ritorna se i due colori forniti in ingresso sono uguali.
        ///</summary>
        public Color GetColor(byte alpha = 255)
        {
            return Color.FromArgb(alpha, _red, _green, _blue);
        }
        ///<summary> 
        /// Metodo di uguaglianza, ritorna se i due colori forniti in ingresso sono uguali.
        ///</summary>
        public Brush GetBrush(byte alpha = 255)
        {
            if (_type == 0)
                return new SolidColorBrush(GetColor(alpha));

            LinearGradientBrush brushColor = new LinearGradientBrush {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            foreach (var point in Points) {
                brushColor.GradientStops.Add(new GradientStop(point.GetColor(), point.Offset));
            }

            return brushColor;
        }
        ///<summary> 
        /// Metodo di uguaglianza, ritorna se i due colori forniti in ingresso sono uguali.
        ///</summary>
        public Tint GetPoint(double offset)
        {
            // Se presente un singolo colore, lo ritorna annullando il resto della funzione.
            if (Points.Count == 1)
                return Clone(Points[0]);

            Tint before = null;
            Tint after = null;

            foreach (Tint point in Points)
            {
                if (point.Offset < offset)
                {
                    before = point;
                }

                if (after == null)
                {
                    if (point.Offset > offset)
                    {
                        after = point;
                    }
                }
            }

            if (before == null)
            {
                return Clone(Points[0]);
            }

            if (after == null)
            {
                return Clone(Points[Points.Count - 1]);
            }

            offset = Math.Round((offset - before.Offset) / (after.Offset - before.Offset), 2);

            return new Tint
            {
                Red = (byte)((after.Red - before.Red) * offset + before.Red),
                Green = (byte)((after.Green - before.Green) * offset + before.Green),
                Blue = (byte)((after.Blue - before.Blue) * offset + before.Blue),
                Offset = offset
            };
        }
        #endregion

        #region Static Functions
        ///<summary> 
        /// Metodo di uguaglianza, ritorna se i due colori forniti in ingresso sono uguali.
        ///</summary>
        public static bool Equals(Tint tint1, Tint tint2)
        {
            return (tint1 == tint2);
        }
        /// <summary>
        /// Restituisce un punto di colore serializzato in una stringa di testo 
        /// in modo che possa essere salvato in memoria.
        /// </summary>
        public static JObject Serialize(Tint tint)
        {
            return JObject.FromObject(tint);
        }  
        /// <summary>
        /// Restituisce un punto di colore deserializzato da una stringa di testo 
        /// salvata in precedenza e recuperata dalla memoria.
        /// </summary>
        public static Tint Deserialize(JObject value)
        {
            if (value == null)
                return new Tint();

            return value.ToObject<Tint>();
        }
        #endregion

        #region Utils
        /// <summary>
        /// Restituisce un numero intero che rappresenta il carattere fornito.
        /// </summary>
        private int ParseHexChar(char c)
        {
            int intChar = (int)c;
            if ((intChar >= 0x30) && (intChar <= (0x30 + 9)))
                return (intChar - 0x30);
            if ((intChar >= 0x61) && (intChar <= (0x61 + 5)))
                return (intChar - 0x61 + 10);
            if ((intChar >= 0x41) && (intChar <= (0x41 + 5)))
                return (intChar - 0x41 + 10);
            return -1;
        }
        /// <summary>
        /// Restituisce un numero intero che rappresenta il carattere fornito.
        /// </summary>
        private float GetHueFromRGB(byte red, byte green, byte blue)
        {
            if (red == green && green == blue)
                return 0;

            float r = (float)red / 255.0f;
            float g = (float)green / 255.0f;
            float b = (float)blue / 255.0f;

            float max, min;
            float delta;
            float hue = 0.0f;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            delta = max - min;

            if (r == max)
            {
                hue = (g - b) / delta;
            }
            else if (g == max)
            {
                hue = 2 + (b - r) / delta;
            }
            else if (b == max)
            {
                hue = 4 + (r - g) / delta;
            }
            hue *= 60;

            if (hue < 0.0f)
            {
                hue += 360.0f;
            }
            return hue;
        }
        /// <summary>
        /// Restituisce un numero intero che rappresenta il carattere fornito.
        /// </summary>
        public Color GetRGBFromHue(double h = 360, double s = 100, double v = 100)
        {
            // Creo un colore.
            Color color = new Color();
            color.A = 255;

            // Riempio il colore in base al hue.
            h = _hue;
            s = (s * 1) / 100;
            v = (v * 1) / 100;

            int hi = (int)Math.Floor(h / 60.0) % 6;
            double f = (h / 60.0) - Math.Floor(h / 60.0);

            double p = v * (1.0 - s);
            double q = v * (1.0 - (f * s));
            double t = v * (1.0 - ((1.0 - f) * s));

            switch (hi)
            {
                case 0:
                    color.R = (byte)(v * 255.0);
                    color.G = (byte)(t * 255.0);
                    color.B = (byte)(p * 255.0);
                    break;
                case 1:
                    color.R = (byte)(q * 255.0);
                    color.G = (byte)(v * 255.0);
                    color.B = (byte)(p * 255.0);
                    break;
                case 2:
                    color.R = (byte)(p * 255.0);
                    color.G = (byte)(v * 255.0);
                    color.B = (byte)(t * 255.0);
                    break;
                case 3:
                    color.R = (byte)(p * 255.0);
                    color.G = (byte)(q * 255.0);
                    color.B = (byte)(v * 255.0);
                    break;
                case 4:
                    color.R = (byte)(t * 255.0);
                    color.G = (byte)(p * 255.0);
                    color.B = (byte)(v * 255.0);
                    break;
                case 5:
                    color.R = (byte)(v * 255.0);
                    color.G = (byte)(p * 255.0);
                    color.B = (byte)(q * 255.0);
                    break;
                default:
                    color.R = 0x00;
                    color.G = 0x00;
                    color.B = 0x00;
                    break;
            }

            return color;
        }
        #endregion
    }
}
