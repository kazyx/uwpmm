﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class PictureDownloader
    {
        private const string DIRECTORY_NAME = "uwpmm";

        private const int BUFFER_SIZE = 2048;

        public static async Task<StorageFile> DownloadToSave(Uri uri)
        {
            DebugUtil.Log("Download picture: " + uri.OriginalString);
            try
            {
                using (var http = new HttpClient())
                {
                    var res = await http.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
                    if (res.StatusCode != HttpStatusCode.OK)
                    {
                        return null;
                    }

                    using (var resStream = await res.Content.ReadAsStreamAsync())
                    {
                        var library = KnownFolders.PicturesLibrary;
                        StorageFolder folder = null;
                        folder = await library.GetFolderAsync(DIRECTORY_NAME);
                        if (folder == null)
                        {
                            DebugUtil.Log("Create folder: " + DIRECTORY_NAME);
                            folder = await library.CreateFolderAsync(DIRECTORY_NAME);
                        }

                        var filename = string.Format(DIRECTORY_NAME + "_{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now);
                        DebugUtil.Log("Create file: " + filename);

                        var file = await folder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                        using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            var buffer = new byte[BUFFER_SIZE];
                            using (var os = stream.GetOutputStreamAt(0))
                            {
                                int read = 0;
                                while ((read = resStream.Read(buffer, 0, BUFFER_SIZE)) != 0)
                                {
                                    await os.WriteAsync(buffer.AsBuffer(0, read));
                                }
                            }
                        }
                        return file;
                    }
                }
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.Message);
                DebugUtil.Log(e.StackTrace);
                return null;
            }
        }
    }
}
