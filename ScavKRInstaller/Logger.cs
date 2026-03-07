using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ScavKRInstaller
{
    public class Logger
    {
        public delegate void NewLogMessageCreated();
        public event NewLogMessageCreated? Logged;
        public List<LogEntry> Journal=new();
        private void SetDeltaForPreviousEntry(int id)
        {
            if (id<=0) return;
            TimeSpan delta = GetEntry(id).time-GetEntry(id-1).time;
            GetEntry(id).delta=delta;
        }
        public LogEntry GetEntry(int id)
        {
            if(id>0)
            {
                return this.Journal[id];
            }
            return this.Journal[0];
        }
        public LogEntry GetLastEntry()
        {
            return GetEntry(this.Journal.Count-1);
        }
        public void Write(string message)
        {
            Journal.Add(new LogEntry(message));
            SetDeltaForPreviousEntry(Journal.Count-1);
            Logged?.Invoke();
        }
        public string GetWholeLog()
        {
            StringBuilder sb=new StringBuilder("");
            foreach(LogEntry entry in Journal)
            {
                sb.AppendLine(entry.ToString());
            }
            return string.Concat(sb.ToString(), "\n");
        }
    }
    public class LogEntry
    {
        internal TimeOnly time;
        internal TimeSpan delta;
        internal string message;
        public LogEntry(string message)
        {
            this.message=message;
            this.time=TimeOnly.FromDateTime(DateTime.Now);
        }
        public LogEntry(string message, TimeSpan delta)
        {
            this.message=message;
            this.time=TimeOnly.FromDateTime(DateTime.Now);
            this.delta=delta;
        }
        public override string ToString()
        {
            return $"dt:[{delta.ToString(@"mm\:ss\.fff")}]|t:[{time.ToString(@"HH\:mm\:ss\.fff")}] {message}";
        }
    }
    public static class LogHandler
    {
        public static Logger Instance=Instance??=new();
    }
}
