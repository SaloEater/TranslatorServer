using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslatorServer
{
    class FileConventer
    {
        public void FileToReadableFile(String origin)
        {
            List<TranslationsHolder> translations = ConvertFromOriginal(origin);
            DeployFormat(translations, CreateWorkFile(origin));
        }

        private string CreateWorkFile(string origin)
        {
            origin = origin.Replace(".tsv", "_work.tsv");
            if (!File.Exists(origin)) File.CreateText(origin).Close();
            return origin;
        }

        public void WorkFileToOrigin(string origin)
        {
            List<Translation> translations = ConvertToOriginal(CreateWorkFile(origin));
            DeployOrigin(translations, CreateResultFile(origin));
        }

        private string CreateResultFile(string origin)
        {
            origin = origin.Replace(".tsv", "_old.tsv");
            if (!File.Exists(origin)) File.CreateText(origin).Close();
            return origin;
        }

        private void DeployFormat(List<TranslationsHolder> translations, String filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(translations).Replace(",", ",\n"));
        }

        private void DeployOrigin(List<Translation> translations, string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(translations).Replace(",", ",\n"));
        }

        private List<TranslationsHolder> ConvertFromOriginal(string filename)
        {
            List<TranslationsHolder> translations = new List<TranslationsHolder>();
            string file = File.ReadAllText(filename);
            Console.WriteLine("Found " + File.ReadLines(filename).Count() / 4 + " lines in " + filename);
            List<Translation> translationList = JsonConvert.DeserializeObject<List<Translation>>(file);
            foreach (Translation translation in translationList)
            {
                TranslationsHolder translationsHolder = new TranslationsHolder();
                translationsHolder.AddTranslation(translation);
                translations.Add(translationsHolder);
            }
            return translations;
        }

        private List<Translation> ConvertToOriginal(string filename)
        {
            List<Translation> translations = new List<Translation>();
            string file = File.ReadAllText(filename);
            Console.WriteLine("Found " + File.ReadLines(filename).Count() / 4 + " lines in " + filename);
            List<TranslationsHolder> translationList = JsonConvert.DeserializeObject<List<TranslationsHolder>>(file);
            Console.WriteLine(translationList.Count);
            foreach (TranslationsHolder translation in translationList)
            {
                Console.WriteLine(translation.translations.Count);
                translations.Add(translation.translations[0]);
            }
            return translations;
        }
    }
}
