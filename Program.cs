using System;
using SDL2;

namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Different file paths for both devices I work on
<<<<<<< HEAD
            FanNEStastic.Run("C:\\Users\\eric1\\OneDrive\\Documents\\NES_Emulator\\Test ROMs\\dk.nes");
            //FanNEStastic.NES_System_Run("C:\\Users\\eric1\\Documents\\Visual Studio 2022\\Projects\\Test ROMs\\icc.nes");
=======
            //FanNEStastic.Run("C:\\Users\\eric1\\OneDrive\\Documents\\NES_Emulator\\Test ROMs\\nestest.nes");
            FanNEStastic.Run("C:\\Users\\eric1\\Documents\\Visual Studio 2022\\Projects\\Test ROMs\\dk.nes");
>>>>>>> 563f96a (Implemented Direct Memory Access in the NES_BUS Class)
        }
    }
}

