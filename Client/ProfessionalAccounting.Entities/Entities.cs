using System;
using System.Linq;

namespace ProfessionalAccounting.Entities
{
    public class PatternUI
    {
        public string Name { get; private set; }
        public string TextPattern { get; set; }
        public string UI { get; private set; }

        public static PatternUI Parse(string str)
        {
            var sp = str.Substring(1).Split(new[] {'='}, 3);
            return new PatternUI
                       {
                           Name = sp[0],
                           TextPattern = sp[1],
                           UI = sp[2]
                       };
        }

        public override string ToString() { return String.Format("P{0}={1}={2}", Name, TextPattern, UI); }
    }

    public class PatternData
    {
        private readonly string[] m_Results;

        public PatternUI Pattern { get; private set; }

        public string this[int index] { get { return m_Results[index]; } set { m_Results[index] = value; } }

        private string Result { get { return String.Join(",", m_Results); } }

        public string Name { get { return String.Format(Pattern.TextPattern, m_Results); } }

        public PatternData(PatternUI pattern)
        {
            Pattern = pattern;
            m_Results = new string[pattern.UI.Count(c => c == ',') + 1];
        }

        private PatternData(PatternUI pattern, string[] results)
        {
            Pattern = pattern;
            m_Results = results;
        }

        public static PatternData Parse(string str, Func<string, PatternUI> getUI)
        {
            var sp = str.Split(',');
            var d = new PatternData(getUI(sp[0]));
            for (var i = 1; i < sp.Length; i++)
                d[i - 1] = sp[i];
            return d;
        }

        public override string ToString() { return String.Format("{0},{1}", Pattern.Name, Result); }

        public PatternData Clone()
        {
            var result = new string[m_Results.Length];
            m_Results.CopyTo(result, 0);
            return new PatternData(Pattern, result);
        }
    }

    public class BalanceItem
    {
        public string Head { get; private set; }
        public string Data { get; private set; }
        public string Balance { get; private set; }

        public static BalanceItem Parse(string str)
        {
            var sp = str.Split('=');
            return new BalanceItem
                       {
                           Head = sp[0],
                           Data = sp[1],
                           Balance = sp[2]
                       };
        }

        public override string ToString() { return String.Format("{0}={1}={2}", Head, Data, Balance); }
    }
}
