using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ProfessionalAccounting.Entities;

namespace ProfessionalAccounting.DAL
{
    public class TextFileHelper : IDbHelper
    {
        private readonly string m_Dir;
        private readonly List<BalanceItem> m_BalanceItems = new List<BalanceItem>();
        private readonly List<PatternUI> m_Patterns = new List<PatternUI>();
        private readonly List<PatternData> m_Datas = new List<PatternData>();

        public TextFileHelper()
        {
            m_Dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!File.Exists(Path.Combine(m_Dir, "BalanceItems")))
                File.Create(Path.Combine(m_Dir, "BalanceItems")).Close();
            if (!File.Exists(Path.Combine(m_Dir, "Patterns")))
                File.Create(Path.Combine(m_Dir, "Patterns")).Close();
            if (!File.Exists(Path.Combine(m_Dir, "Datas")))
                File.Create(Path.Combine(m_Dir, "Datas")).Close();

            try
            {
                foreach (
                    var s in
                        Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(m_Dir, "BalanceItems")))
                                .Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                    AddBalanceItem(BalanceItem.Parse(s));
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                ClearBalanceItems();
            }
            try
            {
                foreach (
                    var s in
                        Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(m_Dir, "Patterns")))
                                .Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                    AddPattern(PatternUI.Parse(s));
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                ClearPattern();
            }
            try
            {
                foreach (
                    var s in
                        Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(m_Dir, "Datas")))
                                .Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                    AddData(PatternData.Parse(s, nm => m_Patterns.Single(p => p.Name == nm)));
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                ClearData();
            }
        }


        public void Dispose() { SaveData(); }
        public IEnumerable<BalanceItem> GetBalanceItems() { return m_BalanceItems.AsReadOnly(); }
        public void AddBalanceItem(BalanceItem item) { m_BalanceItems.Add(item); }
        public void ClearBalanceItems() { m_BalanceItems.Clear(); }
        public IEnumerable<PatternUI> Patterns() { return m_Patterns.AsReadOnly(); }
        public void AddPattern(PatternUI pattern) { m_Patterns.Add(pattern); }
        public void ClearPattern() { m_Patterns.Clear(); }
        public IEnumerable<PatternData> GetDatas() { return m_Datas.AsReadOnly(); }
        public void AddData(PatternData data) { m_Datas.Add(data); }
        public void RemoveData(PatternData data) { m_Datas.Remove(data); }
        public void ClearData() { m_Datas.Clear(); }
        public void ClearData(int count) { m_Datas.RemoveRange(0, count); }

        public void SaveData()
        {
            File.WriteAllBytes(
                               Path.Combine(m_Dir, "BalanceItems"),
                               Encoding.UTF8.GetBytes(
                                                      String.Join(
                                                                  Environment.NewLine,
                                                                  m_BalanceItems.Select(item => item.ToString()))));
            File.WriteAllBytes(
                               Path.Combine(m_Dir, "Patterns"),
                               Encoding.UTF8.GetBytes(
                                                      String.Join(
                                                                  Environment.NewLine,
                                                                  m_Patterns.Select(pattern => pattern.ToString()))));
            File.WriteAllBytes(
                               Path.Combine(m_Dir, "Datas"),
                               Encoding.UTF8.GetBytes(
                                                      String.Join(
                                                                  Environment.NewLine,
                                                                  m_Datas.Select(data => data.ToString()))));
        }
    }
}
