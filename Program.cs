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
            string filePath;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "NES ROM files (*.nes)|*.nes|All files (*.*)|*.*";
                ofd.Title = "Select a NES ROM";
                ofd.FilterIndex = 1;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    filePath = ofd.FileName;
                    Console.WriteLine($"Loading ROM: {filePath}");
                }
                else
                {
                    Console.WriteLine("No ROM selected. Exiting...");
                    return;
                }
            }

            try
            {
                FanNEStastic.Run(filePath);
            }
            catch (InvalidDataException ex)
            {
                MessageBox.Show($"Invalid ROM file:\n{ex.Message}\n\nPlease select a valid .nes file.", "Invalid ROM", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Invalid ROM: {ex.Message}");
            }

        }
    }
}