using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvaluacionSistema
{
    class SFTPManager
    {
        private static String host = "192.168.1.10";
        private static String work_dir = "/var/www/";
        private static String user_sftp = "pi";
        private static String pass_sftp = "raspberry";
        private static int port_ftp = 22;

        public static void Download(String remoteUrl, String localDestinationFilename)
        {
            using (var sftp = new SftpClient(SFTPManager.host, SFTPManager.port_ftp, SFTPManager.user_sftp, SFTPManager.pass_sftp))
            {
                sftp.Connect();

                using (var file = File.OpenWrite(localDestinationFilename))
                {
                    sftp.DownloadFile(SFTPManager.work_dir + remoteUrl, file);
                }

                sftp.Disconnect();
            }
        }

        public static void Upload(String remoteDirectory, String localFilename)
        {
            Console.WriteLine("Creating client and connecting");
            using (var client = new SftpClient(SFTPManager.host, SFTPManager.port_ftp, SFTPManager.user_sftp, SFTPManager.pass_sftp))
            {
                client.Connect();
                Console.WriteLine("Connected to {0}", host);

                client.ChangeDirectory(remoteDirectory);
                Console.WriteLine("Changed directory to {0}", remoteDirectory);

                var listDirectory = client.ListDirectory(remoteDirectory);
                Console.WriteLine("Listing directory:");
                foreach (var fi in listDirectory)
                {
                    Console.WriteLine(" - " + fi.Name);
                }

                using (var fileStream = new FileStream(localFilename, FileMode.Open))
                {
                    Console.WriteLine("Uploading {0} ({1:N0} bytes)",
                                        localFilename, fileStream.Length);
                    client.BufferSize = 4 * 1024; // bypass Payload error large files
                    client.UploadFile(fileStream, Path.GetFileName(localFilename));
                }
            }
        }
    }
}
