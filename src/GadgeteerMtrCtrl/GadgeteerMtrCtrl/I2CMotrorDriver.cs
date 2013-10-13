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
    /// <summary>
    /// DRV8830���[�^�[����N���X
    /// </summary>
    class I2CMotrorDriver
    {
        public const ushort MTR_ADDR_0_0 = 0x60;
        public const ushort MTR_ADDR_0_OPEN = 0x61;
        public const ushort MTR_ADDR_0_1 = 0x62;
        public const ushort MTR_ADDR_OPEN_0 = 0x63;
        public const ushort MTR_ADDR_OPEN_OPEN = 0x64;
        public const ushort MTR_ADDR_OPEN_1 = 0x65;
        public const ushort MTR_ADDR_1_0 = 0x66;
        public const ushort MTR_ADDR_1_OPEN = 0x67;
        public const ushort MTR_ADDR_1_1 = 0x68;

        private const int COMM_TIMEOUT = 100;
        private const byte MTR_FREE = 0x00;
        private const byte MTR_NORMAL = 0x01;
        private const byte MTR_REVERSE = 0x02;
        private const byte MTR_BRAKE = 0x03;
        private const byte MTR_STATE_BIT = 0x03;

        private GTI.I2CBus i2c;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="socket">�ڑ��\�P�b�g</param>
        /// <param name="addr">�h���C�o���W���[���A�h���X</param>
        public I2CMotrorDriver(GT.Socket socket, ushort addr)
        {
            i2c = new GTI.I2CBus(socket, addr, 100, null);
        }

        /// <summary>
        /// �u���[�L
        /// </summary>
        public void Brake()
        {
            byte[] write = new byte[] { 0x00, MTR_BRAKE };
            i2c.Write(write, COMM_TIMEOUT);
        }

        /// <summary>
        /// �X�s�[�h�擾
        /// </summary>
        /// <returns>�X�s�[�h�l</returns>
        public int GetSPeed()
        {
            byte[] write = new byte[] { 0x00 };
            byte[] read = new byte[1];

            i2c.WriteRead(write, read, COMM_TIMEOUT);

            int speed = 0;
            switch (read[1] & MTR_STATE_BIT)
            {
                case MTR_NORMAL:
                    speed = (read[1] >> 2);
                    break;
                case MTR_REVERSE:
                    speed = -1 * (read[1] >> 2);
                    break;
                default:
                    break;
            }

            return speed;
        }

        /// <summary>
        /// �X�s�[�h�ݒ�
        /// </summary>
        /// <param name="speed">�ݒ�l</param>
        public void SetSpeed(int speed)
        {
            byte dir =MTR_FREE;
            byte power = 0;

            if (speed > 0)
            {
                dir = MTR_NORMAL;
                power = (byte)(speed & 0x3f);
            }
            else if (speed < 0)
            {
                dir = MTR_REVERSE;
                power = (byte)((-1 * speed) & 0x3f);
            }

            power = (byte)((power << 2) | dir);

            byte[] write = new byte[] { 0x00, power };

            i2c.Write(write, COMM_TIMEOUT);
        }
    }
}
