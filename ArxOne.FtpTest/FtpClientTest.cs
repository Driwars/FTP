﻿#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.FtpTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using Ftp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///This is a test class for FtpClientTest and is intended
    ///to contain all FtpClientTest Unit Tests
    ///</summary>
    [TestClass]
    public class FtpClientTest
    {
        private static IEnumerable<Tuple<Uri, NetworkCredential>> EnumerateCredentials()
        {
            const string credentialsTxt = "credentials.txt";
            if (!File.Exists(credentialsTxt))
                Assert.Inconclusive("File '{0}' not found", credentialsTxt);
            using (var streamReader = File.OpenText(credentialsTxt))
            {
                for (; ; )
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                        yield break;

                    Uri uri;
                    try
                    {
                        uri = new Uri(line);
                    }
                    catch (UriFormatException)
                    {
                        continue;
                    }
                    var l = HttpUtility.UrlDecode(uri.UserInfo.Replace("_at_", "@"));
                    var up = l.Split(new[] { ':' }, 2);
                    yield return Tuple.Create(uri, new NetworkCredential(up[0], up[1]));
                }
            }
        }

        private static Tuple<Uri, NetworkCredential> GetTestCredential(string protocol)
        {
            var t = EnumerateCredentials().FirstOrDefault(c => c.Item1.Scheme == protocol);
            if (t == null)
                Assert.Inconclusive("Found no configuration for protocol '{0}'", protocol);
            return t;
        }


        /// <summary>
        ///A test for ParseUnix
        ///</summary>
        [TestMethod]
        [TestCategory("FtpClient")]
        public void ParseUnix1Test()
        {
            var entry = FtpClient.ParseUnix("drwxr-xr-x    4 1001     1001         4096 Jan 21 14:41 nas-1");
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.Directory, entry.Type);
            Assert.AreEqual("nas-1", entry.Name);
        }

        /// <summary>
        ///A test for ParseUnix
        ///</summary>
        [TestMethod]
        [TestCategory("FtpClient")]
        public void ParseUnix2Test()
        {
            var entry = FtpClient.ParseUnix("drwxr-xr-x    4 nas-1    nas-1        4096 Jan 21 15:41 nas-1");
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.Directory, entry.Type);
            Assert.AreEqual("nas-1", entry.Name);
        }

        /// <summary>
        ///A test for ParseUnix
        ///</summary>
        [TestMethod]
        [TestCategory("FtpClient")]
        public void ParseUnix3Test()
        {
            var entry = FtpClient.ParseUnix("lrwxrwxrwx    1 0        0               4 Sep 03  2009 lib64 -> /lib");
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.Link, entry.Type);
            Assert.AreEqual("lib64", entry.Name);
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void FtpListTest()
        {
            var ftpTestHost = GetTestCredential("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Item1, ftpTestHost.Item2))
            {
                var list = ftpClient.ListEntries("/");
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void FtpesListTest()
        {
            var ftpTestHost = GetTestCredential("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Item1, ftpTestHost.Item2))
            {
                var list = ftpClient.ListEntries("/");
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void FtpStatTest()
        {
            var ftpTestHost = GetTestCredential("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Item1, ftpTestHost.Item2))
            {
                var list = ftpClient.StatEntries("/");
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void FtpStatNoDotTest()
        {
            var ftpTestHost = GetTestCredential("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Item1, ftpTestHost.Item2))
            {
                var list = ftpClient.StatEntries("/");
                Assert.IsFalse(list.Any(e => e.Name == "." || e.Name == ".."));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void FtpesStatTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                var list = ftpClient.StatEntries("/").ToArray();
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void ReadFileTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                using (var s = ftpClient.Retr("/var/log/installer/status"))
                using (var t = new StreamReader(s, Encoding.UTF8))
                {
                    var m = t.ReadToEnd();
                    Assert.IsTrue(m.Length > 0);
                }
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void CreateFileTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                var path = "/tmp/file." + Guid.NewGuid();
                using (var s = ftpClient.Stor(path))
                {
                    s.WriteByte(65);
                }
                using (var r = ftpClient.Retr(path))
                {
                    Assert.IsNotNull(r);
                    Assert.AreEqual(65, r.ReadByte());
                    Assert.AreEqual(-1, r.ReadByte());
                }
                ftpClient.Dele(path);
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void DeleteFileTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                var path = "/tmp/file." + Guid.NewGuid();
                using (var s = ftpClient.Stor(path))
                {
                    s.WriteByte(65);
                }
                Assert.IsTrue(ftpClient.Dele(path));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void DeleteFolderTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                var path = "/tmp/file." + Guid.NewGuid();
                ftpClient.Mkd(path);
                Assert.IsTrue(ftpClient.Rmd(path));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void DeleteTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                var path = "/tmp/file." + Guid.NewGuid();
                ftpClient.Mkd(path);
                Assert.IsTrue(ftpClient.Delete(path));
                using (var s = ftpClient.Stor(path))
                {
                    s.WriteByte(65);
                }
                Assert.IsTrue(ftpClient.Delete(path));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void CreateSpecialNameFolderTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                var path = "/tmp/file." + Guid.NewGuid() + "(D)";
                ftpClient.Mkd(path);
                Assert.IsTrue(ftpClient.Rmd(path));
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void CreateNonExistingSubFolderTest()
        {
            var ftpesTestHost = GetTestCredential("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Item1, ftpesTestHost.Item2))
            {
                var parent = "/tmp/" + Guid.NewGuid().ToString("N");
                var child = parent + "/" + Guid.NewGuid().ToString("N");
                var reply = ftpClient.SendSingleCommand("MKD", child);
                Assert.AreEqual(550, reply.Code.Code);
                ftpClient.SendSingleCommand("RMD", parent);
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        public void CreateFolderTwiceTest()
        {
            var client = new FtpClient(GetTestCredential("ftpes").Item1, GetTestCredential("ftpes").Item2);
            using (var ftpClient = client)
            {
                var path = "/tmp/" + Guid.NewGuid().ToString("N");
                var reply = ftpClient.SendSingleCommand("MKD", path);
                var reply2 = ftpClient.SendSingleCommand("MKD", path);
                Assert.AreEqual(550, reply2.Code.Code);
                ftpClient.Rmd(path);
            }
        }
    }
}