using System;
using System.Windows.Forms;
using SDL2;

namespace NES_Emulator
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "NES ROM files (*.nes)|*.nes|All files (*.*)|*.*";
                ofd.Title = "Select a NES ROM";
                ofd.FilterIndex = 1;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    Console.WriteLine($"Loading ROM: {filePath}");
                    FanNEStastic.Run(filePath);
                }
                else
                {
                    Console.WriteLine("No ROM selected. Exiting...");
                    return;
                }
            }
        }
    }
}