using System;
using System.Text;
using NAudio.Wave;
using NextMidi.MidiPort.Output.Core;
using System.Threading;


namespace ConsoleMidiRecorder
{
    class Program
    {
        [System.Runtime.InteropServices.DllImport("winmm.dll",
        CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int mciSendString(
            string command,
            System.Text.StringBuilder buffer, int bufferSize, IntPtr hwndCallback
            );

        static void Main(string[] args)
        {

            WaveInEvent waveIn;
            WaveFileWriter waveWriter;

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var asmInfo = asm.GetName();

            string path;
            int devNum = -1, midiDevNum = -1;
            const string alias_file = "mci_cmr_play";


            Console.WriteLine("CONSOLE MIDI RECORDER Version:{0}", asmInfo.Version);
            Console.WriteLine("----------------------------\n");

            Console.WriteLine("[MIDI Devices]");
            int count = MidiOutPortHandle.PortCount;
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine("{0}: {1}", i.ToString(), MidiOutPortHandle.GetPortInformation(i).szPname);
            }
            Console.Write("Please select MIDI Device:");
            while (true)
            {

                midiDevNum = (int)Char.GetNumericValue(Console.ReadKey().KeyChar);
                if (midiDevNum >= 0)
                    break;
                Console.WriteLine("Please select MIDI Device! (Exit: Ctrl+C):");
            }


            Console.WriteLine("\n");

            Console.WriteLine("[Recording Devices]");
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                Console.WriteLine(i.ToString() + ": " + WaveInEvent.GetCapabilities(i).ProductName);
            }
            Console.Write("Please select Recording Device:");
            while (true)
            {

                devNum = (int)Char.GetNumericValue(Console.ReadKey().KeyChar);
                if (devNum >= 0)
                    break;
                Console.WriteLine("Please select Recording Device! (Exit: Ctrl+C):");
            }



            waveIn = new WaveInEvent
            {
                DeviceNumber = devNum,
                WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(devNum).Channels)
            };

            waveWriter = new WaveFileWriter("output.wav", waveIn.WaveFormat);

            Console.WriteLine("\n");

            while (true)
            {
                Console.Write("file?: ");
                path = Console.ReadLine();
                if (path.Length > 0)
                    break;
                Console.WriteLine("Please type Filepath! (Exit: Ctrl+C)");
            }

            // create alias and open the file
            if (mciSendString("open \"" + path + "\" alias " + alias_file, null, 0, IntPtr.Zero) != 0)
            {
                ErrorFunc("Couldn't open file.");
            }

            Console.WriteLine("Loaded.");


            mciSendString("set " + alias_file + " port " + midiDevNum, null, 0, IntPtr.Zero);
            mciSendString("play " + alias_file + "", null, 0, IntPtr.Zero);

            waveIn.DataAvailable += (_, ee) =>
            {
                waveWriter.Write(ee.Buffer, 0, ee.BytesRecorded);
                waveWriter.Flush();
            };
            waveIn.RecordingStopped += (_, __) =>
            {
                waveWriter.Flush();
            };

            waveIn.StartRecording();


            Console.WriteLine("Now Recording: " + path);

            while (true)
            {
                StringBuilder sb = new StringBuilder(128);
                mciSendString("status " + alias_file + " mode", sb, 128, IntPtr.Zero);
                if (sb.ToString() != "playing")
                {
                    break;
                }

            }


            mciSendString("close " + alias_file, null, 0, IntPtr.Zero);

            Thread.Sleep(2000);

            waveIn.StopRecording();
            waveIn.Dispose();
            waveIn = null;



            waveWriter.Close();

            Console.WriteLine("Successful! Press any key to exit...");
            Console.ReadKey();

            Environment.Exit(0);


        }


        static void ErrorFunc(string reason = "unknown")
        {
            Console.WriteLine("An Error occured! " + reason);
            Environment.Exit(1);
        }
    }
}
