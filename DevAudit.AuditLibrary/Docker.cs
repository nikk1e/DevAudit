using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class Docker
    {
        public enum ProcessStatus
        {
            DockerNotInstalled = -1,
            Success = 0,
            Error = 1
        }

        public static string InspectCommand { get; set; } = "inspect {0}"; 

 

        public static bool GetContainer(string container_id, out ProcessStatus process_status, out string process_output, out string process_error)
        {
            return Execute(string.Format(InspectCommand, container_id), out process_status, out process_output, out process_error);
        }

        public static bool ExecuteInContainer(string container_id, string command, out ProcessStatus process_status, out string process_output, out string process_error)
        {
            return Execute(string.Format("exec {0} {1}", container_id, command), out process_status, out process_output, out process_error);
        }



        public static bool Execute(string arguments, out ProcessStatus process_status, out string process_output, out string process_error)
        {
            int? process_exit_code = null;
            string process_out = "";
            string process_err = "";
            ProcessStartInfo psi = new ProcessStartInfo("docker");
            psi.Arguments = arguments;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = psi;
            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_out += e.Data + Environment.NewLine;
                }
            };
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_err += e.Data + Environment.NewLine;
                }

            };
            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                process_exit_code = p.ExitCode;
                p.Close();
            }
            catch (Win32Exception e)
            {
                if (e.Message == "The system cannot find the file specified")
                {
                    process_status = ProcessStatus.DockerNotInstalled;
                    process_error = e.Message;
                    process_output = "";
                    return false;
                }

            }
            finally
            {
                p.Dispose();
            }
            process_output = process_out;
            process_error = process_err;
            if (!string.IsNullOrEmpty(process_error) || (process_exit_code.HasValue && process_exit_code.Value != 0))
            {
                process_status = ProcessStatus.Error;
                return false;
            }
            else
            {
                process_status = ProcessStatus.Success;
                return true;
            }
        }
    }
}
