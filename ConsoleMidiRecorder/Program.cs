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

            string path = "", output = "output.wav";
            int devNum = -1, midiDevNum = -1;
            int freq = 44100, fileLength = 0;
            const string alias_file = "mci_cmr_play";

            Console.WriteLine("CONSOLE MIDI RECORDER Version:{0}", asmInfo.Version);
            Console.WriteLine("----------------------------\n");

            if (args.Length == 0)
            {
                Console.WriteLine("[MIDI Devices]");
                for (int i = 0; i < MidiOutPortHandle.PortCount; i++)
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

                Console.WriteLine("\n");

                while (true)
                {
                    Console.Write("file?: ");
                    path = Console.ReadLine();
                    if (path.Length > 0)
                        break;
                    Console.WriteLine("Please type Filepath! (Exit: Ctrl+C)");
                }

                Console.WriteLine("\n");

                while (true)
                {
                    Console.Write("output?: ");
                    output = Console.ReadLine();
                    if (output.Length > 0)
                        break;
                    Console.WriteLine("Please type Output Filepath! (Exit: Ctrl+C)");
                }
            }
            else if(args.Length == 4 || args.Length == 5)
            {
                path = args[0];
                output = args[1];
                midiDevNum = Int32.Parse(args[2]);
                devNum = Int32.Parse(args[3]);
                freq = args.Length == 5 ? Int32.Parse(args[4]) : freq;
            }
            else
            {
                Console.WriteLine("Invalid argument!\nusage: ConsoleMidiRecorder [path] [output] [midi port] [output port] [freq(option)]");
                Environment.Exit(1);
            }





            waveIn = new WaveInEvent
            {
                DeviceNumber = devNum,
                WaveFormat = new WaveFormat(freq, WaveIn.GetCapabilities(devNum).Channels)
            };

            waveWriter = new WaveFileWriter(output, waveIn.WaveFormat);



            // create alias and open the file
            if (mciSendString("open \"" + path + "\" alias " + alias_file, null, 0, IntPtr.Zero) != 0)
            {
                ErrorFunc("Couldn't open file.");
            }

            Console.WriteLine("Loaded.");

            StringBuilder sb = new StringBuilder(128);
            mciSendString("status " + alias_file + " length", sb, 128, IntPtr.Zero);
            fileLength = Int32.Parse(sb.ToString());


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
                StringBuilder status = new StringBuilder(128);
                StringBuilder position = new StringBuilder(128);
                mciSendString("status " + alias_file + " mode", status, 128, IntPtr.Zero);
                mciSendString("status " + alias_file + " position", position, 128, IntPtr.Zero);
                string posStr = position.ToString();
                if (posStr == "") posStr = "0";
                int pos = Int32.Parse(posStr);
                Console.CursorLeft = 0;
                Console.Write("Completed：{0} / {1} ({2:F3}%)", pos, fileLength, (float)pos/fileLength*100);
                Thread.Sleep(200);
                if (status.ToString() != "playing")
                {
                    break;
                }
            }

            Console.WriteLine("\n");

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
