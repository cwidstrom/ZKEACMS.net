/* http://www.zkea.net/ 
 * Copyright (c) ZKEASOFT. All rights reserved. 
 * http://www.zkea.net/licenses */

using System.IO;
using System.Threading.Tasks;

namespace Easy.Storage
{
    public interface IStorage
    {
        string SaveFile(Stream stream, string fileName);
        Task<string> SaveFileAsync(Stream stream, string fileName);
        string SaveFile(Stream stream, string directory, string fileName);
        Task<string> SaveFileAsync(Stream stream, string directory, string fileName);
        void AppendFile(Stream stream, string filePath);
        Task AppendFileAsync(Stream stream, string filePath);
        Stream GetFile(string filePath);
        void Delete(string filePath);
        void DeleteDirectory(string path);
    }
}
