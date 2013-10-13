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

        void timer_Tick(GT.Timer timer)
        {
            if (!controller.Update())
            {
                if (controller.IsMaxError)
                {
                    controller.ResetError();
                    SetMotorState(0, mtrLeft, ledLeft);
                    SetMotorState(0, mtrRight, ledRight);
                }

                return;
            }

            if (controller.IsUp)
            {
                SetMotorState(40, mtrLeft, ledLeft);
            }
            else if (controller.IsDown)
            {
                SetMotorState(-40, mtrLeft, ledLeft);
            }
            else if (controller.NotUpDown)
            {
                SetMotorState(0, mtrLeft, ledLeft);
            }

            if (controller.IsTriungle)
            {
                SetMotorState(40, mtrRight, ledRight);
            }
            else if (controller.IsCross)
            {
                SetMotorState(-40, mtrRight, ledRight);
            }
            else if (controller.NotTriungleCross)
            {
                SetMotorState(0, mtrRight, ledRight);
            }
        }

        private void SetMotorState(int speed, I2CMotrorDriver mtr, GTM.GHIElectronics.MulticolorLed led)
        {
            if (speed > 0)
            {
                led.TurnGreen();
            }
            else if (speed < 0)
            {
                led.TurnBlue();
            }
            else
            {
                led.TurnOff();
            }

            mtr.SetSpeed(speed);
        }
    }
}
