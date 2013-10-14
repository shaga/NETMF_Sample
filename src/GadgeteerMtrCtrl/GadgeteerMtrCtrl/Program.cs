using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.Interfaces;

namespace GadgeteerMtrCtrl
{
    public partial class Program
    {
        private GTM.GHIElectronics.MulticolorLed ledLeft;
        private GTM.GHIElectronics.MulticolorLed ledRight;

        private I2CMotrorDriver mtrLeft;
        private I2CMotrorDriver mtrRight;

        private SpiController controller;

        private int speed = 10;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/

            // MutliColoer LED Module
            ledLeft = multicolorLed1;
            ledRight = multicolorLed;

            // Create Motor Drive Objects
            GT.Socket sock3 = GT.Socket.GetSocket(3, true, null, null);

            mtrLeft = new I2CMotrorDriver(sock3, I2CMotrorDriver.MTR_ADDR_0_0);
            mtrRight = new I2CMotrorDriver(sock3, I2CMotrorDriver.MTR_ADDR_1_0);

            // Create SPI Controller Objcets
            GT.Socket sock6 = GT.Socket.GetSocket(6, true, null, null);

            controller = new SpiController(sock6.CpuPins[6], sock6.SPIModule);

            // 100msec Timer
            GT.Timer timer = new GT.Timer(100);

            timer.Tick += timer_Tick;

            timer.Start();

            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
        }

        /// <summary>
        /// コントローラー監視
        /// </summary>
        /// <param name="timer"></param>
        void timer_Tick(GT.Timer timer)
        {
            // コントローラー状態更新
            if (!controller.Update())
            {
                // 通信エラー
                if (controller.IsMaxError)
                {
                    // 連続エラー回数が上限を超えた
                    controller.ResetError();                // エラーカウントをリセット
                    SetMotorState(0, mtrLeft, ledLeft);     // 左モーター停止
                    SetMotorState(0, mtrRight, ledRight);   // 右モーター停止
                }

                return;
            }

            if (controller.Mode == SpiController.EMode.Digital)
            {
                mtrLeft.SetSpeed(0);
                mtrRight.SetSpeed(0);
                ledLeft.TurnRed();
                ledRight.TurnRed();
                return;
            }

            if (controller.IsL2)
            {
                this.speed += 10;
            }
            if (controller.IsR2)
            {
                this.speed -= 10;
            }

            if (speed > 100) speed = 100;
            else if (speed < 10) speed = 10;

            if (controller.IsL1)
            {
                mtrLeft.Brake();
                ledLeft.TurnWhite();
            }
            else
            {
                if (controller.NotUpDown)
                {
                    SetMotorStateRate(0, false, mtrLeft, ledLeft);
                }
                else if (controller.IsUp)
                {
                    SetMotorStateRate(speed, true, mtrLeft, ledLeft);
                }
                else if (controller.IsDown)
                {
                    SetMotorStateRate(speed, false, mtrLeft, ledLeft);
                }
            }

            if (controller.IsR1)
            {
                mtrRight.Brake();
                ledRight.TurnWhite();
            }
            else
            {
                if (controller.NotTriungleCross)
                {
                    SetMotorStateRate(0, false, mtrRight, ledRight);
                }
                else if (controller.IsTriungle)
                {
                    SetMotorStateRate(speed, true, mtrRight, ledRight);
                }
                else if (controller.IsCross)
                {
                    SetMotorStateRate(speed, false, mtrRight, ledRight);
                }
            }
        }

        /// <summary>
        /// モーター状態設定
        /// </summary>
        /// <param name="speed">速度</param>
        /// <param name="mtr">対象モーター</param>
        /// <param name="led">対象LED</param>
        private void SetMotorState(int speed, I2CMotrorDriver mtr, GTM.GHIElectronics.MulticolorLed led)
        {
            if (speed > 0)
            {
                // スピードが前進ならLEDを緑に
                led.TurnGreen();
            }
            else if (speed < 0)
            {
                // スピードが後退ならLEDを青に
                led.TurnBlue();
            }
            else
            {
                // 停止ならLEDを消灯
                led.TurnOff();
            }

            mtr.SetSpeed(speed);
        }

        private void SetMotorStateRate(int speed, bool forward, I2CMotrorDriver mtr, GTM.GHIElectronics.MulticolorLed led)
        {
            if (speed == 0)
            {
                led.TurnOff();
                mtr.SetSpeed(0);
                return;
            }

            speed *= forward ? 1 : -1;
            mtr.SetSpeedRate(speed);
            if (forward) led.TurnGreen();
            else led.TurnBlue();
        }
    }
}
