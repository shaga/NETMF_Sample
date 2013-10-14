using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.Interfaces;
using Gadgeteer.Modules.GHIElectronics;

namespace GadgeteerMtrCtrl
{
    class SpiController
    {
        private const int MAX_ERROR_COUNT = 5;
        private const int RCV_MODE_POS = 1;
        private const int RCV_RES_POS = 2;
        private const int RCV_DIGITAL_UPPER_POS = 3;
        private const int RCV_DIGITAL_LOWER_POS = 4;
        private const int RCV_ANALOG_RIGHT_LR = 5;
        private const int RCV_ANALOG_RIGHT_UD = 6;
        private const int RCV_ANALOG_LEFT_LR = 7;
        private const int RCV_ANALGO_LEFT_UD = 8;

        private const byte RCV_OK = 0x5a;
        private const byte RCV_MODE_DIGITAL = 0x82;
        private const byte RCV_MODE_ANALOG = 0xce;

        private const byte ANALOG_VALUE_MIN = 0;
        private const byte ANALOG_VALUE_CEN = 0x80;
        private const byte ANALOG_VALUE_MAX = 0xFF;

        private SPI spi;
        private byte[] write = new byte[] { 0x80, 0x42, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private byte[] read = new byte[9];

        private byte digitalUpper = 0xff;
        private byte digitalLower = 0xff;

        private byte analogRightUD = 0x80;
        private byte analogLeftUD = 0x80;

        private int error_cnt = 0;
        private EMode mode = EMode.Error;

        /// <summary>
        /// モード
        /// </summary>
        public EMode Mode
        {
            get { return mode; }
        }

        [Flags]
        private enum EDigitalUpper
        {
            None = 0x00,
            Left = 0x01,
            Down = 0x02,
            Right = 0x04,
            Up = 0x08,
            Start = 0x10,
            RS = 0x20,
            LS = 0x40,
            Select = 0x80,
        };

        [Flags]
        private enum EDigitalLower
        {
            None = 0x00,
            Square = 0x01,
            Cross = 0x02,
            Circle = 0x04,
            Triungle = 0x08,
            R1 = 0x10,
            L1 = 0x20,
            R2 = 0x40,
            L2 = 0x80,
        };

        public enum EMode
        {
            Error = -1,
            Digital,
            Analog,
        };

        /// <summary>
        /// 上ボタン状態
        /// </summary>
        public bool IsUp
        {
            get
            {
                if (mode == EMode.Error)
                {
                    return false;
                }

                return (~digitalUpper & (int)EDigitalUpper.Up) == (int)EDigitalUpper.Up;
            }
        }

        /// <summary>
        /// 下ボタン状態
        /// </summary>
        public bool IsDown
        {
            get
            {
                if (mode == EMode.Error)
                {
                    return false;
                }

                return (~digitalUpper & (int)EDigitalUpper.Down) == (int)EDigitalUpper.Down;
            }
        }

        /// <summary>
        /// 上下ボタンいずれも非押下
        /// </summary>
        public bool NotUpDown
        {
            get
            {
                return !IsUp && !IsDown;
            }
        }

        /// <summary>
        /// △ボタン状態
        /// </summary>
        public bool IsTriungle
        {
            get
            {
                if (mode == EMode.Error)
                {
                    return false;
                }

                return (~digitalLower & (int)EDigitalLower.Triungle) == (int)EDigitalLower.Triungle;
            }
        }

        /// <summary>
        /// ×ボタン状態
        /// </summary>
        public bool IsCross
        {
            get
            {
                if (mode == EMode.Error)
                {
                    return false;
                }

                return (~digitalLower & (int)EDigitalLower.Cross) == (int)EDigitalLower.Cross;
            }
        }

        /// <summary>
        /// △×ボタンいずれも非押下
        /// </summary>
        public bool NotTriungleCross
        {
            get
            {
                return !IsTriungle && !IsCross;
            }
        }

        /// <summary>
        /// エラーの連続回数が最大数超過
        /// </summary>
        public bool IsMaxError
        {
            get { return error_cnt >= MAX_ERROR_COUNT; }
        }

        public bool IsR1
        {
            get
            {
                return (~digitalLower & (int)EDigitalLower.R1) == (int)EDigitalLower.R1;
            }
        }

        public bool IsR2
        {
            get
            {
                return (~digitalLower & (int)EDigitalLower.R2) == (int)EDigitalLower.R2;
            }
        }

        public bool IsL1
        {
            get
            {
                return (~digitalLower & (int)EDigitalLower.L1) == (int)EDigitalLower.L1;
            }
        }

        public bool IsL2
        {
            get
            {
                return (~digitalLower & (int)EDigitalLower.L2) == (int)EDigitalLower.L2;
            }
        }

        public int RigthUpDown
        {
            get
            {
                return (int)(((analogRightUD & 0xFE) - ANALOG_VALUE_CEN) * 100 / (double)ANALOG_VALUE_CEN);
            }
        }

        public int LeftUpDown
        {
            get
            {
                return (int)(((analogLeftUD & 0xFE) - ANALOG_VALUE_CEN) * 100 / (double)ANALOG_VALUE_CEN);
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pin">SSピン</param>
        /// <param name="module">SPIモジュール</param>
        public SpiController(Cpu.Pin pin, SPI.SPI_module module)
        {
            SPI.Configuration config = new SPI.Configuration(pin, false, 1, 1, false, true, 200, module);
            spi = new SPI(config);
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns>通信状態</returns>
        public bool Update()
        {
            spi.WriteRead(write, read);

            if (read[RCV_RES_POS] != RCV_OK)
            {
                error_cnt++;
                mode = EMode.Error;
                return false;
            }

            switch (read[RCV_MODE_POS])
            {
                case RCV_MODE_DIGITAL:
                    mode = EMode.Digital;
                    break;
                case RCV_MODE_ANALOG:
                    mode = EMode.Analog;
                    break;
                default:
                    error_cnt++;
                    mode = EMode.Error;
                    return false;
            }

            digitalUpper = read[RCV_DIGITAL_UPPER_POS];
            digitalLower = read[RCV_DIGITAL_LOWER_POS];

            error_cnt = 0;

            return true;
        }

        /// <summary>
        /// エラーカウントリセット
        /// </summary>
        public void ResetError()
        {
            error_cnt = 0;
        }
    }
}
