using System;

namespace TrainsEditor.EditorLogic
{
    /// <summary>
    /// Popis jednoho souboru s vlakem na FTP
    /// </summary>
    class FtpFileInfo
    {
        /// <summary>
        /// Složka, ve které se soubor nachází vzhledem k root složce GVD na FTP (standardně může být název měsíce, např. 2023-03, nebo prázdná)
        /// </summary>
        public string Folder { get; private set; }
        
        /// <summary>
        /// Název souboru
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Čas vytvoření záznamu (ale ukládá se jako modified hodnota)
        /// </summary>
        public DateTime ModifiedTime { get; private set; }

        /// <summary>
        /// Složka + soubor (cesta relativní k root složce GVD na FTP)
        /// </summary>
        public string FullPath
        {
            get
            {
                if (!string.IsNullOrEmpty(Folder))
                {
                    return $"{Folder}/{FileName}";
                }
                else
                {
                    return FileName;
                }
            }
        }

        /// <summary>
        /// True, pokud jde o skutečný ZIP archiv (standardně KADR soubory jsou jen zazipovaný stream, ale GVD soubor je pravý archiv)
        /// </summary>
        public bool IsArchive { get; private set; }

        public FtpFileInfo(string folder, string fileName, DateTime modifiedTime, bool isArchive)
        {
            Folder = folder;
            FileName = fileName;
            ModifiedTime = modifiedTime;
            IsArchive = isArchive;
        }

        public override string ToString()
        {
            return $"{FullPath} ({ModifiedTime})";
        }
    }
}
