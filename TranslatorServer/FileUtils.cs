using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslatorServer
{
    class FileUtils
    {
        public TranslationsList ReadNonFormattedFile(string filename)
        {

            string file = File.ReadAllText(filename);
            List<Translation> translationList = JsonConvert.DeserializeObject<List<Translation>>(file);
            TranslationsList translations = ToFormatted(translationList);

            return translations;
        }

        private TranslationsList ToFormatted(List<Translation> translationList)
        {
            TranslationsList translations = new TranslationsList();
            foreach (Translation translation in translationList)
            {
                TranslationsHolder translationsHolder = new TranslationsHolder();
                translationsHolder.AddTranslation(translation);
                translations.translations.Add(translationsHolder);
            }
            return translations;
        }

        public TranslationsList ReadFormattedFile(string filename)
        {
            filename = MakeWorkFile(filename);
            if (!File.Exists(filename))
            {
                var a = File.Create(filename);
                a.Close();
                filename = UnWorkFile(filename);
                TranslationsList translations = ReadNonFormattedFile(filename);
                return translations;
            } else
            {
                string file = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<TranslationsList>(file);
            }
        }

        public void WriteNonFormattedFile(string filename, TranslationsList translations)
        {

        }

        public void WriteFormattedFile(string filename, TranslationsList translations)
        {
            filename = MakeWorkFile(filename);
            File.WriteAllText(filename, JsonConvert.SerializeObject(translations));
        }

        internal void SaveVersions(Dictionary<string, int> versions)
        {
            string filename = "versions.txt";
            File.WriteAllText(filename, JsonConvert.SerializeObject(versions));
        }

        internal Dictionary<string, int> DetectVersions()
        {
            Dictionary<string, int> versions;
            string filename = "versions.txt";
            if (File.Exists(filename))
            {
                string str_versions = File.ReadAllText(filename);
                versions = JsonConvert.DeserializeObject<Dictionary<string, int>>(str_versions);
            }
            else
            {
                versions = new Dictionary<string, int>();
            }
            return versions;
        }

        internal string MakeWorkFile(string filename)
        {
            return filename.Replace(".tsv", "_work.tsv");
        }

        internal string UnWorkFile(string filename)
        {
            return filename.Replace("_work.tsv", ".tsv");
        }

        internal static byte[] GetFileBytes(string filename)
        {
            return File.ReadAllBytes(filename);
        }

        public TranslationsList StringToFormatted(string v)
        {
            return JsonConvert.DeserializeObject<TranslationsList>(v);
        }

        public String FormattedToString(TranslationsList v)
        {
            return JsonConvert.SerializeObject(v);
        }
    }
}
