using System;
using System.Diagnostics;
using System.Management;

namespace Processkiller
{
    class Program
    {
        static void Main(string[] args)
        {
            var param = new Param();
            if (param.TryParseCommandLine(args))
            {
                switch (param.Type)
                {
                    case Param.KillType.Self:
                        KillProcess(param.Pid);
                        break;
                    case Param.KillType.Tree:
                        KillProcessAndChildren(param.Pid);
                        break;
                }
            }
            else
            {
                while (true)
                {
                    Console.Write($"请输入进程PID，或输入Quit退出，或使用参数调用(-Self/-Tree Pid) :");
                    var str = Console.ReadLine();
                    if (string.Compare(str, "Quit", StringComparison.OrdinalIgnoreCase) == 0)
                        break;
                    int pid;
                    if (int.TryParse(str, out pid))
                        KillProcessAndChildren(pid);
                }
            }
        }

        public class Param
        {
            public KillType Type { get; private set; }
            public int Pid { get; private set; }

            public enum KillType
            {
                None, Self, Tree
            }

            public bool TryParseCommandLine(string[] args)
            {
                if (args.Length != 2)
                    return false;
                var type = KillType.None;
                if (string.Compare(args[0], "-" + KillType.Self, StringComparison.OrdinalIgnoreCase) == 0)
                    type = KillType.Self;
                if (string.Compare(args[0], "-" + KillType.Tree, StringComparison.OrdinalIgnoreCase) == 0)
                    type = KillType.Tree;
                int pid;
                if (type != KillType.None && int.TryParse(args[1], out pid))
                {
                    Type = type;
                    Pid = pid;
                    return true;
                }
                return false;
            }
        }

        private static void FetchProcess(int pid, Action<int> action)
        {
            action.Invoke(pid);

            var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            var moc = searcher.Get();
            foreach (var o in moc)
            {
                var mo = (ManagementObject)o;
                if (mo == null)
                    continue;
                action.Invoke(Convert.ToInt32(mo["ProcessID"]));
            }
        }

        private static void KillProcess(int pid)
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                Console.WriteLine(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                /* process already exited */
            }
        }

        /// <summary>
        /// 根据父进程id，杀死与之相关的进程树 
        /// </summary>
        /// <param name="pid"></param>
        private static void KillProcessAndChildren(int pid)
        {
            FetchProcess(pid, KillProcess);
        }
    }
}
