using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FluentFTP;

namespace TrainsEditor.EditorLogic
{
    /// <summary>
    /// Třída pro stahování souborů s vlaky z CIS JŘ (ftp.cisjr.cz). Je schopen stáhnout si jak samotný soubor s GVD, tak i změnové soubory KADR.
    /// 
    /// Použití:
    /// 
    /// Nejdříve se zjistí seznam souborů ke stažení zavoláním <see cref="ListFiles(DateTime)"/>. Ta uloží seznam do <see cref="FilesToDownloadList"/>.
    /// Poté je možné stahovat jednotlivé soubory (po jednom, aby dlouhé stahování nezaseklo UI).
    /// 
    /// Nejdříve (jedenkrát) zavolat <see cref="SortFilesToDownload"/>, <see cref="EnsureDownloadDirectoryExists"/> a <see cref="ConnectFtp"/>.
    /// Pak jednotlivě volat <see cref="DownloadAndUnpackNextFile"/>, dokud vrací true.
    /// Nakonec zavolat <see cref="Disconnect"/>.
    /// </summary>
    class FilesDownloader
    {
        /// <summary>
        /// Lokální složka, kam se ukládají rozbalené XML soubory s vlaky
        /// </summary>
        public string RepositoryFolder { get; private set; }

        /// <summary>
        /// Složka, kam se stahují soubory z FTP (zabalené)
        /// </summary>
        public string DownloadedFilesFolder { get; private set; }

        /// <summary>
        /// Seznam souborů ke stažení. Nastaví se voláním <see cref="ListFiles"/>.
        /// </summary>
        public List<FtpFileInfo> FilesToDownloadList { get; private set; }

        /// <summary>
        /// Počet již stažených a rozbalených souborů (narůstá postupným voláním <see cref="DownloadAndUnpackNextFile"/>).
        /// </summary>
        public int ProcessedFileCount { get; private set; }

        /// <summary>
        /// Rok GVD
        /// </summary>
        public int Year { get; private set; }

        public string RemoteFolderBase => "/draha/celostatni/szdc/" + Year;

        private FtpClient _client;

        /// <summary>
        /// Vytvoří instanci downloaderu.
        /// </summary>
        /// <param name="repositoryFolder">Složka, kam se ukládají rozbalené soubory s vlaky</param>
        /// <param name="year">Rok GVD</param>
        public FilesDownloader(string repositoryFolder, int year)
        {
            RepositoryFolder = Path.Combine(repositoryFolder, year.ToString());
            DownloadedFilesFolder = Path.Combine(RepositoryFolder, "zip");
            Year = year;
        }

        /// <summary>
        /// Vrátí datum nejnovějšího ZIP souboru, který byl stažen a rozbalen.
        /// Je zajištěno, že soubor je ve složce přítomen jako ZIP až po rozbalení, tedy je jistota, že když tam soubor je, byl rozbalen => nemůže nic uniknout.
        /// </summary>
        /// <returns></returns>
        public DateTime? GetNewestModifiedDate()
        {
            if (!Directory.Exists(DownloadedFilesFolder))
            {
                return null;
            }

            var files = Directory.EnumerateFiles(DownloadedFilesFolder, "*.zip");
            DateTime? resultMaxDate = null;
            foreach (var file in files)
            {
                var modifiedDate = File.GetLastWriteTime(file);
                if (!resultMaxDate.HasValue || modifiedDate > resultMaxDate.Value)
                {
                    resultMaxDate = modifiedDate;
                }
            }

            return resultMaxDate;
        }

        /// <summary>
        /// Připraví seznam souborů ke stažení do <see cref="FilesToDownloadList"/>.
        /// </summary>
        /// <param name="lastDownloadedFileCreatedTime">Od jakého data a času mají být soubory stahovány</param>
        public void ListFiles(DateTime lastDownloadedFileCreatedTime)
        {
            var foldersToExplore = GetFoldersToDownload(Year, lastDownloadedFileCreatedTime);

            // Get the object used to communicate with the server.
            var client = new FtpClient("ftp.cisjr.cz", "anonymous", "");
            client.AutoConnect();

            FilesToDownloadList = new List<FtpFileInfo>();
            var gvdItem = client.GetListing($"{RemoteFolderBase}/JR{Year}.zip").FirstOrDefault();
            if (gvdItem != null && gvdItem.Modified > lastDownloadedFileCreatedTime)
            {
                FilesToDownloadList.Add(new FtpFileInfo(null, $"JR{Year}.zip", gvdItem.Modified, true));
            }

            foreach (var folder in foldersToExplore)
            {
                foreach (var item in client.GetListing($"{RemoteFolderBase}/{folder}"))
                {
                    if (item.Type == FtpObjectType.File && item.Name.EndsWith(".xml.zip") && item.Modified >= lastDownloadedFileCreatedTime)
                    {
                        FilesToDownloadList.Add(new FtpFileInfo(folder, item.Name, item.Modified, false));
                    }
                }
            }

            client.Disconnect();
        }

