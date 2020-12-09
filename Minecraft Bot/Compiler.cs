using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GCCBot
{
    public class Compiler
    {
        private static List<Compiler> allCompilers = new List<Compiler>();

        private string _userId;
        private string _abortMessage = "gcc stop";
        private string msg = "";
        private bool _programRunning = false;

        private Process _currentProgram = new Process();
        private SocketDMChannel _currentChannel;
        private StreamWriter sw;

        public Compiler(string userId)
        {
            _userId = userId;
        }

        public string UserId
        {
            get
            {
                return _userId.Replace(' ', '_');
            }
        }

        public string AbortCommand
        {
            get
            {
                return _abortMessage;
            }

            set
            {
                _abortMessage = value;
            }
        }

        public bool ProgramRunning
        {
            get
            {
                return _programRunning;
            }
        }

        public void CompileProgram(out bool compileSucceeded, out string compilerOutput, string url, SocketDMChannel channel)
        {
            Console.WriteLine(url);

            if (File.Exists($"/home/Host/GCCBot/{UserId}.exe"))
            {
                File.Delete($"/home/Host/GCCBot/{UserId}.exe");
            }

            using (var client = new WebClient())
            {
                client.DownloadFile(url, "/home/Host/GCCBot/" + UserId + ".c");
            }

            compilerOutput = ExecuteCmdCommand("/usr/bin/gcc", $"/home/Host/GCCBot/{UserId}.c -o /home/Host/GCCBot/{UserId}.exe");

            compilerOutput.Replace("```", "\\```");

            if (File.Exists($"/home/Host/GCCBot/{UserId}.exe"))
            {
                compileSucceeded = true;

                _currentChannel = channel;

                new Thread(StartProgramRun).Start();

                _programRunning = true;
            }
            else
            {
                compileSucceeded = false;
            }

            File.Delete($"/home/Host/GCCBot/{UserId}.c");
        }

        public void MsgQueue()
        {
            if (msg != "")
            {
                _currentChannel.SendMessageAsync(msg.Substring(0, msg.Length - 1));

                msg = "";
            }
        }

        public void ProgramInput(string input)
        {
            try
            {
                 sw.WriteLine(input);
            }
            catch (Exception ex)
            {
                _currentChannel.SendMessageAsync("Error on ProgramInput: " + ex.Message);
            }
        }

        public void AbortProgram()
        {
            _currentProgram.StandardInput.Close();

            _currentProgram.Kill();

            _programRunning = false;
        }

        private void StartProgramRun()
        {
            try
            {
                _currentProgram = new Process();
                _currentProgram.StartInfo.FileName = $"/home/Host/GCCBot/{UserId}.exe";
                _currentProgram.StartInfo.RedirectStandardInput = true;
                _currentProgram.StartInfo.RedirectStandardOutput = true;
                _currentProgram.StartInfo.RedirectStandardError = true;
                _currentProgram.StartInfo.UseShellExecute = false;
                _currentProgram.EnableRaisingEvents = true;

                _currentProgram.Exited += CurrentProgram_Exited;
                _currentProgram.OutputDataReceived += CurrentProgram_OutputDataReceived;
                _currentProgram.ErrorDataReceived += CurrentProgram_OutputDataReceived;

                _currentProgram.Start();

                _currentProgram.BeginOutputReadLine();
                _currentProgram.BeginErrorReadLine();

                _currentProgram.WaitForInputIdle();
            }
            catch (Exception ex)
            {
                _currentChannel.SendMessageAsync("Error on ProcessStart: " + ex.Message);

                AbortProgram();
            }
        }

        private void CurrentProgram_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                msg += "`" + e.Data + "`\n";
            }
        }

        private void CurrentProgram_Exited(object sender, EventArgs e)
        {
            msg += $"Program has exited with code {_currentProgram?.ExitCode}.\n";

            _programRunning = false;

            if (sw != null)
            sw.Close();

            File.Delete($"/home/Host/GCCBot/{UserId}.exe");
        }

        public static Compiler GetCompiler(string userId)
        {
            foreach (var item in allCompilers)
            {
                if (item.UserId == userId)
                {
                    return item;
                }
            }

            Compiler newComp = new Compiler(userId);

            allCompilers.Add(newComp);

            return newComp;
        }

        public static void Interval()
        {
            foreach (var item in allCompilers)
            {
                item.MsgQueue();
            }
        }

        private static string ExecuteCmdCommand(string command, string args)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = command;
            cmd.StartInfo.Arguments = args;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.WaitForExit();
            return cmd.StandardError.ReadToEnd();
        }
    }
}
