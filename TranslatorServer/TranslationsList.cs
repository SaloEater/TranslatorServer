using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslatorServer
{
    class TranslationsList
    {
        public List<TranslationsHolder> translations { get; set; }

        public TranslationsList()
        {
            translations = new List<TranslationsHolder>();
        }

        public List<Translation> FindByChinese(string ch)
        {
            List<Translation> translations = new List<Translation>();
            foreach(TranslationsHolder tH in this.translations)
            {
                foreach(Translation t in tH.translations)
                {
                    if (t.ch.Equals(ch)) translations.Add(t);
                }
            }
            if (translations.Count == 0) Console.WriteLine("{0} hasn't lines", ch);
            return translations;
        }

    }
}
