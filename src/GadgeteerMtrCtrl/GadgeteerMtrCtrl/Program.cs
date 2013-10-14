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

            if (controller.IsUp)
            {
                // 上ボタン押下なら左モーターを前進
                SetMotorState(40, mtrLeft, ledLeft);
            }
            else if (controller.IsDown)
            {
                // 下ボタン押下なら左モーターを後退
                SetMotorState(-40, mtrLeft, ledLeft);
            }
            else if (controller.NotUpDown)
            {
                // 上下ボタンどちらも非押下なら左モーターを停止
                SetMotorState(0, mtrLeft, ledLeft);
            }

            if (controller.IsTriungle)
            {
                // △ボタン押下なら右モーターを前進
                SetMotorState(40, mtrRight, ledRight);
            }
            else if (controller.IsCross)
            {
                // ×ボタン押下なら右モーターを後退
                SetMotorState(-40, mtrRight, ledRight);
            }
            else if (controller.NotTriungleCross)
            {
                // △×ボタンどちらも非押下なら右モーターを停止
                SetMotorState(0, mtrRight, ledRight);
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
    }
}
