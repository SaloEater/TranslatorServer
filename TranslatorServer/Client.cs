using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranslatorServer
{
    class Client
    {
        // Конструктор класса. Ему нужно передавать принятого клиента от TcpListener
        public Client(TcpClient Client, Server server, int id)
        {
            try
            {
                Console.WriteLine("Client joined");
                NetworkStream stream = Client.GetStream();
                byte[] typeSizeBytes = ReceiveData(Client, stream);
                int type = BitConverter.ToInt16(typeSizeBytes, 0);
                /*byte[] typeSizeBytes = new byte[2];
                int readedByts = stream.Read(typeSizeBytes, 0, 2);
                int type = BitConverter.ToInt16(typeSizeBytes, 0);*/
                switch (type)
                {

                    case 0:
                        //Клиент запрашивает все файлы
                        //byte[] bytes_0 = ReceiveData(Client, stream);
                        Console.WriteLine("{0}) Received request for all files", id);
                        string stringOfFilenames = JsonConvert.SerializeObject(server.GetFiles());

                        byte[] bytes_0 = System.Text.Encoding.UTF8.GetBytes(stringOfFilenames);
                        SendData(bytes_0, stream, 0);
                        Console.WriteLine("{0}) Sent it", id);
                        break;

                    case 1:
                        //Клиент запрашивает какой-то файл. запрос - имя файла
                        Console.WriteLine("{0}) Received request ", id);
                        byte[] bytesFileName_0 = ReceiveData(Client, stream);
                        String fileName_0 = System.Text.Encoding.UTF8.GetString(bytesFileName_0);
                        Console.WriteLine("{0}) aboy file {1}", id, fileName_0);
                        byte[] bytesFileTranslations = server.GetFile(fileName_0);
                        SendData(bytesFileTranslations, stream, 1);
                        Console.WriteLine("{0}) Sent it", id);
                        break;

                    case 2:
                        //Клиент запрашивает переводы для строки на китайском. запрос - (имя файла, строка)
                        Console.WriteLine("{0}) Received request", id);
                        byte[] bytesFileName = ReceiveData(Client, stream);
                        String fileName = System.Text.Encoding.UTF8.GetString(bytesFileName);
                        Console.WriteLine("{0}) for file {1}", id, fileName);

                        byte[] bytesStringCh = ReceiveData(Client, stream);
                        String stringCh = System.Text.Encoding.UTF8.GetString(bytesFileName);

                        List<Translation> responseTranslations = server.GetTranslations(fileName, stringCh);
                        String responseJSON = JsonConvert.SerializeObject(responseTranslations);
                        byte[] response = System.Text.Encoding.UTF8.GetBytes(responseJSON);
                        SendData(response, stream, 2);
                        Console.WriteLine("{0}) Sent it", id);
                        break;

                    case 3:
                        //Клиент отправляет свои переводы. запрос - (имя файла, translationList)
                        Console.WriteLine("{0}) Received request", id);
                        byte[] bytesFileName_1 = ReceiveData(Client, stream);
                        String fileName_1 = System.Text.Encoding.UTF8.GetString(bytesFileName_1);
                        Console.WriteLine("{0}) that send file {1}", id, fileName_1);

                        byte[] bytesTranslations = ReceiveData(Client, stream);
                        String stringTranslations = System.Text.Encoding.UTF8.GetString(bytesTranslations);

                        TranslationsList translationsList = JsonConvert.DeserializeObject<TranslationsList>(stringTranslations);
                        SendData(new byte[0], stream, 3);
                        server.AddTranslations(fileName_1, translationsList);
                        Console.WriteLine("{0}) Sent it", id);
                        break;

                    case 4:
                        //Клиент узнает размер файла. запрос - (имя файла)
                        //Не используется
                        byte[] bytesFileName_4 = ReceiveData(Client, stream);
                        String fileName_4 = System.Text.Encoding.UTF8.GetString(bytesFileName_4);
                        Console.WriteLine("Received request for filesize of {0} ", fileName_4);

                        long fileSize = (new FileInfo(fileName_4)).Length;
                        byte[] bytesSize = BitConverter.GetBytes(fileSize);

                        SendData(bytesSize, stream, 4);
                        break;

                    case 5:
                        //Клиент запрашивает версию файла. запрос - (имя файла, translationList)
                        Console.WriteLine("{0}) Received request", id);
                        byte[] bytesFileName_5 = ReceiveData(Client, stream);
                        String fileName_5 = System.Text.Encoding.UTF8.GetString(bytesFileName_5);
                        Console.WriteLine("{0}) for fileversion for {1}", id, fileName_5);
                        int version = server.files[fileName_5];
                        byte[] bytesVersion = BitConverter.GetBytes(version);
                        SendData(bytesVersion, stream, 5);
                        Console.WriteLine("{0}) Sent it", id);
                        break;

                    default:
                        //Неправильный запрос

                        break;
                }

                //Client.Close();
                Console.WriteLine("Client disconnected");
            } catch(Exception ex)
            {
                Console.WriteLine("Something wrong with client {0}", id);
            }
        }

        private void SendData(byte[] data, NetworkStream stream, int responseType)
        {
            //stream.Write(BitConverter.GetBytes(responseType), 0, 1);

            int bufferSize = 1024;

            byte[] dataLength = BitConverter.GetBytes(data.Length);

            stream.Write(dataLength, 0, 4);

            int bytesSent = 0;
            int bytesLeft = data.Length;

            while (bytesLeft > 0)
            {
                int curDataSize = Math.Min(bufferSize, bytesLeft);

                stream.Write(data, bytesSent, curDataSize);

                bytesSent += curDataSize;
                bytesLeft -= curDataSize;
            }
        }

        private byte[] ReceiveData(TcpClient client, NetworkStream stream)
        {   
            byte[] fileSizeBytes = new byte[4];
            int bytes = stream.Read(fileSizeBytes, 0, 4);
            int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

            int bytesLeft = dataLength;
            byte[] data = new byte[dataLength];

            int bufferSize = 1024;
            int bytesRead = 0;

            while (bytesLeft > 0)
            {
                int curDataSize = Math.Min(bufferSize, bytesLeft);
                if (client.Available < curDataSize)
                {
                    curDataSize = client.Available;
                }

                bytes = stream.Read(data, bytesRead, curDataSize);

                bytesRead += curDataSize;
                bytesLeft -= curDataSize;
            }

            return data;
        }

        private string GetFilesList(ListBox listBox)
        {
            String s = String.Empty;
            foreach(string file in listBox.Items)
            {
                s += file;
                s += '|';
            }
            return s;
        }
    }
}
