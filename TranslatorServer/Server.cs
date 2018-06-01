using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranslatorServer
{
    class Server
    {
        public Dictionary<string, int> files;
        private Dictionary<String, TranslationsList> translations;

        System.Windows.Forms.Timer saveTimer;

        int clientID;

        TcpListener Listener; // Объект, принимающий TCP-клиентов
        FileUtils fileUtils;

        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "Google Sheets API .NET Quickstart";

        // Запуск сервера
        public Server(int Port)
        {
            fileUtils = new FileUtils();
            files = fileUtils.DetectVersions();
            translations = new Dictionary<string, TranslationsList>();
            // Создаем "слушателя" для указанного порта
            clientID = 0;
            saveTimer = new System.Windows.Forms.Timer();
            saveTimer.Interval = 300000;
            saveTimer.Tick += new EventHandler(SaveTimer_Tick);
            save_Timer();
            Listener = new TcpListener(IPAddress.Any, Port);
            
        }

        private void save_Timer()
        {
            saveTimer.Enabled = true;
            saveTimer.Start();
        }

        private void SaveTimer_Tick(object sender, EventArgs e)
        {
            if (clientID > 0)
            {
                Console.WriteLine("Saving files");
                
                SendFilesToGoogleSheets();
                SaveFiles();
                Console.WriteLine("Files saved");
            } else
            {
                Console.WriteLine("No one connected");
            }
            save_Timer();
        }

        // Остановка сервера
        ~Server()
        {
            SaveFiles();
            // Если "слушатель" был создан
            if (Listener != null)
            {
                // Остановим его
                Listener.Stop();
            }
        }

        internal void SendFilesToGoogleSheets()
        {
            SheetsService service = InitializeCredentials();

            string spreadsheetId = "1rbgJdTRGR_KhpZS_lCcLceoiQYBXRAvMDI3oO-2D6ZY";

            foreach(String filename in translations.Keys)
            {
                CreateNewSheet(service, filename, spreadsheetId);
                ValueRange body = new ValueRange();
                List<IList<object>> values;
                List<object> translationString; 

                int translated = 0,
                    untranslated = 0,
                    rows = 0,
                    total = translations[filename].translations.Count;

                values = new List<IList<object>>();
                translationString = new List<object>();

                RunThroughTranslation(out translated, out untranslated, translations[filename]);
                translationString.Add("" + translated + "/" + untranslated);
                translationString.Add("" + translated / (translated + untranslated) * 100 + "%");
                translationString.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                values.Add(translationString);
                string range = filename + "!E2:G2";
                body.Values = values;
                SpreadsheetsResource.ValuesResource.UpdateRequest request = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                request.Execute();
                continue;
                translated = 0;
                untranslated = 0;
                foreach (TranslationsHolder tH in translations[filename].translations.ToArray())
                {
                    translationString = new List<object>();
                    if (tH.translations.Count > 1)
                    {
                        translationString.Add("Да");
                        translationString.Add("" + (tH.translations.Count - 1));
                        translated++;
                    }
                    else
                    {
                        translationString.Add("Нет");
                        translationString.Add("0");
                        untranslated++;
                    }
                    translationString.Add(tH.translations[0].ch);
                    translationString.Add(tH.translations[0].en);
                    values.Add(translationString);
                    rows++;
                    if(rows == 10)
                    {
                        UploadStringsToGS(translated, untranslated, filename, translationString, service, spreadsheetId);
                        values = new List<IList<object>>();
                        rows = 0;
                        total -= 10;
                        Console.WriteLine("Add 10 string, left {0} string", total);
                    }
                }
                UploadStringsToGS(translated, untranslated, filename, translationString, service, spreadsheetId);
            }
            Console.WriteLine("All files loaded to googlesheets");
        }

        private void UploadStringsToGS(int translated, int untranslated, string filename, List<object> translationString, SheetsService service, string spreadsheetId)
        {
            int doneAmount = translated + untranslated,
                            end = doneAmount + 3,
                            start = end<10?3:end - 10;
            string range = filename + "!" + "A" + start + ":D" + end;
            ValueRange body = new ValueRange();
            List<IList<object>> values = new List<IList<object>>();
            values.Add(translationString);
            body.Values = values;
            SpreadsheetsResource.ValuesResource.UpdateRequest request =
                service.Spreadsheets.Values.Update(body, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();
        }

        private void RunThroughTranslation(out int translated, out int untranslated, TranslationsList translationsList)
        {
            translated = 0;
            untranslated = 0;
            foreach(TranslationsHolder tH in translationsList.translations)
            {
                if (tH.translations.Count > 1)
                {
                    translated++;
                }
                else
                {
                    untranslated++;
                }
            }
        }

        private void CreateNewSheet(SheetsService service, string filename, string spreadsheetId)
        {
            try
            {
                string range = filename + "!A1:B1";

                ValueRange body = new ValueRange();
                List<IList<object>> values = new List<IList<object>>();
                body.Values = values;

                SpreadsheetsResource.ValuesResource.UpdateRequest response = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
                response.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                response.Execute();
            } catch(Exception)
            {
                var addSheetRequest = new AddSheetRequest();
                addSheetRequest.Properties = new SheetProperties();
                addSheetRequest.Properties.Title = filename;
                BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
                batchUpdateSpreadsheetRequest.Requests = new List<Request>();

                MergeCellsRequest mergeCells = new MergeCellsRequest();
                
                GridRange gridRange = new GridRange();
                gridRange.SheetId = 2;
                gridRange.StartColumnIndex = 7;
                gridRange.EndColumnIndex = 9;
                gridRange.StartRowIndex = 1;
                gridRange.EndRowIndex = 1;
                mergeCells.Range = gridRange;

                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = addSheetRequest
                    //MergeCells = mergeCells
                });

                var batchUpdateRequest =
                    service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetId);

                batchUpdateRequest.Execute();
                AddHeaderToSheet(service, spreadsheetId, filename);
            }
        }

        private void AddHeaderToSheet(SheetsService service, string spreadsheetId, string filename)
        {
            string range = filename + "!A2:D2";
            ValueRange body = new ValueRange();
            List<IList<object>> values = new List<IList<object>>();
            List<object> value = new List<object>();
            value.Add("Переведена ли");
            value.Add("Русских вариантов");
            value.Add("Китайский");
            value.Add("Английский");

            values.Add(value);
            body.Values = values;
            SpreadsheetsResource.ValuesResource.UpdateRequest request =
                service.Spreadsheets.Values.Update(body, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();

            range = filename + "!E1:G1";
            body = new ValueRange();
            values = new List<IList<object>>();
            value = new List<object>();
            value.Add("Переведено/Всего");
            value.Add("Процент");
            value.Add("Время последнего обновления");

            values.Add(value);
            body.Values = values;
            request = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();
        }

        private SheetsService InitializeCredentials()
        {
            // Create Google Sheets API service.
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            SheetsService service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            return service;
        }

        internal void Start()
        {
            Listener.Start(); // Запускаем его
            Console.WriteLine("Server started!");
            // В бесконечном цикле
            while (true)
            {
                // Принимаем новых клиентов
                new Client(Listener.AcceptTcpClient(), this, ++clientID);
            }
        }

        public void IncreaseVersion(string file)
        {
            files[file]++;
        }

        internal void AddFile(string file)
        {
            if (!files.Keys.Contains(file))
            {
                files.Add(file, 1);
                Console.WriteLine("Added file {0}", file);
            }
        }

        internal void SaveFiles()
        {
            foreach (String file in files.Keys.ToArray())
            {
                if (!translations.ContainsKey(file)) continue;
                fileUtils.WriteFormattedFile(file, translations[file]);
            }
            fileUtils.SaveVersions(files);
            Console.WriteLine("All files saved");
        }

        internal void LoadFiles()
        {
            foreach(String file in files.Keys)
            {
                translations.Add(file, fileUtils.ReadFormattedFile(file));
            }
            LoadFileVersions();
        }

        private void LoadFileVersions()
        {
            Dictionary<string, int> _versions = fileUtils.DetectVersions();
            if (_versions.Count == 0) return;
            foreach (string file in files.Keys.ToArray())
            {
                if (_versions.ContainsKey(file))
                {
                    files[file] = _versions[file];
                }
            }
        }

        internal byte[] GetFile(string fileName)
        {
            string fileTranslations = JsonConvert.SerializeObject(translations[fileName]);
            return System.Text.Encoding.UTF8.GetBytes(fileTranslations);
        }

        internal List<Translation> GetTranslations(string fileName, string stringCh)
        {
            return translations[fileName].FindByChinese(stringCh);
        }

        public Dictionary<string, int> GetFiles()
        {
            return files;
        }

        public void AddTranslations(string fileName, TranslationsList translationsList)
        {
            IncreaseVersion(fileName);
            TranslationsList tL = translations[fileName];
            foreach(TranslationsHolder tH in tL.translations)
            {
                foreach (TranslationsHolder tH2 in translationsList.translations)
                {
                    if(tH.translations[0].en.Equals(tH2.translations[0].en))
                    {
                        foreach(Translation t2 in tH2.translations)
                        {
                            bool found = false;
                            foreach (Translation t in tH.translations)
                            {
                                if(t != null && t2!= null && t.ru == t2.ru)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if(!found && t2.ru!=null)
                            {
                                tH.translations.Add(t2);
                            }
                        }
                    }
                }
            }            
        }
    }
}