        /// <summary>
        /// Seřadí soubory v <see cref="FilesToDownloadList"/> podle data vložení. Důležité, aby byly soubory zpracovávány postupně.
        /// </summary>
        public void SortFilesToDownload()
        {
            FilesToDownloadList.Sort((first, second) => first.ModifiedTime.CompareTo(second.ModifiedTime));
        }

        /// <summary>
        /// Připojí FTP pro stahování souborů
        /// </summary>
        public void ConnectFtp()
        {
            _client = new FtpClient("ftp.cisjr.cz", "anonymous", "");
            _client.AutoConnect();
        }

        /// <summary>
        /// Připraví složku pro stahování souborů
        /// </summary>
        public void EnsureDownloadDirectoryExists()
        {
            if (!Directory.Exists(DownloadedFilesFolder))
            {
                Directory.CreateDirectory(DownloadedFilesFolder);
            }
        }

        /// <summary>
        /// Stáhne a rozbalí další soubor. Teprve až je úspěšně rozbalen v <see cref="RepositoryFolder"/>, uloží jej jako ZIP ve složce <see cref="DownloadedFilesFolder"/>,
        /// aby byla pro příště reference, jaké soubory už byly staženy.
        /// </summary>
        /// <returns></returns>
        public bool DownloadAndUnpackNextFile()
        {
            if (ProcessedFileCount >= FilesToDownloadList.Count)
            {
                return false;
            }

            var fileInfo = FilesToDownloadList[ProcessedFileCount];

            var tempFilePath = Path.Combine(DownloadedFilesFolder, fileInfo.FileName + ".tmp");
            var status = _client.DownloadFile(tempFilePath, $"{RemoteFolderBase}/{fileInfo.FullPath}", FtpLocalExists.Overwrite);
            if (status == FtpStatus.Failed)
            {
                throw new Exception($"Soubor {fileInfo} se nepodařilo stáhnout.");
            }

            if (fileInfo.IsArchive)
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Open))
                {
                    var archive = new ZipArchive(fileStream);
                    foreach (var entry in archive.Entries)
                    {
                        var unpackedFilePath = Path.Combine(RepositoryFolder, entry.FullName);
                        if (!File.Exists(unpackedFilePath))
                        {
                            entry.ExtractToFile(unpackedFilePath, false);
                        }
                    }
                }
            }
            else
            {
                var unpackedFilePath = Path.Combine(RepositoryFolder, Path.GetFileNameWithoutExtension(fileInfo.FileName));
                if (!File.Exists(unpackedFilePath))
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Open))
                    {
                        using (var archiveStream = new GZipStream(fileStream, CompressionMode.Decompress))
                        {
                            var buff = new byte[4096];
                            using (var writeStream = new FileStream(unpackedFilePath, FileMode.CreateNew))
                            {
                                int nRead;
                                do
                                {
                                    nRead = archiveStream.Read(buff, 0, 4096);
                                    writeStream.Write(buff, 0, nRead);
                                } while (nRead == 4096);
                            }
                        }
                    }
                }
            }

            var finalFilePath = Path.Combine(DownloadedFilesFolder, fileInfo.FileName);
            if (!File.Exists(finalFilePath))
            {
                File.Move(tempFilePath, finalFilePath);
                File.SetLastWriteTime(finalFilePath, fileInfo.ModifiedTime);
            }
            else
            {
                File.Delete(tempFilePath);
            }

            ProcessedFileCount++;
            return true;
        }

        /// <summary>
        /// Odpojí FTP
        /// </summary>
        public void Disconnect()
        {
            _client.Disconnect();
        }

        private IEnumerable<string> GetFoldersToDownload(int year, DateTime lastDownloadedFileCreatedTime)
        {
            var startingMonth = new DateTime(year, 1, 1).AddMonths(-1);
            if (lastDownloadedFileCreatedTime > startingMonth)
            {
                startingMonth = new DateTime(lastDownloadedFileCreatedTime.Year, lastDownloadedFileCreatedTime.Month, 1);
            }

            for (var month = startingMonth; month.Year <= year; month = month.AddMonths(1))
            {
                yield return month.ToString("yyyy-MM");
            }
        }
    }
}
